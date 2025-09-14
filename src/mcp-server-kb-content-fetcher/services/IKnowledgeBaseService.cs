using McpServerKbContentFetcher.Models;

namespace McpServerKbContentFetcher.Services;

/// <summary>
/// Interface for knowledge base operations
/// </summary>
public interface IKnowledgeBaseService
{
    /// <summary>
    /// Search for content in the knowledge base
    /// </summary>
    /// <param name="query">Search query string</param>
    /// <param name="maxResults">Maximum number of results to return</param>
    /// <returns>List of search results</returns>
    Task<IEnumerable<SearchResult>> SearchAsync(string query, int maxResults = 3);

    /// <summary>
    /// Get information about the knowledge base
    /// </summary>
    /// <returns>Knowledge base information</returns>
    Task<KnowledgeBaseInfo> GetInfoAsync();

    /// <summary>
    /// Initialize the knowledge base service (load content)
    /// </summary>
    /// <returns>True if initialization successful</returns>
    Task<bool> InitializeAsync();

    /// <summary>
    /// Get the full raw knowledge base content as a single string.
    /// Intended for prototype usage where file size is manageable and
    /// agents may want direct embedding or downstream processing.
    /// </summary>
    /// <returns>Raw knowledge base text content (empty if not initialized)</returns>
    Task<string> GetRawContentAsync();
}