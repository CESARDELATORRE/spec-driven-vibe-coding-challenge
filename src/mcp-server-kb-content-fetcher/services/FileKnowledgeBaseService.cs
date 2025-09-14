using McpServerKbContentFetcher.Configuration;
using McpServerKbContentFetcher.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace McpServerKbContentFetcher.Services;

/// <summary>
/// File-based implementation of the knowledge base service
/// Loads content at startup and provides search functionality
/// </summary>
public class FileKnowledgeBaseService : IKnowledgeBaseService
{
    private readonly ILogger<FileKnowledgeBaseService> _logger;
    private readonly KnowledgeBaseOptions _options;
    private string _content = string.Empty;
    private FileInfo? _fileInfo;
    private bool _isInitialized = false;

    public FileKnowledgeBaseService(
        ILogger<FileKnowledgeBaseService> logger,
        IOptions<ServerOptions> options)
    {
        _logger = logger;
        _options = options.Value.KnowledgeBase;
    }

    /// <summary>
    /// Initialize the knowledge base by loading content from file
    /// </summary>
    public async Task<bool> InitializeAsync()
    {
        try
        {
            var filePath = ResolveKnowledgeBaseFilePath(_options.FilePath);
            if (filePath == null)
            {
                _logger.LogError("Knowledge base file not found. Attempted base path: {ConfiguredPath}", _options.FilePath);
                return false;
            }

            _logger.LogInformation("Initializing knowledge base from file: {FilePath}", filePath);

            _fileInfo = new FileInfo(filePath);
            _content = await File.ReadAllTextAsync(filePath);
            _isInitialized = true;

            _logger.LogInformation("Knowledge base initialized successfully. Content length: {Length} characters", _content.Length);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize knowledge base from file: {FilePath}", _options.FilePath);
            return false;
        }
    }

    /// <summary>
    /// Resolve the configured knowledge base file path using a minimal deterministic sequence:
    ///  1. If absolute and exists -> use
    ///  2. Configured path relative to current working directory
    ///  3. Path relative to AppContext.BaseDirectory (bin/... at runtime)
    ///  4. Path under src/mcp-server-kb-content-fetcher/<configuredPath>
    /// This is sufficient for typical 'dotnet run --project' usage and test execution without
    /// maintaining a large candidate list that increases cognitive load.
    /// </summary>
    /// <param name="configuredPath">The path configured in options (may be relative)</param>
    /// <returns>Full path to existing file or null if not found</returns>
    private string? ResolveKnowledgeBaseFilePath(string configuredPath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(configuredPath)) return null;

            // 1. Absolute path
            if (Path.IsPathRooted(configuredPath))
            {
                _logger.LogDebug("Probing KB path (absolute): {Path}", configuredPath);
                if (File.Exists(configuredPath))
                {
                    _logger.LogDebug("Resolved KB path (absolute): {Path}", configuredPath);
                    return configuredPath;
                }
            }

            // 2. Relative to current working directory
            var cwdPath = Path.GetFullPath(configuredPath);
            _logger.LogDebug("Probing KB path (cwd): {Path} exists={Exists}", cwdPath, File.Exists(cwdPath));
            if (File.Exists(cwdPath)) { _logger.LogDebug("Resolved KB path (cwd): {Path}", cwdPath); return cwdPath; }

            // 3. Relative to AppContext.BaseDirectory (bin folder at runtime)
            var baseDir = AppContext.BaseDirectory;
            var baseDirPath = Path.GetFullPath(Path.Combine(baseDir, configuredPath));
            _logger.LogDebug("Probing KB path (baseDir): {Path} exists={Exists}", baseDirPath, File.Exists(baseDirPath));
            if (File.Exists(baseDirPath)) { _logger.LogDebug("Resolved KB path (baseDir): {Path}", baseDirPath); return baseDirPath; }

            // 4. Project folder candidates (walk up three levels, then try direct + src/* variant)
            var up3 = Path.GetFullPath(Path.Combine(baseDir, "..", "..", ".."));
            var candidateDirectProject = Path.Combine(up3, configuredPath); // when up3 IS the project folder
            var candidateViaSrc = Path.Combine(up3, "src", "mcp-server-kb-content-fetcher", configuredPath); // when up3 is repo root
            var fileName = Path.GetFileName(configuredPath);
            var candidateDatasetsDirect = Path.Combine(up3, "datasets", fileName);
            var candidateDatasetsViaSrc = Path.Combine(up3, "src", "mcp-server-kb-content-fetcher", "datasets", fileName);

            _logger.LogDebug("Probing KB path (project-direct): {Path} exists={Exists}", candidateDirectProject, File.Exists(candidateDirectProject));
            if (File.Exists(candidateDirectProject)) { _logger.LogDebug("Resolved KB path (project-direct): {Path}", candidateDirectProject); return candidateDirectProject; }

            _logger.LogDebug("Probing KB path (project-src): {Path} exists={Exists}", candidateViaSrc, File.Exists(candidateViaSrc));
            if (File.Exists(candidateViaSrc)) { _logger.LogDebug("Resolved KB path (project-src): {Path}", candidateViaSrc); return candidateViaSrc; }

            _logger.LogDebug("Probing KB path (datasets-direct): {Path} exists={Exists}", candidateDatasetsDirect, File.Exists(candidateDatasetsDirect));
            if (File.Exists(candidateDatasetsDirect)) { _logger.LogDebug("Resolved KB path (datasets-direct): {Path}", candidateDatasetsDirect); return candidateDatasetsDirect; }

            _logger.LogDebug("Probing KB path (datasets-src): {Path} exists={Exists}", candidateDatasetsViaSrc, File.Exists(candidateDatasetsViaSrc));
            if (File.Exists(candidateDatasetsViaSrc)) { _logger.LogDebug("Resolved KB path (datasets-src): {Path}", candidateDatasetsViaSrc); return candidateDatasetsViaSrc; }

            _logger.LogWarning("Unable to resolve knowledge base file. Checked absolute, cwd, baseDir, project-direct, project-src, datasets-direct, datasets-src for: {Configured}", configuredPath);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while resolving knowledge base file path for configured value: {Configured}", configuredPath);
            return null;
        }
    }

    /// <summary>
    /// Get information about the knowledge base
    /// </summary>
    public Task<KnowledgeBaseInfo> GetInfoAsync()
    {
        try
        {
            var info = new KnowledgeBaseInfo
            {
                IsAvailable = _isInitialized,
                ContentLength = _content.Length,
                Description = "Azure Managed Grafana knowledge base",
                FileSizeBytes = _fileInfo?.Length ?? 0,
                LastModified = _fileInfo?.LastWriteTime ?? DateTime.MinValue
            };

            _logger.LogDebug("Knowledge base info requested. Available: {IsAvailable}, Content length: {ContentLength}", 
                info.IsAvailable, info.ContentLength);

            return Task.FromResult(info);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting knowledge base info");
            return Task.FromResult(new KnowledgeBaseInfo { IsAvailable = false });
        }
    }

    /// <summary>
    /// Returns the raw knowledge base content (empty string if not initialized)
    /// </summary>
    public Task<string> GetRawContentAsync()
    {
        if (!_isInitialized)
        {
            _logger.LogWarning("Raw content requested before initialization complete");
            return Task.FromResult(string.Empty);
        }
        return Task.FromResult(_content);
    }

}