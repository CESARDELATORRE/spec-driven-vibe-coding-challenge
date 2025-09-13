using Microsoft.Extensions.Logging;
using McpServerKbContentFetcher.Models;
using McpServerKbContentFetcher.Services;
using McpServerKbContentFetcher.Tools;
using Moq;
using Xunit;

namespace UnitTests.Tools;

/// <summary>
/// Unit tests for SearchKnowledgeTool testing parameter validation and result formatting
/// </summary>
public class SearchKnowledgeToolTests
{
    private readonly Mock<ILogger<SearchKnowledgeTool>> _mockLogger;
    private readonly Mock<IKnowledgeBaseService> _mockKnowledgeBaseService;
    private readonly SearchKnowledgeTool _tool;

    public SearchKnowledgeToolTests()
    {
        _mockLogger = new Mock<ILogger<SearchKnowledgeTool>>();
        _mockKnowledgeBaseService = new Mock<IKnowledgeBaseService>();
        _tool = new SearchKnowledgeTool(_mockLogger.Object, _mockKnowledgeBaseService.Object);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidRequest_ReturnsSuccessResponse()
    {
        // Arrange
        var request = new SearchKnowledgeRequest
        {
            Query = "Azure Managed Grafana",
            MaxResults = 2
        };

        var mockSearchResults = new[]
        {
            new SearchResult
            {
                Content = "Azure Managed Grafana is a service",
                Context = "Azure Managed Grafana is a service for monitoring",
                Position = 0,
                ContentLength = 100
            }
        };

        _mockKnowledgeBaseService
            .Setup(x => x.SearchAsync(request.Query, 2))
            .ReturnsAsync(mockSearchResults);

        // Act
        var response = await _tool.ExecuteAsync(request);

        // Assert
        Assert.True(response.Success);
        Assert.Equal(request.Query, response.Query);
        Assert.Equal(mockSearchResults.Length, response.TotalMatches);
        Assert.Equal(mockSearchResults, response.Results);
        Assert.Null(response.ErrorMessage);
    }

    [Fact]
    public async Task ExecuteAsync_WithNullRequest_ReturnsErrorResponse()
    {
        // Act
        var response = await _tool.ExecuteAsync(null!);

        // Assert
        Assert.False(response.Success);
        Assert.NotNull(response.ErrorMessage);
        Assert.Contains("null", response.ErrorMessage);
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyQuery_ReturnsErrorResponse()
    {
        // Arrange
        var request = new SearchKnowledgeRequest
        {
            Query = "",
            MaxResults = 3
        };

        // Act
        var response = await _tool.ExecuteAsync(request);

        // Assert
        Assert.False(response.Success);
        Assert.NotNull(response.ErrorMessage);
        Assert.Contains("empty", response.ErrorMessage);
    }

    [Fact]
    public async Task ExecuteAsync_WithWhitespaceQuery_ReturnsErrorResponse()
    {
        // Arrange
        var request = new SearchKnowledgeRequest
        {
            Query = "   ",
            MaxResults = 3
        };

        // Act
        var response = await _tool.ExecuteAsync(request);

        // Assert
        Assert.False(response.Success);
        Assert.NotNull(response.ErrorMessage);
        Assert.Contains("empty", response.ErrorMessage);
    }

    [Fact]
    public async Task ExecuteAsync_WithMaxResultsGreaterThan5_LimitsTo5()
    {
        // Arrange
        var request = new SearchKnowledgeRequest
        {
            Query = "test",
            MaxResults = 10 // Should be limited to 5
        };

        var mockSearchResults = Array.Empty<SearchResult>();
        _mockKnowledgeBaseService
            .Setup(x => x.SearchAsync(request.Query, 5)) // Should be called with 5, not 10
            .ReturnsAsync(mockSearchResults);

        // Act
        var response = await _tool.ExecuteAsync(request);

        // Assert
        Assert.True(response.Success);
        _mockKnowledgeBaseService.Verify(x => x.SearchAsync(request.Query, 5), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithNullMaxResults_UsesDefault3()
    {
        // Arrange
        var request = new SearchKnowledgeRequest
        {
            Query = "test",
            MaxResults = null
        };

        var mockSearchResults = Array.Empty<SearchResult>();
        _mockKnowledgeBaseService
            .Setup(x => x.SearchAsync(request.Query, 3)) // Should use default 3
            .ReturnsAsync(mockSearchResults);

        // Act
        var response = await _tool.ExecuteAsync(request);

        // Assert
        Assert.True(response.Success);
        _mockKnowledgeBaseService.Verify(x => x.SearchAsync(request.Query, 3), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenServiceThrowsException_ReturnsErrorResponse()
    {
        // Arrange
        var request = new SearchKnowledgeRequest
        {
            Query = "test",
            MaxResults = 3
        };

        _mockKnowledgeBaseService
            .Setup(x => x.SearchAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ThrowsAsync(new Exception("Service error"));

        // Act
        var response = await _tool.ExecuteAsync(request);

        // Assert
        Assert.False(response.Success);
        Assert.NotNull(response.ErrorMessage);
        Assert.Contains("Service error", response.ErrorMessage);
    }

    [Fact]
    public void GetToolMetadata_ReturnsCorrectMetadata()
    {
        // Act
        var metadata = SearchKnowledgeTool.GetToolMetadata();

        // Assert
        Assert.NotNull(metadata);
        // Additional metadata validation can be added here when the structure is finalized
    }
}