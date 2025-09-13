using McpServerKbContentFetcher.Models;
using McpServerKbContentFetcher.Services;
using McpServerKbContentFetcher.Tools;
using Microsoft.Extensions.Logging;
using NSubstitute;

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
        Assert.NotNull(result.Info);
        Assert.Equal("Knowledge base is available and loaded", result.Status);
        Assert.True(result.Info.IsAvailable);
        Assert.Equal(1000, result.Info.ContentLength);
        Assert.Equal(1500, result.Info.FileSizeBytes);
        Assert.Equal("Test knowledge base", result.Info.Description);
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
        Assert.NotNull(result.Info);
        Assert.Equal("Knowledge base is not available", result.Status);
        Assert.False(result.Info.IsAvailable);
        Assert.Equal(0, result.Info.ContentLength);
        Assert.Equal(0, result.Info.FileSizeBytes);
    }

    [Fact]
    public async Task GetInfoAsync_WhenServiceThrows_ReturnsErrorResponse()
    {
        // Arrange
        _knowledgeBaseService.GetInfoAsync().Returns(Task.FromException<KnowledgeBaseInfo>(new Exception("Test exception")));

        // Act
        var result = await _tool.GetInfoAsync();

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Info);
        Assert.Equal("Error retrieving knowledge base information", result.Status);
        Assert.False(result.Info.IsAvailable);
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
        Assert.NotNull(result.Info);
        Assert.Equal("Knowledge base is available and loaded", result.Status);
        Assert.True(result.Info.IsAvailable);
        Assert.Equal(500, result.Info.ContentLength);
        Assert.Equal(0, result.Info.FileSizeBytes);
    }
}