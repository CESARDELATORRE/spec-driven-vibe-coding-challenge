using Microsoft.Extensions.Logging;
using McpServerKbContentFetcher.Models;
using McpServerKbContentFetcher.Services;

namespace McpServerKbContentFetcher.Tools;

/// <summary>
/// MCP tool class implementing get_kb_info functionality
/// TODO: Add [McpTool] attribute when MCP SDK is available
/// </summary>
/// <remarks>
/// This tool provides basic knowledge base information and statistics including file size,
/// content length, and availability status for agent understanding of knowledge scope.
/// </remarks>
public class GetKbInfoTool
{
    private readonly ILogger<GetKbInfoTool> _logger;
    private readonly IKnowledgeBaseService _knowledgeBaseService;

    public GetKbInfoTool(ILogger<GetKbInfoTool> logger, IKnowledgeBaseService knowledgeBaseService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _knowledgeBaseService = knowledgeBaseService ?? throw new ArgumentNullException(nameof(knowledgeBaseService));
    }

    /// <summary>
    /// Retrieve basic knowledge base information and statistics
    /// MCP Tool Name: get_kb_info
    /// </summary>
    /// <returns>Knowledge base info response with statistics and availability status</returns>
    public async Task<GetKbInfoResponse> ExecuteAsync()
    {
        try
        {
            _logger.LogInformation("Executing get_kb_info tool");

            var kbInfo = await _knowledgeBaseService.GetInfoAsync();

            var response = new GetKbInfoResponse
            {
                Info = kbInfo,
                Success = true
            };

            _logger.LogInformation("Knowledge base info retrieved successfully. Available: {IsAvailable}, Content length: {ContentLength}", 
                kbInfo.IsAvailable, kbInfo.ContentLength);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing get_kb_info tool");
            return new GetKbInfoResponse
            {
                Success = false,
                ErrorMessage = "Info retrieval error: " + ex.Message
            };
        }
    }

    /// <summary>
    /// Get tool metadata for MCP discovery
    /// TODO: Replace with proper MCP SDK attributes when available
    /// </summary>
    public static object GetToolMetadata()
    {
        return new
        {
            Name = "get_kb_info",
            Description = "Retrieve basic knowledge base information and statistics",
            Parameters = new
            {
                Type = "object",
                Properties = new { }, // No parameters required
                Required = new string[] { } // No required parameters
            }
        };
    }
}