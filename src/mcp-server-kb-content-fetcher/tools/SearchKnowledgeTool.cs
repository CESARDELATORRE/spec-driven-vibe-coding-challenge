using Microsoft.Extensions.Logging;
using McpServerKbContentFetcher.Models;
using McpServerKbContentFetcher.Services;

namespace McpServerKbContentFetcher.Tools;

/// <summary>
/// MCP tool class implementing search_knowledge functionality
/// TODO: Add [McpTool] attribute when MCP SDK is available
/// </summary>
/// <remarks>
/// This tool provides case-insensitive keyword search with partial matching across knowledge base content.
/// Returns relevant content snippets with surrounding context for meaningful question answering.
/// </remarks>
public class SearchKnowledgeTool
{
    private readonly ILogger<SearchKnowledgeTool> _logger;
    private readonly IKnowledgeBaseService _knowledgeBaseService;

    public SearchKnowledgeTool(ILogger<SearchKnowledgeTool> logger, IKnowledgeBaseService knowledgeBaseService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _knowledgeBaseService = knowledgeBaseService ?? throw new ArgumentNullException(nameof(knowledgeBaseService));
    }

    /// <summary>
    /// Search knowledge base for keyword matches with partial matching
    /// MCP Tool Name: search_knowledge
    /// </summary>
    /// <param name="request">Search request containing query and optional max results</param>
    /// <returns>Search response with content snippets and context</returns>
    public async Task<SearchKnowledgeResponse> ExecuteAsync(SearchKnowledgeRequest request)
    {
        try
        {
            if (request == null)
            {
                _logger.LogWarning("Search request is null");
                return new SearchKnowledgeResponse
                {
                    Success = false,
                    ErrorMessage = "Search request cannot be null"
                };
            }

            if (string.IsNullOrWhiteSpace(request.Query))
            {
                _logger.LogWarning("Empty search query provided");
                return new SearchKnowledgeResponse
                {
                    Success = false,
                    ErrorMessage = "Search query cannot be empty"
                };
            }

            // Validate and set max results (default: 3, max: 5)
            int maxResults = Math.Min(request.MaxResults ?? 3, 5);
            
            _logger.LogInformation("Executing search_knowledge tool for query: '{Query}' with max results: {MaxResults}", 
                request.Query, maxResults);

            var searchResults = await _knowledgeBaseService.SearchAsync(request.Query, maxResults);

            var response = new SearchKnowledgeResponse
            {
                Results = searchResults,
                TotalMatches = searchResults.Length,
                Query = request.Query,
                Success = true
            };

            _logger.LogInformation("Search completed successfully. Found {ResultCount} results", searchResults.Length);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing search_knowledge tool for query: '{Query}'", request?.Query);
            return new SearchKnowledgeResponse
            {
                Query = request?.Query ?? string.Empty,
                Success = false,
                ErrorMessage = "Search error: " + ex.Message
            };
        }
    }

    /// <summary>
    /// Get tool metadata for MCP discovery
    /// TODO: Replace with proper MCP SDK attributes when available
    /// </summary>
    public static object GetToolMetadata()
    {
        return new
        {
            Name = "search_knowledge",
            Description = "Search knowledge base for keyword matches with partial matching",
            Parameters = new
            {
                Type = "object",
                Properties = new
                {
                    Query = new
                    {
                        Type = "string",
                        Description = "Search keywords or phrases"
                    },
                    MaxResults = new
                    {
                        Type = "integer",
                        Description = "Maximum results to return (default: 3, max: 5)",
                        Minimum = 1,
                        Maximum = 5,
                        Default = 3
                    }
                },
                Required = new[] { "Query" }
            }
        };
    }
}