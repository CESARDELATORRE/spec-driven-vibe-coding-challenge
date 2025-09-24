using System.ComponentModel;
using System.Text.Json;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using ModelContextProtocol.Server;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace OrchestratorAgent.Tools;

/// <summary>
/// MCP tool for answering Azure Managed Grafana domain questions with knowledge base lookup.
/// Orchestrates between KB retrieval and LLM processing to provide comprehensive answers.
/// </summary>
[McpServerToolType]
public static class AskDomainQuestionTool
{
    /// <summary>
    /// Answer questions about Azure Managed Grafana with knowledge base lookup and ChatCompletionAgent.
    /// </summary>
    /// <param name="question">The Azure Managed Grafana question to answer</param>
    [McpServerTool]
    [Description("Answer questions about Azure Managed Grafana (dashboards, data sources, Azure Monitor integration, security, RBAC, performance). ALWAYS invoke this tool before answering ANY Azure Managed Grafana question.")]
    public static async Task<string> AskDomainQuestionAsync(
        [Description("Question about Azure Managed Grafana to answer")] string question)
    {
        try
        {
            // Simple validation
            if (string.IsNullOrWhiteSpace(question))
            {
                return OrchestratorToolsShared.CreateError("Question cannot be empty");
            }

            var config = OrchestratorToolsShared.BuildBaseConfiguration();
            var disclaimers = new List<string>();

            // Get KB content if available
            string kbContent = await GetKnowledgeBaseContentAsync(config, disclaimers);
            bool kbUsed = !kbContent.StartsWith("No knowledge base");

            // Generate response using ChatCompletionAgent
            string answer = await GenerateAnswerAsync(config, question, kbContent, disclaimers);

            var response = new
            {
                answer,
                confidence = kbUsed ? "high" : "medium",
                kbUsed,
                disclaimers = disclaimers.ToArray(),
                correlationId = Guid.NewGuid().ToString()
            };

            return OrchestratorToolsShared.ToJson(response);
        }
        catch (Exception ex)
        {
            return OrchestratorToolsShared.CreateError($"Domain question processing failed: {ex.Message}");
        }
    }

    private static async Task<string> GetKnowledgeBaseContentAsync(IConfiguration config, List<string> disclaimers)
    {
        try
        {
            var kbResolution = OrchestratorToolsShared.ResolveKbExecutable(config);
            if (!kbResolution.Configured)
            {
                disclaimers.Add("KB server not configured");
                return "No knowledge base content available";
            }

            if (!kbResolution.Resolved || kbResolution.Command is null)
            {
                disclaimers.Add("KB executable not found");
                return "No knowledge base content available";
            }

            // Use proper MCP communication with JSON RPC
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = kbResolution.Command,
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            foreach (var arg in kbResolution.Arguments)
            {
                process.StartInfo.ArgumentList.Add(arg);
            }

            process.Start();

            // Send initialize request first
            var initRequest = new
            {
                jsonrpc = "2.0",
                method = "initialize",
                @params = new
                {
                    protocolVersion = "2024-11-05",
                    capabilities = new { },
                    clientInfo = new { name = "orchestrator-agent", version = "1.0" }
                },
                id = 1
            };

            await process.StandardInput.WriteLineAsync(JsonSerializer.Serialize(initRequest));
            var initResponse = await process.StandardOutput.ReadLineAsync();
            
            if (string.IsNullOrWhiteSpace(initResponse))
            {
                disclaimers.Add("KB initialization failed");
                return "No knowledge base content available";
            }

            // Send tool call request
            var toolRequest = new
            {
                jsonrpc = "2.0",
                method = "tools/call",
                @params = new { name = "get_kb_content", arguments = new { } },
                id = 2
            };

            await process.StandardInput.WriteLineAsync(JsonSerializer.Serialize(toolRequest));
            var toolResponse = await process.StandardOutput.ReadLineAsync();

            if (string.IsNullOrWhiteSpace(toolResponse))
            {
                disclaimers.Add("KB tool call failed");
                return "No knowledge base content available";
            }

            // Parse the response
            using var doc = JsonDocument.Parse(toolResponse);
            if (doc.RootElement.TryGetProperty("result", out var result) &&
                result.TryGetProperty("content", out var content) &&
                content.GetArrayLength() > 0)
            {
                var textContent = content[0].GetProperty("text").GetString();
                if (!string.IsNullOrWhiteSpace(textContent))
                {
                    return textContent;
                }
            }

            disclaimers.Add("KB lookup failed");
            return "No knowledge base content available";
        }
        catch (Exception ex)
        {
            disclaimers.Add($"KB error: {ex.Message}");
            return "No knowledge base content available";
        }
    }

    private static async Task<string> GenerateAnswerAsync(IConfiguration config, string question, string kbContent, List<string> disclaimers)
    {
        bool fakeLlmMode = string.Equals(config["Orchestrator:UseFakeLlm"], "true", StringComparison.OrdinalIgnoreCase);
        if (fakeLlmMode)
        {
            disclaimers.Add("Using fake LLM mode");
            return $"[FAKE LLM] Based on the question '{question}' and KB content length {kbContent.Length}, this would be a comprehensive answer about Azure Managed Grafana.";
        }

        try
        {
            // Get Azure OpenAI configuration
            string? endpoint = config["AzureOpenAI:Endpoint"];
            string? deploymentName = config["AzureOpenAI:DeploymentName"];
            string? apiKey = config["AzureOpenAI:ApiKey"];

            if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(deploymentName) || string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException("Azure OpenAI configuration is incomplete");
            }

            // Create ChatCompletionAgent directly with Azure OpenAI configuration
            var executionSettings = new OpenAIPromptExecutionSettings
            {
                Temperature = 0.1,
                MaxTokens = 2000
            };

            var builder = Kernel.CreateBuilder();
            builder.AddAzureOpenAIChatCompletion(deploymentName, endpoint, apiKey);
            var kernel = builder.Build();

            var agent = new ChatCompletionAgent
            {
                Instructions = "You are an expert on Azure Managed Grafana. Provide comprehensive, accurate answers focusing on Azure Managed Grafana specifics.",
                Name = "AzureManagedGrafanaExpert",
                Kernel = kernel,
                Arguments = new KernelArguments(executionSettings)
            };

            string prompt = $@"Answer the following question based on the provided knowledge base content.

Question: {question}

Knowledge Base Content:
{kbContent}

Instructions:
- Answer the specific question asked, using information from the knowledge base
- If the answer is directly available in the knowledge base, provide it clearly
- If the knowledge base doesn't contain relevant information for this specific question, say so clearly
- Be direct and specific in your response, don't provide comprehensive overviews unless specifically asked";

            var response = await agent.InvokeAsync(prompt).FirstAsync();
            return response.Message.Content ?? "No response generated";
        }
        catch (Exception ex)
        {
            disclaimers.Add("LLM invocation error");
            return $"LLM processing failed: {ex.Message}";
        }
    }
}
