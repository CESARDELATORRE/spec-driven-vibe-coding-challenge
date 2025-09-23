using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System.Text.Json;

namespace OrchestratorIntegrationTests;

/// <summary>
/// Lightweight stdio MCP client for orchestrator integration tests (mirrors KB test harness with minor tweaks).
/// </summary>
internal sealed class StdioMcpClient : IAsyncDisposable
{
    private readonly Process _process;
    private readonly StreamWriter _stdin;
    private readonly StreamReader _stdout;
    private int _nextId = 1;

    private StdioMcpClient(Process process, StreamWriter stdin, StreamReader stdout)
    {
        _process = process;
        _stdin = stdin;
        _stdout = stdout;
    }

    public static async Task<StdioMcpClient> StartAsync(string serverProjectCsprojPath, CancellationToken ct = default, IDictionary<string, string>? extraEnvironment = null)
    {
        if (!File.Exists(serverProjectCsprojPath))
        {
            throw new FileNotFoundException("Server project file not found", serverProjectCsprojPath);
        }

        var projectDir = Path.GetDirectoryName(serverProjectCsprojPath)!;
        var projectName = Path.GetFileNameWithoutExtension(serverProjectCsprojPath);

        string? dllPath = Directory.GetFiles(projectDir, projectName + ".dll", SearchOption.AllDirectories)
            .FirstOrDefault(p => p.Contains(Path.Combine("bin", "Debug"), StringComparison.OrdinalIgnoreCase) && p.Contains("net9.0"));
        if (dllPath is null)
        {
            throw new FileNotFoundException("Could not locate built orchestrator assembly (.dll). Build project before tests.");
        }

        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"\"{dllPath}\"",
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = Path.GetDirectoryName(dllPath)!
        };

        if (extraEnvironment != null)
        {
            foreach (var kvp in extraEnvironment)
            {
                psi.Environment[kvp.Key] = kvp.Value;
            }
        }

        var proc = new Process { StartInfo = psi };
        if (!proc.Start()) throw new InvalidOperationException("Failed to start orchestrator MCP server");

        _ = Task.Run(async () =>
        {
            try
            {
                string? line;
                while ((line = await proc.StandardError.ReadLineAsync()) != null)
                {
                    Console.Error.WriteLine($"[ORCH STDERR] {line}");
                }
            }
            catch { }
        }, ct);

        var stdin = new StreamWriter(proc.StandardInput.BaseStream, new UTF8Encoding(false)) { AutoFlush = true };
        var stdout = new StreamReader(proc.StandardOutput.BaseStream, Encoding.UTF8);

        await Task.Delay(1000, ct); // warm-up
        return new StdioMcpClient(proc, stdin, stdout);
    }

    public async Task<string> SendRequestAsync(object request, TimeSpan? timeout = null, CancellationToken ct = default)
    {
        var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(
            JsonSerializer.Serialize(request, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }))
            ?? new();
        if (!dict.ContainsKey("id")) dict["id"] = _nextId++;
        var id = dict["id"];
        var json = JsonSerializer.Serialize(dict, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        Console.WriteLine($"=> {json}");
        await _stdin.WriteLineAsync(json);

        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(10));
        Task<string?>? pending = null;
        while (DateTime.UtcNow < deadline)
        {
            if (pending == null) pending = _stdout.ReadLineAsync();
            var slice = Task.Delay(250, ct);
            var winner = await Task.WhenAny(pending, slice);
            if (winner != pending) continue;
            var line = await pending; pending = null;
            if (line == null) break; // EOF
            Console.WriteLine($"<= {line}");
            try
            {
                using var doc = JsonDocument.Parse(line);
                if (doc.RootElement.TryGetProperty("id", out var idEl) && idEl.ToString() == id!.ToString())
                {
                    return line;
                }
            }
            catch { }
        }
        throw new TimeoutException($"Timed out waiting for response id {id}");
    }

    public Task<string> InitializeAsync(string protocolVersion = "2024-11-05", CancellationToken ct = default) =>
        SendRequestAsync(new
        {
            jsonrpc = "2.0",
            method = "initialize",
            @params = new { protocolVersion, capabilities = new { }, clientInfo = new { name = "orch-int-tests", version = "1.0" } }
        }, ct: ct);

    public async ValueTask DisposeAsync()
    {
        try
        {
            _stdin.Close();
            if (!_process.HasExited)
            {
                _process.Kill(entireProcessTree: true);
                await _process.WaitForExitAsync();
            }
        }
        catch { }
        finally
        {
            _stdin.Dispose();
            _stdout.Dispose();
            _process.Dispose();
        }
    }
}
