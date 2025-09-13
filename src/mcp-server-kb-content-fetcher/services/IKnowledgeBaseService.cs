using McpServerKbContentFetcher.Models;

namespace McpServerKbContentFetcher.Services;

/// <summary>
/// Interface for knowledge base operations.
/// Provides abstraction for different knowledge base implementations.
/// </summary>
public interface IKnowledgeBaseService
{
    /// <summary>
    /// Search the knowledge base for the given query.
    /// </summary>
    /// <param name="query">Search query string</param>
    /// <param name="maxResults">Maximum number of results to return</param>
    /// <returns>Array of search results</returns>
    Task<SearchResult[]> SearchAsync(string query, int maxResults);

    /// <summary>
    /// Get information about the knowledge base.
    /// </summary>
    /// <returns>Knowledge base information</returns>
    Task<KnowledgeBaseInfo> GetInfoAsync();

    /// <summary>
    /// Initialize the knowledge base service.
    /// </summary>
    /// <returns>True if initialization was successful, false otherwise</returns>
    Task<bool> InitializeAsync();
}