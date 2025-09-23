# Research: Orchestrator Agent Implementation

**Date**: September 23, 2025  
**Context**: Phase 0 research for orchestrator-agent implementation plan

🏗️ **Architecture Reference**: The Orchestrator Agent design and integration patterns are detailed in [Architecture & Technologies](../../architecture-technologies.md).

## Technical Decision Summary

This document consolidates the pre-resolved technical decisions from the architecture-technologies.md and related documentation that inform the orchestrator agent implementation.

## 1. MCP Integration Pattern

**Decision**: Use MCP .NET SDK 0.3.0-preview.4 with STDIO transport  
**Rationale**: 
- Aligns with constitutional simplicity principle
- Enables local execution testing with GitHub Copilot and Claude MCP clients (FR-011)
- Proven pattern from existing KB MCP Server implementation
- Avoids premature HTTP/SSE complexity

**Implementation Approach**:
```csharp
// Host setup pattern (from example-program)
var builder = Host.CreateEmptyApplicationBuilder(settings: null);
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();
await builder.Build().RunAsync();
```

**Alternatives Considered**: HTTP transport rejected due to prototype scope and added complexity.

## 2. Chat Agent Architecture

**Decision**: In-process Semantic Kernel ChatCompletionAgent (not external service)  
**Rationale**:
- Prototype scope favors co-location over distribution (Variant 1 from architecture doc)
- Avoids HTTP/process boundary complexity
- Enables direct Azure AI Foundry integration
- Future externalization path preserved for Variant 2+

**Implementation Approach** (embedded directly in MCP tool):
```csharp
// High-level pseudocode - add validation, error handling in final implementation
ChatCompletionAgent agent = new()
{
    Instructions = "Answer questions about Azure Managed Grafana using KB tools...",
    Name = "AMG_Domain_Agent",
    Kernel = kernel, // With KB tools as plugins
    Arguments = new KernelArguments(executionSettings),
};

var agentResponse = await agent.InvokeAsync(userQuestion).FirstAsync();
return agentResponse.Message.ToString();
```

**Key Pattern**: Chat agent created fresh for each request within the MCP tool method, avoiding service lifecycle complexity.

**Alternatives Considered**: External Chat Agent service rejected for prototype simplicity.

## 3. Knowledge Base Integration

**Decision**: MCP client connection to existing KB MCP Server  
**Rationale**:
- Leverages existing domain knowledge infrastructure
- Maintains separation of concerns (orchestration vs. knowledge)
- Proven MCP client pattern from example code
- Enables graceful degradation when KB unavailable (FR-007)

**Implementation Approach**:
```csharp
// KB MCP Client pattern
await using IMcpClient kbClient = await McpClientFactory.CreateAsync(
    new StdioClientTransport(new()
    {
        Name = "kb-mcp-server",
        Command = kbServerPath,
        Arguments = Array.Empty<string>()
    }));
```

**Alternatives Considered**: Direct file access rejected to maintain architectural boundaries.

## 4. Configuration Management

**Decision**: Direct environment variable validation + appsettings.json override support  
**Rationale**:
- Follows prototype pattern from example code
- Environment variables for secrets (Azure keys)
- appsettings.json for non-sensitive configuration
- Simple validation without DI complexity

**Implementation Pattern**:
```csharp
// High-level pseudocode - add validation, error handling in final implementation
var azureAiEndpoint = Environment.GetEnvironmentVariable("AZURE_AI_ENDPOINT");
var azureAiKey = Environment.GetEnvironmentVariable("AZURE_AI_KEY");
var kbMcpServerPath = Environment.GetEnvironmentVariable("KB_MCP_SERVER_PATH");

// Validate required configuration exists
if (string.IsNullOrEmpty(azureAiEndpoint) || string.IsNullOrEmpty(azureAiKey))
{
    Console.Error.WriteLine("Error: Azure AI configuration missing.");
    return 1;
}
```

**Configuration Sources**:
1. Environment variables (required for Azure credentials)
2. appsettings.json (optional overrides for non-sensitive settings)
3. Direct validation at startup before MCP server initialization

**Alternative**: IOptions\<T\> pattern rejected for prototype simplicity.

## 5. Error Handling Strategy

**Decision**: Graceful degradation with explicit transparency  
**Rationale**:
- Constitutional observability via simplicity
- Meets FR-016 (graceful error handling) and FR-007 (KB unavailable operation)
- Enables meaningful user feedback without system failure
- Supports debugging through structured logging

**Implementation Pattern**:
```csharp
// High-level pseudocode - add validation, error handling in final implementation
try
{
    // Primary KB lookup
    var kbResponse = await kbClient.CallToolAsync("get_kb_content", parameters);
    // Process with KB context
}
catch (Exception ex)
{
    // Graceful degradation - proceed without KB context
    return new DomainResponse
    {
        Answer = "KB unavailable, providing general response...",
        HasKnowledgeBase = false
    };
}
```

**Error Categories**:
1. **KB Connection Failures**: Graceful degradation with fallback response
2. **Azure AI Failures**: Structured error with retry logic
3. **Configuration Errors**: Fast-fail at startup with clear messages
4. **Validation Errors**: Structured response with specific guidance

**Alternatives Considered**: Retry mechanisms deferred to maintain prototype simplicity.

## 6. Response Structure Design

**Decision**: Structured JSON responses with transparency indicators  
**Rationale**:
- Meets FR-015 (structured responses with usage indicators)
- Enables FR-005 (KB grounding indication)
- Supports debugging and user trust
- Follows DomainResponse model pattern

**Response Schema Implementation**:
```csharp
// High-level pseudocode - add validation, error handling in final implementation
public class DomainResponse
{
    public string Answer { get; set; }
    public string ConfidenceLevel { get; set; } // High, Medium, Low
    public List<KnowledgeSnippet> KnowledgeSnippets { get; set; }
    public bool HasKnowledgeBase { get; set; }
    // + other transparency properties as needed
}

public class KnowledgeSnippet
{
    public string Content { get; set; }
    public string Source { get; set; }
    public double? RelevanceScore { get; set; }
}
```

**Transparency Features**:
- `HasKnowledgeBase`: Indicates if KB was accessible during response
- `KnowledgeSnippets`: Shows specific KB content used
- `ConfidenceLevel`: AI confidence assessment
- `Disclaimers`: Context-specific warnings or limitations

**Alternatives Considered**: Simple string responses rejected due to transparency requirements.

## 7. Testing Strategy

**Decision**: Three-tier testing approach  
**Rationale**:
- Constitutional test coverage where it matters
- Balances thoroughness with prototype constraints
- Enables fast feedback and regression prevention

**Testing Tiers**:
1. **Unit Tests**: Tool logic, validation, error handling (fast, isolated)
2. **Integration Tests**: MCP protocol compliance, KB client interaction
3. **Smoke Tests**: End-to-end scenarios from quickstart

**Tools**: xUnit 2.9.2, FluentAssertions 6.12.0, Microsoft.NET.Test.Sdk 17.12.0

**Alternatives Considered**: Manual testing only rejected due to constitutional requirements.

## 8. Performance Approach

**Decision**: Simple performance targets without optimization  
**Rationale**:
- Prototype scope prioritizes functionality over performance
- Clear targets enable validation without premature optimization
- Constraints from functional requirements (FR-004, implied from OLD spec)

**Targets**:
- Startup: <3s (from OLD spec technical criteria)
- Response: <10s typical domain questions (FR-004)
- Status: <100ms (from OLD spec FR-6)

**Alternatives Considered**: Advanced performance monitoring deferred to maintain simplicity.

## Implementation Readiness

✅ All major technical patterns resolved  
✅ Dependencies identified and versioned  
✅ Architecture alignment confirmed  
✅ Constitutional compliance validated  
✅ Ready for Phase 1 design artifacts

**Next Phase**: Generate data models, contracts, and quickstart scenarios based on these technical foundations.