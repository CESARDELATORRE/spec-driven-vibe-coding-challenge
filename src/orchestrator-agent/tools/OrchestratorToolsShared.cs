using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;

namespace OrchestratorAgent.Tools;

/// <summary>
/// Shared utilities and data structures for orchestrator MCP tools.
/// Contains common helper methods, configuration utilities, and KB resolution logic.
/// This class provides NO MCP tool methods - only shared utilities.
/// </summary>
public static class OrchestratorToolsShared
{
    /// <summary>
    /// Standard JSON serialization with consistent indented formatting.
    /// </summary>
    public static string ToJson(object value) => JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = true });

    /// <summary>
    /// Creates consistent error response payload.
    /// </summary>
    public static string CreateError(string message, string code = "validation") => ToJson(new
    {
        error = new { message, code },
        status = "error",
        correlationId = Guid.NewGuid().ToString()
    });



    /// <summary>
    /// Builds the base configuration for the orchestrator.
    /// </summary>
    public static IConfiguration BuildBaseConfiguration() => new ConfigurationBuilder()
        .SetBasePath(AppContext.BaseDirectory)
        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
        .AddEnvironmentVariables()
        .Build();

    /// <summary>
    /// Result structure for KB executable resolution.
    /// </summary>
    public sealed record KbExecutableResolutionResult(
        bool Configured,
        bool Resolved,
        string? ResolvedPath,
        string? Command,
        string[] Arguments,
        IReadOnlyList<string> ProbedPaths);

    /// <summary>
    /// Resolves the KB executable path from configuration.
    /// </summary>
    public static KbExecutableResolutionResult ResolveKbExecutable(IConfiguration config)
    {
        string? raw = config["KbMcpServer:ExecutablePath"];
        if (string.IsNullOrWhiteSpace(raw))
        {
            return new(false, false, null, null, Array.Empty<string>(), Array.Empty<string>());
        }

        // Normalize surrounding quotes (common when values copied into dev.env with quotes)
        string value = raw.Trim();
        if ((value.StartsWith('"') && value.EndsWith('"')) || (value.StartsWith('\'') && value.EndsWith('\'')))
        {
            value = value.Substring(1, value.Length - 2).Trim();
        }

        var probed = new List<string>();
        try
        {
            static IEnumerable<string> EnumerateRepoRootCandidates(string startingDirectory)
            {
                var dir = new DirectoryInfo(startingDirectory);
                for (int i = 0; i < 8 && dir is not null; i++)
                {
                    bool marker = false;
                    try { marker = dir.EnumerateFiles("*.sln").Any() || dir.EnumerateDirectories(".git").Any(); } catch { }
                    if (marker) yield return dir.FullName;
                    dir = dir.Parent;
                }
            }

            var baseDir = AppContext.BaseDirectory;
            var candidateBases = new List<string>();
            if (Path.IsPathFullyQualified(value))
            {
                candidateBases.Add(value);
            }
            else
            {
                candidateBases.Add(Path.GetFullPath(Path.Combine(baseDir, value)));
                foreach (var repoRoot in EnumerateRepoRootCandidates(baseDir))
                {
                    var repoRelative = Path.GetFullPath(Path.Combine(repoRoot, value));
                    if (!candidateBases.Contains(repoRelative, StringComparer.OrdinalIgnoreCase))
                    {
                        candidateBases.Add(repoRelative);
                    }
                }
            }

            string? chosenCommand = null;
            string[] chosenArgs = Array.Empty<string>();
            string? resolvedPath = null;

            foreach (var resolvedBase in candidateBases)
            {
                probed.Add(resolvedBase);
                probed.Add(resolvedBase + ".exe");
                probed.Add(resolvedBase + ".dll");

                bool existsExact = File.Exists(resolvedBase);
                bool existsExe = File.Exists(resolvedBase + ".exe");
                bool existsDll = File.Exists(resolvedBase + ".dll");

                if (existsExact)
                {
                    chosenCommand = resolvedBase;
                    resolvedPath = resolvedBase;
                }
                else if (existsExe)
                {
                    chosenCommand = resolvedBase + ".exe";
                    resolvedPath = resolvedBase + ".exe";
                }
                else if (existsDll)
                {
                    chosenCommand = "dotnet";
                    chosenArgs = new[] { resolvedBase + ".dll" };
                    resolvedPath = resolvedBase + ".dll";
                }

                if (chosenCommand is not null) break;
            }

            bool resolved = resolvedPath is not null;
            return new(true, resolved, resolvedPath, chosenCommand, chosenArgs, probed);
        }
        catch
        {
            return new(true, false, null, null, Array.Empty<string>(), probed);
        }
    }



    /// <summary>
    /// Invokes the KB MCP server process to get knowledge base content.
    /// </summary>
    public static async Task<string> InvokeKbServerAsync(string command, string[] arguments, string requestPayload, int timeoutMs = 15000)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = command,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        foreach (var arg in arguments)
        {
            process.StartInfo.ArgumentList.Add(arg);
        }

        process.Start();

        // Send the request payload
        await process.StandardInput.WriteLineAsync(requestPayload);
        await process.StandardInput.FlushAsync();
        process.StandardInput.Close();

        // Read the response with timeout
        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();
        var completedTask = await Task.WhenAny(outputTask, Task.Delay(timeoutMs));

        if (completedTask == outputTask)
        {
            var output = await outputTask;
            var error = await errorTask;
            
            if (!string.IsNullOrWhiteSpace(error))
            {
                return $"KB Server Error: {error}";
            }
            
            return output;
        }
        else
        {
            process.Kill();
            return "KB Server timeout after 15 seconds";
        }
    }
}
