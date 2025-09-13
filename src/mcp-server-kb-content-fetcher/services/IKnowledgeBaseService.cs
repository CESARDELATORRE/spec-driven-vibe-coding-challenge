using McpServerKbContentFetcher.Models;

namespace McpServerKbContentFetcher.Services;

/// <summary>
/// Interface defining search and info operations for knowledge base access
/// Enables dependency injection and future knowledge base implementations
/// </summary>
/// <remarks>
/// Interface pattern enables future implementations such as:
/// - DatabaseKnowledgeBaseService: Query SQL databases or document stores
/// - ApiKnowledgeBaseService: Fetch content from REST APIs or GraphQL endpoints  
/// - VectorKnowledgeBaseService: Use vector databases like Azure Cognitive Search
/// - HybridKnowledgeBaseService: Combine multiple knowledge sources
/// </remarks>
public interface IKnowledgeBaseService
{
    /// <summary>
    /// Search the knowledge base for content matching the specified query
    /// </summary>
    /// <param name="query">Search keywords or phrases</param>
    /// <param name="maxResults">Maximum number of results to return (default: 3)</param>
    /// <returns>Array of search results with content snippets and context</returns>
    Task<SearchResult[]> SearchAsync(string query, int maxResults = 3);

    /// <summary>
    /// Get basic information about the knowledge base
    /// </summary>
    /// <returns>Knowledge base statistics and availability status</returns>
    Task<KnowledgeBaseInfo> GetInfoAsync();

    /// <summary>
    /// Initialize the knowledge base service (load content, establish connections, etc.)
    /// </summary>
    /// <returns>True if initialization was successful, false otherwise</returns>
    Task<bool> InitializeAsync();
}