using McpServerKbContentFetcher.Models;
using McpServerKbContentFetcher.Services;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace McpServerKbContentFetcher.Tools;

/// <summary>
/// MCP tool for searching knowledge base content
/// </summary>
public class SearchKnowledgeTool
{
    private readonly IKnowledgeBaseService _knowledgeBaseService;
    private readonly ILogger<SearchKnowledgeTool> _logger;

    public SearchKnowledgeTool(
        IKnowledgeBaseService knowledgeBaseService,
        ILogger<SearchKnowledgeTool> logger)
    {
        _knowledgeBaseService = knowledgeBaseService;
        _logger = logger;
    }

    /// <summary>
    /// Search for knowledge base content based on query terms
    /// </summary>
    /// <param name="query">Search keywords or phrases</param>
    /// <param name="max_results">Maximum number of results to return (optional, default: 3, max: 5)</param>
    /// <returns>Plain object payload with search results (MCP server will wrap into content[])</returns>
    public async Task<object> SearchAsync(
        string query,
        int? max_results = null)
    {
        try
        {
            _logger.LogInformation("SearchKnowledge tool called with query: '{Query}', max_results: {MaxResults}", 
                query, max_results);

            // Validate input
            if (string.IsNullOrWhiteSpace(query))
            {
                _logger.LogWarning("Empty or null query provided to SearchKnowledge tool");
                return new
                {
                    query = query ?? string.Empty,
                    totalMatches = 0,
                    results = Array.Empty<object>()
                };
            }

            // Set default and validate max_results
            var maxResults = max_results ?? 3;
            maxResults = Math.Max(1, Math.Min(maxResults, 5)); // Ensure between 1 and 5

            // Perform search
            var searchResults = await _knowledgeBaseService.SearchAsync(query, maxResults);
            var resultItems = searchResults.Select(result => new
            {
                content = result.Context,
                matchInfo = $"Match strength: {result.MatchStrength}, Position: {result.Position}",
                matchStrength = result.MatchStrength,
                position = result.Position
            }).ToArray();

            _logger.LogInformation("SearchKnowledge tool completed. Found {ResultCount} results for query: '{Query}'", 
                resultItems.Length, query);

            return new
            {
                query,
                totalMatches = resultItems.Length,
                results = resultItems
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SearchKnowledge tool for query: '{Query}'", query);
            return new
            {
                query = query ?? string.Empty,
                totalMatches = 0,
                error = "Search error occurred. Please try again with a different query.",
                details = ex.Message
            };
        }
    }
}