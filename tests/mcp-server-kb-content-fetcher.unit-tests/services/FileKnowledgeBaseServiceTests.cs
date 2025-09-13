using McpServerKbContentFetcher.Configuration;
using McpServerKbContentFetcher.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace UnitTests.Services;

/// <summary>
/// Unit tests for FileKnowledgeBaseService functionality.
/// </summary>
public class FileKnowledgeBaseServiceTests : IDisposable
{
    private readonly Mock<ILogger<FileKnowledgeBaseService>> _mockLogger;
    private readonly string _testFilePath;
    private readonly string _testContent;

    public FileKnowledgeBaseServiceTests()
    {
        _mockLogger = new Mock<ILogger<FileKnowledgeBaseService>>();
        _testFilePath = Path.GetTempFileName();
        _testContent = "Azure Grafana is a fully managed service. " +
                      "It provides scalable experience for monitoring. " +
                      "The service includes authentication and authorization features.";
        
        // Write test content to temporary file
        File.WriteAllText(_testFilePath, _testContent);
    }

    [Fact]
    public async Task InitializeAsync_WithValidFile_ReturnsTrue()
    {
        // Arrange
        var options = Options.Create(new ServerOptions { FilePath = _testFilePath });
        var service = new FileKnowledgeBaseService(options, _mockLogger.Object);

        // Act
        var result = await service.InitializeAsync();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task InitializeAsync_WithMissingFile_ReturnsFalse()
    {
        // Arrange
        var options = Options.Create(new ServerOptions { FilePath = "nonexistent-file.txt" });
        var service = new FileKnowledgeBaseService(options, _mockLogger.Object);

        // Act
        var result = await service.InitializeAsync();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task SearchAsync_WithValidQuery_ReturnsResults()
    {
        // Arrange
        var options = Options.Create(new ServerOptions { FilePath = _testFilePath, ContextLength = 20 });
        var service = new FileKnowledgeBaseService(options, _mockLogger.Object);
        await service.InitializeAsync();

        // Act
        var results = await service.SearchAsync("managed", 5);

        // Assert
        Assert.Single(results); // "managed" appears once in our updated test content
        Assert.Contains("managed", results[0].Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SearchAsync_WithEmptyQuery_ReturnsEmpty()
    {
        // Arrange
        var options = Options.Create(new ServerOptions { FilePath = _testFilePath });
        var service = new FileKnowledgeBaseService(options, _mockLogger.Object);
        await service.InitializeAsync();

        // Act
        var results = await service.SearchAsync("", 5);

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public async Task SearchAsync_WithNonMatchingQuery_ReturnsEmpty()
    {
        // Arrange
        var options = Options.Create(new ServerOptions { FilePath = _testFilePath });
        var service = new FileKnowledgeBaseService(options, _mockLogger.Object);
        await service.InitializeAsync();

        // Act
        var results = await service.SearchAsync("nonexistent", 5);

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public async Task SearchAsync_CaseInsensitive_ReturnsResults()
    {
        // Arrange
        var options = Options.Create(new ServerOptions { FilePath = _testFilePath });
        var service = new FileKnowledgeBaseService(options, _mockLogger.Object);
        await service.InitializeAsync();

        // Act
        var results = await service.SearchAsync("AZURE", 5);

        // Assert
        Assert.Single(results);
        Assert.Contains("Azure", results[0].Content);
    }

    [Fact]
    public async Task SearchAsync_RespectsMaxResults_LimitsResults()
    {
        // Arrange - Create content with multiple occurrences
        var multipleOccurrenceContent = "test test test test test";
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, multipleOccurrenceContent);
        
        var options = Options.Create(new ServerOptions { FilePath = tempFile });
        var service = new FileKnowledgeBaseService(options, _mockLogger.Object);
        await service.InitializeAsync();

        // Act
        var results = await service.SearchAsync("test", 2);

        // Assert
        Assert.Equal(2, results.Length);
        
        // Cleanup
        File.Delete(tempFile);
    }

    [Fact]
    public async Task GetInfoAsync_WithInitializedService_ReturnsCorrectInfo()
    {
        // Arrange
        var options = Options.Create(new ServerOptions { FilePath = _testFilePath });
        var service = new FileKnowledgeBaseService(options, _mockLogger.Object);
        await service.InitializeAsync();

        // Act
        var info = await service.GetInfoAsync();

        // Assert
        Assert.True(info.IsAvailable);
        Assert.Equal(_testFilePath, info.FilePath);
        Assert.Equal(_testContent.Length, info.ContentLength);
        Assert.True(info.FileSizeBytes > 0);
        Assert.Null(info.ErrorMessage);
    }

    [Fact]
    public async Task GetInfoAsync_WithUninitializedService_ReturnsUnavailable()
    {
        // Arrange
        var options = Options.Create(new ServerOptions { FilePath = _testFilePath });
        var service = new FileKnowledgeBaseService(options, _mockLogger.Object);
        // Note: Not calling InitializeAsync

        // Act
        var info = await service.GetInfoAsync();

        // Assert
        Assert.False(info.IsAvailable);
        Assert.Equal(_testFilePath, info.FilePath);
        Assert.Equal(0, info.ContentLength);
    }

    public void Dispose()
    {
        // Cleanup temporary file
        if (File.Exists(_testFilePath))
        {
            File.Delete(_testFilePath);
        }
    }
}