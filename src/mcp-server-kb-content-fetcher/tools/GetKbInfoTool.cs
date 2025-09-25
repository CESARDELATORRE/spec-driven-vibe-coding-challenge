using McpServerKbContentFetcher.Models;
using McpServerKbContentFetcher.Services;
using Microsoft.Extensions.Logging;
// No additional serialization needed; hosting layer handles JSON conversion.

namespace McpServerKbContentFetcher.Tools;

/// <summary>
/// MCP tool for retrieving knowledge base information and statistics
/// </summary>
public class GetKbInfoTool
{
    private readonly IKnowledgeBaseService _knowledgeBaseService;
    private readonly ILogger<GetKbInfoTool> _logger;

    public GetKbInfoTool(
        IKnowledgeBaseService knowledgeBaseService,
        ILogger<GetKbInfoTool> logger)
    {
        _knowledgeBaseService = knowledgeBaseService;
        _logger = logger;
    }

    /// <summary>
    /// Get basic information about the knowledge base
    /// </summary>
    /// <returns>Plain object payload with knowledge base information (MCP server will wrap into content[])</returns>
    public async Task<object> GetInfoAsync()
    {
        try
        {
            _logger.LogInformation("GetKbInfo tool called");

            var info = await _knowledgeBaseService.GetInfoAsync();

            var payload = new
            {
                status = info.IsAvailable ? "ok" : "unavailable",
                info = new
                {
                    fileSizeBytes = info.FileSizeBytes,
                    contentLength = info.ContentLength,
                    isAvailable = info.IsAvailable,
                    description = info.Description,
                    lastModified = info.LastModified.ToString("o")
                }
            };

            _logger.LogInformation("GetKbInfo tool completed. status={Status} length={Length}", payload.status, info.ContentLength);
            return payload;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetKbInfo tool");
            return new
            {
                status = "Error retrieving knowledge base information",
                error = ex.Message
            };
        }
    }
}