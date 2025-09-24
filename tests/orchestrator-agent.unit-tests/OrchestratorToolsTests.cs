using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using OrchestratorAgent.Tools;

namespace UnitTests.OrchestratorAgent;

/// <summary>
/// Tests for the segregated orchestrator tool classes.
/// Validates that each tool class works independently after segregation.
/// </summary>
public class OrchestratorToolsTests
{
    [Fact]
    public async Task AskDomainQuestionTool_EmptyQuestion_ReturnsValidationError()
    {
        // Act
        var json = await AskDomainQuestionTool.AskDomainQuestionAsync("");
        
        // Assert
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("status").GetString().Should().Be("error");
        root.GetProperty("error").GetProperty("code").GetString().Should().Be("validation");
        root.GetProperty("error").GetProperty("message").GetString().Should().Contain("empty");
        root.GetProperty("correlationId").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task AskDomainQuestionTool_ValidQuestion_ReturnsStructuredResponse()
    {
        // Act
        var json = await AskDomainQuestionTool.AskDomainQuestionAsync("What is Azure Managed Grafana?");
        
        // Assert
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("answer").GetString().Should().NotBeNullOrWhiteSpace();
        root.GetProperty("confidence").GetString().Should().NotBeNullOrWhiteSpace();
        root.GetProperty("kbUsed").ValueKind.Should().BeOneOf(JsonValueKind.True, JsonValueKind.False);
        root.GetProperty("disclaimers").EnumerateArray().Should().NotBeEmpty();
        root.GetProperty("correlationId").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void GetOrchestratorStatusTool_ReturnsStatusInfo()
    {
        // Act
        var json = GetOrchestratorStatusTool.GetOrchestratorStatus();
        
        // Assert
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("status").GetString().Should().Be("Alive");
        root.GetProperty("environment").GetString().Should().NotBeNullOrWhiteSpace();
        root.GetProperty("fakeLlmMode").ValueKind.Should().BeOneOf(JsonValueKind.True, JsonValueKind.False);
        root.GetProperty("kbExecutableConfigured").ValueKind.Should().BeOneOf(JsonValueKind.True, JsonValueKind.False);
        root.GetProperty("kbExecutableResolved").ValueKind.Should().BeOneOf(JsonValueKind.True, JsonValueKind.False);
        root.GetProperty("timestampUtc").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void GetOrchestratorDiagnosticsTool_ReturnsDetailedInfo()
    {
        // Act
        var json = GetOrchestratorDiagnosticsTool.GetOrchestratorDiagnosticsInformation();
        
        // Assert
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("status").GetString().Should().Be("Alive");
        root.GetProperty("environment").GetString().Should().NotBeNullOrWhiteSpace();
        root.GetProperty("fakeLlmMode").ValueKind.Should().BeOneOf(JsonValueKind.True, JsonValueKind.False);
        root.GetProperty("kbExecutableConfigured").ValueKind.Should().BeOneOf(JsonValueKind.True, JsonValueKind.False);
        root.GetProperty("kbExecutableResolved").ValueKind.Should().BeOneOf(JsonValueKind.True, JsonValueKind.False);
        root.GetProperty("orchestratorExecutablePath").GetString().Should().NotBeNullOrWhiteSpace();
        root.GetProperty("orchestratorAssemblyLocation").GetString().Should().NotBeNullOrWhiteSpace();
        root.GetProperty("timestampUtc").GetString().Should().NotBeNullOrWhiteSpace();
        root.GetProperty("kbProbedSample").EnumerateArray().Should().NotBeNull();
        root.GetProperty("environmentVariables").EnumerateArray().Should().NotBeNull();
    }

    [Fact]
    public void OrchestratorToolsShared_ToJson_ProducesValidJson()
    {
        // Arrange
        var testObject = new { message = "test", value = 42 };
        
        // Act
        var json = OrchestratorToolsShared.ToJson(testObject);
        
        // Assert
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("message").GetString().Should().Be("test");
        root.GetProperty("value").GetInt32().Should().Be(42);
    }

    [Fact]
    public void OrchestratorToolsShared_CreateError_ReturnsErrorStructure()
    {
        // Act
        var json = OrchestratorToolsShared.CreateError("Test error message", "test_code");
        
        // Assert
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("status").GetString().Should().Be("error");
        root.GetProperty("error").GetProperty("message").GetString().Should().Be("Test error message");
        root.GetProperty("error").GetProperty("code").GetString().Should().Be("test_code");
        root.GetProperty("correlationId").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void OrchestratorToolsShared_IsGreeting_DetectsGreetings()
    {
        // Act & Assert
        OrchestratorToolsShared.IsGreeting("hello").Should().BeTrue();
        OrchestratorToolsShared.IsGreeting("Hi there").Should().BeTrue();
        OrchestratorToolsShared.IsGreeting("hey").Should().BeTrue();
        OrchestratorToolsShared.IsGreeting("greetings").Should().BeTrue();
        OrchestratorToolsShared.IsGreeting("What is Azure?").Should().BeFalse();
        OrchestratorToolsShared.IsGreeting("").Should().BeFalse();
    }

    [Fact]
    public void OrchestratorToolsShared_IsPunctuationOnly_DetectsPunctuationOnlyStrings()
    {
        // Act & Assert
        OrchestratorToolsShared.IsPunctuationOnly("???").Should().BeTrue();
        OrchestratorToolsShared.IsPunctuationOnly("!!!").Should().BeTrue();
        OrchestratorToolsShared.IsPunctuationOnly("...").Should().BeTrue();
        OrchestratorToolsShared.IsPunctuationOnly("!!! ???").Should().BeTrue();
        OrchestratorToolsShared.IsPunctuationOnly("hello?").Should().BeFalse();
        OrchestratorToolsShared.IsPunctuationOnly("").Should().BeTrue(); // Empty string contains no non-punctuation
    }
}
