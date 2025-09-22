using McpServerKbContentFetcher.Models;

namespace McpServerKbContentFetcher.Services;

/// <summary>
/// Immutable content holder for knowledge base data
/// </summary>
public record KnowledgeBaseContent(
    string Content,
    KnowledgeBaseInfo Info)
{
    /// <summary>
    /// Empty content instance for unavailable state
    /// </summary>
    public static KnowledgeBaseContent Empty => new(
        string.Empty,
        new KnowledgeBaseInfo
        {
            IsAvailable = false,
            ContentLength = 0,
            FileSizeBytes = 0,
            Description = "Azure Managed Grafana knowledge base",
            LastModified = DateTime.MinValue
        });
}

/// <summary>
/// Thread-safe, immutable cache for knowledge base content.
/// Initialized once at startup, then provides read-only access.
/// </summary>
public interface IKnowledgeBaseContentCache
{
    /// <summary>
    /// Get the cached knowledge base content (thread-safe, immutable)
    /// </summary>
    Task<KnowledgeBaseContent> GetContentAsync();
    
    /// <summary>
    /// Initialize the cache by loading content from the configured source
    /// </summary>
    Task<bool> InitializeAsync();
    
    /// <summary>
    /// Check if the cache has been successfully initialized
    /// </summary>
    bool IsInitialized { get; }
}

/// <summary>
/// Interface for knowledge base operations
/// </summary>
public interface IKnowledgeBaseService
{

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