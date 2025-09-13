using McpServerKbContentFetcher.Configuration;
using McpServerKbContentFetcher.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace McpServerKbContentFetcher.Services;

/// <summary>
/// File-based implementation of the knowledge base service.
/// Loads content from a text file at startup and provides in-memory search functionality.
/// </summary>
public class FileKnowledgeBaseService : IKnowledgeBaseService
{
    private readonly ServerOptions _options;
    private readonly ILogger<FileKnowledgeBaseService> _logger;
    private string _content = string.Empty;
    private FileInfo? _fileInfo;
    private bool _isInitialized;
    private string? _initializationError;

    public FileKnowledgeBaseService(IOptions<ServerOptions> options, ILogger<FileKnowledgeBaseService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Initialize the service by loading the knowledge base file content.
    /// </summary>
    public async Task<bool> InitializeAsync()
    {
        try
        {
            _logger.LogInformation("Initializing knowledge base from file: {FilePath}", _options.FilePath);

            if (!File.Exists(_options.FilePath))
            {
                _initializationError = $"Knowledge base file not found: {_options.FilePath}";
                _logger.LogError(_initializationError);
                return false;
            }

            _fileInfo = new FileInfo(_options.FilePath);
            _content = await File.ReadAllTextAsync(_options.FilePath);
            _isInitialized = true;

            _logger.LogInformation("Knowledge base loaded successfully. File size: {FileSize} bytes, Content length: {ContentLength} characters",
                _fileInfo.Length, _content.Length);

            return true;
        }
        catch (Exception ex)
        {
            _initializationError = $"Failed to load knowledge base: {ex.Message}";
            _logger.LogError(ex, "Failed to initialize knowledge base from file: {FilePath}", _options.FilePath);
            return false;
        }
    }

    /// <summary>
    /// Search the knowledge base content for case-insensitive partial matches.
    /// </summary>
    public async Task<SearchResult[]> SearchAsync(string query, int maxResults)
    {
        await Task.CompletedTask; // Make async for interface compliance

        if (string.IsNullOrWhiteSpace(query))
        {
            _logger.LogWarning("Empty search query provided");
            return Array.Empty<SearchResult>();
        }

        if (!_isInitialized)
        {
            _logger.LogWarning("Knowledge base not initialized, cannot perform search");
            return Array.Empty<SearchResult>();
        }

        try
        {
            var results = new List<SearchResult>();
            var searchTerm = query.ToLowerInvariant();
            var lowerContent = _content.ToLowerInvariant();
            var startIndex = 0;

            _logger.LogInformation("Searching for query: '{Query}', max results: {MaxResults}", query, maxResults);

            while (results.Count < maxResults && startIndex < _content.Length)
            {
                var matchIndex = lowerContent.IndexOf(searchTerm, startIndex, StringComparison.Ordinal);
                if (matchIndex == -1)
                    break;

                // Calculate context boundaries
                var contextStart = Math.Max(0, matchIndex - _options.ContextLength);
                var contextEnd = Math.Min(_content.Length, matchIndex + searchTerm.Length + _options.ContextLength);
                var contextLength = contextEnd - contextStart;

                var content = _content.Substring(contextStart, Math.Min(contextLength, _options.MaxResultLength));

                results.Add(new SearchResult
                {
                    Content = content,
                    Position = matchIndex,
                    Length = content.Length
                });

                startIndex = matchIndex + 1;
            }

            _logger.LogInformation("Search completed. Found {ResultCount} matches for query: '{Query}'", results.Count, query);
            return results.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during search for query: '{Query}'", query);
            return Array.Empty<SearchResult>();
        }
    }

    /// <summary>
    /// Get information about the knowledge base.
    /// </summary>
    public async Task<KnowledgeBaseInfo> GetInfoAsync()
    {
        await Task.CompletedTask; // Make async for interface compliance

        return new KnowledgeBaseInfo
        {
            FilePath = _options.FilePath,
            IsAvailable = _isInitialized,
            FileSizeBytes = _fileInfo?.Length ?? 0,
            ContentLength = _content.Length,
            ErrorMessage = _initializationError
        };
    }
}