using McpServerKbContentFetcher.Models;
using McpServerKbContentFetcher.Services;
using McpServerKbContentFetcher.Tools;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.Text.Json;

namespace UnitTests.Tools;

public class GetKbInfoToolTests
{
    private readonly IKnowledgeBaseService _knowledgeBaseService;
    private readonly ILogger<GetKbInfoTool> _logger;
    private readonly GetKbInfoTool _tool;

    public GetKbInfoToolTests()
    {
        _knowledgeBaseService = Substitute.For<IKnowledgeBaseService>();
        _logger = Substitute.For<ILogger<GetKbInfoTool>>();
        _tool = new GetKbInfoTool(_knowledgeBaseService, _logger);
    }

    private static JsonElement SerializeToJsonElement(object result)
    {
        var json = JsonSerializer.Serialize(result);
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }

    [Fact]
    public async Task GetInfoAsync_WithAvailableKnowledgeBase_ReturnsAvailableInfo()
    {
        // Arrange
        var knowledgeBaseInfo = new KnowledgeBaseInfo
        {
            IsAvailable = true,
            ContentLength = 1000,
            FileSizeBytes = 1500,
            Description = "Test knowledge base",
            LastModified = DateTime.UtcNow
        };
        _knowledgeBaseService.GetInfoAsync().Returns(knowledgeBaseInfo);

        // Act
        var result = await _tool.GetInfoAsync();

        // Assert
        Assert.NotNull(result);
        var root = SerializeToJsonElement(result);

    Assert.Equal("ok", root.GetProperty("status").GetString());
        var info = root.GetProperty("info");
        Assert.True(info.GetProperty("isAvailable").GetBoolean());
        Assert.Equal(1000, info.GetProperty("contentLength").GetInt32());
        Assert.Equal(1500, info.GetProperty("fileSizeBytes").GetInt32());
        Assert.Equal("Test knowledge base", info.GetProperty("description").GetString());
    }

    [Fact]
    public async Task GetInfoAsync_WithUnavailableKnowledgeBase_ReturnsUnavailableInfo()
    {
        // Arrange
        var knowledgeBaseInfo = new KnowledgeBaseInfo
        {
            IsAvailable = false,
            ContentLength = 0,
            FileSizeBytes = 0,
            Description = "Azure Managed Grafana knowledge base",
            LastModified = DateTime.MinValue
        };
        _knowledgeBaseService.GetInfoAsync().Returns(knowledgeBaseInfo);

        // Act
        var result = await _tool.GetInfoAsync();

        // Assert
        Assert.NotNull(result);
        var root = SerializeToJsonElement(result);

        Assert.Equal("unavailable", root.GetProperty("status").GetString());
        var info = root.GetProperty("info");
        Assert.False(info.GetProperty("isAvailable").GetBoolean());
        Assert.Equal(0, info.GetProperty("contentLength").GetInt32());
        Assert.Equal(0, info.GetProperty("fileSizeBytes").GetInt32());
    }

    [Fact]
    public async Task GetInfoAsync_WhenServiceThrows_ReturnsErrorResponse()
    {
        _knowledgeBaseService.GetInfoAsync().Returns(Task.FromException<KnowledgeBaseInfo>(new Exception("Test exception")));

        // Act
        var result = await _tool.GetInfoAsync();

        // Assert
        Assert.NotNull(result);
        var root = SerializeToJsonElement(result);

        Assert.Equal("Error retrieving knowledge base information", root.GetProperty("status").GetString());
        Assert.True(root.TryGetProperty("error", out _));
    }

    [Fact]
    public async Task GetInfoAsync_CallsKnowledgeBaseServiceOnce()
    {
        // Arrange
        var knowledgeBaseInfo = new KnowledgeBaseInfo { IsAvailable = true };
        _knowledgeBaseService.GetInfoAsync().Returns(knowledgeBaseInfo);

        // Act
        await _tool.GetInfoAsync();

        // Assert
        await _knowledgeBaseService.Received(1).GetInfoAsync();
    }

    [Fact]
    public async Task GetInfoAsync_WithPartialInfo_HandlesGracefully()
    {
        // Arrange
        var knowledgeBaseInfo = new KnowledgeBaseInfo
        {
            IsAvailable = true,
            ContentLength = 500,
            FileSizeBytes = 0, // Missing file size
            Description = null!, // Missing description
            LastModified = DateTime.MinValue
        };
        _knowledgeBaseService.GetInfoAsync().Returns(knowledgeBaseInfo);

        // Act
        var result = await _tool.GetInfoAsync();

        // Assert
        Assert.NotNull(result);
        var root = SerializeToJsonElement(result);

    Assert.Equal("ok", root.GetProperty("status").GetString());
        var info = root.GetProperty("info");
        Assert.True(info.GetProperty("isAvailable").GetBoolean());
        Assert.Equal(500, info.GetProperty("contentLength").GetInt32());
        Assert.Equal(0, info.GetProperty("fileSizeBytes").GetInt32());
    }
}