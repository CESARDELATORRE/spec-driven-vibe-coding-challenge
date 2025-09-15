# Implementation Plan for Orchestration Agent MCP Server

## Overview
This plan implements a simple MCP server that coordinates between Semantic Kernel's ChatCompletionAgent and KB MCP Server for single-turn question-answering. Following the provided code approach, the orchestration agent will use MCP client to connect to the KB server and integrate it directly with Semantic Kernel's ChatCompletionAgent for maximum simplicity.


## Implementation Steps

### - [x] Step 1: Project Setup and Structure
  - **Task**: Create the orchestration agent project with proper folder structure and dependency references. Secrets will be supplied by environment variables (User Secrets optional for local dev only).
  - **Files**:
    - `src/orchestrator-agent/orchestrator-agent.csproj`: Main project file with MCP SDK + Semantic Kernel dependencies.
    - `src/orchestrator-agent/Program.cs`: Host builder with MCP server configuration using `Host.CreateEmptyApplicationBuilder`.
    - `src/orchestrator-agent/tools/OrchestratorTools.cs`: Initial MCP tools static class (status + placeholder AskDomainQuestion) - to be expanded in later steps.
  - **Status**: Implemented. Project compiles structure pending further tool logic (Steps 2+).
  - **Dependencies**: .NET 9, MCP SDK, Semantic Kernel, Azure OpenAI connector.
    ```
    ModelContextProtocol
    Microsoft.SemanticKernel
    Microsoft.SemanticKernel.Agents.Core
    Microsoft.SemanticKernel.Connectors.OpenAI
    Microsoft.Extensions.Hosting
    Microsoft.Extensions.Configuration.EnvironmentVariables
    Microsoft.Extensions.Configuration.UserSecrets (optional dev convenience)
    ```
  - **Environment Variable Contract**: (Required for runtime)
    - `AzureOpenAI__Endpoint`
    - `AzureOpenAI__DeploymentName`
    - `AzureOpenAI__ApiKey` (omit if using AAD later)
  - **Azure OpenAI Configuration (moved from prerequisites)**:
    1. Provision Azure OpenAI & deploy model (e.g., gpt-4o-mini).
    2. Collect values: endpoint URL, deployment name, API key (if not using AAD yet).
    3. Export environment variables (works for local shell, Docker, CI, Kubernetes):
       ```bash
       export AzureOpenAI__Endpoint="https://your-resource-name.openai.azure.com/"
       export AzureOpenAI__DeploymentName="gpt-4o-mini"
       export AzureOpenAI__ApiKey="YOUR_KEY_VALUE"
       ```
    4. Future: Replace `AzureOpenAI__ApiKey` with Managed Identity + Azure Key Vault provider (no code changes required due to configuration abstraction).
    5. Never bake these values into images or commit them. Use orchestrator/hosting platform secret management.
  - **Local Development Secret Handling (Recommended Options)**:
    - Create a git-ignored file at repo root named `dev.env` with:
      ```dotenv
      AzureOpenAI__Endpoint=https://your-resource-name.openai.azure.com/
      AzureOpenAI__DeploymentName=gpt-4o-mini
      AzureOpenAI__ApiKey=YOUR_KEY_VALUE
      ```
      Add `dev.env` (and optionally `*.env`) to `.gitignore`. Load before running:
      ```bash
      set -a; source dev.env; set +a   # bash / Git Bash
      ```
      PowerShell example (one-off):
      ```powershell
      Get-Content dev.env | ForEach-Object { if ($_ -match '^(.*?)=(.*)$') { $name=$matches[1]; $val=$matches[2]; [Environment]::SetEnvironmentVariable($name,$val) } }
      ```
    - Optional VS Code launch (avoid committing secrets): create `.vscode/launch.local.json` (git-ignored) with:
      ```json
      {
        "version": "0.2.0",
        "configurations": [
          {
            "name": "Run Orchestrator Agent",
            "type": "coreclr",
            "request": "launch",
            "program": "${workspaceFolder}/src/orchestrator-agent/bin/Debug/net9.0/orchestrator-agent.dll",
            "env": {
              "AzureOpenAI__Endpoint": "https://your-resource-name.openai.azure.com/",
              "AzureOpenAI__DeploymentName": "gpt-4o-mini",
              "AzureOpenAI__ApiKey": "YOUR_KEY_VALUE"
            }
          }
        ]
      }
      ```
      Do NOT store secrets in the standard `launch.json` if it is committed.
    - User Secrets intentionally omitted to enforce a single portable pattern (env vars) across local + container + CI.
  - **Pseudocode (csproj)**:
    ```xml
    <Project Sdk="Microsoft.NET.Sdk">
      <PropertyGroup>
        <TargetFramework>net9.0</TargetFramework>
      </PropertyGroup>
    </Project>
    ```

### - [x] Step 2: MCP Tools Implementation
  - **Task**: Create MCP tools following the static class pattern, using ChatCompletionAgent directly with configuration loaded from environment variables (User Secrets only if dev environment).
  - **Status**: Scaffold implemented. `AskDomainQuestionAsync` added with config loading, validation placeholders, structured JSON response, heuristic skip placeholder. Full KB + LLM logic deferred to Steps 3-4.
  - **Files**:
  - `src/orchestrator-agent/tools/OrchestratorTools.cs`: Static class with `[McpServerToolType]` containing both tools.
  - **Dependencies**: MCP tool attributes, Semantic Kernel ChatCompletionAgent, configuration providers (env vars + optional user secrets), LINQ.
  - **Pseudocode**:
  ```csharp
  [McpServerToolType]
  public static class OrchestratorTools
  {
    [McpServerTool, Description("Ask domain question with KB lookup")]
    public static async Task<string> AskDomainQuestionAsync(
      string question, bool includeKb = true, int maxKbResults = 2)
    {
      // 1. Build configuration (env vars first-class; user secrets optional for dev)
      var env = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";
      var configBuilder = new ConfigurationBuilder()
        .AddEnvironmentVariables();
      if (string.Equals(env, "Development", StringComparison.OrdinalIgnoreCase))
      {
        configBuilder.AddUserSecrets<Program>(optional: true); // no-op in container
      }
      var config = configBuilder.Build();

      // 2. Validate Azure OpenAI configuration
      // 3. Optionally connect to KB MCP server (includeKb + heuristics)
      // 4. Build kernel with Azure OpenAI connector
      // 5. Add KB tools to kernel if available
      // 6. Create ChatCompletionAgent
      // 7. Invoke agent with user question
      // 8. Return structured JSON response
    }
        
    [McpServerTool, Description("Get orchestrator status")]
    public static async Task<string> GetOrchestratorStatusAsync()
    {
      // Report availability of required env vars + (optional) KB path
    }
  }
  ```

### - [x] Step 3: KB MCP Client Integration (Inline)
  - **Task**: Implement KB MCP client connection directly in the MCP tool method using configurable server path
  - **Status**: Implemented. `AskDomainQuestionAsync` now launches KB MCP server process (if configured), lists tools, invokes `get_kb_content` when available, captures/truncates content, and adds disclaimers on degradation. `search_knowledge` invocation deferred to later refinement.
  - **Files**: No separate files - integrated into OrchestratorTools.cs
  - **Dependencies**: MCP SDK client components, configuration
  - **Pseudocode**:
    ```csharp
    // Inside AskDomainQuestionAsync method
    if (includeKb && !ShouldSkipKb(question))
    {
        try
        {
            // Get KB server path from configuration
            var kbServerPath = config["KbMcpServer:ExecutablePath"];
            if (string.IsNullOrEmpty(kbServerPath))
            {
                Console.Error.WriteLine("KB MCP Server path not configured");
                usedKb = false;
                disclaimers.Add("KB server not configured");
                return; // Continue without KB
            }

            string resolvedPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, kbServerPath));
            if (!File.Exists(resolvedPath))
            {
                Console.Error.WriteLine($"KB MCP Server executable not found: {resolvedPath}");
                usedKb = false;
                disclaimers.Add("KB server executable not found");
                return; // Continue without KB
            }
            
            await using IMcpClient kbClient = await McpClientFactory.CreateAsync(
                new StdioClientTransport(new() {
                    Name = "kb-mcp-server",
                    Command = resolvedPath,
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

### - [x] Step 4: Direct ChatCompletionAgent Usage with Secure Configuration
  - **Task**: Use ChatCompletionAgent directly with Azure OpenAI settings loaded from environment variables.
  - **Status**: Implemented. Semantic Kernel prompt invocation added; constructs prompt with optional KB snippets, system instructions, concise answer requirement (<200 words), graceful degradation when config missing or LLM fails.
  - **Files**: Integrated into `OrchestratorTools.cs`.
  - **Dependencies**: Semantic Kernel, Azure OpenAI connector.
  - **Pseudocode**:
  ```csharp
  // Inside AskDomainQuestionAsync - secure configuration validation via env vars
  string? apiKey = config["AzureOpenAI:ApiKey"]; // populated by AzureOpenAI__ApiKey env var
  string? endpoint = config["AzureOpenAI:Endpoint"]; // AzureOpenAI__Endpoint
  string? deploymentName = config["AzureOpenAI:DeploymentName"]; // AzureOpenAI__DeploymentName

  if (string.IsNullOrWhiteSpace(endpoint)) return "Missing AzureOpenAI__Endpoint environment variable";
  if (string.IsNullOrWhiteSpace(deploymentName)) return "Missing AzureOpenAI__DeploymentName environment variable";
  if (string.IsNullOrWhiteSpace(apiKey)) return "Missing AzureOpenAI__ApiKey environment variable (or configure AAD auth)";

  var kernel = Kernel.CreateBuilder()
    .AddAzureOpenAIChatCompletion(endpoint, deploymentName, apiKey)
    .Build();

  OpenAIPromptExecutionSettings execSettings = new() {
    Temperature = 0,
    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
  };

  var agent = new ChatCompletionAgent {
    Instructions = "Answer questions using available KB tools when relevant. Be concise and domain-focused.",
    Name = "DomainQA_Agent",
    Kernel = kernel,
    Arguments = new KernelArguments(execSettings)
  };
  ```

### - [x] Step 5: Error Handling and Validation (Inline)
  - **Task**: Add input validation, environment variable presence checks, and graceful degradation.
  - **Files**: Integrated into `OrchestratorTools.cs`.
  - **Dependencies**: Configuration validation, logging.
  - **Status**: Implemented. Added structured error responses (status=error, correlationId), minimum length & punctuation-only validation, maxKbResults clamping with diagnostics, greeting-based heuristic now configuration-aware, consistent disclaimer wording, and diagnostics enriched with requested/effective KB results + clamped flag.
  - **Pseudocode**:
  ```csharp
  // Never log actual secrets
  if (string.IsNullOrWhiteSpace(apiKey))
  {
    const string error = "Azure OpenAI API key missing (AzureOpenAI__ApiKey).";
    Console.Error.WriteLine(error);
    return error;
  }

  if (string.IsNullOrWhiteSpace(question)) return "Question is required";
  if (question.Length < 5) return "Question must be at least 5 characters";
  maxKbResults = Math.Clamp(maxKbResults, 1, 3);

  bool ShouldSkipKb(string q, IConfiguration cfg)
  {
    if (q.Length < 5) return true;
    var greetings = cfg.GetSection("GreetingPatterns").Get<string[]>() ?? Array.Empty<string>();
    if (greetings.Length == 0) return false; // fallback: do not skip
    var pattern = @"^\s*(" + string.Join("|", greetings.Select(Regex.Escape)) + @")\b.*";
    return Regex.IsMatch(q, pattern, RegexOptions.IgnoreCase);
  }
  ```

### - [x] Step 6: Program.cs Setup
  - **Task**: Configure MCP server startup following the working pattern
  - **Files**:
    - `src/orchestrator-agent/Program.cs`: Update with MCP server configuration using `Host.CreateEmptyApplicationBuilder`
  - **Dependencies**: Host builder, MCP server extensions
  - **Status**: Implemented. Added configuration layering (appsettings optional placeholder until Step 7), environment variables, dev user secrets, stderr-only logging, and MCP server registration.
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

### - [x] Step 7: Configuration File
  - **Task**: Provide non-secret configuration (KB path, patterns). Secrets remain only in env vars.
  - **Files**:
    - `src/orchestrator-agent/appsettings.json`: Non-sensitive defaults.
  - **Status**: Implemented. Added default KB executable path (relative), greeting patterns array consumed by heuristic, and logging levels. Program.cs now treats appsettings.json as required.
  - **Pseudocode**:
    ```json
    {
      "KbMcpServer": {
        "ExecutablePath": "../mcp-server-kb-content-fetcher/bin/Debug/net9.0/mcp-server-kb-content-fetcher" 
      },
      "GreetingPatterns": ["hi", "hello", "hey", "greetings"],
      "Logging": { "LogLevel": { "Default": "Information" } }
    }
    ```
  - **Override Strategy**: For dynamic paths in CI/containers, set `KbMcpServer__ExecutablePath` environment variable.

### - [x] Step 8: Build and Run Application
  - **Task**: Ensure application builds and runs successfully with proper secrets configuration
  - **Files**: No new files - validation step
  - **Commands**:
    ```bash
    dotnet build src/orchestrator-agent/orchestrator-agent.csproj
    dotnet run --project src/orchestrator-agent/orchestrator-agent.csproj
    ```
  - **User Intervention**: Ensure required environment variables are exported before running.
  - **Dependencies**: All previous steps completed, Azure OpenAI secrets configured
  - **Status**: Validated. Application launches with `appsettings.json` copied to output (csproj updated with CopyToOutput). No runtime errors without Azure OpenAI env vars (graceful disclaimers remain for LLM path).

### - [x] Step 9: Unit Tests
  - **Task**: Create unit tests for MCP tools with mock configuration
  - **Files**:
    - `tests/orchestrator-agent.unit-tests/orchestrator-agent.unit-tests.csproj`: Test project
    - `tests/orchestrator-agent.unit-tests/tools/OrchestratorToolsTests.cs`: Tool behavior tests with mock secrets
  - **Dependencies**: xUnit, test fixtures for mock configuration, secure test patterns
  - **Status**: Implemented. Added unit tests covering validation errors (empty, punctuation-only, too short), correlationId format, greeting heuristic, and maxKbResults clamping diagnostics.
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

### - [x] Step 10: Integration Tests
  - **Task**: Test MCP server coordination with real KB server using test secrets
  - **Files**:
    - `tests/orchestrator-agent.integration-tests/orchestrator-agent.integration-tests.csproj`: Integration test project
    - `tests/orchestrator-agent.integration-tests/StdioMcpClient.cs`: Minimal stdio harness (orchestrator)
    - `tests/orchestrator-agent.integration-tests/OrchestratorServerIntegrationTests.cs`: End-to-end tool invocation tests
  - **Dependencies**: Orchestrator project, xUnit, custom stdio client harness
  - **Status**: Implemented. Tests cover initialize + tools/list discovery and ask_domain_question degraded path (no Azure OpenAI config) with disclaimers & scaffold status.
  - **User Intervention**: Optional — set AzureOpenAI__* env vars to exercise LLM success path later.

### - [x] Step 11: Run All Tests
  - **Task**: Execute complete test suite to validate implementation with security requirements
  - **Files**: No new files - validation step
  - **Commands Executed**:
    - `dotnet test spec-driven-vibe-coding-challenge-orchestrator-code.sln` (full solution)
    - Individual integration suites run separately during Step 10 confirmation.
  - **Result**: PASS — 32 total tests (unit + smoke + integration across KB and orchestrator) succeeded with 0 failures.
  - **Dependencies**: All test projects present and green; Azure OpenAI env vars intentionally absent for degraded-path validation (no secrets required).
  - **Status**: Implemented.

## Key Implementation Details

### Secure Configuration Pattern (Steps 1, 4, 5)
```csharp
// Layering: appsettings.json (added in Program.cs), then env vars; user secrets only if Development
var env = builder.Environment.EnvironmentName;
builder.Configuration
  .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
  .AddJsonFile($"appsettings.{env}.json", optional: true)
  .AddEnvironmentVariables();
if (builder.Environment.IsDevelopment())
{
  builder.Configuration.AddUserSecrets<Program>(optional: true); // convenience only
}

// Later inside tool method:
string? apiKey = config["AzureOpenAI:ApiKey"]; // from AzureOpenAI__ApiKey
```

### Environment Variable Reference
| Purpose | Key | Notes |
|---------|-----|-------|
| Azure OpenAI endpoint | AzureOpenAI__Endpoint | Required |
| Azure OpenAI deployment | AzureOpenAI__DeploymentName | Required |
| Azure OpenAI API key | AzureOpenAI__ApiKey | Required unless using AAD |
| KB MCP server executable path | KbMcpServer__ExecutablePath | Optional override |

> Double underscore maps to section nesting in .NET configuration (e.g., AzureOpenAI__Endpoint -> AzureOpenAI:Endpoint).

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

### Local Dev Convenience (Optional User Secrets)
Developers may still use `dotnet user-secrets set AzureOpenAI:ApiKey <value>` etc. This is **never required** for containers and should not be relied on in CI/CD. Environment variables remain the contract.

### Why Not Rely Solely on User Secrets?
- **Non-portable**: User Secrets only exist on a dev machine file system.
- **Containers**: They are not present in container images unless manually copied (risk) or mounted (friction).
- **Scaling**: Environment-driven configuration is stateless and orchestration-friendly.
- **Future Upgrade Path**: Easy to introduce Azure Key Vault provider later without refactoring tool code.

### Security Requirements
- Never log or expose API keys or raw configuration values.
- Supply secrets via environment variables (production & containers). Optional: user secrets only in local Development.
- Validate all required configuration at startup/tool entry; fail fast with clear, non-sensitive error messages.
- Prepare for future secret provider layering (e.g., Azure Key Vault) without changing tool code (centralize config access).
- Implement graceful degradation when KB server is unavailable (respond without KB context + disclaimers).

## Notes
- Secrets are never baked into images, code, or committed files.
- Local optional user secrets do not replace env vars as the portability contract.
- Follow the working `Host.CreateEmptyApplicationBuilder` pattern.
- Use ChatCompletionAgent directly without extra abstraction.
- Integrate KB MCP server as external process via STDIO transport.
- Log to stderr to avoid MCP protocol interference, never log secrets
- Keep KB server path configurable for different environments
- Focus on single-turn interactions, defer conversation memory
- Avoid over-engineering - keep everything in the MCP tools class for prototype simplicity
- Security-first approach: validate configuration, handle missing secrets gracefully
- Use configuration-based KB server path for flexibility across environments
- Support relative paths resolved from application base directory
- Graceful degradation when KB server not found or misconfigured
