using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using McpServerKbContentFetcher.Configuration;
using McpServerKbContentFetcher.Services;
using Moq;
using Xunit;

namespace UnitTests.Services;

/// <summary>
/// Unit tests for FileKnowledgeBaseService testing search functionality, file loading, and edge cases
/// </summary>
public class FileKnowledgeBaseServiceTests : IDisposable
{
    private readonly Mock<ILogger<FileKnowledgeBaseService>> _mockLogger;
    private readonly IOptions<ServerOptions> _options;
    private readonly string _testFilePath;
    private readonly string _testContent;

    public FileKnowledgeBaseServiceTests()
    {
        _mockLogger = new Mock<ILogger<FileKnowledgeBaseService>>();
        
        // Create a temporary test file
        _testFilePath = Path.GetTempFileName();
        _testContent = @"Azure Managed Grafana Overview
Azure Managed Grafana is a fully managed service for data visualization.
It provides powerful dashboards and monitoring capabilities.
Key features include alerting, data source integration, and scalability.
The service supports multiple authentication methods and role-based access control.";
        
        File.WriteAllText(_testFilePath, _testContent);
        
        _options = Options.Create(new ServerOptions
        {
            KnowledgeBaseFilePath = _testFilePath,
            MaxSearchResults = 3,
            MaxContentLength = 1000,
            ContextCharacters = 50
        });
    }

    public void Dispose()
    {
        if (File.Exists(_testFilePath))
        {
            File.Delete(_testFilePath);
        }
    }

    [Fact]
    public async Task InitializeAsync_WithValidFile_ReturnsTrue()
    {
        // Arrange
        var service = new FileKnowledgeBaseService(_mockLogger.Object, _options);

        // Act
        var result = await service.InitializeAsync();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task InitializeAsync_WithMissingFile_ReturnsFalse()
    {
        // Arrange
        var invalidOptions = Options.Create(new ServerOptions
        {
            KnowledgeBaseFilePath = "nonexistent-file.txt"
        });
        var service = new FileKnowledgeBaseService(_mockLogger.Object, invalidOptions);

        // Act
        var result = await service.InitializeAsync();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task SearchAsync_WithValidQuery_ReturnsResults()
    {
        // Arrange
        var service = new FileKnowledgeBaseService(_mockLogger.Object, _options);
        await service.InitializeAsync();

        // Act
        var results = await service.SearchAsync("Grafana");

        // Assert
        Assert.NotEmpty(results);
        Assert.Contains("Grafana", results[0].Content);
    }

    [Fact]
    public async Task SearchAsync_CaseInsensitive_ReturnsResults()
    {
        // Arrange
        var service = new FileKnowledgeBaseService(_mockLogger.Object, _options);
        await service.InitializeAsync();

        // Act
        var results = await service.SearchAsync("AZURE");

        // Assert
        Assert.NotEmpty(results);
        Assert.Contains("Azure", results[0].Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SearchAsync_WithEmptyQuery_ReturnsEmpty()
    {
        // Arrange
        var service = new FileKnowledgeBaseService(_mockLogger.Object, _options);
        await service.InitializeAsync();

        // Act
        var results = await service.SearchAsync("");

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public async Task SearchAsync_WithNoMatches_ReturnsEmpty()
    {
        // Arrange
        var service = new FileKnowledgeBaseService(_mockLogger.Object, _options);
        await service.InitializeAsync();

        // Act
        var results = await service.SearchAsync("NonExistentTerm");

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public async Task SearchAsync_RespectsMaxResults()
    {
        // Arrange
        var service = new FileKnowledgeBaseService(_mockLogger.Object, _options);
        await service.InitializeAsync();

        // Act
        var results = await service.SearchAsync("and", 2); // "and" appears multiple times

        // Assert
        Assert.True(results.Length <= 2);
    }

    [Fact]
    public async Task GetInfoAsync_AfterInitialization_ReturnsValidInfo()
    {
        // Arrange
        var service = new FileKnowledgeBaseService(_mockLogger.Object, _options);
        await service.InitializeAsync();

        // Act
        var info = await service.GetInfoAsync();

        // Assert
        Assert.True(info.IsAvailable);
        Assert.Equal(_testFilePath, info.FilePath);
        Assert.True(info.ContentLength > 0);
        Assert.True(info.FileSize > 0);
        Assert.Null(info.ErrorMessage);
    }

    [Fact]
    public async Task GetInfoAsync_BeforeInitialization_ReturnsUnavailable()
    {
        // Arrange
        var service = new FileKnowledgeBaseService(_mockLogger.Object, _options);

        // Act
        var info = await service.GetInfoAsync();

        // Assert
        Assert.False(info.IsAvailable);
        Assert.NotNull(info.ErrorMessage);
    }

    [Fact]
    public async Task SearchAsync_BeforeInitialization_ReturnsEmpty()
    {
        // Arrange
        var service = new FileKnowledgeBaseService(_mockLogger.Object, _options);

        // Act
        var results = await service.SearchAsync("test");

        // Assert
        Assert.Empty(results);
    }
}