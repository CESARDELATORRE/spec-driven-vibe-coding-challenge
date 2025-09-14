using McpServerKbContentFetcher.Models;
using McpServerKbContentFetcher.Services;
using Microsoft.Extensions.Logging;
using System.Text.Json;

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
    /// <returns>MCP content array with knowledge base information</returns>
    public async Task<object[]> GetInfoAsync()
    {
        try
        {
            _logger.LogInformation("GetKbInfo tool called");

            var info = await _knowledgeBaseService.GetInfoAsync();
            
            var status = info.IsAvailable 
                ? "Knowledge base is available and loaded"
                : "Knowledge base is not available";

            // Format as structured info for MCP content
            var infoText = JsonSerializer.Serialize(new
            {
                status,
                info = new
                {
                    fileSizeBytes = info.FileSizeBytes,
                    contentLength = info.ContentLength,
                    isAvailable = info.IsAvailable,
                    description = info.Description,
                    lastModified = info.LastModified.ToString("o")
                }
            }, new JsonSerializerOptions { WriteIndented = true });

            _logger.LogInformation("GetKbInfo tool completed. Available: {IsAvailable}, Content length: {ContentLength}", 
                info.IsAvailable, info.ContentLength);

            // Return MCP content array format
            return new object[]
            {
                new { type = "text", text = infoText }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetKbInfo tool");
            
            var errorText = JsonSerializer.Serialize(new
            {
                status = "Error retrieving knowledge base information",
                error = ex.Message
            }, new JsonSerializerOptions { WriteIndented = true });

            return new object[]
            {
                new { type = "text", text = errorText }
            };
        }
    }
}