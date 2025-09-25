using System.ComponentModel;
using ModelContextProtocol.Server;

namespace OrchestratorAgent.Tools;

/// <summary>
/// MCP tool for retrieving orchestrator service health and operational metrics.
/// Provides basic status information including environment, configuration state, and KB connectivity.
/// </summary>
[McpServerToolType]
public static class GetOrchestratorStatusTool
{
    /// <summary>
    /// Get orchestrator service status and operational metrics.
    /// Returns health status, environment info, and KB connectivity state.
    /// </summary>
    [McpServerTool]
    [Description("Get orchestrator service status and operational metrics")]
    public static string GetOrchestratorStatus()
    {
        try
        {
            var config = OrchestratorToolsShared.BuildBaseConfiguration();
            var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";
            bool fakeLlmMode = string.Equals(config["Orchestrator:UseFakeLlm"], "true", StringComparison.OrdinalIgnoreCase);
            var kbResolution = OrchestratorToolsShared.ResolveKbExecutable(config);

            var payload = new
            {
                status = "Alive",
                environment,
                fakeLlmMode,
                kbExecutableConfigured = kbResolution.Configured,
                kbExecutableResolved = kbResolution.Resolved,
                timestampUtc = DateTime.UtcNow.ToString("o")
            };

            return OrchestratorToolsShared.ToJson(payload);
        }
        catch (Exception ex)
        {
            return OrchestratorToolsShared.CreateError($"Status check failed: {ex.Message}");
        }
    }
}
