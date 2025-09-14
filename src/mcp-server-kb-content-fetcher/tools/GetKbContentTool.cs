using McpServerKbContentFetcher.Services;
using Microsoft.Extensions.Logging;

namespace McpServerKbContentFetcher.Tools;

/// <summary>
/// MCP tool that returns the full raw knowledge base content.
/// Prototype-only convenience: suitable for small/medium text files.
/// </summary>
public class GetKbContentTool
{
    private readonly IKnowledgeBaseService _kb;
    private readonly ILogger<GetKbContentTool> _logger;

    public GetKbContentTool(
        IKnowledgeBaseService kb,
        ILogger<GetKbContentTool> logger)
    {
        _kb = kb;
        _logger = logger;
    }

    /// <summary>
    /// Return full knowledge base content plus basic metadata.
    /// </summary>
    public async Task<object> GetContentAsync()
    {
        try
        {
            _logger.LogInformation("GetKbContent tool called");
            var raw = await _kb.GetRawContentAsync();
            var length = raw.Length;
            _logger.LogInformation("GetKbContent tool returning content length {Length}", length);
            return new
            {
                status = length > 0 ? "ok" : "empty",
                contentLength = length,
                content = raw
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetKbContent tool");
            return new
            {
                status = "error",
                error = ex.Message
            };
        }
    }
}