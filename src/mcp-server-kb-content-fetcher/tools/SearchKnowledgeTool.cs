using McpServerKbContentFetcher.Models;
using McpServerKbContentFetcher.Services;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace McpServerKbContentFetcher.Tools;

/// <summary>
/// MCP tool for searching the knowledge base.
/// Provides case-insensitive keyword search with partial matching.
/// </summary>
[McpServerToolType]
public class SearchKnowledgeTool
{
    private readonly IKnowledgeBaseService _knowledgeBaseService;
    private readonly ILogger<SearchKnowledgeTool> _logger;

    public SearchKnowledgeTool(IKnowledgeBaseService knowledgeBaseService, ILogger<SearchKnowledgeTool> logger)
    {
        _knowledgeBaseService = knowledgeBaseService;
        _logger = logger;
    }

    /// <summary>
    /// Search the knowledge base for content matching the given query.
    /// </summary>
    /// <param name="query">Search query string</param>
    /// <param name="maxResults">Maximum number of results to return (optional, default from configuration)</param>
    /// <returns>Search results with matching content snippets</returns>
    [McpServerTool, Description("Search the knowledge base for content matching the given query")]
    public async Task<string> SearchKnowledgeAsync(
        [Description("Search query string")] string query,
        [Description("Maximum number of results to return")] int? maxResults = null)
    {
        try
        {
            _logger.LogInformation("Executing search_knowledge tool with query: '{Query}', maxResults: {MaxResults}", query, maxResults);

            if (string.IsNullOrWhiteSpace(query))
            {
                _logger.LogWarning("Empty query provided to search_knowledge tool");
                return "No search query provided.";
            }

            var effectiveMaxResults = maxResults ?? 3; // Default from specification
            var results = await _knowledgeBaseService.SearchAsync(query, effectiveMaxResults);

            if (results.Length == 0)
            {
                return "No results found for the search query.";
            }

            var resultTexts = results.Select((result, index) => 
                $"Result {index + 1}:\n{result.Content}").ToArray();

            var response = $"Found {results.Length} result(s) for query '{query}':\n\n" + 
                          string.Join("\n\n---\n\n", resultTexts);

            _logger.LogInformation("search_knowledge tool completed. Found {ResultCount} matches", results.Length);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in search_knowledge tool with query: '{Query}'", query);
            return $"Error performing search: {ex.Message}";
        }
    }
}