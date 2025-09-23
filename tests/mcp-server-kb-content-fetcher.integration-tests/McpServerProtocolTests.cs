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

        var init = await client.InitializeAsync();

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
            names.Should().Contain("get_kb_content");
        }
    }

    [Fact(Timeout = 30000)]
    public async Task GetKbInfo_Tool_Should_Return_Knowledge_Base_Status()
    {
        var path = Path.GetFullPath(ServerProjectPath);
        Console.Error.WriteLine($"[TEST] Using server project path: {path}");
        File.Exists(path).Should().BeTrue();
        await using var client = await StdioMcpClient.StartAsync(path);

        await client.InitializeAsync();

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
        content.GetArrayLength().Should().Be(1);
        var outerTextElement = content[0].GetProperty("text").GetString();
        outerTextElement.Should().NotBeNullOrEmpty();
        // Some tool implementations (current state) double-serialize an array of text items; attempt to detect and unwrap
        JsonDocument? innerDoc = null;
        try { innerDoc = JsonDocument.Parse(outerTextElement!); } catch { }
        innerDoc.Should().NotBeNull("Outer text should be valid JSON");
        var innerRoot = innerDoc!.RootElement;
        if (innerRoot.ValueKind == JsonValueKind.Array && innerRoot.GetArrayLength() > 0)
        {
            // Take first element's text as actual payload
            var nested = innerRoot[0];
            nested.TryGetProperty("text", out var nestedTextEl).Should().BeTrue();
            var nestedText = nestedTextEl.GetString();
            nestedText.Should().NotBeNullOrEmpty();
            using var payload = JsonDocument.Parse(nestedText!);
            payload.RootElement.TryGetProperty("status", out _).Should().BeTrue();
            payload.RootElement.TryGetProperty("info", out var info).Should().BeTrue();
            info.TryGetProperty("isAvailable", out _).Should().BeTrue();
            info.TryGetProperty("contentLength", out _).Should().BeTrue();
        }
        else if (innerRoot.ValueKind == JsonValueKind.Object)
        {
            innerRoot.TryGetProperty("status", out _).Should().BeTrue();
            innerRoot.TryGetProperty("info", out var info).Should().BeTrue();
            info.TryGetProperty("isAvailable", out _).Should().BeTrue();
            info.TryGetProperty("contentLength", out _).Should().BeTrue();
        }
    }

    // Removed excerpt prototype tool test (search_knowledge) after deprecation.

    [Fact(Timeout = 30000)]
    public async Task GetKbContent_Tool_Should_Return_Full_Content()
    {
        var path = Path.GetFullPath(ServerProjectPath);
        Console.Error.WriteLine($"[TEST] Using server project path: {path}");
        File.Exists(path).Should().BeTrue();
        await using var client = await StdioMcpClient.StartAsync(path);

        await client.InitializeAsync();

        var response = await client.SendRequestAsync(new
        {
            jsonrpc = "2.0",
            method = "tools/call",
            @params = new { name = "get_kb_content", arguments = new { } }
        });

        response.Should().NotBeNullOrEmpty();
        using var doc = JsonDocument.Parse(response);
        doc.RootElement.TryGetProperty("result", out var result).Should().BeTrue();
        result.TryGetProperty("content", out var content).Should().BeTrue();
        var text = content[0].GetProperty("text").GetString();
        text.Should().NotBeNullOrEmpty();
        using var payload = JsonDocument.Parse(text!);
        payload.RootElement.TryGetProperty("status", out var statusEl).Should().BeTrue();
        payload.RootElement.TryGetProperty("contentLength", out var lenEl).Should().BeTrue();
        lenEl.GetInt32().Should().BeGreaterThan(0);
        payload.RootElement.TryGetProperty("content", out var rawEl).Should().BeTrue();
        rawEl.GetString().Should().Contain("Azure Managed Grafana");
    }
}
