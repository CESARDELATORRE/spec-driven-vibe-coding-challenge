using McpServerKbContentFetcher.Models;
using McpServerKbContentFetcher.Services;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace McpServerKbContentFetcher.Tools;

/// <summary>
/// MCP tool now repurposed (prototype simplification) to expose a truncated excerpt
/// of the full knowledge base content (up to a caller-provided or default max length).
/// Original search functionality has been removed per updated prototype requirement
/// to eliminate the fixed query and instead return raw content (capped) for downstream
/// embedding / reasoning without extra round-trips.
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
    /// Return an excerpt (prefix) of the full raw knowledge base content.
    /// </summary>
    /// <param name="maxLength">Maximum characters to return (default 3000)</param>
    /// <returns>Payload describing excerpt and total length</returns>
    public async Task<object> GetExcerptAsync(int maxLength = 3000)
    {
        try
        {
            if (maxLength <= 0) maxLength = 1;
            if (maxLength > 10000) maxLength = 10000; // hard ceiling safety

            _logger.LogInformation("SearchKnowledge tool (excerpt mode) requested. maxLength={MaxLength}", maxLength);

            var raw = await _knowledgeBaseService.GetRawContentAsync();
            if (string.IsNullOrEmpty(raw))
            {
                _logger.LogWarning("Knowledge base content empty or not initialized when excerpt requested");
                return new
                {
                    status = "empty",
                    totalLength = 0,
                    excerptLength = 0,
                    truncated = false,
                    excerpt = string.Empty
                };
            }

            var truncated = raw.Length > maxLength;
            var excerpt = truncated ? raw.Substring(0, maxLength) : raw;

            return new
            {
                status = "ok",
                totalLength = raw.Length,
                excerptLength = excerpt.Length,
                truncated,
                excerpt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating content excerpt");
            return new
            {
                status = "error",
                error = "Failed to generate content excerpt",
                details = ex.Message
            };
        }
    }
}