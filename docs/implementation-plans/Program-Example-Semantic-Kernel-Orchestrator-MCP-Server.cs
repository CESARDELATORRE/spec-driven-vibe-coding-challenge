using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Net.Http;
using System.Text;
using ModelContextProtocol.Server;
using ModelContextProtocol;
using ModelContextProtocol.Client; // Added namespace for MCP-related functionality
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Connectors.OpenAI;


// Works with MCP
var builder = Host.CreateEmptyApplicationBuilder(settings: null);
builder.Services
            .AddMcpServer()
            .WithStdioServerTransport()
            .WithToolsFromAssembly();

/*
// Dows not work with MCP
var builder = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((context, config) =>
    {
        config.AddUserSecrets<Program>();
    })
    .ConfigureServices((context, services) =>
    {
        services.AddMcpServer()
                .WithStdioServerTransport()
                .WithToolsFromAssembly();
    });
*/

await builder.Build().RunAsync();

[McpServerToolType]
public static class WorkflowOrchestratorTool
{
    [McpServerTool, Description("Runs a simple workflow orchestration and returns the concatenated output.")]
    public static async Task<string> RunWorkflowOrchestrationAsync()
    {
        var config = new ConfigurationBuilder()
                        .AddUserSecrets<Program>()
                        .AddEnvironmentVariables()
                        .Build();

        var result = new StringBuilder();

        if (config["AzureOpenAI:ApiKey"] is not { } apiKey)
        {
            string error = "Please provide a valid AzureOpenAI:ApiKey to run this sample.";
            Console.Error.WriteLine(error);
            return error;
        }

        if (config["AzureOpenAI:DeploymentName"] is not { } deploymentName)
        {
            string error = "Please provide a valid AzureOpenAI:DeploymentName to run this sample.";
            Console.Error.WriteLine(error);
            return error;
        }

        if (config["AzureOpenAI:Endpoint"] is not { } endpoint)
        {
            string error = "Please provide a valid AzureOpenAI:Endpoint to run this sample.";
            Console.Error.WriteLine(error);
            return error;
        }

        // Check env vars
        result.AppendLine($"AzureOpenAI:Endpoint: {endpoint}" );
        result.AppendLine($"AzureOpenAI:DeploymentName: {deploymentName}");
        result.AppendLine($"AzureOpenAI:ApiKey: {apiKey}");

        result.AppendLine($"Base Directory of Orchestrator: {AppContext.BaseDirectory}");

        string mcpClientFilePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..\\..\\..\\..", "agent-step-1-sk-mcp-csharp/bin/Debug/net9.0/agent-step-1-sk-mcp-csharp.exe"));
        result.AppendLine($"MCP Client FilePath: {mcpClientFilePath}");

        // Create MCP client
        await using IMcpClient mcpClientStep1 = await McpClientFactory.CreateAsync(
                                                        new StdioClientTransport(
            new()
            {
                Name = "agent-step-1-sk-mcp-csharp",
                Command = mcpClientFilePath,  
                Arguments = Array.Empty<string>()
            }));

        // Retrieve tools
        var step1Tools = await mcpClientStep1.ListToolsAsync().ConfigureAwait(false);

        
        result.AppendLine("Available tools:");
        // Log the available tools
        foreach (var tool in step1Tools)
        {
            result.AppendLine($"Tool Name: {tool.Name}, Description: {tool.Description}");
            
            //Console.WriteLine($"Tool Name: {tool.Name}, Description: {tool.Description}");
        }

        // Create a Semantic Kernel instance 
        var kernelBuilder = Kernel.CreateBuilder();
        kernelBuilder.Services
            .AddLogging(c =>
            {
                c.AddDebug().SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Information);
                c.AddConsole(); // Add console logging
            });

        kernelBuilder.Services.AddAzureOpenAIChatCompletion(
            endpoint: endpoint,
            deploymentName: deploymentName,
            apiKey: apiKey);

        Kernel kernel = kernelBuilder.Build();

        // Add Step1Tools to Semantic Kernel
#pragma warning disable SKEXP0001
        kernel.Plugins.AddFromFunctions("Step1Tools", step1Tools.Select(tool => tool.AsKernelFunction()));
#pragma warning restore SKEXP0001

        // Enable automatic function calling
#pragma warning disable SKEXP0001
        OpenAIPromptExecutionSettings executionSettings = new()
        {
            Temperature = 0,
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(options: new() { RetainArgumentTypes = true })
        };
#pragma warning restore SKEXP0001

        // 1. Run InvokePrompt to summarize what's returned by Step1Tools.ExecuteStep1 tool
        var prompt = "Summarize in ten words the content returned by Step1Tools.ExecuteStep1 tool: ";
        var promptResult = await kernel.InvokePromptAsync(prompt, new(executionSettings)).ConfigureAwait(false);
        result.AppendLine($"Prompt: {prompt}");
        result.AppendLine($"Prompt's result against Step1Tool: {promptResult}");


        // 2.

        // Using a prompt to make a question about the content returned by Step1Tools.ExecuteStep1
        var promptQA = "Answer questions about content returned by the tool Step1Tools.ExecuteStep1. What's the name of the Job Position?";
        var promptQAResult = await kernel.InvokePromptAsync(promptQA, new(executionSettings)).ConfigureAwait(false);
        result.AppendLine($"Prompt QA: {promptQA}");
        result.AppendLine($"Prompt QA result against Step1Tool: {promptQAResult}");


        // 3. 
        // Use ChatCompletionAgent to answer questions about the content returned by Step1Tools.ExecuteStep1
        // Using ChatCompletionAgent

        ChatCompletionAgent agent = new()
        {
            Instructions = "Answer questions about content returned by the tool Step1Tools.ExecuteStep1.",
            Name = "QA_Agent_for_Step1Tools.ExecuteStep1", //Name must not have spaces or will violate the expected pattern.
            Kernel = kernel,
            Arguments = new KernelArguments(executionSettings),
        };

        // Declare agentResponseItem with the correct type
        AgentResponseItem<Microsoft.SemanticKernel.ChatMessageContent>? agentResponseItem = null;
        var qaAgentResponse = string.Empty;
        try
        {
            // Respond to user input, invoking functions where appropriate.
            agentResponseItem = await agent.InvokeAsync("What's the name of the Job Position?").FirstAsync();
            qaAgentResponse = agentResponseItem.Message.ToString(); // Use the Message property to access the content
            //Console.WriteLine($"\n\nResponse from QA Agent:\n{qaAgentResponse}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error during ChatCompletionAgent execution: {ex.Message}");
            Console.Error.WriteLine("Raw message parsing failed. Logging raw message for debugging:");
            Console.Error.WriteLine(agentResponseItem?.Message?.ToString() ?? "No message available");
            throw;
        }
        

        // Define the other steps of the workflow
        var step1 = "Step 1 execution: " + promptResult.ToString();
        var step2 = "Step 2 execution: " + promptQAResult.ToString();
        var step3 = "Step 3 execution: " + qaAgentResponse;
        //var step3 = "Step 3 execution";

        // Concatenate the outputs in an understandable way
        //var result = new StringBuilder();
        result.AppendLine("Workflow Orchestration Output:");
        result.AppendLine(step1); 
        result.AppendLine(step2);
        result.AppendLine(step3);

        // Ensure results are returned for VS Code Chat Agent Mode
        var finalResult = result.ToString();
        //Console.WriteLine(finalResult); // Log results to console
        return finalResult;
    }
}

