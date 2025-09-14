using McpServerKbContentFetcher.Services;
using McpServerKbContentFetcher.Tools;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.Text.Json;

namespace UnitTests.Tools;

/// <summary>
/// Unit tests for GetKbContentTool validating success, empty, and error paths.
/// </summary>
public class GetKbContentToolTests
{
    private readonly IKnowledgeBaseService _kb;
    private readonly ILogger<GetKbContentTool> _logger;
    private readonly GetKbContentTool _tool;

    public GetKbContentToolTests()
    {
        _kb = Substitute.For<IKnowledgeBaseService>();
        _logger = Substitute.For<ILogger<GetKbContentTool>>();
        _tool = new GetKbContentTool(_kb, _logger);
    }

    private static JsonElement ToJson(object result)
    {
        var json = JsonSerializer.Serialize(result);
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }

    [Fact]
    public async Task GetContentAsync_WhenContentAvailable_ReturnsOkWithContent()
    {
        var sample = "Azure Managed Grafana Sample Content";
        _kb.GetRawContentAsync().Returns(Task.FromResult(sample));

        var result = await _tool.GetContentAsync();
        var root = ToJson(result);

        Assert.Equal("ok", root.GetProperty("status").GetString());
        Assert.Equal(sample.Length, root.GetProperty("contentLength").GetInt32());
        Assert.Contains("Managed Grafana", root.GetProperty("content").GetString());
    }

    [Fact]
    public async Task GetContentAsync_WhenEmpty_ReturnsEmptyStatus()
    {
        _kb.GetRawContentAsync().Returns(Task.FromResult(string.Empty));

        var result = await _tool.GetContentAsync();
        var root = ToJson(result);

        Assert.Equal("empty", root.GetProperty("status").GetString());
        Assert.Equal(0, root.GetProperty("contentLength").GetInt32());
        Assert.Equal(string.Empty, root.GetProperty("content").GetString());
    }

    [Fact]
    public async Task GetContentAsync_WhenServiceThrows_ReturnsError()
    {
        _kb.GetRawContentAsync().Returns(Task.FromException<string>(new Exception("boom")));

        var result = await _tool.GetContentAsync();
        var root = ToJson(result);

        Assert.Equal("error", root.GetProperty("status").GetString());
        Assert.True(root.TryGetProperty("error", out var err));
        Assert.Contains("boom", err.GetString());
    }
}
