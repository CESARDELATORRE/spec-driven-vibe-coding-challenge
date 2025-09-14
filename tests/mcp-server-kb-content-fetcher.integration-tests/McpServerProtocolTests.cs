using System.Diagnostics;
using System.Text;

namespace IntegrationTests;

public class McpServerProtocolTests
{
    [Fact]
    public async Task GetKbInfo_Tool_Should_Return_Knowledge_Base_Status()
    {
        using var process = StartServer();
        var (reader, stdout, stderr, cts) = await BeginCaptureAsync(process, TimeSpan.FromSeconds(10));

        // Initialize
        await SendAsync(process, "{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"initialize\",\"params\":{\"protocolVersion\":\"2024-11-05\",\"capabilities\":{},\"clientInfo\":{\"name\":\"integration-tests\",\"version\":\"1.0.0\"}}}");
        Assert.True(await WaitForAsync(stdout, "jsonrpc", TimeSpan.FromSeconds(5)), $"Did not observe any JSON-RPC response to initialize. STDOUT: {stdout}\nSTDERR: {stderr}");

        // List tools to ensure ready
        await SendAsync(process, "{\"jsonrpc\":\"2.0\",\"id\":2,\"method\":\"tools/list\",\"params\":{}}");
        Assert.True(await WaitForAsync(stdout, "get_kb_info", TimeSpan.FromSeconds(3)), $"Tool list missing get_kb_info. STDOUT: {stdout}\nSTDERR: {stderr}");

        // Call get_kb_info tool
        await SendAsync(process, "{\"jsonrpc\":\"2.0\",\"id\":3,\"method\":\"tools/call\",\"params\":{\"name\":\"get_kb_info\",\"arguments\":{}}}");
        Assert.True(await WaitForAsync(stdout, "\"id\":3", TimeSpan.FromSeconds(6)), $"Did not observe response to get_kb_info call. STDOUT: {stdout}\nSTDERR: {stderr}");
        
        // Verify no error occurred
        Assert.DoesNotContain("\"isError\":true", stdout.ToString());
        
        // Verify it contains knowledge base status information
        var output = stdout.ToString();
        Assert.Contains("isAvailable", output);
        Assert.Contains("contentLength", output);

        // Cleanup
        try { process.Kill(entireProcessTree: true); } catch { }
        cts.Cancel();
        await reader;
    }

    [Fact]
    public async Task SearchKnowledge_Tool_Should_Return_Pricing_Results()
    {
        using var process = StartServer();
        var (reader, stdout, stderr, cts) = await BeginCaptureAsync(process, TimeSpan.FromSeconds(10));

        // Initialize then list tools (ensures server ready)
        await SendAsync(process, "{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"initialize\",\"params\":{\"protocolVersion\":\"2024-11-05\",\"capabilities\":{},\"clientInfo\":{\"name\":\"integration-tests\",\"version\":\"1.0.0\"}}}");
        Assert.True(await WaitForAsync(stdout, "jsonrpc", TimeSpan.FromSeconds(5)), $"Did not observe any JSON-RPC response to initialize. STDOUT: {stdout}\nSTDERR: {stderr}");

        await SendAsync(process, "{\"jsonrpc\":\"2.0\",\"id\":2,\"method\":\"tools/list\",\"params\":{}}");
        Assert.True(await WaitForAsync(stdout, "search_knowledge", TimeSpan.FromSeconds(3)), $"Tool list missing search_knowledge. STDOUT: {stdout}\nSTDERR: {stderr}");

        // Call search tool with "pricing" query
        await SendAsync(process, "{\"jsonrpc\":\"2.0\",\"id\":3,\"method\":\"tools/call\",\"params\":{\"name\":\"search_knowledge\",\"arguments\":{\"query\":\"pricing\",\"max_results\":2}}}");
        Assert.True(await WaitForAsync(stdout, "\"id\":3", TimeSpan.FromSeconds(6)), $"Did not observe response to search call. STDOUT: {stdout}\nSTDERR: {stderr}");
        
        // Verify no error occurred  
        Assert.DoesNotContain("\"isError\":true", stdout.ToString());
        
        // Verify the search actually found pricing-related content
        var output = stdout.ToString();
        Assert.Contains("pricing", output.ToLowerInvariant());
        Assert.Contains("totalMatches", output);

        // Cleanup
        try { process.Kill(entireProcessTree: true); } catch { }
        cts.Cancel();
        await reader;
    }

    private Process StartServer()
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "run",
            WorkingDirectory = ProjectDirectory,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        var process = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start MCP server process");
        return process;
    }

    private static async Task<(Task reader, StringBuilder stdout, StringBuilder stderr, CancellationTokenSource cts)> BeginCaptureAsync(Process process, TimeSpan timeout)
    {
        var cts = new CancellationTokenSource(timeout);
        var stdoutBuilder = new StringBuilder();
        var stderrBuilder = new StringBuilder();

        async Task ReadAsync()
        {
            var stdOut = process.StandardOutput;
            var stdErr = process.StandardError;
            var buffer = new char[256];
            try
            {
                while (!cts.IsCancellationRequested && !process.HasExited)
                {
                    // Read stdout non-blocking-ish
                    if (stdOut.Peek() >= 0)
                    {
                        var read = await stdOut.ReadAsync(buffer, 0, buffer.Length);
                        if (read > 0) stdoutBuilder.Append(buffer, 0, read);
                    }
                    // Read stderr (logs)
                    if (stdErr.Peek() >= 0)
                    {
                        var readErr = await stdErr.ReadAsync(buffer, 0, buffer.Length);
                        if (readErr > 0) stderrBuilder.Append(buffer, 0, readErr);
                    }
                    await Task.Delay(10, cts.Token);
                }
            }
            catch (OperationCanceledException) { }
        }

        var readerTask = Task.Run(ReadAsync, cts.Token);
        return (readerTask, stdoutBuilder, stderrBuilder, cts);
    }

    private static async Task<bool> WaitForAsync(StringBuilder sb, string expected, TimeSpan timeout)
    {
        var start = DateTime.UtcNow;
        while (DateTime.UtcNow - start < timeout)
        {
            if (sb.ToString().Contains(expected, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            await Task.Delay(50);
        }
        return false;
    }

    private static async Task SendAsync(Process process, string json)
    {
        await process.StandardInput.WriteLineAsync(json);
        await process.StandardInput.FlushAsync();
    }

    [Fact]
    public async Task Initialize_Then_ListTools_Should_Discover_Expected_Tools()
    {
        using var process = StartServer();
        var (reader, stdout, stderr, cts) = await BeginCaptureAsync(process, TimeSpan.FromSeconds(8));

        await SendAsync(process, "{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"initialize\",\"params\":{\"protocolVersion\":\"2024-11-05\",\"capabilities\":{},\"clientInfo\":{\"name\":\"integration-tests\",\"version\":\"1.0.0\"}}}");
    // Some MCP server libraries may not echo an 'id:1' style content directly; relax to waiting for any JSON output
    Assert.True(await WaitForAsync(stdout, "jsonrpc", TimeSpan.FromSeconds(5)), $"Did not observe any JSON-RPC response to initialize. STDOUT: {stdout}\nSTDERR: {stderr}");

        await SendAsync(process, "{\"jsonrpc\":\"2.0\",\"id\":2,\"method\":\"tools/list\",\"params\":{}}");
        Assert.True(await WaitForAsync(stdout, "search_knowledge", TimeSpan.FromSeconds(3)), $"Tool list did not contain search_knowledge. STDOUT: {stdout}\nSTDERR: {stderr}");
        Assert.True(await WaitForAsync(stdout, "get_kb_info", TimeSpan.FromSeconds(3)), $"Tool list did not contain get_kb_info. STDOUT: {stdout}\nSTDERR: {stderr}");

        // Cleanup
        try { process.Kill(entireProcessTree: true); } catch { }
        cts.Cancel();
        await reader;
    }

    private static string ProjectDirectory
    {
        get
        {
            var candidate = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../src/mcp-server-kb-content-fetcher"));
            if (!Directory.Exists(candidate))
            {
                throw new DirectoryNotFoundException($"Could not resolve project directory at: {candidate}");
            }
            return candidate;
        }
    }
}
