using System.ComponentModel;
using ModelContextProtocol.Server;

namespace OrchestratorAgent.Tools;

/// <summary>
/// MCP tool for retrieving detailed orchestrator diagnostics and configuration information.
/// Provides comprehensive system information, environment variables, and troubleshooting data.
/// </summary>
[McpServerToolType]
public static class GetOrchestratorDiagnosticsTool
{
    /// <summary>
    /// Get detailed orchestrator diagnostics and configuration information.
    /// Returns comprehensive system state, environment variables, and KB path resolution details.
    /// </summary>
    [McpServerTool]
    [Description("Get detailed orchestrator diagnostics and configuration information")]
    public static string GetOrchestratorDiagnosticsInformation()
    {
        try
        {
            var config = OrchestratorToolsShared.BuildBaseConfiguration();
            var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";
            bool fakeLlmMode = string.Equals(config["Orchestrator:UseFakeLlm"], "true", StringComparison.OrdinalIgnoreCase);
            var kbResolution = OrchestratorToolsShared.ResolveKbExecutable(config);

            // Only include environment variables relevant to MCP servers (as per dev.env.example)
            string[] relevantNames = new[]
            {
                "AzureOpenAI__Endpoint",
                "AzureOpenAI__DeploymentName", 
                "AzureOpenAI__ApiKey",
                "Orchestrator__UseFakeLlm",
                "KbMcpServer__ExecutablePath"
            };

            var ordered = relevantNames
                .Select(n => new { name = n, value = Environment.GetEnvironmentVariable(n) })
                .Where(p => p.value is not null)
                .OrderBy(p => p.name, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            var payload = new
            {
                status = "Alive",
                environment,
                fakeLlmMode,
                kbExecutableConfigured = kbResolution.Configured,
                kbExecutableResolved = kbResolution.Resolved,
                kbResolvedPath = kbResolution.ResolvedPath is null ? null : Path.GetFileName(kbResolution.ResolvedPath),
                orchestratorExecutablePath = Environment.ProcessPath,
                orchestratorAssemblyLocation = typeof(GetOrchestratorDiagnosticsTool).Assembly.Location,
                timestampUtc = DateTime.UtcNow.ToString("o"),
                kbProbedSample = kbResolution.ProbedPaths.Count == 0 ? Array.Empty<string>() : kbResolution.ProbedPaths.Take(6).ToArray(),
                environmentVariables = ordered
            };

            return OrchestratorToolsShared.ToJson(payload);
        }
        catch (Exception ex)
        {
            return OrchestratorToolsShared.CreateError($"Diagnostics failed: {ex.Message}");
        }
    }
}
