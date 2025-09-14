using McpServerKbContentFetcher.Models;
using McpServerKbContentFetcher.Services;
using McpServerKbContentFetcher.Tools;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.Text.Json;

namespace UnitTests.Tools;

public class SearchKnowledgeToolTests
{
    private readonly IKnowledgeBaseService _knowledgeBaseService;
    private readonly ILogger<SearchKnowledgeTool> _logger;
    private readonly SearchKnowledgeTool _tool;

    public SearchKnowledgeToolTests()
    {
        _knowledgeBaseService = Substitute.For<IKnowledgeBaseService>();
        _logger = Substitute.For<ILogger<SearchKnowledgeTool>>();
        _tool = new SearchKnowledgeTool(_knowledgeBaseService, _logger);
    }

    private static JsonElement SerializeToJsonElement(object result)
    {
        var json = JsonSerializer.Serialize(result);
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }

    [Fact]
    public async Task GetExcerptAsync_Returns_Truncated_When_Exceeds_MaxLength()
    {
        // Arrange
        var raw = new string('A', 5000);
        _knowledgeBaseService.GetRawContentAsync().Returns(raw);

        // Act
        var result = await _tool.GetExcerptAsync(3000);

        // Assert
        var root = SerializeToJsonElement(result);
        Assert.Equal("ok", root.GetProperty("status").GetString());
        Assert.Equal(5000, root.GetProperty("totalLength").GetInt32());
        Assert.Equal(3000, root.GetProperty("excerptLength").GetInt32());
        Assert.True(root.GetProperty("truncated").GetBoolean());
        Assert.Equal(3000, root.GetProperty("excerpt").GetString()!.Length);
    }

    [Fact]
    public async Task GetExcerptAsync_Returns_Full_When_Shorter_Than_MaxLength()
    {
        var raw = "short content";
        _knowledgeBaseService.GetRawContentAsync().Returns(raw);

        var result = await _tool.GetExcerptAsync(3000);

        var root = SerializeToJsonElement(result);
        Assert.Equal("ok", root.GetProperty("status").GetString());
        Assert.Equal(raw.Length, root.GetProperty("totalLength").GetInt32());
        Assert.Equal(raw.Length, root.GetProperty("excerptLength").GetInt32());
        Assert.False(root.GetProperty("truncated").GetBoolean());
        Assert.Equal(raw, root.GetProperty("excerpt").GetString());
    }

    [Fact]
    public async Task GetExcerptAsync_When_Raw_Content_Empty_Returns_Empty_Status()
    {
        _knowledgeBaseService.GetRawContentAsync().Returns(string.Empty);
        var result = await _tool.GetExcerptAsync();
        var root = SerializeToJsonElement(result);
        Assert.Equal("empty", root.GetProperty("status").GetString());
        Assert.Equal(0, root.GetProperty("totalLength").GetInt32());
        Assert.False(root.GetProperty("truncated").GetBoolean());
    }

    [Fact]
    public async Task GetExcerptAsync_Negative_MaxLength_Clamped_To_1()
    {
        _knowledgeBaseService.GetRawContentAsync().Returns("abc");
        var result = await _tool.GetExcerptAsync(0);
        var root = SerializeToJsonElement(result);
        Assert.Equal(1, root.GetProperty("excerptLength").GetInt32());
    }

    [Fact]
    public async Task GetExcerptAsync_MaxLength_UpperBound_Enforced()
    {
        var raw = new string('B', 20000);
        _knowledgeBaseService.GetRawContentAsync().Returns(raw);
        var result = await _tool.GetExcerptAsync(50000); // request beyond ceiling
        var root = SerializeToJsonElement(result);
        Assert.Equal(10000, root.GetProperty("excerptLength").GetInt32());
        Assert.True(root.GetProperty("truncated").GetBoolean());
    }

    // Removed obsolete max_results clamping tests (search removed)

    [Fact]
    public async Task GetExcerptAsync_When_ServiceThrows_Returns_Error()
    {
        _knowledgeBaseService.GetRawContentAsync().Returns(Task.FromException<string>(new Exception("boom")));
        var result = await _tool.GetExcerptAsync();
        var root = SerializeToJsonElement(result);
        Assert.Equal("error", root.GetProperty("status").GetString());
        Assert.True(root.TryGetProperty("details", out _));
    }

    // Removed multi-result formatting test (search removed in excerpt mode)
}