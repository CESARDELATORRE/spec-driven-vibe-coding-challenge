using McpServerKbContentFetcher.Configuration;
using McpServerKbContentFetcher.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;

var builder = Host.CreateApplicationBuilder(args);

// Configure logging to go to stderr to avoid corrupting MCP STDIO channel
builder.Logging.AddConsole(consoleLogOptions =>
{
    // Route all log levels to stderr to avoid corrupting MCP stdio (stdout) channel
    consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
});

// Configure server options from appsettings.json
builder.Services.Configure<ServerOptions>(
    builder.Configuration.GetSection(ServerOptions.SectionName));

// Register knowledge base service
builder.Services.AddSingleton<IKnowledgeBaseService, FileKnowledgeBaseService>();

// Register MCP server with STDIO transport and tool auto-discovery
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

// Build and initialize the knowledge base service
var app = builder.Build();

// Initialize the knowledge base service before starting the server
var knowledgeBaseService = app.Services.GetRequiredService<IKnowledgeBaseService>();
var logger = app.Services.GetRequiredService<ILogger<Program>>();

logger.LogInformation("Initializing knowledge base service...");
var initResult = await knowledgeBaseService.InitializeAsync();

if (!initResult)
{
    logger.LogError("Failed to initialize knowledge base service. Server will continue but tools may not function properly.");
}
else
{
    logger.LogInformation("Knowledge base service initialized successfully.");
}

logger.LogInformation("Starting MCP server with STDIO transport...");

// Run the MCP server
await app.RunAsync();
