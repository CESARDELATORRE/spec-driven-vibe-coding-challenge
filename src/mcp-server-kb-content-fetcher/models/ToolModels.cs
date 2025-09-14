namespace McpServerKbContentFetcher.Models;

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