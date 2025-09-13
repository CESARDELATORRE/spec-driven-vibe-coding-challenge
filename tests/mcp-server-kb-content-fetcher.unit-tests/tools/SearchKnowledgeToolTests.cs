using McpServerKbContentFetcher.Models;
using McpServerKbContentFetcher.Services;
using McpServerKbContentFetcher.Tools;
using Microsoft.Extensions.Logging;
using NSubstitute;

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

    [Fact]
    public async Task SearchAsync_WithValidQuery_ReturnsResults()
    {
        // Arrange
        var query = "test query";
        var searchResults = new List<SearchResult>
        {
            new() { Content = "test content", Context = "context around test content", Position = 0, MatchStrength = 1, Length = 12 }
        };

        _knowledgeBaseService.SearchAsync(query, 3).Returns(searchResults);

        // Act
        var result = await _tool.SearchAsync(query, null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(query, result.Query);
        Assert.Equal(1, result.TotalMatches);
        Assert.Single(result.Results);
        Assert.Equal("context around test content", result.Results[0].Content);
        Assert.Contains("Match strength: 1", result.Results[0].MatchInfo);
    }

    [Fact]
    public async Task SearchAsync_WithEmptyQuery_ReturnsEmptyResults()
    {
        // Arrange
        var query = "";

        // Act
        var result = await _tool.SearchAsync(query, null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(query, result.Query);
        Assert.Equal(0, result.TotalMatches);
        Assert.Empty(result.Results);
        
        // Verify that the knowledge base service was not called
        await _knowledgeBaseService.DidNotReceive().SearchAsync(Arg.Any<string>(), Arg.Any<int>());
    }

    [Fact]
    public async Task SearchAsync_WithNullQuery_ReturnsEmptyResults()
    {
        // Arrange
        string? query = null;

        // Act
        var result = await _tool.SearchAsync(query!, null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(string.Empty, result.Query);
        Assert.Equal(0, result.TotalMatches);
        Assert.Empty(result.Results);
        
        // Verify that the knowledge base service was not called
        await _knowledgeBaseService.DidNotReceive().SearchAsync(Arg.Any<string>(), Arg.Any<int>());
    }

    [Fact]
    public async Task SearchAsync_WithMaxResults_PassesCorrectValue()
    {
        // Arrange
        var query = "test";
        var maxResults = 2;
        var searchResults = new List<SearchResult>();

        _knowledgeBaseService.SearchAsync(query, maxResults).Returns(searchResults);

        // Act
        var result = await _tool.SearchAsync(query, maxResults);

        // Assert
        await _knowledgeBaseService.Received(1).SearchAsync(query, maxResults);
    }

    [Fact]
    public async Task SearchAsync_WithoutMaxResults_UsesDefault()
    {
        // Arrange
        var query = "test";
        var searchResults = new List<SearchResult>();

        _knowledgeBaseService.SearchAsync(query, 3).Returns(searchResults);

        // Act
        var result = await _tool.SearchAsync(query, null);

        // Assert
        await _knowledgeBaseService.Received(1).SearchAsync(query, 3);
    }

    [Theory]
    [InlineData(0, 1)]  // Below minimum becomes 1
    [InlineData(6, 5)]  // Above maximum becomes 5
    [InlineData(3, 3)]  // Valid value stays the same
    public async Task SearchAsync_WithMaxResults_ClampsValues(int input, int expected)
    {
        // Arrange
        var query = "test";
        var searchResults = new List<SearchResult>();

        _knowledgeBaseService.SearchAsync(query, expected).Returns(searchResults);

        // Act
        var result = await _tool.SearchAsync(query, input);

        // Assert
        await _knowledgeBaseService.Received(1).SearchAsync(query, expected);
    }

    [Fact]
    public async Task SearchAsync_WhenServiceThrows_ReturnsErrorResponse()
    {
        // Arrange
        var query = "test";
        _knowledgeBaseService.SearchAsync(query, Arg.Any<int>()).Returns(Task.FromException<IEnumerable<SearchResult>>(new Exception("Test exception")));

        // Act
        var result = await _tool.SearchAsync(query, null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(query, result.Query);
        Assert.Equal(0, result.TotalMatches);
        Assert.Single(result.Results);
        Assert.Contains("Search error occurred", result.Results[0].Content);
        Assert.Equal("Error", result.Results[0].MatchInfo);
    }

    [Fact]
    public async Task SearchAsync_WithMultipleResults_FormatsCorrectly()
    {
        // Arrange
        var query = "test";
        var searchResults = new List<SearchResult>
        {
            new() { Content = "first", Context = "first context", Position = 0, MatchStrength = 2, Length = 5 },
            new() { Content = "second", Context = "second context", Position = 10, MatchStrength = 1, Length = 6 }
        };

        _knowledgeBaseService.SearchAsync(query, 3).Returns(searchResults);

        // Act
        var result = await _tool.SearchAsync(query, null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalMatches);
        Assert.Equal(2, result.Results.Count);
        
        Assert.Equal("first context", result.Results[0].Content);
        Assert.Contains("Match strength: 2", result.Results[0].MatchInfo);
        Assert.Contains("Position: 0", result.Results[0].MatchInfo);
        
        Assert.Equal("second context", result.Results[1].Content);
        Assert.Contains("Match strength: 1", result.Results[1].MatchInfo);
        Assert.Contains("Position: 10", result.Results[1].MatchInfo);
    }
}