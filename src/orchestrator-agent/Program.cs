using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;

// Program.cs sets up the MCP server for the Orchestrator Agent (prototype scope)
// STEP 6: Establish configuration layering & stderr logging ready for Step 7's appsettings.json.

var builder = Host.CreateEmptyApplicationBuilder(settings: null);

// Configuration layering:
// 1. (Future) appsettings.json (added in Step 7) -> keep optional for now so build succeeds without file
// 2. Environment variables (primary portable contract)
// 3. User Secrets ONLY in Development for local convenience
builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true) // will become non-optional once added in Step 7
    .AddEnvironmentVariables();

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>(optional: true);
}

// Logging: route ALL log levels to stderr to avoid corrupting MCP STDIO protocol on stdout.
builder.Logging.ClearProviders();
builder.Logging.AddConsole(o =>
{
    o.LogToStandardErrorThreshold = LogLevel.Trace; // send everything to stderr
});
builder.Logging.SetMinimumLevel(LogLevel.Information);

// MCP server registration with tools discovered via reflection.
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

var host = builder.Build();
await host.RunAsync();
