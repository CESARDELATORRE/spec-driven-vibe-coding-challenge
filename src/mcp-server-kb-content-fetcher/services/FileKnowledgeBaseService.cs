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
            var filePath = Path.GetFullPath(_options.FilePath);
            _logger.LogInformation("Initializing knowledge base from file: {FilePath}", filePath);

            if (!File.Exists(filePath))
            {
                _logger.LogError("Knowledge base file not found: {FilePath}", filePath);
                return false;
            }

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
    /// Search for content in the knowledge base using case-insensitive partial matching
    /// </summary>
    public async Task<IEnumerable<SearchResult>> SearchAsync(string query, int maxResults = 3)
    {
        try
        {
            if (!_isInitialized || string.IsNullOrWhiteSpace(query))
            {
                _logger.LogWarning("Search attempted but knowledge base not initialized or empty query");
                return Array.Empty<SearchResult>();
            }

            // Ensure maxResults doesn't exceed configuration limit
            maxResults = Math.Min(maxResults, _options.MaxResultsPerSearch);

            _logger.LogInformation("Searching knowledge base for query: '{Query}', max results: {MaxResults}", query, maxResults);

            var results = new List<SearchResult>();
            var searchTerms = query.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var contentLower = _content.ToLowerInvariant();

            // Find all matches for all search terms
            var matches = new List<(int Position, int Length, int TermMatches)>();

            for (int i = 0; i <= contentLower.Length - query.Length; i++)
            {
                var matchCount = 0;
                var longestMatch = 0;

                // Check how many search terms match at this position
                foreach (var term in searchTerms)
                {
                    if (i + term.Length <= contentLower.Length &&
                        contentLower.Substring(i, term.Length) == term)
                    {
                        matchCount++;
                        longestMatch = Math.Max(longestMatch, term.Length);
                    }
                }

                // If we found any term matches, look for broader context matches
                if (matchCount == 0)
                {
                    foreach (var term in searchTerms)
                    {
                        var termIndex = contentLower.IndexOf(term, i, StringComparison.Ordinal);
                        if (termIndex >= i && termIndex < i + 100) // Within reasonable distance
                        {
                            matchCount++;
                            longestMatch = Math.Max(longestMatch, term.Length);
                            break; // Only count one match per position
                        }
                    }
                }

                if (matchCount > 0)
                {
                    matches.Add((i, longestMatch, matchCount));
                }
            }

            // Sort by relevance (more term matches = higher relevance)
            matches = matches.OrderByDescending(m => m.TermMatches)
                           .ThenByDescending(m => m.Length)
                           .Take(maxResults)
                           .ToList();

            foreach (var match in matches)
            {
                var result = CreateSearchResult(match.Position, match.Length, match.TermMatches);
                if (result != null)
                {
                    results.Add(result);
                }
            }

            _logger.LogInformation("Search completed. Found {ResultCount} results for query: '{Query}'", results.Count, query);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during search for query: '{Query}'", query);
            return Array.Empty<SearchResult>();
        }
    }

    /// <summary>
    /// Get information about the knowledge base
    /// </summary>
    public async Task<KnowledgeBaseInfo> GetInfoAsync()
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

            return info;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting knowledge base info");
            return new KnowledgeBaseInfo { IsAvailable = false };
        }
    }

    /// <summary>
    /// Create a search result with context around the matched content
    /// </summary>
    private SearchResult? CreateSearchResult(int position, int length, int matchStrength)
    {
        try
        {
            // Define context window (characters before and after match)
            const int contextWindow = 100;
            
            var startPos = Math.Max(0, position - contextWindow);
            var endPos = Math.Min(_content.Length, position + length + contextWindow);
            var contextLength = endPos - startPos;

            var contextContent = _content.Substring(startPos, contextLength);
            var matchedContent = _content.Substring(position, Math.Min(length, _content.Length - position));

            // Limit content length as per configuration
            if (contextContent.Length > _options.MaxContentLengthPerResult)
            {
                contextContent = contextContent.Substring(0, _options.MaxContentLengthPerResult) + "...";
            }

            return new SearchResult
            {
                Content = matchedContent,
                Context = contextContent,
                Position = position,
                Length = length,
                MatchStrength = matchStrength
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating search result for position: {Position}", position);
            return null;
        }
    }
}