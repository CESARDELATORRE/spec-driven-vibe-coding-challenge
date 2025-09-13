namespace McpServerKbContentFetcher.Models;

/// <summary>
/// Request model for the search_knowledge MCP tool
/// </summary>
public class SearchKnowledgeRequest
{
    /// <summary>
    /// Search keywords or phrases
    /// </summary>
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// Maximum number of results to return (default: 3, max: 5)
    /// </summary>
    public int? MaxResults { get; set; }
}

/// <summary>
/// Response model for the search_knowledge MCP tool
/// </summary>
public class SearchKnowledgeResponse
{
    /// <summary>
    /// Array of search results
    /// </summary>
    public List<SearchResultItem> Results { get; set; } = new();

    /// <summary>
    /// Total number of matches found
    /// </summary>
    public int TotalMatches { get; set; }

    /// <summary>
    /// Search query that was executed
    /// </summary>
    public string Query { get; set; } = string.Empty;
}

/// <summary>
/// Individual search result item for tool responses
/// </summary>
public class SearchResultItem
{
    /// <summary>
    /// The matched content with context
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Relevance or match information
    /// </summary>
    public string MatchInfo { get; set; } = string.Empty;
}

/// <summary>
/// Response model for the get_kb_info MCP tool
/// </summary>
public class GetKbInfoResponse
{
    /// <summary>
    /// Knowledge base information
    /// </summary>
    public KnowledgeBaseInfo Info { get; set; } = new();

    /// <summary>
    /// Status message
    /// </summary>
    public string Status { get; set; } = string.Empty;
}