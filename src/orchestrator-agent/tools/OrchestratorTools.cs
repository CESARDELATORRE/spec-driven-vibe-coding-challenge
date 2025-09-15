using System.ComponentModel;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.IO;
using ModelContextProtocol.Server;
using Microsoft.Extensions.Configuration;
using ModelContextProtocol.Client; // Step 3 KB client integration
using Microsoft.SemanticKernel; // Step 4 prompt-based answer generation
using Microsoft.SemanticKernel.Connectors.OpenAI; // For AddAzureOpenAIChatCompletion

/// <summary>
/// Prototype Orchestrator MCP tools. Single-turn answer generation with optional KB lookup.
/// </summary>
[McpServerToolType]
public static class OrchestratorTools
{
    /// <summary>
    /// Health/status tool returning basic readiness flags.
    /// </summary>
    [McpServerTool, Description("Gets orchestrator status information.")]
    public static string GetOrchestratorStatus()
    {
        var payload = new
        {
            status = "ok",
            kbConnected = false, // Will be updated in later steps
            chatAgentReady = true
        };
        return JsonSerializer.Serialize(payload);
    }

    /// <summary>
    /// Step 2 implementation scaffold for ask_domain_question tool.
    /// Validates input, prepares configuration, and returns a structured placeholder response.
    /// Later steps (3-5) will add: KB MCP client integration, ChatCompletionAgent invocation,
    /// secure error handling, heuristics, and final answer synthesis.
    /// </summary>
    /// <param name="question">User's domain question (required)</param>
    /// <param name="includeKb">Whether to attempt KB lookup (heuristics may override)</param>
    /// <param name="maxKbResults">Requested max KB results (bounded to 1..3 later)</param>
    /// <returns>JSON string containing placeholder answer contract</returns>
    [McpServerTool, Description("Ask a domain question (Step 2 scaffold – logic to be completed in later steps).")]
    public static async Task<string> AskDomainQuestionAsync(
        string question,
        bool includeKb = true,
        int maxKbResults = 2)
    {
        // STEP 5: VALIDATION & ERROR HANDLING HARDENING
        // Early sanitize input (trim) for consistent checks.
        question = question?.Trim() ?? string.Empty;

        // Helper local function for consistent error payloads.
        static string CreateError(string message, string code = "validation")
        {
            var payload = new
            {
                error = new { message, code },
                status = "error",
                correlationId = Guid.NewGuid().ToString()
            };
            return JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
        }

        if (string.IsNullOrWhiteSpace(question))
        {
            return CreateError("Question is required");
        }

        // Reject ultra-short or punctuation-only questions (spec requirement)
        if (question.Length < 5 || IsPunctuationOnly(question))
        {
            return CreateError("Question must be at least 5 non-punctuation characters");
        }

        // Clamp and record requested / effective values + add disclaimer if adjusted
        int requestedMaxKb = maxKbResults;
        if (maxKbResults < 1) maxKbResults = 1; else if (maxKbResults > 3) maxKbResults = 3;
        bool kbResultsClamped = requestedMaxKb != maxKbResults;

        // Configuration layering (Step 2: env vars first-class; user secrets only if dev)
    var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";
        var configBuilder = new ConfigurationBuilder()
            .AddEnvironmentVariables();
        if (string.Equals(environment, "Development", StringComparison.OrdinalIgnoreCase))
        {
            // Optional; safe no-op outside dev.
            configBuilder.AddUserSecrets<Program>(optional: true);
        }
        var config = configBuilder.Build();

        // Extract (not yet strictly enforced until Step 5)
        string? endpoint = config["AzureOpenAI:Endpoint"]; // AzureOpenAI__Endpoint
        string? deploymentName = config["AzureOpenAI:DeploymentName"]; // AzureOpenAI__DeploymentName
        string? apiKey = config["AzureOpenAI:ApiKey"]; // AzureOpenAI__ApiKey

    // Heuristic (enhanced in Step 5): skip KB for greeting-like questions (not short ones — those already validated above)
    bool heuristicSkipKb = ShouldSkipKb(question, config);
    bool attemptKb = includeKb && !heuristicSkipKb;

        // Placeholder arrays for future KB & answer data
        var kbResults = new List<object>();
        var disclaimers = new List<string>();

        bool usedKb = false;
        if (heuristicSkipKb)
        {
            disclaimers.Add("KB skipped (greeting heuristic)");
        }
        else if (attemptKb)
        {
            // Step 3: Attempt to spin up and connect to KB MCP server and list tools / fetch content.
            try
            {
                var kbPathConfig = config["KbMcpServer:ExecutablePath"]; // overrideable via KbMcpServer__ExecutablePath
                if (string.IsNullOrWhiteSpace(kbPathConfig))
                {
                    disclaimers.Add("KB server path not configured (KbMcpServer__ExecutablePath)");
                }
                else
                {
                    // Resolve relative path from orchestrator base dir
                    string resolved = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, kbPathConfig));
                    if (!File.Exists(resolved) && File.Exists(resolved + ".exe"))
                    {
                        resolved = resolved + ".exe"; // Windows dev convenience
                    }
                    if (!File.Exists(resolved))
                    {
                        disclaimers.Add($"KB executable not found at resolved path: {resolved}");
                    }
                    else
                    {
                        await using IMcpClient kbClient = await McpClientFactory.CreateAsync(
                            new StdioClientTransport(new()
                            {
                                Name = "kb-mcp-server",
                                Command = resolved,
                                Arguments = Array.Empty<string>()
                            }));

                        var kbToolList = await kbClient.ListToolsAsync().ConfigureAwait(false);
                        // Identify content tool(s) for this prototype (search or get_kb_content)
                        var contentTool = kbToolList.FirstOrDefault(t => t.Name == "get_kb_content" || t.Name == "search_knowledge");
                        if (contentTool is null)
                        {
                            disclaimers.Add("KB tools available but no recognized content tool (get_kb_content/search_knowledge)");
                        }
                        else
                        {
                            // For Step 3 we do a minimal invocation only if get_kb_content exists (cheap). search_knowledge requires args (defer until Step 5 refinement)
                            if (contentTool.Name == "get_kb_content")
                            {
                                try
                                {
                                    var invocation = await kbClient.CallToolAsync(contentTool.Name, new Dictionary<string, object?>()).ConfigureAwait(false);
                                    string? raw = invocation?.ToString();
                                    if (!string.IsNullOrWhiteSpace(raw))
                                    {
                                        // Truncate overly large content for prototype (3000 char cap per spec rationale)
                                        const int cap = 3000;
                                        string snippet = raw.Length > cap ? raw.Substring(0, cap) + "...[truncated]" : raw;
                                        kbResults.Add(new { snippet, truncated = raw.Length > cap });
                                        usedKb = true;
                                    }
                                    else
                                    {
                                        disclaimers.Add("KB content tool returned empty result");
                                    }
                                }
                                catch (Exception exContent)
                                {
                                    Console.Error.WriteLine($"KB content invocation failed: {exContent.Message}");
                                    disclaimers.Add("Failed to retrieve KB content");
                                }
                            }
                            else
                            {
                                // search_knowledge will be implemented with query args in Step 5 once we shape prompt & error flows.
                                disclaimers.Add("search_knowledge tool detected but query invocation deferred to later step");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"KB integration error (graceful degradation): {ex.Message}");
                disclaimers.Add("Answer generated without knowledge base due to KB error");
            }
        }

        // Chat / LLM not yet invoked (Step 4). Provide placeholder answer.
        // Step 4: Attempt answer synthesis using Azure OpenAI via Semantic Kernel.
    string answer;
    int tokensEstimate;
    bool chatAgentReady = false;
    bool fakeLlmMode = string.Equals(config["Orchestrator:UseFakeLlm"], "true", StringComparison.OrdinalIgnoreCase);
        try
        {
            if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(deploymentName) || string.IsNullOrWhiteSpace(apiKey))
            {
                answer = usedKb
                    ? "KB context retrieved, but Azure OpenAI configuration missing; cannot synthesize answer."
                    : "Azure OpenAI configuration missing; cannot generate answer.";
                disclaimers.Add("Missing Azure OpenAI configuration (Endpoint / Deployment / ApiKey)");
            }
            else
            {
                if (fakeLlmMode)
                {
                    // Simulated successful LLM path for integration testing without external calls.
                    chatAgentReady = true;
                    if (!usedKb)
                    {
                        disclaimers.Add("Answer generated without knowledge base grounding");
                    }
                    disclaimers.Add("Simulated LLM answer (fake mode)");
                    answer = $"FAKE_LLM_ANSWER: {question}";
                }
                else
                {
                    var kernelBuilder = Kernel.CreateBuilder();
                    kernelBuilder.AddAzureOpenAIChatCompletion(endpoint: endpoint!, deploymentName: deploymentName!, apiKey: apiKey!);
                    Kernel kernel = kernelBuilder.Build();
                    chatAgentReady = true;

                    // Build prompt with optional KB snippets
                    var kbSection = string.Empty;
                    if (usedKb && kbResults.Count > 0)
                    {
                        var joined = string.Join("\n---\n", kbResults.Select(r => (r as dynamic)?.snippet ?? r.ToString()));
                        kbSection = $"Knowledge Base Snippets:\n{joined}\n\n";
                    }
                    else
                    {
                        disclaimers.Add("Answer generated without knowledge base grounding");
                    }

                    string systemInstructions = "You are a concise domain Q&A assistant. Use provided KB snippets if present. Keep answer under 200 words. If no KB snippets, clearly state answer may lack domain grounding.";
                    string fullPrompt = $"{systemInstructions}\n\n{kbSection}User Question: {question}\n\nAnswer:";

                    var execSettings = new OpenAIPromptExecutionSettings
                    {
                        Temperature = 0.2,
                    };

                    var promptResult = await kernel.InvokePromptAsync(fullPrompt, new(execSettings)).ConfigureAwait(false);
                    answer = promptResult.ToString();
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Answer generation failed: {ex.Message}");
            disclaimers.Add("LLM answer generation failed; returning placeholder");
            answer = "Answer generation failed due to internal error (placeholder).";
        }

        // Rough token estimate based on character counts (prototype heuristic)
        int kbChars = kbResults.Sum(r => ((r as dynamic)?.snippet as string)?.Length ?? 0);
        tokensEstimate = Math.Max(20, (question.Length + kbChars + answer.Length) / 4);

        var response = new
        {
            answer,
            usedKb,
            kbResults,
            disclaimers = disclaimers.ToArray(),
            tokensEstimate,
            diagnostics = new
            {
                environment,
                endpointConfigured = !string.IsNullOrWhiteSpace(endpoint),
                deploymentConfigured = !string.IsNullOrWhiteSpace(deploymentName),
                apiKeyConfigured = !string.IsNullOrWhiteSpace(apiKey),
                attemptedKb = attemptKb,
                heuristicSkipKb,
                chatAgentReady,
                requestedMaxKbResults = requestedMaxKb,
                effectiveMaxKbResults = maxKbResults,
                kbResultsClamped,
                fakeLlmMode
            },
            status = "scaffold"
        };

        // Simulate async path (future code will await real operations)
        await Task.Yield();
        return JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true });
    }

    // --- Helper methods (private) ---
    /// <summary>
    /// Simple heuristic for Step 2 (expanded in Step 5): skip KB for very short or greeting inputs.
    /// </summary>
    private static bool ShouldSkipKb(string question, IConfiguration config)
    {
        // At this point ultra-short already rejected. Only greetings here.
        var patterns = config.GetSection("GreetingPatterns").Get<string[]>() ?? new[] { "hi", "hello", "hey", "greetings" };
        string joined = string.Join("|", patterns.Select(Regex.Escape));
        if (string.IsNullOrWhiteSpace(joined)) return false;
        var greetingPattern = "^(" + joined + ")\\b";
        return Regex.IsMatch(question.Trim(), greetingPattern, RegexOptions.IgnoreCase);
    }

    private static bool IsPunctuationOnly(string input)
    {
        foreach (char c in input)
        {
            if (!char.IsPunctuation(c) && !char.IsWhiteSpace(c)) return false;
        }
        return true;
    }
}
