namespace McpServerKbContentFetcher.Configuration;

/// <summary>
/// Configuration options for the KB MCP Server
/// Contains settings for knowledge base file path and server configuration
/// </summary>
public class ServerOptions
{
    /// <summary>
    /// Path to the knowledge base text file
    /// Default: "datasets/knowledge-base.txt"
    /// </summary>
    public string KnowledgeBaseFilePath { get; set; } = "datasets/knowledge-base.txt";

    /// <summary>
    /// Maximum number of search results to return per query
    /// Default: 3
    /// </summary>
    public int MaxSearchResults { get; set; } = 3;

    /// <summary>
    /// Maximum length of content to return per search result (in characters)
    /// Default: 3000
    /// </summary>
    public int MaxContentLength { get; set; } = 3000;

    /// <summary>
    /// Number of context characters to include around search matches
    /// Default: 100
    /// </summary>
    public int ContextCharacters { get; set; } = 100;
}