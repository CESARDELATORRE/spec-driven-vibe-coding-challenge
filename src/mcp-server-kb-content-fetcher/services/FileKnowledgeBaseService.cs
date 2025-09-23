using McpServerKbContentFetcher.Configuration;
using McpServerKbContentFetcher.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace McpServerKbContentFetcher.Services;

/// <summary>
/// Thread-safe, immutable cache implementation for file-based knowledge base content
/// </summary>
public class FileKnowledgeBaseContentCache : IKnowledgeBaseContentCache
{
    private readonly ILogger<FileKnowledgeBaseContentCache> _logger;
    private readonly KnowledgeBaseOptions _options;
    private readonly object _initLock = new object();

    private volatile KnowledgeBaseContent? _cachedContent;
    private volatile bool _isInitialized = false;

    public FileKnowledgeBaseContentCache(
        ILogger<FileKnowledgeBaseContentCache> logger,
        IOptions<ServerOptions> options)
    {
        _logger = logger;
        _options = options.Value.KnowledgeBase;
    }

    public bool IsInitialized => _isInitialized;

    public Task<KnowledgeBaseContent> GetContentAsync()
    {
        return Task.FromResult(_cachedContent ?? KnowledgeBaseContent.Empty);
    }

    public Task<bool> InitializeAsync()
    {
        if (_isInitialized) return Task.FromResult(true);

        lock (_initLock)
        {
            if (_isInitialized) return Task.FromResult(true);

            try
            {
                var filePath = ResolveKnowledgeBaseFilePath(_options.FilePath);
                if (filePath == null)
                {
                    _logger.LogError("Knowledge base file not found. Attempted base path: {ConfiguredPath}", _options.FilePath);
                    _cachedContent = KnowledgeBaseContent.Empty;
                    _isInitialized = true;
                    return Task.FromResult(false);
                }

                _logger.LogInformation("Initializing knowledge base cache from file: {FilePath}", filePath);

                var fileInfo = new FileInfo(filePath);
                var content = File.ReadAllText(filePath);

                var info = new KnowledgeBaseInfo
                {
                    IsAvailable = true,
                    ContentLength = content.Length,
                    Description = "Azure Managed Grafana knowledge base",
                    FileSizeBytes = fileInfo.Length,
                    LastModified = fileInfo.LastWriteTime
                };

                _cachedContent = new KnowledgeBaseContent(content, info);
                _isInitialized = true;

                _logger.LogInformation("Knowledge base cache initialized successfully. Content length: {Length} characters", content.Length);
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize knowledge base cache from file: {FilePath}", _options.FilePath);
                _cachedContent = KnowledgeBaseContent.Empty;
                _isInitialized = true;
                return Task.FromResult(false);
            }
        }
    }

    /// <summary>
    /// Resolve the configured knowledge base file path using a simplified deterministic sequence
    /// </summary>
    private string? ResolveKnowledgeBaseFilePath(string configuredPath)
    {
        if (string.IsNullOrWhiteSpace(configuredPath)) return null;

        var candidates = new[]
        {
            // 1. Absolute path
            Path.IsPathRooted(configuredPath) ? configuredPath : null,
            // 2. Relative to current working directory  
            Path.GetFullPath(configuredPath),
            // 3. Relative to AppContext.BaseDirectory
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, configuredPath)),
            // 4. Project structure relative paths
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", configuredPath)),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "src", "mcp-server-kb-content-fetcher", configuredPath))
        };

        return candidates
            .Where(path => path != null && File.Exists(path))
            .FirstOrDefault();
    }
}

/// <summary>
/// Stateless file-based implementation of the knowledge base service.
/// Delegates to an immutable content cache for thread safety and performance.
/// </summary>
public class FileKnowledgeBaseService : IKnowledgeBaseService
{
    private readonly IKnowledgeBaseContentCache _cache;
    private readonly ILogger<FileKnowledgeBaseService> _logger;

    public FileKnowledgeBaseService(
        IKnowledgeBaseContentCache cache,
        ILogger<FileKnowledgeBaseService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Initialize by delegating to the cache
    /// </summary>
    public async Task<bool> InitializeAsync()
    {
        return await _cache.InitializeAsync();
    }

    /// <summary>
    /// Get information about the knowledge base from cache
    /// </summary>
    public async Task<KnowledgeBaseInfo> GetInfoAsync()
    {
        try
        {
            var content = await _cache.GetContentAsync();
            return content.Info;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting knowledge base info");
            return new KnowledgeBaseInfo { IsAvailable = false };
        }
    }

    /// <summary>
    /// Returns the raw knowledge base content from cache
    /// </summary>
    public async Task<string> GetRawContentAsync()
    {
        try
        {
            var content = await _cache.GetContentAsync();
            return content.Content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting raw content");
            return string.Empty;
        }
    }
}