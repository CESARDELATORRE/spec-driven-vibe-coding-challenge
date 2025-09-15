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

    [McpServerTool, Description("Gets orchestrator diagnostics information (raw environment variables included).")]
    public static string GetOrchestratorDiagnosticsInformation()
    {
        // Minimal config snapshot
        var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";
        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables()
            .Build();

        bool fakeLlmMode = string.Equals(config["Orchestrator:UseFakeLlm"], "true", StringComparison.OrdinalIgnoreCase);
        string? kbPathConfig = config["KbMcpServer:ExecutablePath"];

        // Simple KB executable presence check (kept but no complex probing now)
        bool kbExecutableConfigured = !string.IsNullOrWhiteSpace(kbPathConfig);
        bool kbExecutableResolved = false;
        string? kbResolvedPath = null;
        if (kbExecutableConfigured)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(kbPathConfig))
                {
                    var candidate = Path.IsPathFullyQualified(kbPathConfig) ? kbPathConfig : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, kbPathConfig));
                    if (File.Exists(candidate)) { kbExecutableResolved = true; kbResolvedPath = candidate; }
                    else if (File.Exists(candidate + ".exe")) { kbExecutableResolved = true; kbResolvedPath = candidate + ".exe"; }
                    else if (File.Exists(candidate + ".dll")) { kbExecutableResolved = true; kbResolvedPath = candidate + ".dll"; }
                }
            }
            catch { }
        }

        // Raw environment variables (no masking, per user request)
        var env = Environment.GetEnvironmentVariables();
        var envVars = new List<object>();
        foreach (System.Collections.DictionaryEntry e in env)
        {
            envVars.Add(new { name = e.Key?.ToString() ?? string.Empty, value = e.Value?.ToString() ?? string.Empty });
        }
        var ordered = envVars
            .Select(x => (dynamic)x)
            .OrderBy(x => (string)x.name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var payload = new
        {
            status = "Alive",
            environment,
            fakeLlmMode,
            kbExecutableConfigured,
            kbExecutableResolved,
            kbResolvedPath = kbResolvedPath is null ? null : Path.GetFileName(kbResolvedPath),
            timestampUtc = DateTime.UtcNow.ToString("o"),
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
                var kbPathConfig = config["KbMcpServer:ExecutablePath"]; // overrideable via KbMcpServer__ExecutablePath
                if (string.IsNullOrWhiteSpace(kbPathConfig))
                {
                    disclaimers.Add("KB server path not configured (KbMcpServer__ExecutablePath)");
                }
                else
                {
                            // Resolve the KB server executable robustly.
                            // Strategy:
                            // 1. Treat configured value as either absolute or relative.
                            // 2. First attempt: relative to AppContext.BaseDirectory (existing behaviour).
                            // 3. Fallback: attempt relative to repository root (walk up until .sln or .git detected) then combine.
                            // 4. For each resolved base candidate, probe: exact, .exe, .dll.
                            // 5. Use first successful probe.

                            static IEnumerable<string> EnumerateRepoRootCandidates(string startingDirectory)
                            {
                                // Walk up 8 levels max looking for solution marker or .git
                                var dir = new DirectoryInfo(startingDirectory);
                                for (int i = 0; i < 8 && dir is not null; i++)
                                {
                                    bool marker = dir.EnumerateFiles("*.sln").Any() || dir.EnumerateDirectories(".git").Any();
                                    if (marker)
                                    {
                                        yield return dir.FullName;
                                    }
                                    dir = dir.Parent;
                                }
                            }

                            var baseDir = AppContext.BaseDirectory;
                            var candidateBases = new List<string>();

                            // If absolute provided, just use directly; else build relative candidates.
                            if (Path.IsPathFullyQualified(kbPathConfig))
                            {
                                candidateBases.Add(kbPathConfig);
                            }
                            else
                            {
                                // Provided relative path.
                                candidateBases.Add(Path.GetFullPath(Path.Combine(baseDir, kbPathConfig)));

                                // Repository-root relative fallback (only add if different)
                                foreach (var repoRoot in EnumerateRepoRootCandidates(baseDir))
                                {
                                    var repoRelative = Path.GetFullPath(Path.Combine(repoRoot, kbPathConfig));
                                    if (!candidateBases.Contains(repoRelative, StringComparer.OrdinalIgnoreCase))
                                    {
                                        candidateBases.Add(repoRelative);
                                    }
                                }
                            }

                            string? chosenCommand = null;
                            string[] chosenArgs = Array.Empty<string>();
                            string? chosenResolvedBase = null;
                            var probed = new List<string>();

                            foreach (var resolvedBase in candidateBases)
                            {
                                bool existsExact = File.Exists(resolvedBase);
                                bool existsDll = File.Exists(resolvedBase + ".dll");
                                bool existsExe = File.Exists(resolvedBase + ".exe");
                                probed.Add(resolvedBase);
                                probed.Add(resolvedBase + ".exe");
                                probed.Add(resolvedBase + ".dll");

                                if (existsExact)
                                {
                                    chosenCommand = resolvedBase; // already executable (or script)
                                    chosenResolvedBase = resolvedBase;
                                }
                                else if (existsExe)
                                {
                                    chosenCommand = resolvedBase + ".exe"; // Windows apphost
                                    chosenResolvedBase = resolvedBase + ".exe";
                                }
                                else if (existsDll)
                                {
                                    chosenCommand = "dotnet";
                                    chosenArgs = new[] { resolvedBase + ".dll" };
                                    chosenResolvedBase = resolvedBase + ".dll";
                                }

                                if (chosenCommand is not null) break; // Stop at first success
                            }

                            if (chosenCommand is null)
                            {
                                disclaimers.Add($"KB executable not found after probing {probed.Count} paths");
                                // Add a compact diagnostic entry listing first few probed paths.
                                disclaimers.Add("Probed paths sample: " + string.Join(" | ", probed.Take(5)) + (probed.Count > 5 ? " ..." : string.Empty));
                            }
                            else
                            {
                                try
                                {
                                    await using IMcpClient kbClient = await McpClientFactory.CreateAsync(
                                        new StdioClientTransport(new()
                                        {
                                            Name = "kb-mcp-server",
                                            Command = chosenCommand!,
                                            Arguments = chosenArgs
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
                                                kbResults.Add(new { snippet, truncated = trunc, source = Path.GetFileName(chosenResolvedBase) });
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
