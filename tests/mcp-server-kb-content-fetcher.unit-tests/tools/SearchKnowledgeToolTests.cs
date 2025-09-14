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

    private static string ExtractTextFromMcpResponse(object[] mcpResponse)
    {
        Assert.NotNull(mcpResponse);
        Assert.Single(mcpResponse);
        
        var contentItem = mcpResponse[0];
        Assert.NotNull(contentItem);
        
        // Use reflection to access the properties since it's an anonymous object
        var typeProperty = contentItem.GetType().GetProperty("type");
        var textProperty = contentItem.GetType().GetProperty("text");
        
        Assert.NotNull(typeProperty);
        Assert.NotNull(textProperty);
        
        var type = typeProperty.GetValue(contentItem)?.ToString();
        var text = textProperty.GetValue(contentItem)?.ToString();
        
        Assert.Equal("text", type);
        Assert.NotNull(text);
        
        return text;
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
        var jsonText = ExtractTextFromMcpResponse(result);
        
        using var jsonDoc = JsonDocument.Parse(jsonText);
        var root = jsonDoc.RootElement;
        
        Assert.Equal(query, root.GetProperty("query").GetString());
        Assert.Equal(1, root.GetProperty("totalMatches").GetInt32());
        
        var results = root.GetProperty("results").EnumerateArray().ToArray();
        Assert.Single(results);
        Assert.Equal("context around test content", results[0].GetProperty("content").GetString());
        Assert.Contains("Match strength: 1", results[0].GetProperty("matchInfo").GetString()!);
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
        var jsonText = ExtractTextFromMcpResponse(result);
        
        using var jsonDoc = JsonDocument.Parse(jsonText);
        var root = jsonDoc.RootElement;
        
        Assert.Equal(query, root.GetProperty("query").GetString());
        Assert.Equal(0, root.GetProperty("totalMatches").GetInt32());
        
        var results = root.GetProperty("results").EnumerateArray().ToArray();
        Assert.Empty(results);
        
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
        var jsonText = ExtractTextFromMcpResponse(result);
        
        using var jsonDoc = JsonDocument.Parse(jsonText);
        var root = jsonDoc.RootElement;
        
        Assert.Equal(string.Empty, root.GetProperty("query").GetString());
        Assert.Equal(0, root.GetProperty("totalMatches").GetInt32());
        
        var results = root.GetProperty("results").EnumerateArray().ToArray();
        Assert.Empty(results);
        
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
        var jsonText = ExtractTextFromMcpResponse(result);
        
        using var jsonDoc = JsonDocument.Parse(jsonText);
        var root = jsonDoc.RootElement;
        
        Assert.Equal(query, root.GetProperty("query").GetString());
        Assert.Equal(0, root.GetProperty("totalMatches").GetInt32());
        
        // In error cases, there's no "results" array, but there are "error" and "details" fields
        Assert.True(root.TryGetProperty("error", out var errorElement));
        Assert.Contains("Search error occurred", errorElement.GetString()!);
        Assert.True(root.TryGetProperty("details", out _));
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
        var jsonText = ExtractTextFromMcpResponse(result);
        
        using var jsonDoc = JsonDocument.Parse(jsonText);
        var root = jsonDoc.RootElement;
        
        Assert.Equal(2, root.GetProperty("totalMatches").GetInt32());
        
        var results = root.GetProperty("results").EnumerateArray().ToArray();
        Assert.Equal(2, results.Length);
        
        Assert.Equal("first context", results[0].GetProperty("content").GetString());
        Assert.Contains("Match strength: 2", results[0].GetProperty("matchInfo").GetString()!);
        Assert.Contains("Position: 0", results[0].GetProperty("matchInfo").GetString()!);
        
        Assert.Equal("second context", results[1].GetProperty("content").GetString());
        Assert.Contains("Match strength: 1", results[1].GetProperty("matchInfo").GetString()!);
        Assert.Contains("Position: 10", results[1].GetProperty("matchInfo").GetString()!);
    }
}