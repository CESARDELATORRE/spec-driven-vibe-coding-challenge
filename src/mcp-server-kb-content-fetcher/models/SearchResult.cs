namespace McpServerKbContentFetcher.Models;

/// <summary>
/// Represents a search result from the knowledge base.
/// </summary>
public class SearchResult
{
    /// <summary>
    /// The matched content snippet.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Position of the match in the original content.
    /// </summary>
    public int Position { get; set; }

    /// <summary>
    /// Length of the matched snippet.
    /// </summary>
    public int Length { get; set; }
}

/// <summary>
/// Represents basic information about the knowledge base.
/// </summary>
public class KnowledgeBaseInfo
{
    /// <summary>
    /// Size of the knowledge base file in bytes.
    /// </summary>
    public long FileSizeBytes { get; set; }

    /// <summary>
    /// Length of the content in characters.
    /// </summary>
    public int ContentLength { get; set; }

    /// <summary>
    /// Whether the knowledge base is available and loaded.
    /// </summary>
    public bool IsAvailable { get; set; }

    /// <summary>
    /// Path to the knowledge base file.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Any error message if the knowledge base is not available.
    /// </summary>
    public string? ErrorMessage { get; set; }
}