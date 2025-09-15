using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace UnitTests.OrchestratorAgent;

public class OrchestratorToolsTests
{
    [Fact]
    public async Task AskDomainQuestionAsync_Empty_ReturnsValidationError()
    {
        var json = await OrchestratorTools.AskDomainQuestionAsync("");
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("status").GetString().Should().Be("error");
        root.GetProperty("error").GetProperty("code").GetString().Should().Be("validation");
        root.GetProperty("error").GetProperty("message").GetString().Should().Contain("required");
        root.GetProperty("correlationId").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task AskDomainQuestionAsync_PunctuationOnly_ReturnsValidationError()
    {
        var json = await OrchestratorTools.AskDomainQuestionAsync("!!!??");
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("status").GetString().Should().Be("error");
    }

    [Fact]
    public async Task AskDomainQuestionAsync_MaxKbResults_ClampedHigh()
    {
        var json = await OrchestratorTools.AskDomainQuestionAsync("Explain orchestrator architecture design decisions", includeKb: true, maxKbResults: 99);
        using var doc = JsonDocument.Parse(json);
        var diag = doc.RootElement.GetProperty("diagnostics");
        diag.GetProperty("requestedMaxKbResults").GetInt32().Should().Be(99);
        diag.GetProperty("effectiveMaxKbResults").GetInt32().Should().Be(3);
        diag.GetProperty("kbResultsClamped").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task AskDomainQuestionAsync_MaxKbResults_ClampedLow()
    {
        var json = await OrchestratorTools.AskDomainQuestionAsync("Explain orchestrator architecture design decisions", includeKb: true, maxKbResults: 0);
        using var doc = JsonDocument.Parse(json);
        var diag = doc.RootElement.GetProperty("diagnostics");
        diag.GetProperty("requestedMaxKbResults").GetInt32().Should().Be(0);
        diag.GetProperty("effectiveMaxKbResults").GetInt32().Should().Be(1);
        diag.GetProperty("kbResultsClamped").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task AskDomainQuestionAsync_Greeting_UsesHeuristic()
    {
        var json = await OrchestratorTools.AskDomainQuestionAsync("hello orchestrator agent, tell me something", includeKb: true);
        using var doc = JsonDocument.Parse(json);
        var disclaimers = doc.RootElement.GetProperty("disclaimers");
        disclaimers.EnumerateArray().Any(e => e.GetString()!.Contains("greeting heuristic")).Should().BeTrue();
    }

    [Fact]
    public async Task AskDomainQuestionAsync_SetsCorrelationIdOnValidationError()
    {
        var json = await OrchestratorTools.AskDomainQuestionAsync("bad"); // too short (<5)
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("status").GetString().Should().Be("error");
        var correlationId = root.GetProperty("correlationId").GetString();
        correlationId.Should().NotBeNullOrWhiteSpace();
        Regex.IsMatch(correlationId!, "^[a-f0-9-]{36}$", RegexOptions.IgnoreCase).Should().BeTrue();
    }

    [Fact]
    public async Task AskDomainQuestionAsync_FakeLlmMode_ProducesDeterministicAnswerAndDiagnostics()
    {
        // Arrange: set env vars to satisfy config + enable fake mode
        Environment.SetEnvironmentVariable("AzureOpenAI__Endpoint", "https://fake-endpoint.example.com/");
        Environment.SetEnvironmentVariable("AzureOpenAI__DeploymentName", "gpt-fake");
        Environment.SetEnvironmentVariable("AzureOpenAI__ApiKey", "FAKE_KEY_VALUE");
        Environment.SetEnvironmentVariable("Orchestrator__UseFakeLlm", "true");

        var json = await OrchestratorTools.AskDomainQuestionAsync("Explain configuration layering strategy", includeKb: false, maxKbResults: 2);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("status").GetString().Should().Be("scaffold");
        var answer = root.GetProperty("answer").GetString();
        answer.Should().StartWith("FAKE_LLM_ANSWER:");
        var diags = root.GetProperty("diagnostics");
        diags.GetProperty("fakeLlmMode").GetBoolean().Should().BeTrue();
        diags.GetProperty("chatAgentReady").GetBoolean().Should().BeTrue();
        diags.GetProperty("endpointConfigured").GetBoolean().Should().BeTrue();
        diags.GetProperty("deploymentConfigured").GetBoolean().Should().BeTrue();
        diags.GetProperty("apiKeyConfigured").GetBoolean().Should().BeTrue();
        var disclaimers = root.GetProperty("disclaimers");
        disclaimers.EnumerateArray().Any(e => e.GetString()!.Contains("Simulated LLM answer", StringComparison.OrdinalIgnoreCase)).Should().BeTrue();

        // Cleanup (avoid leaking into other tests)
        Environment.SetEnvironmentVariable("Orchestrator__UseFakeLlm", null);
        Environment.SetEnvironmentVariable("AzureOpenAI__Endpoint", null);
        Environment.SetEnvironmentVariable("AzureOpenAI__DeploymentName", null);
        Environment.SetEnvironmentVariable("AzureOpenAI__ApiKey", null);
    }
}
