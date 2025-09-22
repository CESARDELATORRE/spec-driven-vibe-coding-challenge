using McpServerKbContentFetcher.Configuration;
using McpServerKbContentFetcher.Services;
using McpServerKbContentFetcher.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace UnitTests.Services;

public class FileKnowledgeBaseServiceTests
{
    private readonly IKnowledgeBaseContentCache _mockCache;
    private readonly ILogger<FileKnowledgeBaseService> _logger;
    private readonly string _testDataPath;

    public FileKnowledgeBaseServiceTests()
    {
        _mockCache = Substitute.For<IKnowledgeBaseContentCache>();
        _logger = Substitute.For<ILogger<FileKnowledgeBaseService>>();
        
        // Use test data file that should be copied to output directory
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        _testDataPath = Path.Combine(baseDir, "test-data", "test-knowledge-base.txt");
    }

    [Fact]
    public void TestFilePath_ShouldExist()
    {
        // This test is to debug the file path issue
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var testDataPath = Path.Combine(baseDir, "test-data", "test-knowledge-base.txt");
        
        Assert.True(File.Exists(testDataPath), $"Test data file should exist at: {testDataPath}");
        
        var content = File.ReadAllText(testDataPath);
        Assert.True(content.Length > 0, "Test data file should not be empty");
        Assert.Contains("Azure Managed Grafana", content);
    }

    [Fact]
    public async Task InitializeAsync_WithValidCache_ReturnsTrue()
    {
        // Arrange
        _mockCache.InitializeAsync().Returns(Task.FromResult(true));
        var service = new FileKnowledgeBaseService(_mockCache, _logger);

        // Act
        var result = await service.InitializeAsync();

        // Assert
        Assert.True(result);
        await _mockCache.Received(1).InitializeAsync();
    }

    [Fact]
    public async Task InitializeAsync_WithFailedCache_ReturnsFalse()
    {
        // Arrange
        _mockCache.InitializeAsync().Returns(Task.FromResult(false));
        var service = new FileKnowledgeBaseService(_mockCache, _logger);

        // Act
        var result = await service.InitializeAsync();

        // Assert
        Assert.False(result);
        await _mockCache.Received(1).InitializeAsync();
    }

    [Fact]
    public async Task GetInfoAsync_WithValidCache_ReturnsInfo()
    {
        // Arrange
        var expectedInfo = new KnowledgeBaseInfo
        {
            IsAvailable = true,
            ContentLength = 1000,
            FileSizeBytes = 1000,
            Description = "Azure Managed Grafana knowledge base",
            LastModified = DateTime.UtcNow
        };
        
        var expectedContent = new KnowledgeBaseContent("Test content", expectedInfo);
        
        _mockCache.GetContentAsync().Returns(Task.FromResult(expectedContent));
        var service = new FileKnowledgeBaseService(_mockCache, _logger);

        // Act
        var info = await service.GetInfoAsync();

        // Assert
        Assert.NotNull(info);
        Assert.True(info.IsAvailable);
        Assert.Equal(1000, info.ContentLength);
        Assert.Equal(1000, info.FileSizeBytes);
        Assert.Equal("Azure Managed Grafana knowledge base", info.Description);
        await _mockCache.Received(1).GetContentAsync();
    }

    [Fact]
    public async Task GetInfoAsync_WithCacheException_ReturnsUnavailableInfo()
    {
        // Arrange
        _mockCache.GetContentAsync().Returns(Task.FromException<KnowledgeBaseContent>(new Exception("Cache error")));
        var service = new FileKnowledgeBaseService(_mockCache, _logger);

        // Act
        var info = await service.GetInfoAsync();

        // Assert
        Assert.NotNull(info);
        Assert.False(info.IsAvailable);
    }

    [Fact]
    public async Task GetRawContentAsync_WithValidCache_ReturnsContent()
    {
        // Arrange
        var expectedInfo = new KnowledgeBaseInfo
        {
            IsAvailable = true,
            ContentLength = 1000,
            FileSizeBytes = 1000,
            Description = "Azure Managed Grafana knowledge base",
            LastModified = DateTime.UtcNow
        };
        
        var expectedContent = new KnowledgeBaseContent("Test raw content", expectedInfo);
        
        _mockCache.GetContentAsync().Returns(Task.FromResult(expectedContent));
        var service = new FileKnowledgeBaseService(_mockCache, _logger);

        // Act
        var content = await service.GetRawContentAsync();

        // Assert
        Assert.Equal("Test raw content", content);
        await _mockCache.Received(1).GetContentAsync();
    }

    [Fact]
    public async Task GetRawContentAsync_WithCacheException_ReturnsEmptyString()
    {
        // Arrange
        _mockCache.GetContentAsync().Returns(Task.FromException<KnowledgeBaseContent>(new Exception("Cache error")));
        var service = new FileKnowledgeBaseService(_mockCache, _logger);

        // Act
        var content = await service.GetRawContentAsync();

        // Assert
        Assert.Equal(string.Empty, content);
    }
}
