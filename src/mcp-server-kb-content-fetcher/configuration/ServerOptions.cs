namespace McpServerKbContentFetcher.Configuration;

/// <summary>
/// Configuration options for the knowledge base server
/// </summary>
public class ServerOptions
{
    /// <summary>
    /// Knowledge base specific configuration
    /// </summary>
    public KnowledgeBaseOptions KnowledgeBase { get; set; } = new();
}

/// <summary>
/// Configuration options for knowledge base operations
/// </summary>
public class KnowledgeBaseOptions
{
    /// <summary>
    /// Path to the knowledge base text file
    /// </summary>
    public string FilePath { get; set; } = "datasets/knowledge-base.txt";

    /// <summary>
    /// Maximum number of search results to return per query
    /// </summary>
    public int MaxResultsPerSearch { get; set; } = 3;

    /// <summary>
    /// Maximum content length per search result
    /// </summary>
    public int MaxContentLengthPerResult { get; set; } = 3000;
}