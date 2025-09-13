using ModelContextProtocol.Client;
using ModelContextProtocol.Server;
using System.Diagnostics;
using System.Text.Json;

namespace IntegrationTests;

/// <summary>
/// Integration tests for MCP server protocol compliance and end-to-end functionality.
/// </summary>
public class McpServerIntegrationTests : IDisposable
{
    private readonly string _testKnowledgeBasePath;
    private Process? _serverProcess;

    public McpServerIntegrationTests()
    {
        // Use the test knowledge base file
        _testKnowledgeBasePath = Path.GetFullPath("../fixtures/test-knowledge-content.txt");
        
        if (!File.Exists(_testKnowledgeBasePath))
        {
            throw new FileNotFoundException($"Test knowledge base file not found: {_testKnowledgeBasePath}");
        }
    }

    [Fact]
    public async Task McpServer_CanStart_AndRespondsToBasicQueries()
    {
        // This test verifies the server can start and basic MCP protocol works
        using var client = await CreateMcpClientAsync();
        
        // Test basic server capabilities
        var tools = await client.ListToolsAsync();
        
        Assert.NotEmpty(tools);
        Assert.Contains(tools, t => t.Name == "SearchKnowledgeAsync");
        Assert.Contains(tools, t => t.Name == "GetKnowledgeBaseInfoAsync");
    }

    [Fact]
    public async Task SearchKnowledgeTool_WithValidQuery_ReturnsResults()
    {
        using var client = await CreateMcpClientAsync();
        
        // Call the search tool
        var result = await client.CallToolAsync(
            "SearchKnowledgeAsync",
            new Dictionary<string, object?>
            {
                ["query"] = "Azure",
                ["maxResults"] = 2
            });

        // Verify response
        Assert.NotNull(result);
        Assert.NotEmpty(result.Content);
        
        var textContent = result.Content.FirstOrDefault(c => c.Type == "text");
        Assert.NotNull(textContent);
        Assert.Contains("Azure", textContent.Text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SearchKnowledgeTool_WithEmptyQuery_ReturnsNoResults()
    {
        using var client = await CreateMcpClientAsync();
        
        // Call the search tool with empty query
        var result = await client.CallToolAsync(
            "SearchKnowledgeAsync",
            new Dictionary<string, object?>
            {
                ["query"] = "",
                ["maxResults"] = 2
            });

        // Verify response indicates no results
        Assert.NotNull(result);
        Assert.NotEmpty(result.Content);
        
        var textContent = result.Content.FirstOrDefault(c => c.Type == "text");
        Assert.NotNull(textContent);
        Assert.Contains("No search query provided", textContent.Text);
    }

    [Fact]
    public async Task GetKbInfoTool_ReturnsCorrectInformation()
    {
        using var client = await CreateMcpClientAsync();
        
        // Call the get info tool
        var result = await client.CallToolAsync(
            "GetKnowledgeBaseInfoAsync",
            new Dictionary<string, object?>());

        // Verify response
        Assert.NotNull(result);
        Assert.NotEmpty(result.Content);
        
        var textContent = result.Content.FirstOrDefault(c => c.Type == "text");
        Assert.NotNull(textContent);
        Assert.Contains("Knowledge Base Information", textContent.Text);
        Assert.Contains("Available: True", textContent.Text);
    }

    [Fact]
    public async Task SearchKnowledgeTool_CaseInsensitive_ReturnsResults()
    {
        using var client = await CreateMcpClientAsync();
        
        // Test case insensitive search
        var result = await client.CallToolAsync(
            "SearchKnowledgeAsync",
            new Dictionary<string, object?>
            {
                ["query"] = "GRAFANA",
                ["maxResults"] = 1
            });

        // Verify response
        Assert.NotNull(result);
        Assert.NotEmpty(result.Content);
        
        var textContent = result.Content.FirstOrDefault(c => c.Type == "text");
        Assert.NotNull(textContent);
        Assert.Contains("Grafana", textContent.Text, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<IMcpClient> CreateMcpClientAsync()
    {
        // Build the path to the compiled server executable
        var serverPath = Path.GetFullPath("../../src/mcp-server-kb-content-fetcher/bin/Debug/net8.0/mcp-server-kb-content-fetcher.dll");
        
        if (!File.Exists(serverPath))
        {
            throw new FileNotFoundException($"Server executable not found: {serverPath}");
        }

        // Create STDIO transport to communicate with the server process
        var clientTransport = new StdioClientTransport(new StdioClientTransportOptions
        {
            Name = "IntegrationTestClient",
            Command = "dotnet",
            Arguments = [serverPath],
            WorkingDirectory = Path.GetDirectoryName(serverPath),
            EnvironmentVariables = new Dictionary<string, string>
            {
                // Override knowledge base path for testing
                ["KnowledgeBase__FilePath"] = _testKnowledgeBasePath
            }
        });

        // Create and connect the MCP client
        var client = await McpClientFactory.CreateAsync(clientTransport);
        return client;
    }

    public void Dispose()
    {
        _serverProcess?.Kill();
        _serverProcess?.Dispose();
    }
}