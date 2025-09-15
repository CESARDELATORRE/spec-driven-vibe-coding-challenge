using System;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using System.Linq;

// NOTE: These are lightweight smoke tests (not exhaustive unit tests) to validate
// the current scaffold behavior of AskDomainQuestionAsync without requiring
// real Azure OpenAI or KB server configuration.

public class Smoke_AskDomainQuestion_Tests
{
    [Fact]
    public async Task AskDomainQuestionAsync_ReturnsScaffoldResponse_WhenNoConfig()
    {
        var json = await OrchestratorTools.AskDomainQuestionAsync("What is Azure Managed Grafana?", includeKb: false);
        json.Should().NotBeNullOrWhiteSpace();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("answer").GetString().Should().NotBeNull();
        root.GetProperty("status").GetString().Should().Be("scaffold");
        root.TryGetProperty("disclaimers", out _).Should().BeTrue();
    }

    [Fact]
    public async Task AskDomainQuestionAsync_ShortQuestion_ReturnsValidationError()
    {
        var json = await OrchestratorTools.AskDomainQuestionAsync("hi", includeKb: true);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("status").GetString().Should().Be("error");
        root.GetProperty("error").GetProperty("code").GetString().Should().Be("validation");
    }

    [Fact]
    public async Task AskDomainQuestionAsync_Greeting_SkipsKbHeuristically()
    {
        var json = await OrchestratorTools.AskDomainQuestionAsync("hello there, how are you?", includeKb: true);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("status").GetString().Should().Be("scaffold");
        var disclaimers = root.GetProperty("disclaimers");
        disclaimers.EnumerateArray().Any(e => e.GetString()!.Contains("greeting heuristic")).Should().BeTrue();
    }

    [Fact]
    public async Task AskDomainQuestionAsync_ConfiguredGreetingPattern_TriggersSkip()
    {
        // Using default pattern 'greetings' present in appsettings.json
        var json = await OrchestratorTools.AskDomainQuestionAsync("greetings orchestrator, status?", includeKb: true);
        using var doc = JsonDocument.Parse(json);
        var disclaimers = doc.RootElement.GetProperty("disclaimers");
        disclaimers.EnumerateArray().Any(e => e.GetString()!.Contains("greeting heuristic")).Should().BeTrue();
    }
}
