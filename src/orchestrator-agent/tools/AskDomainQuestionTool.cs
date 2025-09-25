using System.ComponentModel;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using ModelContextProtocol.Server;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
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

            // Get KB content if available by using the decoupled KB MCP server
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
            if (!kbResolution.Configured || !kbResolution.Resolved || kbResolution.Command is null)
            {
                disclaimers.Add("KB server not available");
                return "No knowledge base content available";
            }

            // Use MCP client factory - the proper way
            await using var mcpClient = await McpClientFactory.CreateAsync(new StdioClientTransport(new()
            {
                Name = "kb-content-fetcher",
                Command = kbResolution.Command,
                Arguments = kbResolution.Arguments.ToArray()
            }));

            // Call the KB tool and extract the raw content directly
            var result = await mcpClient.CallToolAsync("get_kb_content", new Dictionary<string, object?>());
            var contentBlock = result.Content.FirstOrDefault();
            
            if (contentBlock != null)
            {
                // Access the Text property directly from TextContentBlock
                if (contentBlock is ModelContextProtocol.Protocol.TextContentBlock textBlock)
                {
                    var jsonResponse = textBlock.Text;
                    if (!string.IsNullOrWhiteSpace(jsonResponse))
                    {
                        using var doc = JsonDocument.Parse(jsonResponse);
                        if (doc.RootElement.TryGetProperty("content", out var rawContent))
                        {
                            return rawContent.GetString() ?? "No knowledge base content available";
                        }
                    }
                }
            }
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
