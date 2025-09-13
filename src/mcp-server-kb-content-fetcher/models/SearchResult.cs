namespace McpServerKbContentFetcher.Models;

/// <summary>
/// Represents a search result from the knowledge base
/// </summary>
public class SearchResult
{
    /// <summary>
    /// The matched content snippet
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Context surrounding the matched content (before and after)
    /// </summary>
    public string Context { get; set; } = string.Empty;

    /// <summary>
    /// Position of the match in the original text
    /// </summary>
    public int Position { get; set; }

    /// <summary>
    /// Relevance score or match strength (basic implementation)
    /// </summary>
    public int MatchStrength { get; set; }

    /// <summary>
    /// Length of the matched content
    /// </summary>
    public int Length { get; set; }
}

/// <summary>
/// Basic information about the knowledge base
/// </summary>
public class KnowledgeBaseInfo
{
    /// <summary>
    /// Size of the knowledge base file in bytes
    /// </summary>
    public long FileSizeBytes { get; set; }

    /// <summary>
    /// Length of the content in characters
    /// </summary>
    public int ContentLength { get; set; }

    /// <summary>
    /// Whether the knowledge base is available and loaded
    /// </summary>
    public bool IsAvailable { get; set; }

    /// <summary>
    /// Basic description of the knowledge base content
    /// </summary>
    public string Description { get; set; } = "Azure Managed Grafana knowledge base";

    /// <summary>
    /// Last modified timestamp of the knowledge base file
    /// </summary>
    public DateTime LastModified { get; set; }
}