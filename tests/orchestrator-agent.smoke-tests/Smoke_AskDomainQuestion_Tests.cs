using System;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using System.Linq;
using OrchestratorAgent.Tools;

namespace SmokeTests.OrchestratorAgent;

// NOTE: These are lightweight smoke tests (not exhaustive unit tests) to validate
// the current behavior of AskDomainQuestionAsync without requiring
// real Azure OpenAI or KB server configuration.

public class Smoke_AskDomainQuestion_Tests
{
    [Fact]
    public async Task AskDomainQuestionAsync_ReturnsSuccessResponse_WhenNoConfig()
    {
        var json = await AskDomainQuestionTool.AskDomainQuestionAsync("What is Azure Managed Grafana?");
        json.Should().NotBeNullOrWhiteSpace();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("answer").GetString().Should().NotBeNull();
        root.GetProperty("status").GetString().Should().Be("success");
        root.TryGetProperty("disclaimers", out _).Should().BeTrue();
    }

    [Fact]
    public async Task AskDomainQuestionAsync_ShortQuestion_ReturnsValidationError()
    {
        var json = await AskDomainQuestionTool.AskDomainQuestionAsync("hi");
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("status").GetString().Should().Be("error");
        root.GetProperty("error").GetProperty("code").GetString().Should().Be("validation");
    }

    [Fact]
    public async Task AskDomainQuestionAsync_Greeting_SkipsKbHeuristically()
    {
        var json = await AskDomainQuestionTool.AskDomainQuestionAsync("hello there, how are you?");
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("status").GetString().Should().Be("success");
        var disclaimers = root.GetProperty("disclaimers");
        disclaimers.EnumerateArray().Any(e => e.GetString()!.Contains("greeting heuristic")).Should().BeTrue();
    }

    [Fact]
    public async Task AskDomainQuestionAsync_ConfiguredGreetingPattern_TriggersSkip()
    {
        // Using default pattern 'greetings' present in appsettings.json
        var json = await AskDomainQuestionTool.AskDomainQuestionAsync("greetings orchestrator, status?");
        using var doc = JsonDocument.Parse(json);
        var disclaimers = doc.RootElement.GetProperty("disclaimers");
        disclaimers.EnumerateArray().Any(e => e.GetString()!.Contains("greeting heuristic")).Should().BeTrue();
    }
}
