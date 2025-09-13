namespace McpServerKbContentFetcher.Models;

/// <summary>
/// Request/response models for MCP tool parameters and results
/// </summary>

/// <summary>
/// Request model for search_knowledge MCP tool
/// </summary>
public class SearchKnowledgeRequest
{
    /// <summary>
    /// Search keywords or phrases
    /// </summary>
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// Maximum results to return (default: 3, max: 5)
    /// </summary>
    public int? MaxResults { get; set; }
}

/// <summary>
/// Response model for search_knowledge MCP tool
/// </summary>
public class SearchKnowledgeResponse
{
    /// <summary>
    /// Array of content snippets with context
    /// </summary>
    public SearchResult[] Results { get; set; } = Array.Empty<SearchResult>();

    /// <summary>
    /// Total number of matches found
    /// </summary>
    public int TotalMatches { get; set; }

    /// <summary>
    /// Search query that was executed
    /// </summary>
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// Success status of the search operation
    /// </summary>
    public bool Success { get; set; } = true;

    /// <summary>
    /// Error message if search failed
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Response model for get_kb_info MCP tool
/// </summary>
public class GetKbInfoResponse
{
    /// <summary>
    /// Knowledge base information and statistics
    /// </summary>
    public KnowledgeBaseInfo Info { get; set; } = new();

    /// <summary>
    /// Success status of the info operation
    /// </summary>
    public bool Success { get; set; } = true;

    /// <summary>
    /// Error message if info retrieval failed
    /// </summary>
    public string? ErrorMessage { get; set; }
}