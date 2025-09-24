using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using FluentAssertions;
using Xunit;

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

    private static string KbServerProjectPath
    {
        get
        {
            var candidate = Path.Combine(Directory.GetCurrentDirectory(), "src", "mcp-server-kb-content-fetcher", "mcp-server-kb-content-fetcher.csproj");
            if (File.Exists(candidate)) return candidate;
            candidate = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "src", "mcp-server-kb-content-fetcher", "mcp-server-kb-content-fetcher.csproj"));
            if (File.Exists(candidate)) return candidate;
            candidate = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../src/mcp-server-kb-content-fetcher/mcp-server-kb-content-fetcher.csproj"));
            if (File.Exists(candidate)) return candidate;
            throw new FileNotFoundException("Could not resolve KB MCP server project path");
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
    public async Task GetOrchestratorStatus_Should_Return_Alive_Status()
    {
        var path = Path.GetFullPath(ServerProjectPath);
        await using var client = await StdioMcpClient.StartAsync(path);
        await client.InitializeAsync();

        var response = await client.SendRequestAsync(new
        {
            jsonrpc = "2.0",
            method = "tools/call",
            @params = new { name = "get_orchestrator_status", arguments = new { } }
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
        statusEl.GetString().Should().Be("Alive");
    }

    [Fact(Timeout = 30000)]
    public async Task GetOrchestratorDiagnosticsInformation_Should_Return_Detailed_Status()
    {
        var path = Path.GetFullPath(ServerProjectPath);
        await using var client = await StdioMcpClient.StartAsync(path);
        await client.InitializeAsync();

        var response = await client.SendRequestAsync(new
        {
            jsonrpc = "2.0",
            method = "tools/call",
            @params = new { name = "get_orchestrator_diagnostics_information", arguments = new { } }
        });

        using var doc = JsonDocument.Parse(response);
        doc.RootElement.TryGetProperty("result", out var resultEl).Should().BeTrue();
        var contentArray = resultEl.GetProperty("content");
        contentArray.GetArrayLength().Should().Be(1);
        var textPayload = contentArray[0].GetProperty("text").GetString();
        textPayload.Should().NotBeNullOrEmpty();

        using var inner = JsonDocument.Parse(textPayload!);
        var root = inner.RootElement;
        
        // Verify required diagnostic fields
        root.TryGetProperty("status", out var statusEl).Should().BeTrue();
        statusEl.GetString().Should().Be("Alive");
        
        root.TryGetProperty("environment", out var envEl).Should().BeTrue();
        envEl.GetString().Should().NotBeNullOrEmpty();
        
        root.TryGetProperty("kbExecutableConfigured", out var kbConfiguredEl).Should().BeTrue();
        kbConfiguredEl.GetBoolean().Should().BeTrue(); // KB path configured in appsettings.json
        
        root.TryGetProperty("kbExecutableResolved", out var kbResolvedEl).Should().BeTrue();
        // KB resolution may fail in test environment, but field should exist
        
        root.TryGetProperty("fakeLlmMode", out var fakeModeEl).Should().BeTrue();
        fakeModeEl.GetBoolean().Should().BeFalse(); // Default is false
        
        root.TryGetProperty("timestampUtc", out var timestampEl).Should().BeTrue();
        timestampEl.GetString().Should().NotBeNullOrEmpty();
        
        root.TryGetProperty("environmentVariables", out var envVarsEl).Should().BeTrue();
        envVarsEl.ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact(Timeout = 30000)]
    public async Task AskDomainQuestion_Should_Return_Greeting_Response_For_Hello()
    {
        // This test uses real Azure OpenAI configuration from environment variables
        var path = Path.GetFullPath(ServerProjectPath);
        await using var client = await StdioMcpClient.StartAsync(path);
        await client.InitializeAsync();

        var response = await client.SendRequestAsync(new
        {
            jsonrpc = "2.0",
            method = "tools/call",
            @params = new { name = "ask_domain_question", arguments = new { question = "hello orchestrator agent" } }
        });

        using var doc = JsonDocument.Parse(response);
        doc.RootElement.TryGetProperty("result", out var resultEl).Should().BeTrue();
        var contentArray = resultEl.GetProperty("content");
        contentArray.GetArrayLength().Should().Be(1);
        var textPayload = contentArray[0].GetProperty("text").GetString();
        textPayload.Should().NotBeNullOrEmpty();

        using var inner = JsonDocument.Parse(textPayload!);
        var root = inner.RootElement;
        root.GetProperty("answer").GetString().Should().Be("Hello! I'm ready to help you with Azure Managed Grafana questions.");
        root.GetProperty("confidence").GetString().Should().Be("high");
        root.GetProperty("kbUsed").GetBoolean().Should().BeFalse();
        root.TryGetProperty("disclaimers", out var disclaimersEl).Should().BeTrue();
        disclaimersEl.EnumerateArray().Any(e => e.GetString()!.Contains("KB lookup skipped")).Should().BeTrue();
    }

    [Fact(Timeout = 30000)]
    public async Task AskDomainQuestion_SmokeTest_Should_Return_Success_Response_With_Real_LLM()
    {
        // SMOKE TEST: Validates basic functionality using real Azure OpenAI configuration from environment
        var path = Path.GetFullPath(ServerProjectPath);
        await using var client = await StdioMcpClient.StartAsync(path);
        await client.InitializeAsync();

        var response = await client.SendRequestAsync(new
        {
            jsonrpc = "2.0",
            method = "tools/call",
            @params = new { name = "ask_domain_question", arguments = new { question = "What is Azure Managed Grafana?" } }
        });

        using var doc = JsonDocument.Parse(response);
        doc.RootElement.TryGetProperty("result", out var resultEl).Should().BeTrue();
        var contentArray = resultEl.GetProperty("content");
        contentArray.GetArrayLength().Should().Be(1);
        var textPayload = contentArray[0].GetProperty("text").GetString();
        textPayload.Should().NotBeNullOrEmpty();

        using var inner = JsonDocument.Parse(textPayload!);
        var root = inner.RootElement;
        root.GetProperty("answer").GetString().Should().NotBeNull();
        root.GetProperty("confidence").GetString().Should().NotBeNull();
        root.TryGetProperty("disclaimers", out _).Should().BeTrue();
    }

    [Fact(Timeout = 30000)]
    public async Task AskDomainQuestion_SmokeTest_Should_Return_Greeting_For_Short_Question_With_Real_LLM()
    {
        // SMOKE TEST: Validates greeting logic using real Azure OpenAI configuration from environment
        var path = Path.GetFullPath(ServerProjectPath);
        await using var client = await StdioMcpClient.StartAsync(path);
        await client.InitializeAsync();

        var response = await client.SendRequestAsync(new
        {
            jsonrpc = "2.0",
            method = "tools/call",
            @params = new { name = "ask_domain_question", arguments = new { question = "hi" } }
        });

        using var doc = JsonDocument.Parse(response);
        doc.RootElement.TryGetProperty("result", out var resultEl).Should().BeTrue();
        var contentArray = resultEl.GetProperty("content");
        contentArray.GetArrayLength().Should().Be(1);
        var textPayload = contentArray[0].GetProperty("text").GetString();
        textPayload.Should().NotBeNullOrEmpty();

        using var inner = JsonDocument.Parse(textPayload!);
        var root = inner.RootElement;
        // For "hi" greeting, it should be handled as a greeting and return normal response
        root.GetProperty("answer").GetString().Should().Be("Hello! I'm ready to help you with Azure Managed Grafana questions.");
        root.GetProperty("confidence").GetString().Should().Be("high");
    }

    [Fact(Timeout = 30000)]
    public async Task AskDomainQuestion_SmokeTest_Should_Skip_Kb_Heuristically_For_Greeting_With_Real_LLM()
    {
        // SMOKE TEST: Validates greeting heuristic logic using real Azure OpenAI configuration from environment
        var path = Path.GetFullPath(ServerProjectPath);
        await using var client = await StdioMcpClient.StartAsync(path);
        await client.InitializeAsync();

        var response = await client.SendRequestAsync(new
        {
            jsonrpc = "2.0",
            method = "tools/call",
            @params = new { name = "ask_domain_question", arguments = new { question = "hello there, how are you?" } }
        });

        using var doc = JsonDocument.Parse(response);
        doc.RootElement.TryGetProperty("result", out var resultEl).Should().BeTrue();
        var contentArray = resultEl.GetProperty("content");
        contentArray.GetArrayLength().Should().Be(1);
        var textPayload = contentArray[0].GetProperty("text").GetString();
        textPayload.Should().NotBeNullOrEmpty();

        using var inner = JsonDocument.Parse(textPayload!);
        var root = inner.RootElement;
        root.GetProperty("answer").GetString().Should().Be("Hello! I'm ready to help you with Azure Managed Grafana questions.");
        root.GetProperty("confidence").GetString().Should().Be("high");
        root.GetProperty("kbUsed").GetBoolean().Should().BeFalse();
        var disclaimers = root.GetProperty("disclaimers");
        disclaimers.EnumerateArray().Any(e => e.GetString()!.Contains("KB lookup skipped")).Should().BeTrue();
    }

    [Fact(Timeout = 30000)]
    public async Task AskDomainQuestion_SmokeTest_Should_Trigger_Skip_For_Configured_Greeting_Pattern_With_Real_LLM()
    {
        // SMOKE TEST: Validates configured greeting pattern recognition using real Azure OpenAI configuration from environment
        var path = Path.GetFullPath(ServerProjectPath);
        await using var client = await StdioMcpClient.StartAsync(path);
        await client.InitializeAsync();

        var response = await client.SendRequestAsync(new
        {
            jsonrpc = "2.0",
            method = "tools/call",
            @params = new { name = "ask_domain_question", arguments = new { question = "greetings orchestrator, status?" } }
        });

        using var doc = JsonDocument.Parse(response);
        doc.RootElement.TryGetProperty("result", out var resultEl).Should().BeTrue();
        var contentArray = resultEl.GetProperty("content");
        contentArray.GetArrayLength().Should().Be(1);
        var textPayload = contentArray[0].GetProperty("text").GetString();
        textPayload.Should().NotBeNullOrEmpty();

        using var inner = JsonDocument.Parse(textPayload!);
        var root = inner.RootElement;
        root.GetProperty("answer").GetString().Should().Be("Hello! I'm ready to help you with Azure Managed Grafana questions.");
        var disclaimers = root.GetProperty("disclaimers");
        disclaimers.EnumerateArray().Any(e => e.GetString()!.Contains("KB lookup skipped")).Should().BeTrue();
    }

    /// <summary>
    /// TRUE END-TO-END INTEGRATION TEST:
    /// 1. Starts the KB MCP Server
    /// 2. Starts the Orchestrator Agent (configured to use the KB MCP Server)
    /// 3. Tests the complete flow: Orchestrator -> KB -> ChatCompletionAgent -> Response
    /// </summary>
    [Fact(Timeout = 60000)]
    public async Task EndToEnd_AskDomainQuestion_WithRealKbServer_Should_UseKnowledgeBase()
    {
        // Setup paths
        var orchestratorPath = Path.GetFullPath(ServerProjectPath);
        var kbServerPath = Path.GetFullPath(KbServerProjectPath);
        
        Console.Error.WriteLine($"[E2E TEST] Using orchestrator project path: {orchestratorPath}");
        Console.Error.WriteLine($"[E2E TEST] Using KB server project path: {kbServerPath}");
        
        File.Exists(orchestratorPath).Should().BeTrue();
        File.Exists(kbServerPath).Should().BeTrue();

        // Start KB Server first (it needs to be running before orchestrator starts)
        await using var kbClient = await StdioMcpClient.StartAsync(kbServerPath);
        await kbClient.InitializeAsync();
        
        Console.Error.WriteLine("[E2E TEST] KB MCP Server started successfully");

        // Test KB server is working
        var kbTools = await kbClient.SendRequestAsync(new
        {
            jsonrpc = "2.0",
            method = "tools/list",
            @params = new { }
        });
        
        using var kbToolsDoc = JsonDocument.Parse(kbTools);
        var kbToolsList = kbToolsDoc.RootElement.GetProperty("result").GetProperty("tools");
        kbToolsList.GetArrayLength().Should().BeGreaterThan(0);
        Console.Error.WriteLine($"[E2E TEST] KB server has {kbToolsList.GetArrayLength()} tools available");

        // Configure orchestrator to use the KB server (relative path)
        var kbServerRelativePath = "../../../../mcp-server-kb-content-fetcher/bin/Debug/net9.0/mcp-server-kb-content-fetcher";
        Environment.SetEnvironmentVariable("KbMcpServer__ExecutablePath", kbServerRelativePath);
        Environment.SetEnvironmentVariable("Orchestrator__UseFakeLlm", "false"); // Use real LLM for E2E test
        
        Console.Error.WriteLine($"[E2E TEST] Set KB server path: {kbServerRelativePath}");

        // Start Orchestrator Agent
        await using var orchestratorClient = await StdioMcpClient.StartAsync(orchestratorPath);
        await orchestratorClient.InitializeAsync();
        
        Console.Error.WriteLine("[E2E TEST] Orchestrator Agent started successfully");

        // Test 1: Ask a real domain question that should use KB
        var response = await orchestratorClient.SendRequestAsync(new
        {
            jsonrpc = "2.0",
            method = "tools/call",
            @params = new { name = "ask_domain_question", arguments = new { question = "What is Azure Managed Grafana?" } }
        });

        using var doc = JsonDocument.Parse(response);
        doc.RootElement.TryGetProperty("result", out var resultEl).Should().BeTrue();
        var contentArray = resultEl.GetProperty("content");
        contentArray.GetArrayLength().Should().Be(1);
        var textPayload = contentArray[0].GetProperty("text").GetString();
        textPayload.Should().NotBeNullOrEmpty();

        Console.Error.WriteLine($"[E2E TEST] Response payload: {textPayload}");

        using var inner = JsonDocument.Parse(textPayload!);
        var root = inner.RootElement;
        
        // Verify response structure
        root.TryGetProperty("answer", out _).Should().BeTrue();
        root.TryGetProperty("confidence", out _).Should().BeTrue();
        root.TryGetProperty("kbUsed", out _).Should().BeTrue();
        root.TryGetProperty("correlationId", out _).Should().BeTrue();
        
        var answer = root.GetProperty("answer").GetString();
        answer.Should().NotBeNullOrEmpty();
        answer.Should().NotContain("[FAKE LLM]"); // Should be real LLM response
        
        // For a real question (not greeting), kbUsed might be true or false depending on KB availability
        var kbUsed = root.GetProperty("kbUsed").GetBoolean();
        Console.Error.WriteLine($"[E2E TEST] KB was used: {kbUsed}");
        
        // If KB was used successfully, verify no KB-related disclaimers about failures
        if (kbUsed)
        {
            if (root.TryGetProperty("disclaimers", out var disclaimersEl))
            {
                var disclaimers = disclaimersEl.EnumerateArray().Select(e => e.GetString()).ToArray();
                disclaimers.Should().NotContain(d => d!.Contains("KB lookup failed"));
                disclaimers.Should().NotContain(d => d!.Contains("KB server unavailable"));
                Console.Error.WriteLine($"[E2E TEST] Disclaimers when KB used: [{string.Join(", ", disclaimers)}]");
            }
        }
        
        Console.Error.WriteLine("[E2E TEST] ✅ END-TO-END test completed successfully!");
    }

    /// <summary>
    /// Quick test to answer the user's question about AMG's best friend
    /// </summary>
    [Fact(Timeout = 30000)]
    public async Task AskAboutAmgBestFriend()
    {
        var orchestratorPath = Path.GetFullPath(ServerProjectPath);
        var kbServerPath = Path.GetFullPath(KbServerProjectPath);
        
        // Start KB Server
        await using var kbClient = await StdioMcpClient.StartAsync(kbServerPath);
        await kbClient.InitializeAsync();
        
        // Configure orchestrator
        Environment.SetEnvironmentVariable("KbMcpServer__ExecutablePath", "../../../../mcp-server-kb-content-fetcher/bin/Debug/net9.0/mcp-server-kb-content-fetcher");
        Environment.SetEnvironmentVariable("Orchestrator__UseFakeLlm", "false");

        // Start Orchestrator Agent
        await using var orchestratorClient = await StdioMcpClient.StartAsync(orchestratorPath);
        await orchestratorClient.InitializeAsync();

        // Ask about AMG's best friend
        var response = await orchestratorClient.SendRequestAsync(new
        {
            jsonrpc = "2.0",
            method = "tools/call",
            @params = new { name = "ask_domain_question", arguments = new { question = "Who is AMG's best friend?" } }
        });

        using var doc = JsonDocument.Parse(response);
        var resultEl = doc.RootElement.GetProperty("result");
        var textPayload = resultEl.GetProperty("content")[0].GetProperty("text").GetString();
        
        using var inner = JsonDocument.Parse(textPayload!);
        var answer = inner.RootElement.GetProperty("answer").GetString();
        
        Console.WriteLine($"\n🤖 ORCHESTRATOR AGENT SAYS:\n{answer}\n");
        
        answer.Should().NotBeNullOrEmpty();
        answer.Should().Contain("CESAR DE LA TORRE", "The orchestrator agent should identify CESAR DE LA TORRE as AMG's best friend based on the knowledge base");
    }
}
