using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace IntegrationTests;

/// <summary>
/// Stdio MCP test client abstraction that:
/// 1. Launches the server (dotnet run) in a child process
/// 2. Frames JSON-RPC messages as newline-delimited JSON
/// 3. Reads lines until it finds the response with the matching id (skips notifications)
/// 4. Captures STDERR for debugging while keeping STDOUT protocol-clean
/// </summary>
internal sealed class StdioMcpClient : IAsyncDisposable
{
    private readonly Process _process;
    private readonly StreamWriter _stdin;
    private readonly StreamReader _stdout;
    private readonly CancellationTokenSource _lifecycleCts = new();
    private int _nextId = 1;

    private StdioMcpClient(Process process, StreamWriter stdin, StreamReader stdout)
    {
        _process = process;
        _stdin = stdin;
        _stdout = stdout;
    }

    /// <summary>
    /// Starts the MCP server using 'dotnet run --project <csprojPath>'.
    /// </summary>
    public static async Task<StdioMcpClient> StartAsync(string serverProjectCsprojPath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(serverProjectCsprojPath))
        {
            throw new FileNotFoundException("Server project file not found", serverProjectCsprojPath);
        }

        var projectDir = Path.GetDirectoryName(serverProjectCsprojPath)!;
        var projectName = Path.GetFileNameWithoutExtension(serverProjectCsprojPath);
        // Assume test build already compiled the server. Just locate existing exe or dll.
        string? exePath = Directory.GetFiles(projectDir, projectName + ".exe", SearchOption.AllDirectories)
            .FirstOrDefault(p => p.Contains(Path.Combine("bin","Debug"), StringComparison.OrdinalIgnoreCase) && p.Contains("net9.0"));
        string? dllPath = null;
        if (exePath == null)
        {
            dllPath = Directory.GetFiles(projectDir, projectName + ".dll", SearchOption.AllDirectories)
                .FirstOrDefault(p => p.Contains(Path.Combine("bin","Debug"), StringComparison.OrdinalIgnoreCase) && p.Contains("net9.0"));
            if (dllPath == null)
            {
                throw new FileNotFoundException("Could not locate built server binary (exe or dll)");
            }
        }

        ProcessStartInfo psi;
        if (exePath != null)
        {
            psi = new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = string.Empty,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(exePath)!
            };
        }
        else
        {
            psi = new ProcessStartInfo
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
        }

        var proc = new Process { StartInfo = psi, EnableRaisingEvents = true };
        if (!proc.Start())
        {
            throw new InvalidOperationException("Failed to start MCP server process");
        }

        // Asynchronously drain stderr so it doesn't block and surface logs for debugging.
        _ = Task.Run(async () =>
        {
            try
            {
                string? line;
                while ((line = await proc.StandardError.ReadLineAsync()) != null)
                {
                    Console.Error.WriteLine($"[SERVER STDERR] {line}");
                }
            }
            catch { /* ignore */ }
        }, cancellationToken);

        var stdin = new StreamWriter(proc.StandardInput.BaseStream, new UTF8Encoding(false)) { AutoFlush = true };
        var stdout = new StreamReader(proc.StandardOutput.BaseStream, Encoding.UTF8);

    // Allow warm-up; occasionally first request raced process startup in CI.
    await Task.Delay(1250, cancellationToken);

        return new StdioMcpClient(proc, stdin, stdout);
    }

    /// <summary>
    /// Sends an arbitrary request object (anonymous / strongly typed) and waits for a response with matching id.
    /// </summary>
    public async Task<string> SendRequestAsync(object request, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        var requestDict = JsonSerializer.Deserialize<Dictionary<string, object>>(
            JsonSerializer.Serialize(request, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }))
            ?? new Dictionary<string, object>();

        if (!requestDict.ContainsKey("id"))
        {
            requestDict["id"] = _nextId++;
        }

        var id = requestDict["id"];
        var json = JsonSerializer.Serialize(requestDict, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        Console.WriteLine($"=> {json}");
        await _stdin.WriteLineAsync(json);

        var effectiveTimeout = timeout ?? TimeSpan.FromSeconds(10);
        var deadline = DateTime.UtcNow + effectiveTimeout;
        Task<string?>? pendingRead = null; // ensure only one ReadLineAsync in-flight to avoid InvalidOperationException

        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (pendingRead == null)
            {
                pendingRead = _stdout.ReadLineAsync();
            }

            var slice = TimeSpan.FromMilliseconds(250);
            var winner = await Task.WhenAny(pendingRead, Task.Delay(slice, cancellationToken));
            if (winner != pendingRead)
            {
                // timeout slice elapsed; loop again keeping the same pending read alive
                continue;
            }

            // Read completed
            var line = await pendingRead; // may be null at EOF
            pendingRead = null; // allow next line read
            if (line == null)
            {
                // EOF: break early
                break;
            }

            Console.WriteLine($"<= {line}");
            try
            {
                using var doc = JsonDocument.Parse(line);
                if (doc.RootElement.TryGetProperty("id", out var idEl))
                {
                    if (idEl.ToString() == id!.ToString())
                    {
                        return line; // matched response
                    }
                }
            }
            catch
            {
                // Ignore malformed line
            }
        }

        throw new TimeoutException($"Timed out waiting for response with id {id}");
    }

    /// <summary>
    /// Convenience wrapper for tests doing an initialize then returning the raw response.
    /// </summary>
    public Task<string> InitializeAsync(string protocolVersion = "2024-11-05", CancellationToken ct = default) =>
        SendRequestAsync(new
        {
            jsonrpc = "2.0",
            method = "initialize",
            @params = new
            {
                protocolVersion,
                capabilities = new { },
                clientInfo = new { name = "integration-tests", version = "1.0" }
            }
        }, cancellationToken: ct);

    // (Removed ReadLineWithTimeoutAsync to avoid multiple concurrent ReadLineAsync calls which caused InvalidOperationException)

    public async ValueTask DisposeAsync()
    {
        try
        {
            _lifecycleCts.Cancel();
            _stdin?.Close();
            if (!_process.HasExited)
            {
                _process.Kill(entireProcessTree: true);
                await _process.WaitForExitAsync();
            }
        }
        catch { }
        finally
        {
            _stdin?.Dispose();
            _stdout?.Dispose();
            _process.Dispose();
            _lifecycleCts.Dispose();
        }
    }
}