using McpServerKbContentFetcher.Models;
using McpServerKbContentFetcher.Services;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace McpServerKbContentFetcher.Tools;

/// <summary>
/// MCP tool for retrieving knowledge base information.
/// Provides metadata about the knowledge base including size and availability.
/// </summary>
[McpServerToolType]
public class GetKbInfoTool
{
    private readonly IKnowledgeBaseService _knowledgeBaseService;
    private readonly ILogger<GetKbInfoTool> _logger;

    public GetKbInfoTool(IKnowledgeBaseService knowledgeBaseService, ILogger<GetKbInfoTool> logger)
    {
        _knowledgeBaseService = knowledgeBaseService;
        _logger = logger;
    }

    /// <summary>
    /// Get information about the knowledge base including size, availability, and status.
    /// </summary>
    /// <returns>Knowledge base information and metadata</returns>
    [McpServerTool, Description("Get information about the knowledge base including size, availability, and status")]
    public async Task<string> GetKnowledgeBaseInfoAsync()
    {
        try
        {
            _logger.LogInformation("Executing get_kb_info tool");

            var info = await _knowledgeBaseService.GetInfoAsync();

            var response = $"Knowledge Base Information:\n" +
                          $"- File Path: {info.FilePath}\n" +
                          $"- Available: {info.IsAvailable}\n" +
                          $"- File Size: {info.FileSizeBytes:N0} bytes\n" +
                          $"- Content Length: {info.ContentLength:N0} characters";

            if (!string.IsNullOrEmpty(info.ErrorMessage))
            {
                response += $"\n- Error: {info.ErrorMessage}";
            }

            _logger.LogInformation("get_kb_info tool completed. Knowledge base available: {IsAvailable}", info.IsAvailable);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in get_kb_info tool");
            return $"Error retrieving knowledge base information: {ex.Message}";
        }
    }
}