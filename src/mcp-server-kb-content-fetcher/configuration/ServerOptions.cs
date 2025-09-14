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

    // (Removed) Search-related limits no longer applicable in excerpt-only prototype
}