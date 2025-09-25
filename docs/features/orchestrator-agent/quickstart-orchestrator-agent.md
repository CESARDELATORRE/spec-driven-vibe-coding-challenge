# Quickstart: Orchestrator Agent

**Date**: September 23, 2025  
**Context**: Phase 1 quickstart validation scenarios for orchestrator-agent implementation

🏗️ **Architecture Reference**: The Orchestrator Agent design and integration patterns are detailed in [Architecture & Technologies](../../architecture-technologies.md).

## Overview

This document provides end-to-end validation scenarios that demonstrate the orchestrator agent's core functionality. These scenarios validate the acceptance criteria from the feature specification and serve as integration test templates.

## Prerequisites

### Required Configuration
1. **Azure AI Foundry Setup**:
   ```bash
   # Set environment variables (or use dev.env file)
   export AzureOpenAI__ApiKey="your-azure-openai-key"
   export AzureOpenAI__Endpoint="https://your-resource.openai.azure.com/"
   export AzureOpenAI__DeploymentName="your-model-deployment"
   ```

2. **Knowledge Base MCP Server**:
   ```bash
   # Ensure KB MCP Server is built and available
   dotnet build src/mcp-server-kb-content-fetcher/mcp-server-kb-content-fetcher.csproj
   ```

3. **MCP Client Environment**:
   - GitHub Copilot with MCP support, OR
   - Claude Desktop with MCP configuration, OR  
   - MCP CLI testing tool

4. **MCP Configuration**:
   ```json
   // Example mcp.json configuration for MCP clients
   {
     "mcpServers": {
       "orchestrator-agent": {
         "command": "dotnet",
         "args": ["run", "--project", "src/orchestrator-agent/orchestrator-agent.csproj"],
         "cwd": "/path/to/spec-driven-vibe-coding-challenge-WIP"
       }
     }
   }
   ```

### Build and Setup
```bash
# Build the orchestrator agent (must be built before MCP client can launch it)
dotnet build src/orchestrator-agent/orchestrator-agent.csproj

# The orchestrator agent is launched automatically by MCP clients
# No direct execution needed - MCP clients handle process lifecycle
```

## Validation Scenarios

### Scenario 1: Valid AMG Question with Knowledge Base Available
**Acceptance Criteria**: AS-1 from specification  
**Goal**: Verify domain-grounded responses using knowledge base content

**Setup**:
1. Ensure KB MCP Server is running with AMG content
2. Configure orchestrator agent in MCP client (GitHub Copilot/Claude)
3. Verify KB connectivity through status check

**Test Steps**:
1. **Action**: Ask domain question
   ```
   Tool: ask_domain_question
   Input: { "question": "What are the key security features of Azure Managed Grafana?" }
   ```

2. **Expected Response Structure**:
   ```json
   {
     "answer": "Azure Managed Grafana provides several key security features...",
     "usedKb": true,
     "kbResults": [
       {
         "content": "...security-related content from KB...",
         "source": "mcp-server-kb-content-fetcher.exe",
         "truncated": false,
         "retrievalTimestamp": "2025-09-23T..."
       }
     ],
     "disclaimers": [],
     "tokensEstimate": 150,
     "diagnostics": {
       "environment": "Development",
       "endpointConfigured": true,
       "deploymentConfigured": true,
       "apiKeyConfigured": true,
       "attemptedKb": true,
       "heuristicSkipKb": false,
       "chatAgentReady": true
     },
     "provenance": {
       "provider": "azure-openai",
       "mode": "agent",
       "kbGrounded": true
     },
     "status": "success"
   }
   ```

3. **Validation Criteria**:
   - ✅ `usedKb` is `true`
   - ✅ `kbResults` contains relevant AMG content
   - ✅ `answer` mentions specific AMG security features
   - ✅ Response length is 50-200 words
   - ✅ `provenance.kbGrounded` is `true`
   - ✅ Response time < 10 seconds

### Scenario 2: Health Check and Status Information  
**Acceptance Criteria**: AS-2 from specification  
**Goal**: Verify orchestrator status monitoring functionality

**Test Steps**:
1. **Action**: Request orchestrator status
   ```
   Tool: get_orchestrator_status
   Input: {}
   ```

2. **Expected Response Structure**:
   ```json
   {
     "orchestratorHealth": "Healthy",
     "kbServerStatus": "Connected", 
     "chatAgentStatus": "Ready",
     "version": "1.0.0",
     "timestamp": "2025-09-23T10:30:00Z",
     "environment": "Development",
     "uptime": "00:05:23",
     "dependencies": {
       "azureOpenAI": {
         "configured": true,
         "endpoint": "https://*****.openai.azure.com/",
         "deployment": "gpt-4o-mini-model"
       },
       "kbMcpServer": {
         "connected": true,
         "serverPath": "..../mcp-server-kb-content-fetcher.exe",
         "availableTools": ["get_kb_info", "get_kb_content"]
       }
     },
     "performance": {
       "averageResponseTime": 2500,
       "totalRequests": 5,
       "successfulRequests": 5,
       "failedRequests": 0
     }
   }
   ```

3. **Validation Criteria**:
   - ✅ All health statuses are positive (Healthy/Connected/Ready)
   - ✅ Version follows semantic versioning
   - ✅ Dependencies show proper configuration
   - ✅ Response time < 100ms
   - ✅ Performance metrics are reasonable

### Scenario 3: Knowledge Base Unavailable Graceful Degradation
**Acceptance Criteria**: AS-4 from specification  
**Goal**: Verify system continues operating when KB is unavailable

**Setup**:
1. Stop KB MCP Server (simulate unavailability)
2. Ensure orchestrator agent is configured in MCP client

**Test Steps**:
1. **Action**: Ask domain question with KB unavailable
   ```
   Tool: ask_domain_question  
   Input: { "question": "How does Azure Managed Grafana handle data retention?" }
   ```

2. **Expected Response Structure**:
   ```json
   {
     "answer": "Based on general Azure service patterns...",
     "usedKb": false,
     "kbResults": [],
     "disclaimers": ["Knowledge base was not available for this response"],
     "tokensEstimate": 120,
     "diagnostics": {
       "attemptedKb": true,
       "heuristicSkipKb": false,
       "chatAgentReady": true
     },
     "provenance": {
       "provider": "azure-openai", 
       "mode": "agent",
       "kbGrounded": false
     },
     "status": "success"
   }
   ```

3. **Validation Criteria**:
   - ✅ `usedKb` is `false`
   - ✅ `kbResults` is empty array
   - ✅ `disclaimers` contains KB unavailability warning
   - ✅ `answer` still provides reasonable response
   - ✅ `provenance.kbGrounded` is `false`
   - ✅ No system failure or error

### Scenario 4: Input Validation and Error Handling
**Acceptance Criteria**: AS-5 from specification  
**Goal**: Verify proper validation of empty/invalid questions

**Test Steps**:
1. **Action**: Submit empty question
   ```
   Tool: ask_domain_question
   Input: { "question": "" }
   ```

2. **Expected Error Response**:
   ```json
   {
     "error": {
       "code": "VALIDATION_ERROR",
       "message": "Question is required",
       "correlationId": "req-12345",
       "details": {
         "field": "question",
         "constraint": "minLength",
         "value": ""
       }
     }
   }
   ```

3. **Validation Criteria**:
   - ✅ Clear validation error returned
   - ✅ Specific error message: "Question is required"
   - ✅ Error categorized as `VALIDATION_ERROR`
   - ✅ No system crash or unhandled exception

### Scenario 5: Content Truncation Handling
**Acceptance Criteria**: AS-7 from specification  
**Goal**: Verify large content is properly truncated with indication

**Setup**:
1. Ensure KB contains large content that exceeds limits
2. Configure truncation threshold for testing

**Test Steps**:
1. **Action**: Ask question that retrieves large content
   ```
   Tool: ask_domain_question
   Input: { "question": "Tell me everything about Azure Managed Grafana configuration options" }
   ```

2. **Expected Response Elements**:
   ```json
   {
     "kbResults": [
       {
         "content": "Large content text...[truncated]",
         "truncated": true,
         "originalLength": 5000
       }
     ],
     "diagnostics": {
       "kbResultsClamped": true
     }
   }
   ```

3. **Validation Criteria**:
   - ✅ `truncated` flag is `true` for large content
   - ✅ Content ends with truncation indicator
   - ✅ `originalLength` > content length
   - ✅ System handles large content without failure

## Performance Validation

### Response Time Testing
```bash
# Response times are measured through MCP client interactions
# Target: < 10 seconds for domain questions
# Target: < 100ms for status checks
# (Times measured from MCP client request to response)
```

### Load Testing (Optional)
```bash
# Test multiple consecutive requests through MCP client
# Validation: No unhandled exceptions or degraded performance
```

## Troubleshooting Guide

### Common Issues and Solutions

1. **Azure AI Configuration Errors**:
   ```
   Error: "Please provide a valid AzureOpenAI:ApiKey"
   Solution: Verify environment variables are set correctly
   ```

2. **KB MCP Server Connection Issues**:
   ```
   Status: kbServerStatus = "Disconnected"
   Solution: Ensure KB MCP Server is built and executable path is correct
   ```

3. **MCP Protocol Issues**:
   ```
   Error: Tool discovery fails
   Solution: Verify STDIO transport and MCP client compatibility
   ```

### Debugging Commands
```bash
# Check environment variables
env | grep AzureOpenAI

# Verify KB server build status
dotnet build src/mcp-server-kb-content-fetcher/mcp-server-kb-content-fetcher.csproj

# Verify orchestrator agent build status
dotnet build src/orchestrator-agent/orchestrator-agent.csproj

# For development/testing only - direct STDIO testing
# (NOT for normal operation - MCP clients handle process lifecycle)
dotnet run --project src/orchestrator-agent/orchestrator-agent.csproj --verbosity normal
```

## Success Criteria Summary

✅ **Functional Validation**:
- Domain questions return relevant, grounded answers
- Status checks provide comprehensive health information
- System degrades gracefully when dependencies unavailable
- Input validation prevents invalid requests
- Large content is properly truncated

✅ **Performance Validation**: 
- Response times meet targets (<10s domain, <100ms status)
- System handles multiple consecutive requests
- No memory leaks or resource exhaustion

✅ **Integration Validation**:
- MCP protocol compliance with GitHub Copilot/Claude
- KB MCP Server integration functions correctly
- Azure AI Foundry integration works with proper configuration

**Next Phase**: Implement tasks following this quickstart as integration test template.