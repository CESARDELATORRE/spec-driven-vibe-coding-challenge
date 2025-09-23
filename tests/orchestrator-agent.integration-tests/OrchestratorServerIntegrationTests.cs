using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using FluentAssertions;

namespace OrchestratorIntegrationTests;

public class OrchestratorServerIntegrationTests
{
    private static string ServerProjectPath
    {
        get
        {
            var candidate = Path.Combine(Directory.GetCurrentDirectory(), "src", "orchestrator-agent", "orchestrator-agent.csproj");
            if (File.Exists(candidate)) return candidate;
            candidate = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "src", "orchestrator-agent", "orchestrator-agent.csproj"));
            if (File.Exists(candidate)) return candidate;
            candidate = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../src/orchestrator-agent/orchestrator-agent.csproj"));
            if (File.Exists(candidate)) return candidate;
            throw new FileNotFoundException("Could not resolve orchestrator server project path");
        }
    }

    [Fact(Timeout = 30000)]
    public async Task Initialize_Then_ListTools_Should_Contain_AskDomainQuestion()
    {
        var path = Path.GetFullPath(ServerProjectPath);
        Console.Error.WriteLine($"[TEST] Using orchestrator project path: {path}");
        File.Exists(path).Should().BeTrue();
        await using var client = await StdioMcpClient.StartAsync(path);

        var init = await client.InitializeAsync();
        using (var doc = JsonDocument.Parse(init))
        {
            doc.RootElement.TryGetProperty("result", out _).Should().BeTrue();
            doc.RootElement.TryGetProperty("error", out _).Should().BeFalse();
        }

        var toolsList = await client.SendRequestAsync(new { jsonrpc = "2.0", method = "tools/list", @params = new { } });
        using var listDoc = JsonDocument.Parse(toolsList);
        var tools = listDoc.RootElement.GetProperty("result").GetProperty("tools");
        tools.EnumerateArray().Select(t => t.GetProperty("name").GetString()).Should().Contain(new[] { "ask_domain_question", "get_orchestrator_status" });
    }

    [Fact(Timeout = 30000)]
    public async Task AskDomainQuestion_Should_Return_Scaffold_Status_And_Disclaimers_When_No_LLM_Config()
    {
        // Ensure no Azure OpenAI env vars leak into this test environment
        Environment.SetEnvironmentVariable("AzureOpenAI__Endpoint", null);
        Environment.SetEnvironmentVariable("AzureOpenAI__DeploymentName", null);
        Environment.SetEnvironmentVariable("AzureOpenAI__ApiKey", null);

        var path = Path.GetFullPath(ServerProjectPath);
        await using var client = await StdioMcpClient.StartAsync(path);
        await client.InitializeAsync();

        var response = await client.SendRequestAsync(new
        {
            jsonrpc = "2.0",
            method = "tools/call",
            @params = new { name = "ask_domain_question", arguments = new { question = "hello orchestrator agent", includeKb = true, maxKbResults = 3 } }
        });

        using var doc = JsonDocument.Parse(response);
        doc.RootElement.TryGetProperty("result", out var resultEl).Should().BeTrue();
        var contentArray = resultEl.GetProperty("content");
        contentArray.GetArrayLength().Should().Be(1);
        var textPayload = contentArray[0].GetProperty("text").GetString();
        textPayload.Should().NotBeNullOrEmpty();

        using var inner = JsonDocument.Parse(textPayload!);
        var root = inner.RootElement;
        root.TryGetProperty("status", out var statusEl).Should().BeTrue();
        statusEl.GetString().Should().Be("scaffold");
        root.TryGetProperty("disclaimers", out var disclaimersEl).Should().BeTrue();
        disclaimersEl.EnumerateArray().Any(e => e.GetString()!.Contains("Missing Azure OpenAI", StringComparison.OrdinalIgnoreCase)).Should().BeTrue();
        root.TryGetProperty("diagnostics", out var diagEl).Should().BeTrue();
        diagEl.TryGetProperty("heuristicSkipKb", out _).Should().BeTrue();
    }
}
