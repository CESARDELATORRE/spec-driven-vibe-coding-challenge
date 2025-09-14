using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;

// NOTE: These are lightweight smoke tests (not exhaustive unit tests) to validate
// the current scaffold behavior of AskDomainQuestionAsync without requiring
// real Azure OpenAI or KB server configuration.

public class Smoke_AskDomainQuestion_Tests
{
    [Fact]
    public async Task AskDomainQuestionAsync_ReturnsScaffoldResponse_WhenNoConfig()
    {
        // Arrange
        var question = "What is Azure Managed Grafana?";

        // Act
        var json = await OrchestratorTools.AskDomainQuestionAsync(question, includeKb: false);

        // Assert
        json.Should().NotBeNullOrWhiteSpace();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("answer").GetString().Should().NotBeNull();
        root.GetProperty("usedKb").GetBoolean().Should().BeFalse();
        root.GetProperty("diagnostics").GetProperty("endpointConfigured").GetBoolean().Should().BeFalse();
        root.GetProperty("status").GetString().Should().Be("scaffold");
    }

    [Fact]
    public async Task AskDomainQuestionAsync_ShortQuestion_TriggersHeuristicSkip()
    {
        var json = await OrchestratorTools.AskDomainQuestionAsync("hi", includeKb: true);
        using var doc = JsonDocument.Parse(json);
        var disclaimers = doc.RootElement.GetProperty("disclaimers");
        disclaimers.GetArrayLength().Should().BeGreaterThan(0);
    }
}
