namespace McpServerKbContentFetcher.Models;

/// <summary>
/// Model containing matched content, surrounding context, and basic metadata from a knowledge base search
/// </summary>
public class SearchResult
{
    /// <summary>
    /// The matched content snippet
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Context surrounding the matched content for better understanding
    /// </summary>
    public string Context { get; set; } = string.Empty;

    /// <summary>
    /// Relevance score or position in the original content (for debugging/ordering)
    /// </summary>
    public int Position { get; set; }

    /// <summary>
    /// Length of the original content being searched
    /// </summary>
    public int ContentLength { get; set; }

    /// <summary>
    /// Indicates if the content was truncated due to length limits
    /// </summary>
    public bool IsTruncated { get; set; }
}

/// <summary>
/// Model containing basic knowledge base information and statistics
/// </summary>
public class KnowledgeBaseInfo
{
    /// <summary>
    /// Path to the knowledge base file
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Size of the knowledge base file in bytes
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// Length of the loaded content in characters
    /// </summary>
    public int ContentLength { get; set; }

    /// <summary>
    /// Indicates if the knowledge base is available and loaded
    /// </summary>
    public bool IsAvailable { get; set; }

    /// <summary>
    /// Last modified timestamp of the knowledge base file
    /// </summary>
    public DateTime LastModified { get; set; }

    /// <summary>
    /// Any error message if the knowledge base is not available
    /// </summary>
    public string? ErrorMessage { get; set; }
}