using McpServerKbContentFetcher.Configuration;
using McpServerKbContentFetcher.Services;
using McpServerKbContentFetcher.Tools;
using McpServerKbContentFetcher.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

// Optional CLI Mode (diagnostics): Enabled ONLY when --cli-mode flag is present.
// This ensures default usage remains pure MCP server behavior.
// Usage examples (diagnostic only, not part of MCP protocol):
// dotnet run --project src/mcp-server-kb-content-fetcher -- --cli-mode --get-kb-info
var originalArgs = args.ToList();
var cliMode = originalArgs.Remove("--cli-mode");

// Parse additional CLI flags only if cli-mode is active
var useCliGetInfo = false;
// (Deprecated) search CLI options removed
if (cliMode)
{
    useCliGetInfo = originalArgs.Contains("--get-kb-info", StringComparer.OrdinalIgnoreCase);
}

// Remove custom flags before building host (avoid leaking into generic host / config binding)
if (cliMode)
{
    originalArgs.Remove("--get-kb-info");
    // removed legacy search flags
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

// Register tool classes for dependency injection (singletons sharing the SAME service instances as the host)
builder.Services.AddSingleton<SearchKnowledgeTool>();
builder.Services.AddSingleton<GetKbInfoTool>();
builder.Services.AddSingleton<GetKbContentTool>();

// We'll resolve the concrete tool instances AFTER the host is built so that we do not create a second
// independent service provider (the previous approach caused a second IKnowledgeBaseService instance
// that was never initialized, leading to empty / unavailable KB state inside tools).
SearchKnowledgeTool? searchToolRef = null;
GetKbInfoTool? getKbInfoToolRef = null;
GetKbContentTool? getKbContentToolRef = null;

// Configure MCP server tools. The delegates capture the above refs which will be populated post-build.
builder.Services.AddMcpServer()
    .WithStdioServerTransport()
    .WithTools(new[]
    {
        // Re-purposed: return a truncated excerpt (prefix) of the full KB content (default 3000 chars)
        McpServerTool.Create(
            () =>
            {
                if (searchToolRef is null) throw new InvalidOperationException("SearchKnowledgeTool not initialized");
                return searchToolRef.GetExcerptAsync(3000);
            },
            new McpServerToolCreateOptions
            {
                Name = "search_knowledge",
                Description = "Return a truncated excerpt (<=3000 chars) of the knowledge base content"
            }),
        McpServerTool.Create(
            () =>
            {
                if (getKbInfoToolRef is null) throw new InvalidOperationException("GetKbInfoTool not initialized");
                return getKbInfoToolRef.GetInfoAsync();
            },
            new McpServerToolCreateOptions
            {
                Name = "get_kb_info",
                Description = "Get information about the Azure Managed Grafana knowledge base"
            }),
        McpServerTool.Create(
            () =>
            {
                if (getKbContentToolRef is null) throw new InvalidOperationException("GetKbContentTool not initialized");
                return getKbContentToolRef.GetContentAsync();
            },
            new McpServerToolCreateOptions
            {
                Name = "get_kb_content",
                Description = "Return full raw knowledge base content (prototype convenience tool)"
            })
    });

var app = builder.Build();

// Resolve tool instances now (single shared provider) so delegates use the initialized KB service instance
searchToolRef = app.Services.GetRequiredService<SearchKnowledgeTool>();
getKbInfoToolRef = app.Services.GetRequiredService<GetKbInfoTool>();
getKbContentToolRef = app.Services.GetRequiredService<GetKbContentTool>();

// If CLI mode requested, perform action(s) and exit (skip MCP server startup)
if (cliMode && useCliGetInfo)
{
    var cliLogger = app.Services.GetRequiredService<ILogger<Program>>();
    var kbService = app.Services.GetRequiredService<IKnowledgeBaseService>();
    await kbService.InitializeAsync();

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
