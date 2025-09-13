using McpServerKbContentFetcher.Models;
using McpServerKbContentFetcher.Services;
using Microsoft.Extensions.Logging;

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
    /// <returns>Knowledge base information including size, availability, and basic statistics</returns>
    public async Task<GetKbInfoResponse> GetInfoAsync()
    {
        try
        {
            _logger.LogInformation("GetKbInfo tool called");

            var info = await _knowledgeBaseService.GetInfoAsync();
            
            var status = info.IsAvailable 
                ? "Knowledge base is available and loaded"
                : "Knowledge base is not available";

            var response = new GetKbInfoResponse
            {
                Info = info,
                Status = status
            };

            _logger.LogInformation("GetKbInfo tool completed. Available: {IsAvailable}, Content length: {ContentLength}", 
                info.IsAvailable, info.ContentLength);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetKbInfo tool");
            
            return new GetKbInfoResponse
            {
                Info = new KnowledgeBaseInfo { IsAvailable = false },
                Status = "Error retrieving knowledge base information"
            };
        }
    }
}