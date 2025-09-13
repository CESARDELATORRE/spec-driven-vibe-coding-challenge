using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using McpServerKbContentFetcher.Configuration;
using McpServerKbContentFetcher.Models;

namespace McpServerKbContentFetcher.Services;

/// <summary>
/// Implementation that loads text file content at startup and provides case-insensitive partial search with context
/// </summary>
public class FileKnowledgeBaseService : IKnowledgeBaseService
{
    private readonly ILogger<FileKnowledgeBaseService> _logger;
    private readonly ServerOptions _options;
    private string _content = string.Empty;
    private string _filePath = string.Empty;
    private FileInfo? _fileInfo;
    private bool _isInitialized = false;

    public FileKnowledgeBaseService(ILogger<FileKnowledgeBaseService> logger, IOptions<ServerOptions> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Initialize the service by loading the knowledge base file content
    /// </summary>
    public async Task<bool> InitializeAsync()
    {
        try
        {
            _filePath = Path.IsPathRooted(_options.KnowledgeBaseFilePath) 
                ? _options.KnowledgeBaseFilePath 
                : Path.Combine(Directory.GetCurrentDirectory(), _options.KnowledgeBaseFilePath);

            _logger.LogInformation("Loading knowledge base from: {FilePath}", _filePath);

            if (!File.Exists(_filePath))
            {
                _logger.LogError("Knowledge base file not found: {FilePath}", _filePath);
                return false;
            }

            _fileInfo = new FileInfo(_filePath);
            _content = await File.ReadAllTextAsync(_filePath);
            _isInitialized = true;

            _logger.LogInformation("Knowledge base loaded successfully. File size: {FileSize} bytes, Content length: {ContentLength} characters", 
                _fileInfo.Length, _content.Length);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize knowledge base from file: {FilePath}", _filePath);
            return false;
        }
    }

    /// <summary>
    /// Search for content matching the query with case-insensitive partial matching
    /// </summary>
    public async Task<SearchResult[]> SearchAsync(string query, int maxResults = 3)
    {
        if (!_isInitialized)
        {
            _logger.LogWarning("Knowledge base not initialized. Call InitializeAsync first.");
            return Array.Empty<SearchResult>();
        }

        if (string.IsNullOrWhiteSpace(query))
        {
            _logger.LogWarning("Empty search query provided");
            return Array.Empty<SearchResult>();
        }

        try
        {
            _logger.LogInformation("Searching for: '{Query}'", query);

            var results = new List<SearchResult>();
            var searchTerm = query.ToLowerInvariant();
            var contentLower = _content.ToLowerInvariant();
            int startIndex = 0;

            // Find all matches
            while (results.Count < maxResults && startIndex < _content.Length)
            {
                int foundIndex = contentLower.IndexOf(searchTerm, startIndex, StringComparison.Ordinal);
                if (foundIndex == -1)
                    break;

                // Calculate context window
                int contextStart = Math.Max(0, foundIndex - _options.ContextCharacters);
                int contextEnd = Math.Min(_content.Length, foundIndex + searchTerm.Length + _options.ContextCharacters);
                
                string context = _content.Substring(contextStart, contextEnd - contextStart);
                
                // Extract the matched content with some surrounding text
                int contentStart = Math.Max(0, foundIndex - (_options.ContextCharacters / 2));
                int contentEnd = Math.Min(_content.Length, foundIndex + searchTerm.Length + (_options.ContextCharacters / 2));
                string matchedContent = _content.Substring(contentStart, contentEnd - contentStart);

                // Truncate if necessary
                bool isTruncated = false;
                if (matchedContent.Length > _options.MaxContentLength)
                {
                    matchedContent = matchedContent.Substring(0, _options.MaxContentLength);
                    isTruncated = true;
                }

                results.Add(new SearchResult
                {
                    Content = matchedContent,
                    Context = context,
                    Position = foundIndex,
                    ContentLength = _content.Length,
                    IsTruncated = isTruncated
                });

                startIndex = foundIndex + 1; // Continue searching from next position
            }

            _logger.LogInformation("Search completed. Found {ResultCount} results for query: '{Query}'", results.Count, query);
            return results.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during search for query: '{Query}'", query);
            return Array.Empty<SearchResult>();
        }
    }

    /// <summary>
    /// Get basic information about the knowledge base
    /// </summary>
    public async Task<KnowledgeBaseInfo> GetInfoAsync()
    {
        try
        {
            return new KnowledgeBaseInfo
            {
                FilePath = _filePath,
                FileSize = _fileInfo?.Length ?? 0,
                ContentLength = _content.Length,
                IsAvailable = _isInitialized,
                LastModified = _fileInfo?.LastWriteTime ?? DateTime.MinValue,
                ErrorMessage = _isInitialized ? null : "Knowledge base not initialized"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting knowledge base info");
            return new KnowledgeBaseInfo
            {
                FilePath = _filePath,
                IsAvailable = false,
                ErrorMessage = ex.Message
            };
        }
    }
}