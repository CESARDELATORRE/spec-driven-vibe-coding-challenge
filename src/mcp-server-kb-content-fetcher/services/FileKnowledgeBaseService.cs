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
    /// Attempts to resolve the configured knowledge base file path using multiple fallbacks.
    /// This allows running the server from the repository root (e.g. with 'dotnet run --project ...')
    /// while the dataset file physically lives under the project folder.
    /// </summary>
    /// <param name="configuredPath">The path configured in options (may be relative)</param>
    /// <returns>Full path to existing file or null if not found</returns>
    private string? ResolveKnowledgeBaseFilePath(string configuredPath)
    {
        try
        {
            var candidates = new List<string>();

            // 1. As provided (relative to current working directory)
            candidates.Add(Path.GetFullPath(configuredPath));

            // 2. Relative to the AppContext base directory (bin/... folder at runtime)
            var baseDir = AppContext.BaseDirectory;
            candidates.Add(Path.GetFullPath(Path.Combine(baseDir, configuredPath)));

            // 3. Walk up from bin folder to repo root and reapply relative path
            // Typical depth: bin/Debug/net9.0 => go up 3 levels
            var up3 = Path.GetFullPath(Path.Combine(baseDir, "..", "..", ".."));
            candidates.Add(Path.Combine(up3, configuredPath));

            // 4. If path starts with 'datasets', try prefixing project folder path
            if (!configuredPath.StartsWith("src", StringComparison.OrdinalIgnoreCase))
            {
                candidates.Add(Path.Combine(up3, "src", "mcp-server-kb-content-fetcher", configuredPath));
            }

            // 5. If still not found and configured path isn't absolute but file name exists, brute force search under src project folder
            if (!Path.IsPathRooted(configuredPath))
            {
                var fileName = Path.GetFileName(configuredPath);
                var projectDatasets = Path.Combine(up3, "src", "mcp-server-kb-content-fetcher", "datasets", fileName);
                candidates.Add(projectDatasets);
            }

            foreach (var path in candidates.Where(p => !string.IsNullOrWhiteSpace(p)))
            {
                try
                {
                    if (File.Exists(path))
                    {
                        _logger.LogDebug("Resolved knowledge base file path candidate: {ResolvedPath}", path);
                        return path;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogTrace(ex, "Error while probing candidate path: {Candidate}", path);
                }
            }

            _logger.LogWarning("Unable to resolve knowledge base file. Tried candidates: {Candidates}", string.Join("; ", candidates));
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

    // Removed legacy search logic (prototype no longer supports ad-hoc term searches)
}