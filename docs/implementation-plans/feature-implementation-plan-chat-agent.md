# Chat Agent Implementation Plan

## Overview
This document provides a step-by-step implementation plan for the Chat Agent feature based on the functional specification.

**Prototype Simplification**: For the initial prototype/POC, consider implementing the Chat Agent in-process within the Orchestrator Agent using Semantic Kernel's ChatCompletionAgent. This approach:
- Eliminates the need for a separate MCP server project
- Avoids inter-process communication complexity
- Simplifies deployment and debugging
- Can be easily extracted into a separate service in future iterations

## Implementation Steps

### Step 1: Evaluate Implementation Approach
**Objective**: Decide between in-process or separate MCP server implementation

**Tasks**:
1. Review current orchestrator implementation
2. Assess complexity of adding in-process chat capabilities
3. Document decision and rationale

**For In-Process Implementation**:
- Skip to Step 8 (implement within orchestrator project)
- Use Semantic Kernel's ChatCompletionAgent
- Focus on FR-1 through FR-5 within existing orchestrator

**For Separate MCP Server** (future consideration):
- Continue with Steps 2-7 for full MCP server setup

### Step 2: Project Setup (If Separate MCP Server)
**Objective**: Create the Chat Agent MCP server project structure

**Tasks**:
1. Create new .NET 8 project: `src/chat-agent/`
2. Add project file: `chat-agent.csproj`
3. Add required NuGet packages:
   - `Microsoft.Extensions.Hosting`
   - `McpDotNet`
   - `Microsoft.SemanticKernel`
   - `Microsoft.Extensions.Configuration`
   - `Microsoft.Extensions.Logging`

### Step 3: Configuration Setup (If Separate MCP Server)
**Objective**: Set up configuration for Azure AI Foundry integration

**Tasks**:
1. Create `appsettings.json` with Azure AI settings structure
2. Set up environment variable support for secrets
3. Create configuration models for typed access

### Step 4: Semantic Kernel Integration (If Separate MCP Server)
**Objective**: Integrate Semantic Kernel for LLM capabilities

**Tasks**:
1. Configure Semantic Kernel with Azure OpenAI
2. Set up ChatCompletionService
3. Create prompt templates for AMG domain

### Step 5: MCP Tool Implementation (If Separate MCP Server)
**Objective**: Implement MCP tools for chat functionality

**Tasks**:
1. Create `ProcessQueryTool` for handling user queries
2. Implement tool attributes and parameters
3. Add response structure with confidence levels

### Step 6: Chat Service Implementation (If Separate MCP Server)
**Objective**: Implement core chat processing logic

**Tasks**:
1. Create `IChatService` interface
2. Implement chat service with LLM integration
3. Add error handling and fallback responses

### Step 7: MCP Server Host (If Separate MCP Server)
**Objective**: Set up MCP server host

**Tasks**:
1. Configure MCP server in Program.cs
2. Set up STDIO transport
3. Add health check endpoints

### Step 8: In-Process Implementation (Recommended for Prototype)
**Objective**: Add chat capabilities within orchestrator

**Tasks**:
1. Add Semantic Kernel NuGet package to orchestrator project
2. Create `IChatService` interface in orchestrator
3. Implement `ChatService` using ChatCompletionAgent
4. Integrate chat service into `AskDomainQuestionTool`
5. Configure Azure AI Foundry settings in existing configuration

### Step 9: Testing
**Objective**: Validate chat functionality

**Tasks**:
1. Create unit tests for chat service
2. Add integration tests for LLM interaction
3. Test error handling scenarios
4. Validate AMG domain responses

### Step 10: Documentation
**Objective**: Document the implementation

**Tasks**:
1. Update orchestrator README with chat configuration
2. Document Azure AI Foundry setup requirements
3. Add example usage and test queries

## Implementation Notes

### For In-Process Implementation
- Leverage existing orchestrator infrastructure
- Share configuration and logging setup
- Direct method calls instead of MCP communication
- Easier debugging and deployment

### For Future MCP Server Implementation
- Follow established patterns from KB MCP Server
- Ensure consistent error handling
- Plan for migration path from in-process

## Dependencies
- Orchestrator Agent (for in-process approach)
- Azure AI Foundry account and API keys
- Semantic Kernel SDK

## Success Criteria
- Chat service successfully processes AMG queries
- Responses are contextually appropriate
- Error handling works gracefully
- Integration with orchestrator is seamless

---

**Document Version**: 1.0  
**Last Updated**: September 2025  
**Status**: Ready for implementation