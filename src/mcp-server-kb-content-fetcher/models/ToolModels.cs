namespace McpServerKbContentFetcher.Models;

/// <summary>
/// Request model for the search_knowledge MCP tool.
/// </summary>
public class SearchKnowledgeRequest
{
    /// <summary>
    /// Search query string.
    /// </summary>
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// Maximum number of results to return (optional, defaults to server configuration).
    /// </summary>
    public int? MaxResults { get; set; }
}

/// <summary>
/// Response model for the search_knowledge MCP tool.
/// </summary>
public class SearchKnowledgeResponse
{
    /// <summary>
    /// Array of search results.
    /// </summary>
    public SearchResult[] Results { get; set; } = Array.Empty<SearchResult>();

    /// <summary>
    /// Query that was executed.
    /// </summary>
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// Total number of matches found (may be higher than returned results).
    /// </summary>
    public int TotalMatches { get; set; }
}

/// <summary>
/// Response model for the get_kb_info MCP tool.
/// </summary>
public class GetKbInfoResponse
{
    /// <summary>
    /// Knowledge base information.
    /// </summary>
    public KnowledgeBaseInfo Info { get; set; } = new();
}