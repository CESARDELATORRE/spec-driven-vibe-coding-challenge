# Implementation Plan for Orchestration Agent MCP Server

## Overview
This plan implements a simple MCP server that coordinates between Semantic Kernel's ChatCompletionAgent and KB MCP Server for single-turn question-answering. Following the provided code approach, the orchestration agent will use MCP client to connect to the KB server and integrate it directly with Semantic Kernel's ChatCompletionAgent for maximum simplicity.

## Prerequisites: Azure OpenAI Configuration
Before starting implementation, configure Azure OpenAI user secrets:

1. **Provision Azure OpenAI Service**: Deploy a model (e.g., gpt-4o-mini) in Azure OpenAI Services
2. **Collect Configuration**: From Azure portal, obtain:
   - AzureOpenAI:Endpoint
   - AzureOpenAI:DeploymentName  
   - AzureOpenAI:ApiKey
3. **Create User Secrets**: Create `secrets.json` at the user secrets location
   - **Note**: The GUID `8ca9129d-caec-411b-aa66-b43ef94e65c1` is arbitrary and can be changed
   - **Location**: `%APPDATA%\Microsoft\UserSecrets\{UserSecretsId}\secrets.json`
   - **Default for this project**: `%APPDATA%\Microsoft\UserSecrets\8ca9129d-caec-411b-aa66-b43ef94e65c1\secrets.json`
4. **Populate Secrets**:
   ```json
   {
     "AzureOpenAI:Endpoint": "https://your-endpoint-url/",
     "AzureOpenAI:DeploymentName": "your-model-deployment-name",
     "AzureOpenAI:ApiKey": "your-model-api-key"
   }
   ```

**Alternative**: Generate new GUID with `dotnet user-secrets init` in the project directory.

## Implementation Steps

- [ ] Step 1: Project Setup and Structure
  - **Task**: Create the orchestration agent project with proper folder structure, dependencies, and user secrets configuration
  - **Files**:
    - `src/orchestrator-agent/orchestrator-agent.csproj`: Main project file with MCP SDK, Semantic Kernel dependencies, and configurable user secrets ID
    - `src/orchestrator-agent/Program.cs`: Host builder with MCP server configuration using `Host.CreateEmptyApplicationBuilder`
  - **Dependencies**: .NET 9, MCP SDK, Semantic Kernel, Azure OpenAI connector. NuGet Packages:
    ```
    ModelContextProtocol
    Microsoft.SemanticKernel
    Microsoft.SemanticKernel.Agents.Core
    Microsoft.SemanticKernel.Connectors.OpenAI
    Microsoft.Extensions.Configuration.UserSecrets
    Microsoft.Extensions.Hosting
    ```
  - **User Intervention**: Choose to use provided GUID `8ca9129d-caec-411b-aa66-b43ef94e65c1` or generate new one with `dotnet user-secrets init`
  - **Pseudocode**:
    ```xml
    <!-- orchestrator-agent.csproj -->
    <Project Sdk="Microsoft.NET.Sdk">
      <PropertyGroup>
        <TargetFramework>net9.0</TargetFramework>
        <!-- Use provided GUID or generate new with: dotnet user-secrets init -->
        <UserSecretsId>8ca9129d-caec-411b-aa66-b43ef94e65c1</UserSecretsId>
      </PropertyGroup>
      <!-- Package references -->
    </Project>
    ```

- [ ] Step 2: MCP Tools Implementation
  - **Task**: Create MCP tools following the provided static class pattern, using ChatCompletionAgent directly with secure configuration
  - **Files**:
    - `src/orchestrator-agent/tools/OrchestratorTools.cs`: Static class with `[McpServerToolType]` containing both tools
  - **Dependencies**: MCP tool attributes, Semantic Kernel ChatCompletionAgent, configuration with user secrets
  - **Pseudocode**:
    ```csharp
    [McpServerToolType]
    public static class OrchestratorTools
    {
        [McpServerTool, Description("Ask domain question with KB lookup")]
        public static async Task<string> AskDomainQuestionAsync(
            string question, bool includeKb = true, int maxKbResults = 2)
        {
            // 1. Get secure configuration via ConfigurationBuilder with user secrets
            var config = new ConfigurationBuilder()
                .AddUserSecrets<Program>()
                .AddEnvironmentVariables()
                .Build();
            
            // 2. Validate Azure OpenAI configuration
            // 3. Create MCP client to KB server (if includeKb)
            // 4. Create kernel with Azure OpenAI using secrets
            // 5. Add KB tools to kernel as functions (if KB available)
            // 6. Create ChatCompletionAgent directly
            // 7. Invoke agent with user question
            // 8. Return formatted response with disclaimers
        }
        
        [McpServerTool, Description("Get orchestrator status")]
        public static async Task<string> GetOrchestratorStatusAsync()
        {
            // Simple status check including Azure OpenAI connectivity
        }
    }
    ```

- [ ] Step 3: KB MCP Client Integration (Inline)
  - **Task**: Implement KB MCP client connection directly in the MCP tool method
  - **Files**: No separate files - integrated into OrchestratorTools.cs
  - **Dependencies**: MCP SDK client components
  - **Pseudocode**:
    ```csharp
    // Inside AskDomainQuestionAsync method
    if (includeKb && !ShouldSkipKb(question))
    {
        try
        {
            string kbMcpServerPath = Path.GetFullPath(Path.Combine(
                AppContext.BaseDirectory, "../../../../mcp-server-kb-content-fetcher/bin/Debug/net9.0/mcp-server-kb-content-fetcher.exe"));
            
            await using IMcpClient kbClient = await McpClientFactory.CreateAsync(
                new StdioClientTransport(new() {
                    Name = "kb-mcp-server",
                    Command = kbMcpServerPath,
                    Arguments = Array.Empty<string>()
                }));
            
            var kbTools = await kbClient.ListToolsAsync();
            kernel.Plugins.AddFromFunctions("KBTools", 
                kbTools.Select(tool => tool.AsKernelFunction()));
            usedKb = true;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"KB unavailable: {ex.Message}");
            usedKb = false;
            disclaimers.Add("Answer generated without knowledge base");
        }
    }
    ```

- [ ] Step 4: Direct ChatCompletionAgent Usage with Secure Configuration
  - **Task**: Use ChatCompletionAgent directly in the MCP tool with Azure OpenAI secrets
  - **Files**: Integrated into OrchestratorTools.cs
  - **Dependencies**: Semantic Kernel, Azure OpenAI connector, user secrets
  - **Pseudocode**:
    ```csharp
    // Inside AskDomainQuestionAsync method - secure configuration validation
    if (config["AzureOpenAI:ApiKey"] is not { } apiKey)
    {
        return "Please configure AzureOpenAI:ApiKey in user secrets";
    }
    if (config["AzureOpenAI:Endpoint"] is not { } endpoint)
    {
        return "Please configure AzureOpenAI:Endpoint in user secrets";
    }
    if (config["AzureOpenAI:DeploymentName"] is not { } deploymentName)
    {
        return "Please configure AzureOpenAI:DeploymentName in user secrets";
    }

    var kernel = Kernel.CreateBuilder()
        .AddAzureOpenAIChatCompletion(endpoint, deploymentName, apiKey)
        .Build();
    
    // Add KB tools if available (from step 3)
    
    OpenAIPromptExecutionSettings executionSettings = new() {
        Temperature = 0,
        FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
    };
    
    ChatCompletionAgent agent = new() {
        Instructions = "Answer questions using available KB tools when relevant. Be concise and domain-focused.",
        Name = "DomainQA_Agent",
        Kernel = kernel,
        Arguments = new KernelArguments(executionSettings)
    };
    
    var response = await agent.InvokeAsync(question).FirstAsync();
    ```

- [ ] Step 5: Error Handling and Validation (Inline)
  - **Task**: Add comprehensive input validation, security checks, and error handling directly in tool methods
  - **Files**: Integrated into OrchestratorTools.cs
  - **Dependencies**: Configuration validation, logging, security validation
  - **Pseudocode**:
    ```csharp
    // Security validation - never log secrets
    if (config["AzureOpenAI:ApiKey"] is not { } apiKey)
    {
        string error = "Please provide a valid AzureOpenAI:ApiKey in user secrets";
        Console.Error.WriteLine(error);
        return error;
    }
    
    // Input validation
    if (string.IsNullOrWhiteSpace(question))
    {
        return "Question is required";
    }
    if (question.Length < 5)
    {
        return "Question must be at least 5 characters";
    }
    if (maxKbResults < 1 || maxKbResults > 3)
    {
        maxKbResults = Math.Clamp(maxKbResults, 1, 3);
    }
    
    // Simple heuristics for KB skip
    bool ShouldSkipKb(string question)
    {
        if (question.Length < 5) return true;
        if (Regex.IsMatch(question, @"^\s*(hi|hello|hey)\b.*", RegexOptions.IgnoreCase)) return true;
        return false;
    }
    ```

- [ ] Step 6: Program.cs Setup
  - **Task**: Configure MCP server startup following the working pattern
  - **Files**:
    - `src/orchestrator-agent/Program.cs`: Update with MCP server configuration using `Host.CreateEmptyApplicationBuilder`
  - **Dependencies**: Host builder, MCP server extensions
  - **Pseudocode**:
    ```csharp
    // Use the working pattern from provided code - minimal setup
    var builder = Host.CreateEmptyApplicationBuilder(settings: null);
    builder.Services
        .AddMcpServer()
        .WithStdioServerTransport()
        .WithToolsFromAssembly();
    
    await builder.Build().RunAsync();
    ```

- [ ] Step 7: Build and Run Application
  - **Task**: Ensure application builds and runs successfully with proper secrets configuration
  - **Files**: No new files - validation step
  - **Commands**:
    ```bash
    dotnet build src/orchestrator-agent/orchestrator-agent.csproj
    dotnet run --project src/orchestrator-agent/orchestrator-agent.csproj
    ```
  - **User Intervention**: Verify user secrets are configured before running
  - **Dependencies**: All previous steps completed, Azure OpenAI secrets configured

- [ ] Step 8: Unit Tests
  - **Task**: Create unit tests for MCP tools with mock configuration
  - **Files**:
    - `tests/orchestrator-agent.unit-tests/orchestrator-agent.unit-tests.csproj`: Test project
    - `tests/orchestrator-agent.unit-tests/tools/OrchestratorToolsTests.cs`: Tool behavior tests with mock secrets
  - **Dependencies**: xUnit, test fixtures for mock configuration, secure test patterns
  - **Pseudocode**:
    ```csharp
    // Test configuration validation without real secrets
    [Fact]
    public async Task AskDomainQuestion_MissingApiKey_ReturnsError()
    {
        // Test with empty configuration
        // Verify proper error message returned
        // Ensure no secrets logged
    }
    ```

- [ ] Step 9: Integration Tests
  - **Task**: Test MCP server coordination with real KB server using test secrets
  - **Files**:
    - `tests/orchestrator-agent.integration-tests/orchestrator-agent.integration-tests.csproj`: Integration test project
    - `tests/orchestrator-agent.integration-tests/McpServerIntegrationTests.cs`: End-to-end tool invocation tests
  - **Dependencies**: In-process test host, MCP test harness, real KB MCP server, test Azure OpenAI configuration
  - **User Intervention**: Configure test user secrets or environment variables for integration tests

- [ ] Step 10: Run All Tests
  - **Task**: Execute complete test suite to validate implementation with security requirements
  - **Files**: No new files - validation step
  - **Commands**:
    ```bash
    dotnet test tests/orchestrator-agent.unit-tests/orchestrator-agent.unit-tests.csproj
    dotnet test tests/orchestrator-agent.integration-tests/orchestrator-agent.integration-tests.csproj
    ```
  - **Dependencies**: All test projects completed, test secrets configured

## Key Implementation Details

### Secure Configuration Pattern (Steps 1, 4, 5)
```csharp
// Use ConfigurationBuilder with user secrets - never hardcode secrets
var config = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .AddEnvironmentVariables()
    .Build();

// Validate required configuration without logging sensitive data
if (config["AzureOpenAI:ApiKey"] is not { } apiKey)
{
    return "Please configure AzureOpenAI:ApiKey in user secrets";
}
```

### User Secrets Project Configuration
```xml
<PropertyGroup>
  <UserSecretsId>8ca9129d-caec-411b-aa66-b43ef94e65c1</UserSecretsId>
</PropertyGroup>
```

### Response Format with Security Considerations
```csharp
// Return structured response with disclaimers and usage flags
return JsonSerializer.Serialize(new {
    answer = response.Message.ToString(),
    usedKb = usedKb,
    kbResults = kbSnippets,
    disclaimers = disclaimers.ToArray(),
    tokensEstimate = estimatedTokens
}, new JsonSerializerOptions { WriteIndented = true });
```

### User Secrets Configuration Flexibility
```xml
<!-- Option 1: Use provided GUID for consistency with examples -->
<UserSecretsId>8ca9129d-caec-411b-aa66-b43ef94e65c1</UserSecretsId>

<!-- Option 2: Generate new GUID -->
<!-- Run: dotnet user-secrets init (generates new GUID automatically) -->
```

### Why This GUID?
- **Consistency**: Ensures all team members use same secrets location during development
- **Examples**: Aligns with provided code samples and documentation
- **Arbitrary**: No technical significance - any valid GUID works
- **Changeable**: Can be updated anytime by changing .csproj and moving secrets file

### Security Requirements
- Never log or expose API keys, endpoints, or sensitive configuration
- Use user secrets for all Azure OpenAI configuration
- Validate all configuration before use
- Provide clear error messages for missing configuration
- Implement graceful degradation when services unavailable

## Success Criteria
- MCP server starts successfully via STDIO transport using `Host.CreateEmptyApplicationBuilder` pattern
- Azure OpenAI configuration loaded securely from user secrets
- ask_domain_question tool handles sample AMG queries by coordinating KB lookup and LLM response
- ChatCompletionAgent directly integrates KB tools and provides contextual answers
- Graceful degradation when KB MCP Server offline
- Input validation prevents malformed requests
- No secrets exposed in logs or error messages
- Response time < 5 seconds for typical queries
- All unit and integration tests pass

## Notes
- The user secrets GUID `8ca9129d-caec-411b-aa66-b43ef94e65c1` is arbitrary and changeable
- For new projects, consider using `dotnet user-secrets init` to generate a fresh GUID
- All developers must use the same GUID for shared development environments
- The GUID has no relationship to Azure services - it's purely for local secrets organization
- Follow the working `Host.CreateEmptyApplicationBuilder` pattern from provided code
- Use `ConfigurationBuilder` with user secrets for secure configuration access
- Use ChatCompletionAgent directly without wrapper services for maximum simplicity
- Integrate KB MCP server as external process via STDIO transport
- Log to stderr to avoid MCP protocol interference, never log secrets
- Keep KB server path configurable for different environments
- Focus on single-turn interactions, defer conversation memory
- Avoid over-engineering - keep everything in the MCP tools class for prototype simplicity
- Security-first approach: validate configuration, handle missing secrets gracefully
