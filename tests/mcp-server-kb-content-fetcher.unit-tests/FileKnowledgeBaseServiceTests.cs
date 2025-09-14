using McpServerKbContentFetcher.Configuration;
using McpServerKbContentFetcher.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace UnitTests.Services;

public class FileKnowledgeBaseServiceTests
{
    private readonly ILogger<FileKnowledgeBaseService> _logger;
    private readonly IOptions<ServerOptions> _options;
    private readonly string _testDataPath;

    public FileKnowledgeBaseServiceTests()
    {
        _logger = Substitute.For<ILogger<FileKnowledgeBaseService>>();
        
        // Use test data file that should be copied to output directory
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        _testDataPath = Path.Combine(baseDir, "test-data", "test-knowledge-base.txt");
        
        var serverOptions = new ServerOptions
        {
            KnowledgeBase = new KnowledgeBaseOptions
            {
                FilePath = _testDataPath
            }
        };
        
        _options = Substitute.For<IOptions<ServerOptions>>();
        _options.Value.Returns(serverOptions);
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
    public async Task InitializeAsync_WithValidFile_ReturnsTrue()
    {
        // Arrange
        var service = new FileKnowledgeBaseService(_logger, _options);

        // Act
        var result = await service.InitializeAsync();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task InitializeAsync_WithInvalidFile_ReturnsFalse()
    {
        // Arrange
        var serverOptions = new ServerOptions
        {
            KnowledgeBase = new KnowledgeBaseOptions
            {
                FilePath = "non-existent-file.txt"
            }
        };
        
        var invalidOptions = Substitute.For<IOptions<ServerOptions>>();
        invalidOptions.Value.Returns(serverOptions);
        
        var service = new FileKnowledgeBaseService(_logger, invalidOptions);

        // Act
        var result = await service.InitializeAsync();

        // Assert
        Assert.False(result);
    }


    [Fact]
    public async Task GetInfoAsync_WithInitializedService_ReturnsValidInfo()
    {
        // Arrange
        var service = new FileKnowledgeBaseService(_logger, _options);
        await service.InitializeAsync();

        // Act
        var info = await service.GetInfoAsync();

        // Assert
        Assert.NotNull(info);
        Assert.True(info.IsAvailable);
        Assert.True(info.ContentLength > 0);
        Assert.True(info.FileSizeBytes > 0);
        Assert.Equal("Azure Managed Grafana knowledge base", info.Description);
    }

    [Fact]
    public async Task GetInfoAsync_WithUninitializedService_ReturnsUnavailableInfo()
    {
        // Arrange
        var service = new FileKnowledgeBaseService(_logger, _options);
        // Note: Not calling InitializeAsync

        // Act
        var info = await service.GetInfoAsync();

        // Assert
        Assert.NotNull(info);
        Assert.False(info.IsAvailable);
        Assert.Equal(0, info.ContentLength);
    }

}
