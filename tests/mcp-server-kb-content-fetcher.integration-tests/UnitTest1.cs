using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using McpServerKbContentFetcher.Configuration;
using McpServerKbContentFetcher.Services;
using McpServerKbContentFetcher.Tools;
using Xunit;

namespace IntegrationTests;

/// <summary>
/// Basic integration tests for MCP server components working together
/// </summary>
public class McpServerIntegrationTests : IDisposable
{
    private readonly string _testFilePath;
    private readonly string _testContent;
    private readonly IHost _host;

    public McpServerIntegrationTests()
    {
        // Create test knowledge base content
        _testFilePath = Path.GetTempFileName();
        _testContent = @"Azure Managed Grafana Integration Test Content
This is test content for Azure Managed Grafana integration testing.
It includes information about monitoring, dashboards, and alerting capabilities.
The service provides comprehensive data visualization and analytics features.";
        
        File.WriteAllText(_testFilePath, _testContent);

        // Build test host similar to Program.cs but with test configuration
        var builder = Host.CreateApplicationBuilder();
        
        // Configure test options
        builder.Services.Configure<ServerOptions>(options =>
        {
            options.KnowledgeBaseFilePath = _testFilePath;
            options.MaxSearchResults = 3;
            options.MaxContentLength = 1000;
            options.ContextCharacters = 50;
        });

        // Register services
        builder.Services.AddSingleton<IKnowledgeBaseService, FileKnowledgeBaseService>();
        builder.Services.AddTransient<SearchKnowledgeTool>();
        builder.Services.AddTransient<GetKbInfoTool>();

        _host = builder.Build();
    }

    public void Dispose()
    {
        _host?.Dispose();
        if (File.Exists(_testFilePath))
        {
            File.Delete(_testFilePath);
        }
    }

    [Fact]
    public async Task KnowledgeBaseService_CanInitializeAndSearch()
    {
        // Arrange
        var kbService = _host.Services.GetRequiredService<IKnowledgeBaseService>();

        // Act - Initialize
        var initResult = await kbService.InitializeAsync();
        
        // Assert - Initialization
        Assert.True(initResult);

        // Act - Search
        var searchResults = await kbService.SearchAsync("Azure Managed Grafana");

        // Assert - Search
        Assert.NotEmpty(searchResults);
        Assert.Contains("Azure Managed Grafana", searchResults[0].Content);
    }

    [Fact]
    public async Task SearchKnowledgeTool_EndToEndFunctionality()
    {
        // Arrange
        var kbService = _host.Services.GetRequiredService<IKnowledgeBaseService>();
        var searchTool = _host.Services.GetRequiredService<SearchKnowledgeTool>();

        await kbService.InitializeAsync();

        var request = new McpServerKbContentFetcher.Models.SearchKnowledgeRequest
        {
            Query = "monitoring",
            MaxResults = 2
        };

        // Act
        var response = await searchTool.ExecuteAsync(request);

        // Assert
        Assert.True(response.Success);
        Assert.Equal("monitoring", response.Query);
        Assert.NotEmpty(response.Results);
        Assert.Contains("monitoring", response.Results[0].Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetKbInfoTool_ReturnsCorrectInformation()
    {
        // Arrange
        var kbService = _host.Services.GetRequiredService<IKnowledgeBaseService>();
        var infoTool = _host.Services.GetRequiredService<GetKbInfoTool>();

        await kbService.InitializeAsync();

        // Act
        var response = await infoTool.ExecuteAsync();

        // Assert
        Assert.True(response.Success);
        Assert.True(response.Info.IsAvailable);
        Assert.Equal(_testFilePath, response.Info.FilePath);
        Assert.True(response.Info.ContentLength > 0);
        Assert.True(response.Info.FileSize > 0);
    }

    [Fact]
    public async Task DependencyInjection_AllServicesResolve()
    {
        // Act & Assert - Services should resolve without exceptions
        var kbService = _host.Services.GetRequiredService<IKnowledgeBaseService>();
        var searchTool = _host.Services.GetRequiredService<SearchKnowledgeTool>();
        var infoTool = _host.Services.GetRequiredService<GetKbInfoTool>();
        var options = _host.Services.GetRequiredService<IOptions<ServerOptions>>();

        Assert.NotNull(kbService);
        Assert.NotNull(searchTool);
        Assert.NotNull(infoTool);
        Assert.NotNull(options);
        Assert.Equal(_testFilePath, options.Value.KnowledgeBaseFilePath);
    }

    [Fact]
    public async Task FullWorkflow_InitializeSearchAndGetInfo()
    {
        // Arrange
        var kbService = _host.Services.GetRequiredService<IKnowledgeBaseService>();
        var searchTool = _host.Services.GetRequiredService<SearchKnowledgeTool>();
        var infoTool = _host.Services.GetRequiredService<GetKbInfoTool>();

        // Act & Assert - Full workflow
        
        // 1. Initialize knowledge base
        var initResult = await kbService.InitializeAsync();
        Assert.True(initResult);

        // 2. Get knowledge base info
        var infoResponse = await infoTool.ExecuteAsync();
        Assert.True(infoResponse.Success);
        Assert.True(infoResponse.Info.IsAvailable);

        // 3. Search for content
        var searchRequest = new McpServerKbContentFetcher.Models.SearchKnowledgeRequest
        {
            Query = "Azure",
            MaxResults = 1
        };
        var searchResponse = await searchTool.ExecuteAsync(searchRequest);
        Assert.True(searchResponse.Success);
        Assert.NotEmpty(searchResponse.Results);

        // 4. Verify search result contains expected content
        Assert.Contains("Azure", searchResponse.Results[0].Content);
    }
}