# Data Model: Orchestrator Agent

**Date**: September 23, 2025  
**Context**: Phase 1 data model design for orchestrator-agent implementation

🏗️ **Architecture Reference**: The Orchestrator Agent design and integration patterns are detailed in [Architecture & Technologies](../../architecture-technologies.md).

## Overview

This document defines the data models and entities required to implement the orchestrator agent functional requirements, focusing on the structured response format (FR-015) and the key entities identified in the specification.

## Core Entities

### 1. Domain Question
**Purpose**: Represents user input for domain-specific queries  
**Source**: FR-001 (natural language questions), FR-006 (validation requirements)

**Properties**:
- `Text`: string (required) - The user's natural language question
- `Length`: int (calculated) - Character count for validation
- `IsValid`: bool (calculated) - Validation result

**Validation Rules**:
- Must not be null or empty (FR-006)
- Must not be whitespace only
- Length constraints applied for basic heuristics

**State Transitions**: Input → Validated → Processed

### 2. Domain Response  
**Purpose**: Comprehensive response structure combining KB content and LLM generation  
**Source**: FR-015 (structured responses), FR-005 (grounding indication), FR-009 (concise responses)

**Properties**:
- `Answer`: string - The synthesized response (50-200 words target)
- `UsedKb`: bool - Indicates if knowledge base was consulted
- `KbResults`: KnowledgeSnippet[] - Array of retrieved content
- `Disclaimers`: string[] - Warnings about response quality/limitations
- `TokensEstimate`: int - Approximate token usage for transparency
- `Diagnostics`: DiagnosticInfo - Technical details for debugging
- `Provenance`: ProvenanceInfo - Source and generation details

**Validation Rules**:
- Answer must not be null or empty
- UsedKb must match presence of KbResults
- TokensEstimate must be non-negative

### 3. Orchestrator Status
**Purpose**: Health and connectivity information for monitoring  
**Source**: FR-003 (status information), FR-017 (real-time status)

**Properties**:
- `OrchestratorHealth`: HealthStatus - Overall orchestrator status
- `KbServerStatus`: ConnectionStatus - KB MCP Server connectivity
- `ChatAgentStatus`: AgentStatus - In-process chat agent availability  
- `Version`: string - Application version information
- `Timestamp`: DateTime - Status check timestamp
- `Environment`: string - Runtime environment indicator

**Validation Rules**:
- All status fields must have valid enum values
- Timestamp must be recent (within service lifetime)
- Version must follow semantic versioning

### 4. Knowledge Snippet
**Purpose**: Individual piece of retrieved domain content with metadata  
**Source**: FR-008 (KB result limits), FR-014 (content truncation)

**Properties**:
- `Content`: string - The retrieved content text
- `Source`: string - Identifier for content source
- `Truncated`: bool - Indicates if content was truncated
- `OriginalLength`: int - Length before truncation
- `RetrievalTimestamp`: DateTime - When content was retrieved

**Validation Rules**:
- Content must not be null (empty allowed for edge cases)
- If Truncated = true, OriginalLength > Content.Length
- Source must be provided for traceability

## Supporting Types

### HealthStatus Enumeration
```
Healthy = 0,      // All systems operational
Degraded = 1,     // Some non-critical issues
Unhealthy = 2     // Critical issues present
```

### ConnectionStatus Enumeration  
```
Connected = 0,    // Active connection established
Disconnected = 1, // No connection available
Error = 2         // Connection failed with error
```

### AgentStatus Enumeration
```
Ready = 0,        // Agent initialized and ready
ConfigError = 1,  // Configuration issues
Unavailable = 2   // Agent not accessible
```

### DiagnosticInfo
**Purpose**: Technical debugging information  
**Properties**:
- `Environment`: string - Production/Development indicator
- `EndpointConfigured`: bool - Azure AI endpoint availability
- `DeploymentConfigured`: bool - Model deployment availability
- `ApiKeyConfigured`: bool - Authentication status
- `AttemptedKb`: bool - Whether KB lookup was attempted
- `HeuristicSkipKb`: bool - Whether KB was skipped by heuristic
- `ChatAgentReady`: bool - Chat agent initialization status

### ProvenanceInfo
**Purpose**: Source and generation tracking  
**Properties**:
- `Provider`: string - LLM provider (e.g., "azure-openai")
- `ServiceId`: string - Service identifier
- `Deployment`: string - Model deployment name
- `Temperature`: float - Generation temperature setting
- `Mode`: string - Generation mode (e.g., "agent")
- `KbGrounded`: bool - Whether response used KB content

## Entity Relationships

```
DomainQuestion (1) ──────► DomainResponse (1)
                            │
                            ├─► KnowledgeSnippet (0..*)
                            ├─► DiagnosticInfo (1)
                            └─► ProvenanceInfo (1)

OrchestratorStatus (standalone entity for health checks)
```

## Serialization Requirements

### JSON Schema Compliance
- All entities must serialize to/from JSON for MCP protocol
- Property names use camelCase for JSON (PascalCase in C#)
- Nullable properties explicitly marked
- Enum values serialize as strings

### MCP Tool Integration
- DomainQuestion maps to `ask_domain_question` input schema
- DomainResponse maps to `ask_domain_question` output schema  
- OrchestratorStatus maps to `get_orchestrator_status` output schema

### Backwards Compatibility
- New properties added as optional
- Existing property types remain stable
- Deprecation warnings for removed properties

## Error Handling Data

### Error Response Structure
**Purpose**: Standardized error information for failed operations  
**Properties**:
- `ErrorCode`: string - Categorized error identifier
- `Message`: string - Human-readable error description
- `CorrelationId`: string - Request tracking identifier
- `Details`: object - Additional error-specific information

### Common Error Codes
- `VALIDATION_ERROR`: Input validation failures (FR-006)
- `KB_UNAVAILABLE`: Knowledge base connection issues (FR-007)
- `LLM_ERROR`: Chat agent generation failures (FR-016)
- `CONFIGURATION_ERROR`: Missing or invalid configuration
- `TIMEOUT_ERROR`: Response time limit exceeded (FR-004)

## Implementation Notes

### Memory Efficiency
- Knowledge snippets implement content truncation (FR-014)
- Large responses use streaming where possible
- Diagnostic information includes memory usage estimates

### Thread Safety
- All entities are immutable after construction
- Builder patterns used for complex entity creation
- No shared mutable state between requests (FR-018)

### Validation Strategy
- Input validation at entity boundaries
- Business rule validation in service layer
- MCP protocol validation at transport layer

**Next Phase**: Generate JSON schema contracts and implement entity classes following this data model.