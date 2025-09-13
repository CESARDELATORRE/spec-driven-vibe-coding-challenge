using McpServerKbContentFetcher.Configuration;
using McpServerKbContentFetcher.Services;
using McpServerKbContentFetcher.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

// Create the host builder with MCP server configuration
var builder = Host.CreateApplicationBuilder(args);

// Configure logging to stderr to avoid corrupting MCP STDIO communication
builder.Logging.ClearProviders();
builder.Logging.AddConsole(options =>
{
    // Route all log levels to stderr to avoid corrupting MCP stdio (stdout) channel
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});

// Configure strongly-typed options
builder.Services.Configure<ServerOptions>(
    builder.Configuration);

// Register knowledge base service
builder.Services.AddSingleton<IKnowledgeBaseService, FileKnowledgeBaseService>();

// Register tool classes for dependency injection
builder.Services.AddSingleton<SearchKnowledgeTool>();
builder.Services.AddSingleton<GetKbInfoTool>();

// Configure MCP server
builder.Services.AddMcpServer()
    .WithStdioServerTransport()
    .WithTools(new[]
    {
        McpServerTool.Create(
            (SearchKnowledgeTool tool, string query, int? max_results) => tool.SearchAsync(query, max_results),
            new McpServerToolCreateOptions
            {
                Name = "search_knowledge",
                Description = "Search the Azure Managed Grafana knowledge base for relevant information"
            }),

        McpServerTool.Create(
            (GetKbInfoTool tool) => tool.GetInfoAsync(),
            new McpServerToolCreateOptions
            {
                Name = "get_kb_info",
                Description = "Get information about the Azure Managed Grafana knowledge base"
            })
    });

var app = builder.Build();

// Initialize knowledge base service
var logger = app.Services.GetRequiredService<ILogger<Program>>();
var knowledgeBaseService = app.Services.GetRequiredService<IKnowledgeBaseService>();

logger.LogInformation("Starting KB MCP Server...");

try
{
    // Initialize the knowledge base
    var initialized = await knowledgeBaseService.InitializeAsync();
    if (!initialized)
    {
        logger.LogError("Failed to initialize knowledge base. Exiting.");
        return 1;
    }

    logger.LogInformation("Knowledge base initialized successfully. Starting MCP server...");

    // Run the MCP server
    await app.RunAsync();
    
    logger.LogInformation("KB MCP Server stopped.");
    return 0;
}
catch (Exception ex)
{
    logger.LogError(ex, "Fatal error occurred while running KB MCP Server");
    return 1;
}
