using System.Text.Json;
using FluentAssertions;

namespace IntegrationTests;

/// <summary>
/// Black-box protocol-level integration tests using a simplified MCP stdio client.
/// </summary>
public class McpServerProtocolTests
{
    private static string ServerProjectPath
    {
        get
        {
            // Primary: relative to repository root when tests run from solution root
            var candidate = Path.Combine(Directory.GetCurrentDirectory(), "src", "mcp-server-kb-content-fetcher", "mcp-server-kb-content-fetcher.csproj");
            if (File.Exists(candidate)) return candidate;

            // Fallback: original relative (in case working dir is tests/ project folder)
            candidate = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "src", "mcp-server-kb-content-fetcher", "mcp-server-kb-content-fetcher.csproj"));
            if (File.Exists(candidate)) return candidate;

            // Fallback: based on AppContext.BaseDirectory (bin/Debug/netX)
            candidate = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../src/mcp-server-kb-content-fetcher/mcp-server-kb-content-fetcher.csproj"));
            if (File.Exists(candidate)) return candidate;

            throw new FileNotFoundException("Could not resolve server project path", candidate);
        }
    }

    [Fact(Timeout = 30000)]
    public async Task Initialize_Then_ListTools_Should_Discover_Expected_Tools()
    {
    var path = Path.GetFullPath(ServerProjectPath);
    Console.Error.WriteLine($"[TEST] Using server project path: {path}");
    path.Should().NotBeNull();
    File.Exists(path).Should().BeTrue("Server project file must exist for integration test to run");
    await using var client = await StdioMcpClient.StartAsync(path);

        var init = await client.SendRequestAsync(new
        {
            jsonrpc = "2.0",
            method = "initialize",
            @params = new { protocolVersion = "2024-11-05", capabilities = new { }, clientInfo = new { name = "integration-tests", version = "1.0" } }
        });

        init.Should().NotBeNullOrEmpty();
        using (var doc = JsonDocument.Parse(init))
        {
            doc.RootElement.TryGetProperty("result", out _).Should().BeTrue();
            doc.RootElement.TryGetProperty("error", out _).Should().BeFalse();
        }

        var toolsList = await client.SendRequestAsync(new { jsonrpc = "2.0", method = "tools/list", @params = new { } });
        toolsList.Should().NotBeNullOrEmpty();
        using (var doc = JsonDocument.Parse(toolsList))
        {
            doc.RootElement.TryGetProperty("result", out var result).Should().BeTrue();
            result.TryGetProperty("tools", out var tools).Should().BeTrue();
            tools.ValueKind.Should().Be(JsonValueKind.Array);
            tools.GetArrayLength().Should().BeGreaterThan(0);
            var names = tools.EnumerateArray().Select(t => t.GetProperty("name").GetString()).ToList();
            names.Should().Contain("get_kb_info");
            names.Should().Contain("search_knowledge");
        }
    }

    [Fact(Timeout = 30000)]
    public async Task GetKbInfo_Tool_Should_Return_Knowledge_Base_Status()
    {
    var path = Path.GetFullPath(ServerProjectPath);
    Console.Error.WriteLine($"[TEST] Using server project path: {path}");
    File.Exists(path).Should().BeTrue();
    await using var client = await StdioMcpClient.StartAsync(path);

        await client.SendRequestAsync(new
        {
            jsonrpc = "2.0",
            method = "initialize",
            @params = new { protocolVersion = "2024-11-05", capabilities = new { }, clientInfo = new { name = "integration-tests", version = "1.0" } }
        });

        var response = await client.SendRequestAsync(new
        {
            jsonrpc = "2.0",
            method = "tools/call",
            @params = new { name = "get_kb_info", arguments = new { } }
        });

        response.Should().NotBeNullOrEmpty();
        using var doc = JsonDocument.Parse(response);
        doc.RootElement.TryGetProperty("result", out var result).Should().BeTrue();
        result.TryGetProperty("content", out var content).Should().BeTrue();
        content.ValueKind.Should().Be(JsonValueKind.Array);
        content.GetArrayLength().Should().BeGreaterThan(0);
        var first = content[0];
        first.TryGetProperty("text", out var textEl).Should().BeTrue();
        var text = textEl.GetString();
        text.Should().NotBeNullOrEmpty();
        using var payload = JsonDocument.Parse(text!);
        payload.RootElement.TryGetProperty("status", out _).Should().BeTrue();
        payload.RootElement.TryGetProperty("info", out var info).Should().BeTrue();
        info.TryGetProperty("isAvailable", out _).Should().BeTrue();
        info.TryGetProperty("contentLength", out _).Should().BeTrue();
    }

    [Fact(Timeout = 30000)]
    public async Task SearchKnowledge_Tool_Should_Return_Pricing_Results()
    {
    var path = Path.GetFullPath(ServerProjectPath);
    Console.Error.WriteLine($"[TEST] Using server project path: {path}");
    File.Exists(path).Should().BeTrue();
    await using var client = await StdioMcpClient.StartAsync(path);

        await client.SendRequestAsync(new
        {
            jsonrpc = "2.0",
            method = "initialize",
            @params = new { protocolVersion = "2024-11-05", capabilities = new { }, clientInfo = new { name = "integration-tests", version = "1.0" } }
        });

        var response = await client.SendRequestAsync(new
        {
            jsonrpc = "2.0",
            method = "tools/call",
            @params = new { name = "search_knowledge", arguments = new { query = "pricing" } }
        });

        response.Should().NotBeNullOrEmpty();
        using var doc = JsonDocument.Parse(response);
        doc.RootElement.TryGetProperty("result", out var result).Should().BeTrue();
        result.TryGetProperty("content", out var content).Should().BeTrue();
        var array = content.EnumerateArray().ToArray();
        array.Length.Should().BeGreaterThan(0);
        var first = array[0];
        first.TryGetProperty("text", out var textEl).Should().BeTrue();
        var text = textEl.GetString();
        text.Should().NotBeNullOrEmpty();
        using var payload = JsonDocument.Parse(text!);
        payload.RootElement.TryGetProperty("query", out var queryEl).Should().BeTrue();
        queryEl.GetString().Should().Be("pricing");
        payload.RootElement.TryGetProperty("totalMatches", out var totalMatchesEl).Should().BeTrue();
        totalMatchesEl.GetInt32().Should().BeGreaterThan(0);
    }
}
