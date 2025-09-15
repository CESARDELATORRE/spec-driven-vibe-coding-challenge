using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;

namespace OrchestratorIntegrationTests;

/// <summary>
/// Positive-path test exercising simulated LLM success via Orchestrator__UseFakeLlm=true.
/// This avoids external Azure OpenAI dependency while validating the code path that
/// would normally create a kernel and produce an answer.
/// </summary>
public class OrchestratorServerPositiveLlmIntegrationTests
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
    public async Task AskDomainQuestion_Should_Produce_Fake_LLM_Answer_When_Fake_Mode_Enabled()
    {
        var path = Path.GetFullPath(ServerProjectPath);
        var env = new System.Collections.Generic.Dictionary<string,string>
        {
            ["AzureOpenAI__Endpoint"] = "https://fake-endpoint.example.com/",
            ["AzureOpenAI__DeploymentName"] = "gpt-fake-test",
            ["AzureOpenAI__ApiKey"] = "FAKE_KEY_VALUE",
            ["Orchestrator__UseFakeLlm"] = "true"
        };
        await using var client = await StdioMcpClient.StartAsync(path, extraEnvironment: env);
        await client.InitializeAsync();

        // Act
        var response = await client.SendRequestAsync(new {
            jsonrpc = "2.0",
            method = "tools/call",
            @params = new { name = "ask_domain_question", arguments = new { question = "Explain the configuration layering", includeKb = false, maxKbResults = 2 } }
        });

        // Assert
        using var outer = JsonDocument.Parse(response);
        var result = outer.RootElement.GetProperty("result");
        var content = result.GetProperty("content");
        content.GetArrayLength().Should().Be(1);
        var text = content[0].GetProperty("text").GetString();
        text.Should().NotBeNull();
        using var inner = JsonDocument.Parse(text!);
    inner.RootElement.GetProperty("answer").GetString()!.Should().StartWith("FAKE_LLM_ANSWER:");
        var diags = inner.RootElement.GetProperty("diagnostics");
        diags.GetProperty("fakeLlmMode").GetBoolean().Should().BeTrue();
        diags.GetProperty("chatAgentReady").GetBoolean().Should().BeTrue();
        inner.RootElement.GetProperty("disclaimers").EnumerateArray()
            .Any(e => e.GetString()!.Contains("Simulated LLM answer", StringComparison.OrdinalIgnoreCase))
            .Should().BeTrue();
    }
}
