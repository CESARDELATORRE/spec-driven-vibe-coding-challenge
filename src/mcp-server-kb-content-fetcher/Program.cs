using McpServerKbContentFetcher.Configuration;
using McpServerKbContentFetcher.Services;
using McpServerKbContentFetcher.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

// Simple CLI parsing for direct (non-MCP) usage to avoid JSON piping complexity
// Supported examples:
// dotnet run --project src/mcp-server-kb-content-fetcher -- --get-kb-info
// dotnet run --project src/mcp-server-kb-content-fetcher -- --search "Azure Managed Grafana pricing" --max-results 5
var cliArgs = args.ToList();
var useCliGetInfo = cliArgs.Contains("--get-kb-info", StringComparer.OrdinalIgnoreCase);
var searchFlagIndex = cliArgs.FindIndex(a => string.Equals(a, "--search", StringComparison.OrdinalIgnoreCase));
var hasCliSearch = searchFlagIndex >= 0 && searchFlagIndex + 1 < cliArgs.Count;
var maxResults = 0;
var maxResultsIndex = cliArgs.FindIndex(a => string.Equals(a, "--max-results", StringComparison.OrdinalIgnoreCase));
if (maxResultsIndex >= 0 && maxResultsIndex + 1 < cliArgs.Count && int.TryParse(cliArgs[maxResultsIndex + 1], out var parsed))
{
    maxResults = parsed;
}

// Create the host builder with MCP server configuration
var builder = Host.CreateApplicationBuilder(args); // we keep full host even for CLI simplicity & option binding

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

// If simple CLI mode requested, perform action and exit (skip MCP server startup)
if (useCliGetInfo || hasCliSearch)
{
    var cliLogger = app.Services.GetRequiredService<ILogger<Program>>();
    var kbService = app.Services.GetRequiredService<IKnowledgeBaseService>();
    await kbService.InitializeAsync();

    if (useCliGetInfo)
    {
        var info = await kbService.GetInfoAsync();
        var payload = new
        {
            info = new
            {
                fileSizeBytes = info.FileSizeBytes,
                contentLength = info.ContentLength,
                isAvailable = info.IsAvailable,
                description = info.Description,
                lastModified = info.LastModified.ToUniversalTime().ToString("o")
            },
            status = info.IsAvailable ? "available" : "unavailable"
        };
        Console.Out.WriteLine(System.Text.Json.JsonSerializer.Serialize(payload));
        return 0;
    }

    if (hasCliSearch)
    {
        var query = cliArgs[searchFlagIndex + 1];
        if (maxResults <= 0) maxResults = 3;
        var results = await kbService.SearchAsync(query, maxResults);
        var payload = new
        {
            query,
            total = results.Count(),
            results = results.Select(r => new
            {
                r.MatchStrength,
                r.Position,
                r.Length,
                r.Content,
                r.Context
            })
        };
        Console.Out.WriteLine(System.Text.Json.JsonSerializer.Serialize(payload));
        return 0;
    }
}

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
