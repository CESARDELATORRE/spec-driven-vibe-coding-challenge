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

        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{serverProjectCsprojPath}\"",
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = Path.GetDirectoryName(serverProjectCsprojPath)
        };

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
        var start = DateTime.UtcNow;
        while (DateTime.UtcNow - start < effectiveTimeout)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var line = await ReadLineWithTimeoutAsync(_stdout, TimeSpan.FromMilliseconds(250), cancellationToken);
            if (line == null) continue; // keep waiting

            Console.WriteLine($"<= {line}");
            try
            {
                using var doc = JsonDocument.Parse(line);
                if (doc.RootElement.TryGetProperty("id", out var idEl))
                {
                    // Match numeric or string id
                    if (idEl.ToString() == id!.ToString())
                    {
                        return line; // Found our response
                    }
                }
                // Otherwise it's likely a notification; continue loop.
            }
            catch
            {
                // Non JSON line (should not happen on stdout), ignore.
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

    private static async Task<string?> ReadLineWithTimeoutAsync(StreamReader reader, TimeSpan timeout, CancellationToken ct)
    {
        // ReadLineAsync already returns a Task<string?> in this target framework; no need for AsTask()
        var task = reader.ReadLineAsync();
        var completed = await Task.WhenAny(task, Task.Delay(timeout, ct));
        if (completed == task)
        {
            return await task; // may be null (EOF)
        }
        return null; // timed slice expired; caller decides to continue / loop
    }

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