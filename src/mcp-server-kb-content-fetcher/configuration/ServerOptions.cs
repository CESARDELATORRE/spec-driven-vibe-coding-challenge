namespace McpServerKbContentFetcher.Configuration;

/// <summary>
/// Configuration options for the knowledge base server.
/// </summary>
public class ServerOptions
{
    /// <summary>
    /// Configuration section name for binding.
    /// </summary>
    public const string SectionName = "KnowledgeBase";

    /// <summary>
    /// Path to the knowledge base text file.
    /// </summary>
    public string FilePath { get; set; } = "datasets/knowledge-base.txt";

    /// <summary>
    /// Maximum number of search results to return per query.
    /// </summary>
    public int MaxSearchResults { get; set; } = 3;

    /// <summary>
    /// Maximum length of each result in characters.
    /// </summary>
    public int MaxResultLength { get; set; } = 3000;

    /// <summary>
    /// Number of characters to include around search matches for context.
    /// </summary>
    public int ContextLength { get; set; } = 100;
}