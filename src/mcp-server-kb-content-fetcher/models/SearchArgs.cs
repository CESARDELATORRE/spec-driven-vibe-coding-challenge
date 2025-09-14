namespace McpServerKbContentFetcher.Models;

/// <summary>
/// Arguments DTO for the search_knowledge MCP tool (simplified binding model)
/// </summary>
public class SearchArgs
{
    /// <summary>Search query text.</summary>
    public string Query { get; set; } = string.Empty;
    /// <summary>Optional max results (null = use default in service/tool).</summary>
    public int? MaxResults { get; set; }
}
