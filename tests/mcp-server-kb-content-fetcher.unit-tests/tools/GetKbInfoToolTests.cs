using Microsoft.Extensions.Logging;
using McpServerKbContentFetcher.Models;
using McpServerKbContentFetcher.Services;
using McpServerKbContentFetcher.Tools;
using Moq;
using Xunit;

namespace UnitTests.Tools;

/// <summary>
/// Unit tests for GetKbInfoTool testing info retrieval functionality
/// </summary>
public class GetKbInfoToolTests
{
    private readonly Mock<ILogger<GetKbInfoTool>> _mockLogger;
    private readonly Mock<IKnowledgeBaseService> _mockKnowledgeBaseService;
    private readonly GetKbInfoTool _tool;

    public GetKbInfoToolTests()
    {
        _mockLogger = new Mock<ILogger<GetKbInfoTool>>();
        _mockKnowledgeBaseService = new Mock<IKnowledgeBaseService>();
        _tool = new GetKbInfoTool(_mockLogger.Object, _mockKnowledgeBaseService.Object);
    }

    [Fact]
    public async Task ExecuteAsync_WithAvailableKnowledgeBase_ReturnsSuccessResponse()
    {
        // Arrange
        var mockKbInfo = new KnowledgeBaseInfo
        {
            FilePath = "/test/path/knowledge-base.txt",
            FileSize = 5000,
            ContentLength = 4800,
            IsAvailable = true,
            LastModified = DateTime.UtcNow,
            ErrorMessage = null
        };

        _mockKnowledgeBaseService
            .Setup(x => x.GetInfoAsync())
            .ReturnsAsync(mockKbInfo);

        // Act
        var response = await _tool.ExecuteAsync();

        // Assert
        Assert.True(response.Success);
        Assert.Equal(mockKbInfo, response.Info);
        Assert.Null(response.ErrorMessage);
    }

    [Fact]
    public async Task ExecuteAsync_WithUnavailableKnowledgeBase_ReturnsSuccessWithUnavailableInfo()
    {
        // Arrange
        var mockKbInfo = new KnowledgeBaseInfo
        {
            FilePath = "/test/path/missing-file.txt",
            FileSize = 0,
            ContentLength = 0,
            IsAvailable = false,
            LastModified = DateTime.MinValue,
            ErrorMessage = "File not found"
        };

        _mockKnowledgeBaseService
            .Setup(x => x.GetInfoAsync())
            .ReturnsAsync(mockKbInfo);

        // Act
        var response = await _tool.ExecuteAsync();

        // Assert
        Assert.True(response.Success);
        Assert.Equal(mockKbInfo, response.Info);
        Assert.False(response.Info.IsAvailable);
        Assert.NotNull(response.Info.ErrorMessage);
        Assert.Null(response.ErrorMessage); // Tool itself succeeded, KB just unavailable
    }

    [Fact]
    public async Task ExecuteAsync_WhenServiceThrowsException_ReturnsErrorResponse()
    {
        // Arrange
        _mockKnowledgeBaseService
            .Setup(x => x.GetInfoAsync())
            .ThrowsAsync(new Exception("Service connection error"));

        // Act
        var response = await _tool.ExecuteAsync();

        // Assert
        Assert.False(response.Success);
        Assert.NotNull(response.ErrorMessage);
        Assert.Contains("Service connection error", response.ErrorMessage);
    }

    [Fact]
    public async Task ExecuteAsync_CallsKnowledgeBaseServiceOnce()
    {
        // Arrange
        var mockKbInfo = new KnowledgeBaseInfo
        {
            IsAvailable = true,
            ContentLength = 100
        };

        _mockKnowledgeBaseService
            .Setup(x => x.GetInfoAsync())
            .ReturnsAsync(mockKbInfo);

        // Act
        await _tool.ExecuteAsync();

        // Assert
        _mockKnowledgeBaseService.Verify(x => x.GetInfoAsync(), Times.Once);
    }

    [Fact]
    public void GetToolMetadata_ReturnsCorrectMetadata()
    {
        // Act
        var metadata = GetKbInfoTool.GetToolMetadata();

        // Assert
        Assert.NotNull(metadata);
        // Additional metadata validation can be added here when the structure is finalized
    }
}