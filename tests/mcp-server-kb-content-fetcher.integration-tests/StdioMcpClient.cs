using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace IntegrationTests;

/// <summary>
/// Simplified stdio MCP client for integration testing.
/// Uses newline-delimited JSON for reliable message framing.
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

    /// <summary>
    /// Starts the MCP server process and returns a connected client.
    /// </summary>
    public static async Task<StdioMcpClient> StartAsync(string serverDllPath, CancellationToken cancellationToken = default)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{serverDllPath}\"",
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        var proc = new Process { StartInfo = psi };
        
        if (!proc.Start())
        {
            throw new InvalidOperationException("Failed to start MCP server process");
        }

        // Capture stderr for debugging
        _ = Task.Run(async () =>
        {
            var stderr = proc.StandardError;
            string? line;
            while ((line = await stderr.ReadLineAsync()) != null)
            {
                Console.WriteLine($"[SERVER STDERR] {line}");
            }
        });

        var stdin = new StreamWriter(proc.StandardInput.BaseStream, Encoding.UTF8) { AutoFlush = true };
        var stdout = new StreamReader(proc.StandardOutput.BaseStream, Encoding.UTF8);

        // Give server time to start
        await Task.Delay(1000, cancellationToken);

        return new StdioMcpClient(proc, stdin, stdout);
    }

    /// <summary>
    /// Sends a JSON-RPC request and returns the response.
    /// Uses newline-delimited JSON format.
    /// </summary>
    public async Task<string> SendRequestAsync(object request, CancellationToken cancellationToken = default)
    {
        // Ensure request has an ID
        var requestDict = JsonSerializer.Deserialize<Dictionary<string, object>>(
            JsonSerializer.Serialize(request, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
        
        if (requestDict != null && !requestDict.ContainsKey("id"))
        {
            requestDict["id"] = _nextId++;
        }

        var json = JsonSerializer.Serialize(requestDict, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        
        Console.WriteLine($"=> {json}");

        // Send request (newline-delimited)
        await _stdin.WriteLineAsync(json);

        // Read response (newline-delimited)
        var response = await _stdout.ReadLineAsync();
        if (response == null)
        {
            throw new InvalidOperationException("Server closed connection unexpectedly");
        }

        Console.WriteLine($"<= {response}");
        return response;
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            _stdin?.Close();
            
            if (!_process.HasExited)
            {
                _process.Kill(entireProcessTree: true);
                await _process.WaitForExitAsync();
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
        finally
        {
            _stdin?.Dispose();
            _stdout?.Dispose();
            _process?.Dispose();
        }
    }
}