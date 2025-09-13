using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using McpServerKbContentFetcher.Configuration;
using McpServerKbContentFetcher.Services;

/// <summary>
/// Main entry point for the KB MCP Server
/// Implements Host.CreateApplicationBuilder pattern with MCP SDK fluent configuration
/// </summary>
namespace McpServerKbContentFetcher;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        // Configure logging to stderr to avoid MCP STDIO conflicts
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole(options =>
        {
            // Route all log levels to stderr to avoid corrupting MCP stdio (stdout) channel
            options.LogToStandardErrorThreshold = LogLevel.Trace;
        });

        // Configure strongly-typed options
        builder.Services.Configure<ServerOptions>(
            builder.Configuration.GetSection(nameof(ServerOptions)));

        // Register knowledge base service with dependency injection
        builder.Services.AddSingleton<IKnowledgeBaseService, FileKnowledgeBaseService>();

        // Register MCP tools
        builder.Services.AddTransient<Tools.SearchKnowledgeTool>();
        builder.Services.AddTransient<Tools.GetKbInfoTool>();

        // TODO: Add MCP server configuration once SDK is available
        // builder.Services.AddMcpServer()
        //     .WithStdioServerTransport()
        //     .WithToolsFromAssembly();

        var app = builder.Build();

        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Starting KB MCP Server...");

        try
        {
            // Initialize knowledge base service
            var knowledgeBaseService = app.Services.GetRequiredService<IKnowledgeBaseService>();
            bool initialized = await knowledgeBaseService.InitializeAsync();
            
            if (!initialized)
            {
                logger.LogError("Failed to initialize knowledge base service");
                return;
            }
            
            logger.LogInformation("KB MCP Server started successfully");
            
            // Run the host
            await app.RunAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "KB MCP Server failed to start");
            throw;
        }
    }
}
