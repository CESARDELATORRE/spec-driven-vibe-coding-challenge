namespace McpServerKbContentFetcher.Models;

/// <summary>
/// Represents metadata about the knowledge base content file loaded by the server.
/// </summary>
public class KnowledgeBaseInfo
{
    /// <summary>
    /// Size of the knowledge base file in bytes.
    /// </summary>
    public long FileSizeBytes { get; set; }

    /// <summary>
    /// Length of the in-memory content in characters.
    /// </summary>
    public int ContentLength { get; set; }

    /// <summary>
    /// True when the knowledge base file was successfully loaded and content is available.
    /// </summary>
    public bool IsAvailable { get; set; }

    /// <summary>
    /// Simple human-readable description of the knowledge base domain.
    /// </summary>
    public string Description { get; set; } = "Azure Managed Grafana knowledge base";

    /// <summary>
    /// Last modified timestamp of the underlying knowledge base file (UTC preferred).
    /// </summary>
    public DateTime LastModified { get; set; }
}