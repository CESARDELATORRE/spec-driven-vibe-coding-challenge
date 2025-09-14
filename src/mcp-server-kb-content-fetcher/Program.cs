using McpServerKbContentFetcher.Configuration;
using McpServerKbContentFetcher.Services;
using McpServerKbContentFetcher.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

// Optional CLI Mode (diagnostics): Enabled ONLY when --cli-mode flag is present.
// This ensures default usage remains pure MCP server behavior.
// Usage examples (diagnostic only, not part of MCP protocol):
// dotnet run --project src/mcp-server-kb-content-fetcher -- --cli-mode --get-kb-info
// dotnet run --project src/mcp-server-kb-content-fetcher -- --cli-mode --search "Azure Managed Grafana pricing" --max-results 5
var originalArgs = args.ToList();
var cliMode = originalArgs.Remove("--cli-mode");

// Parse additional CLI flags only if cli-mode is active
var useCliGetInfo = false;
var hasCliSearch = false;
var searchFlagIndex = -1;
var maxResults = 0;
string? cliSearchQuery = null; // captured early so we can safely remove args later
if (cliMode)
{
    useCliGetInfo = originalArgs.Contains("--get-kb-info", StringComparer.OrdinalIgnoreCase);
    searchFlagIndex = originalArgs.FindIndex(a => string.Equals(a, "--search", StringComparison.OrdinalIgnoreCase));
    hasCliSearch = searchFlagIndex >= 0 && searchFlagIndex + 1 < originalArgs.Count;
    if (hasCliSearch)
    {
        // capture the raw search term before we mutate/remove args
        cliSearchQuery = originalArgs[searchFlagIndex + 1];
    }
    var maxResultsIndex = originalArgs.FindIndex(a => string.Equals(a, "--max-results", StringComparison.OrdinalIgnoreCase));
    if (maxResultsIndex >= 0 && maxResultsIndex + 1 < originalArgs.Count && int.TryParse(originalArgs[maxResultsIndex + 1], out var parsed))
    {
        maxResults = parsed;
    }
}

// Remove custom flags before building host (avoid leaking into generic host / config binding)
if (cliMode)
{
    originalArgs.Remove("--get-kb-info");
    if (searchFlagIndex >= 0 && searchFlagIndex + 1 < originalArgs.Count)
    {
        // remove search term value first, then flag
        var term = originalArgs[searchFlagIndex + 1];
        originalArgs.Remove(term);
        originalArgs.Remove("--search");
    }
    originalArgs.Remove("--max-results");
}

// Create the host builder with sanitized args (MCP path unaffected when cliMode=false)
var builder = Host.CreateApplicationBuilder(originalArgs.ToArray());

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

// Pre-build a lightweight service provider for tool resolution (avoids leaking DI parameter into schema)
var toolServiceProvider = builder.Services.BuildServiceProvider();

// Configure MCP server with delegates that ignore the JSON 'tool' placeholder object
builder.Services.AddMcpServer()
    .WithStdioServerTransport()
    .WithTools(new[]
    {
        McpServerTool.Create(
            (string query, int? max_results) =>
            {
                var tool = toolServiceProvider.GetRequiredService<SearchKnowledgeTool>();
                return tool.SearchAsync(query, max_results);
            },
            new McpServerToolCreateOptions
            {
                Name = "search_knowledge",
                Description = "Search the Azure Managed Grafana knowledge base for relevant information"
            }),

        McpServerTool.Create(
            () =>
            {
                var tool = toolServiceProvider.GetRequiredService<GetKbInfoTool>();
                return tool.GetInfoAsync();
            },
            new McpServerToolCreateOptions
            {
                Name = "get_kb_info",
                Description = "Get information about the Azure Managed Grafana knowledge base"
            })
    });

var app = builder.Build();

// If CLI mode requested, perform action(s) and exit (skip MCP server startup)
if (cliMode && (useCliGetInfo || hasCliSearch))
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

        if (hasCliSearch && !string.IsNullOrWhiteSpace(cliSearchQuery))
        {
            var query = cliSearchQuery;
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
