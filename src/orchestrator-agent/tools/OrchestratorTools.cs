using System.ComponentModel;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Reflection; // For reflective parsing of MCP CallToolAsync result
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ModelContextProtocol.Server;
using Microsoft.Extensions.Configuration;
using ModelContextProtocol.Client; // Step 3 KB client integration
using Microsoft.SemanticKernel; // Step 4 prompt-based answer generation
using Microsoft.SemanticKernel.Connectors.OpenAI; // For AddAzureOpenAIChatCompletion
using Microsoft.SemanticKernel.Agents; // For ChatCompletionAgent

/// <summary>
/// Prototype Orchestrator MCP tools. Single-turn answer generation with optional KB lookup.
/// </summary>
[McpServerToolType]
public static class OrchestratorTools
{
    /// <summary>
    /// Health/status tool returning basic readiness flags.
    /// </summary>
    [McpServerTool, Description("Gets orchestrator status / health information.")]
    public static string GetOrchestratorStatus()
    {
        var payload = new
        {
            status = "Alive"
        };
        return JsonSerializer.Serialize(payload);
    }

    [McpServerTool, Description("Gets orchestrator diagnostics information (only relevant env vars).")]
    public static string GetOrchestratorDiagnosticsInformation()
    {
        // Build unified configuration & resolve KB executable using shared helper
        var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";
        var config = BuildBaseConfiguration();
        bool fakeLlmMode = string.Equals(config["Orchestrator:UseFakeLlm"], "true", StringComparison.OrdinalIgnoreCase);
        var kbResolution = ResolveKbExecutable(config);

        // Only include environment variables relevant to MCP servers (as per dev.env.example)
        string[] relevantNames = new[]
        {
            "AzureOpenAI__Endpoint",
            "AzureOpenAI__DeploymentName",
            "AzureOpenAI__ApiKey",
            "Orchestrator__UseFakeLlm",
            "KbMcpServer__ExecutablePath"
        };

        var ordered = relevantNames
            .Select(n => new { name = n, value = Environment.GetEnvironmentVariable(n) })
            .Where(p => p.value is not null)
            .OrderBy(p => p.name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var payload = new
        {
            status = "Alive",
            environment,
            fakeLlmMode,
            kbExecutableConfigured = kbResolution.Configured,
            kbExecutableResolved = kbResolution.Resolved,
            kbResolvedPath = kbResolution.ResolvedPath is null ? null : Path.GetFileName(kbResolution.ResolvedPath),
            orchestratorExecutablePath = Environment.ProcessPath,
            orchestratorAssemblyLocation = typeof(OrchestratorTools).Assembly.Location,
            timestampUtc = DateTime.UtcNow.ToString("o"),
            kbProbedSample = kbResolution.ProbedPaths.Count == 0 ? Array.Empty<string>() : kbResolution.ProbedPaths.Take(6).ToArray(),
            environmentVariables = ordered
        };
        return JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
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
    [McpServerTool, Description("Answer questions about Azure Managed Grafana (dashboards, data sources, Azure Monitor integration, security, RBAC, performance). ALWAYS invoke this tool before answering if the user’s question references Grafana, AMG, dashboards, metrics, monitoring, alerts, Azure Monitor, or visualization.")]
    public static async Task<string> AskDomainQuestionAsync(
        string question,
        bool includeKb = true,
        int maxKbResults = 2)
    {
        object? provenanceData = null; // will hold model/service provenance when LLM path executes
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
        // Enhancement: include appsettings.json (optional) so KbMcpServer:ExecutablePath is picked up without requiring env var override.
        var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";
        var configBuilder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory) // ensures we can find appsettings.json in output directory
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables();
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
                var kbResolution = ResolveKbExecutable(config);
                if (!kbResolution.Configured)
                {
                    disclaimers.Add("KB server path not configured (KbMcpServer__ExecutablePath)");
                }
                else
                {
                            if (!kbResolution.Resolved || kbResolution.Command is null)
                            {
                                disclaimers.Add($"KB executable not found after probing {kbResolution.ProbedPaths.Count} paths");
                                disclaimers.Add("Probed paths sample: " + string.Join(" | ", kbResolution.ProbedPaths.Take(5)) + (kbResolution.ProbedPaths.Count > 5 ? " ..." : string.Empty));
                            }
                            else
                            {
                                try
                                {
                                    await using IMcpClient kbClient = await McpClientFactory.CreateAsync(
                                        new StdioClientTransport(new()
                                        {
                                            Name = "kb-mcp-server",
                                            Command = kbResolution.Command!,
                                            Arguments = kbResolution.Arguments
                                        }));

                                    var kbToolList = await kbClient.ListToolsAsync().ConfigureAwait(false);
                                    var contentTool = kbToolList.FirstOrDefault(t => t.Name == "get_kb_content" || t.Name == "search_knowledge");
                                    if (contentTool is null)
                                    {
                                        disclaimers.Add("KB tools available but no recognized content tool (get_kb_content/search_knowledge)");
                                    }
                                    else if (contentTool.Name == "get_kb_content")
                                    {
                                        try
                                        {
                                            var invocation = await kbClient.CallToolAsync(contentTool.Name, new Dictionary<string, object?>()).ConfigureAwait(false);

                                            // Reflective extraction of first text content entry following MCP spec shape:
                                            // { content: [ { type: "text", text: "..." } ] }
                                            string? extracted = null;
                                            try
                                            {
                                                if (invocation is not null)
                                                {
                                                    var contentProp = invocation.GetType().GetProperty("Content", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                                                    var contentVal = contentProp?.GetValue(invocation);
                                                    if (contentVal is System.Collections.IEnumerable enumerable)
                                                    {
                                                        foreach (var item in enumerable)
                                                        {
                                                            if (item is null) continue;
                                                            // Try common property names
                                                            string? candidate = null;
                                                            var itemType = item.GetType();
                                                            var textProp = itemType.GetProperty("Text", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                                                            if (textProp?.GetValue(item) is string tp) candidate = tp;
                                                            else if (item is string s) candidate = s;
                                                            // Fallback: serialize item to JSON and attempt to parse {"text":"..."}
                                                            if (candidate is null)
                                                            {
                                                                try
                                                                {
                                                                    var json = JsonSerializer.Serialize(item);
                                                                    using var doc = JsonDocument.Parse(json);
                                                                    if (doc.RootElement.TryGetProperty("text", out var tEl) && tEl.ValueKind == JsonValueKind.String)
                                                                    {
                                                                        candidate = tEl.GetString();
                                                                    }
                                                                }
                                                                catch { /* swallow */ }
                                                            }
                                                            if (!string.IsNullOrWhiteSpace(candidate))
                                                            {
                                                                extracted = candidate;
                                                                break;
                                                            }
                                                        }
                                                    }
                                                    // Additional fallback: attempt full object JSON + search for first long string
                                                    if (extracted is null)
                                                    {
                                                        try
                                                        {
                                                            var json = JsonSerializer.Serialize(invocation);
                                                            using var doc = JsonDocument.Parse(json);
                                                            if (doc.RootElement.TryGetProperty("content", out var contentArray) && contentArray.ValueKind == JsonValueKind.Array)
                                                            {
                                                                foreach (var el in contentArray.EnumerateArray())
                                                                {
                                                                    if (el.ValueKind == JsonValueKind.Object && el.TryGetProperty("text", out var t) && t.ValueKind == JsonValueKind.String)
                                                                    {
                                                                        extracted = t.GetString();
                                                                        break;
                                                                    }
                                                                }
                                                            }
                                                        }
                                                        catch { /* ignore */ }
                                                    }
                                                }
                                            }
                                            catch (Exception exExtract)
                                            {
                                                Console.Error.WriteLine($"KB snippet extraction reflection error: {exExtract.Message}");
                                            }

                                            if (!string.IsNullOrWhiteSpace(extracted))
                                            {
                                                const int cap = 3000;
                                                bool trunc = extracted!.Length > cap;
                                                string snippet = trunc ? extracted.Substring(0, cap) + "...[truncated]" : extracted;
                                                kbResults.Add(new { snippet, truncated = trunc, source = Path.GetFileName(kbResolution.ResolvedPath) });
                                                usedKb = true;
                                            }
                                            else
                                            {
                                                disclaimers.Add("KB content tool returned no textual content (extraction failed)");
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
                                        disclaimers.Add("search_knowledge tool detected but query invocation deferred to later step");
                                    }
                                }
                                catch (Exception exLaunch)
                                {
                                    Console.Error.WriteLine($"KB launch/integration error: {exLaunch.Message}");
                                    disclaimers.Add("Answer generated without knowledge base due to KB launch error");
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
                // Named serviceId to explicitly identify the registered Azure OpenAI chat completion service
                const string azureServiceId = "primary-azure-openai"; // provenance: stable identifier for this service registration
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
                    // Register with explicit serviceId for provenance
                    kernelBuilder.AddAzureOpenAIChatCompletion(serviceId: azureServiceId, endpoint: endpoint!, deploymentName: deploymentName!, apiKey: apiKey!);
                    Kernel kernel = kernelBuilder.Build();
                    chatAgentReady = true;

                    // Build dynamic instructions including KB context (placed in Instructions so the user question stays clean)
                    string baseInstructions = "You are a concise domain Q&A assistant. Use provided KB snippets if present. Keep answer under 200 words. If no KB snippets, clearly state answer may lack domain grounding.";
                    string kbInstructionBlock = string.Empty;
                    if (usedKb && kbResults.Count > 0)
                    {
                        var joined = string.Join("\n---\n", kbResults.Select(r => (r as dynamic)?.snippet ?? r.ToString()));
                        kbInstructionBlock = $"\n\nKnowledge Base Snippets:\n{joined}\n\n";
                    }
                    else
                    {
                        disclaimers.Add("Answer generated without knowledge base grounding");
                    }

                    var execSettings = new OpenAIPromptExecutionSettings { Temperature = 0.2, ServiceId = azureServiceId };

                    ChatCompletionAgent agent = new()
                    {
                        Instructions = baseInstructions + kbInstructionBlock,
                        Name = "orchestrator_qa_agent", // no spaces per guidance
                        Kernel = kernel,
                        Arguments = new KernelArguments(execSettings)
                    };

                    AgentResponseItem<Microsoft.SemanticKernel.ChatMessageContent>? agentResponseItem = null;
                    bool usedAgent = false; bool usedFallback = false;
                    try
                    {
                        agentResponseItem = await agent.InvokeAsync(question).FirstAsync().ConfigureAwait(false);
                        answer = agentResponseItem?.Message?.ToString() ?? "(empty response)";
                        disclaimers.Add("ChatCompletionAgent used");
                        usedAgent = true;
                    }
                    catch (Exception agentEx)
                    {
                        Console.Error.WriteLine($"ChatCompletionAgent failure: {agentEx.Message}");
                        // Fallback: attempt a minimal direct prompt if agent path fails
                        try
                        {
                            var fallbackPrompt = $"{baseInstructions}{kbInstructionBlock}\nUser Question: {question}\nAnswer:";
                            var fallbackKernel = kernel; // reuse
                            var fallbackResult = await fallbackKernel.InvokePromptAsync(fallbackPrompt, new(execSettings)).ConfigureAwait(false);
                            answer = fallbackResult.ToString();
                            disclaimers.Add("Agent failed; fallback direct prompt used");
                            usedFallback = true;
                        }
                        catch (Exception fallbackEx)
                        {
                            Console.Error.WriteLine($"Fallback prompt also failed: {fallbackEx.Message}");
                            answer = "Answer generation failed due to internal error (agent + fallback).";
                            disclaimers.Add("Agent and fallback failed");
                        }
                    }
                    // Store minimal provenance context for later inclusion
                    provenanceData = new
                    {
                        provider = "azure-openai",
                        serviceId = azureServiceId,
                        deployment = deploymentName,
                        temperature = execSettings.Temperature,
                        mode = usedAgent ? "agent" : usedFallback ? "fallback-direct" : "unknown",
                        kbGrounded = usedKb,
                        explanations = new
                        {
                            serviceId = "Identifier assigned when registering the chat completion service in the Kernel (lets you choose among multiple models).",
                            deployment = "Azure OpenAI deployment name (maps to a specific model + configuration in your Azure resource).",
                            temperature = "Sampling variability 0..2; lower is more deterministic and focused."
                        }
                    };                    
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
            provenance = provenanceData,
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

    // -------------------------------------------------------------------------
    // Shared helper methods & lightweight data containers (added for refactor)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Represents the result of attempting to resolve the KB MCP server executable.
    /// </summary>
    private sealed record KbExecutableResolutionResult(
        bool Configured,
        bool Resolved,
        string? ResolvedPath,
        string? Command,
        string[] Arguments,
        List<string> ProbedPaths);

    /// <summary>
    /// Builds the base configuration (appsettings + env vars) uniformly for all tools.
    /// </summary>
    private static IConfiguration BuildBaseConfiguration() => new ConfigurationBuilder()
        .SetBasePath(AppContext.BaseDirectory)
        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
        .AddEnvironmentVariables()
        .Build();

    /// <summary>
    /// Centralized KB executable resolution used by both diagnostics and ask-domain tool.
    /// Normalizes quotes, supports relative + repo-root, and probes . / .exe / .dll.
    /// </summary>
    private static KbExecutableResolutionResult ResolveKbExecutable(IConfiguration config)
    {
        string? raw = config["KbMcpServer:ExecutablePath"];
        if (string.IsNullOrWhiteSpace(raw))
        {
            return new(false, false, null, null, Array.Empty<string>(), new List<string>());
        }

        // Normalize surrounding quotes (common when values copied into dev.env with quotes)
        string value = raw.Trim();
        if ((value.StartsWith('"') && value.EndsWith('"')) || (value.StartsWith('\'') && value.EndsWith('\'')))
        {
            value = value.Substring(1, value.Length - 2).Trim();
        }

        var probed = new List<string>();
        try
        {
            static IEnumerable<string> EnumerateRepoRootCandidates(string startingDirectory)
            {
                var dir = new DirectoryInfo(startingDirectory);
                for (int i = 0; i < 8 && dir is not null; i++)
                {
                    bool marker = false;
                    try { marker = dir.EnumerateFiles("*.sln").Any() || dir.EnumerateDirectories(".git").Any(); } catch { }
                    if (marker) yield return dir.FullName;
                    dir = dir.Parent;
                }
            }

            var baseDir = AppContext.BaseDirectory;
            var candidateBases = new List<string>();
            if (Path.IsPathFullyQualified(value))
            {
                candidateBases.Add(value);
            }
            else
            {
                candidateBases.Add(Path.GetFullPath(Path.Combine(baseDir, value)));
                foreach (var repoRoot in EnumerateRepoRootCandidates(baseDir))
                {
                    var repoRelative = Path.GetFullPath(Path.Combine(repoRoot, value));
                    if (!candidateBases.Contains(repoRelative, StringComparer.OrdinalIgnoreCase))
                    {
                        candidateBases.Add(repoRelative);
                    }
                }
            }

            string? chosenCommand = null;
            string[] chosenArgs = Array.Empty<string>();
            string? resolvedPath = null;

            foreach (var resolvedBase in candidateBases)
            {
                probed.Add(resolvedBase);
                probed.Add(resolvedBase + ".exe");
                probed.Add(resolvedBase + ".dll");

                bool existsExact = File.Exists(resolvedBase);
                bool existsExe = File.Exists(resolvedBase + ".exe");
                bool existsDll = File.Exists(resolvedBase + ".dll");

                if (existsExact)
                {
                    chosenCommand = resolvedBase;
                    resolvedPath = resolvedBase;
                }
                else if (existsExe)
                {
                    chosenCommand = resolvedBase + ".exe";
                    resolvedPath = resolvedBase + ".exe";
                }
                else if (existsDll)
                {
                    chosenCommand = "dotnet";
                    chosenArgs = new[] { resolvedBase + ".dll" };
                    resolvedPath = resolvedBase + ".dll";
                }

                if (chosenCommand is not null) break;
            }

            bool resolved = resolvedPath is not null;
            return new(true, resolved, resolvedPath, chosenCommand, chosenArgs, probed);
        }
        catch
        {
            return new(true, false, null, null, Array.Empty<string>(), probed);
        }
    }
}
