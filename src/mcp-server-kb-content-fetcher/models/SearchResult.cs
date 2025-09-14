namespace McpServerKbContentFetcher.Models;

// (Removed) SearchResult class – legacy search functionality deprecated.

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