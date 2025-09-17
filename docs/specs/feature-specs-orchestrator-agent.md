# Feature Specification: Orchestration Agent (MCP Server)

## 1. Overview
The Orchestration Agent provides a simple MCP server that coordinates:
- A Chat Agent (LLM prompt + response construction)
- The KB MCP Server (domain knowledge retrieval)

🏗️ **Architecture Reference**: This component serves as the central coordination layer in our [Architecture & Technologies](../04-architecture-technologies.md) design.

Prototype scope: single-turn question → (optional KB lookup) → LLM answer.  
Future scope: multi-agent workflows, multiple tools, planning, streaming.

## 2. User Journey (Prototype/POC)
1. User asks a domain question via an MCP-compatible UI.
2. Orchestration Agent receives the request through its MCP tool.
3. Agent (a) decides if KB search is needed (simple heuristic) and calls KB MCP Server, then (b) calls Chat Agent with combined context.
4. Orchestration Agent returns synthesized answer to client.

## 3. MCP Tools

The Orchestrator Agent exposes the following MCP tools:

### 1. `ask_domain_question`
**Purpose**: Primary tool for processing user questions about the domain
**Input Parameters**:
- `question` (string, required): The user's natural language question
**Output**: 
- Structured response containing the answer, confidence level, and sources
**Implementation**: Orchestrates between the in-process Chat Agent (using Semantic Kernel's ChatCompletionAgent) and the KB MCP Server to provide comprehensive answers

### 2. `get_orchestrator_status`
**Purpose**: Health check and status monitoring tool
**Input Parameters**: None
**Output**: 
- Status information including:
  - Orchestrator health status
  - KB MCP Server connection status
  - Chat Agent availability (in-process status)
  - Version information
**Implementation**: Provides real-time status of the orchestrator and its dependencies

## 4. Functional Requirements

### FR-1: MCP Server Startup
Acceptance:
- Starts via STDIO transport.
- Exposes both tools in discovery.
- Logs startup success or clear error.

### FR-2: Tool Invocation Handling
Acceptance:
- Rejects empty question with validation error.
- Enforces maxKbResults bounds (1–3) with default fallback.
- Returns MCP-compliant error object on failures.

### FR-3: KB Lookup Integration
Acceptance:
- When includeKb=true, performs up to maxKbResults searches via KB MCP Server (single search call; truncates results >3000 chars preserving indicator).
- If KB unavailable, continues with LLM only and sets usedKb=false.
- Logs query + result count.

### FR-4: Chat Agent Coordination
Acceptance:
- Constructs prompt with (a) user question, (b) formatted KB snippets (if any), (c) brief system instructions.
- Returns concise answer (target ≤200 words).
- Adds disclaimer when no KB data used.

### FR-5: Basic Heuristic (Optional KB Skip)
Acceptance:
- If question length < 5 characters OR contains only generic greeting (e.g., "hi", "hello"), skip KB automatically.
- Heuristic outcome reflected in usedKb flag.

### FR-6: Status Tool
Acceptance:
- Reports real-time booleans without triggering external calls (cached readiness check allowed).
- Always responds <100ms (excluding process scheduling).

## 5. Non-Functional Requirements (Prototype)
- Response latency target: <5s (KB + LLM).
- No persistence/state across calls.
- Console logging only (stdout reserved for MCP protocol outputs, logs to stderr).
- No secrets stored in code (LLM key via environment/config).

## 6. Error & Edge Behavior
- Empty question → validation error (message: "Question is required").
- Downstream KB error → proceed without KB, add disclaimer.
- Downstream LLM error → return error (message: "Answer generation failed") with correlation id.
- Oversized KB snippet → truncate with suffix "...[truncated]".

## 7. Out of Scope (Prototype)
- Multi-turn conversation memory.
- Planning / autonomous multi-step reasoning.
- Multiple concurrent KB sources.
- Streaming responses.
- Advanced ranking / semantic search.
- Retry/backoff strategies.
- Metrics/telemetry pipelines.

## 8. Success Criteria

### Functional
- ask_domain_question returns valid answer for sample AMG queries.
- Graceful degradation when KB offline.
- Validation prevents malformed inputs.

### Technical
- Startup < 3s.
- No unhandled exceptions during 20 consecutive tool invocations in manual test.

### User Experience
- Answers feel domain-aware when KB data available.
- Clear indication (disclaimer) when answer lacks KB grounding.

## 9. Dependencies
- KB MCP Server (search_knowledge tool).
- Chat Agent (LLM invocation API wrapper).
- MCP .NET SDK (STDIO transport).
- Azure AI Foundry (LLM endpoint/key).

## 10. Simple Heuristic Rules (Prototype)
- Greeting detection regex: ^\s*(hi|hello|hey)\b.* → skip KB.
- Ultra-short (<5 chars) or purely punctuation → validation error.

## 11. Future Evolution (Not in Prototype)
- Multi-agent choreography (planning, role specialization).
- HTTP/SSE transport.
- Memory (short-term conversation context).
- Observability (traces, structured metrics).
- Tool arbitration & ranking.
- Safety filters and redaction.

## 12. Open Questions (Track Later)
- Token budgeting vs. full snippet inclusion.
- Consistent correlation IDs across downstream calls.
- Prompt template versioning.

## 13. Glossary (Selective)
- Orchestration Agent: MCP server coordinating Chat + KB.
- Chat Agent: LLM interaction component.
- KB MCP Server: External MCP server providing domain snippets.

---

**Document Version**: 1.0  
**Last Updated**: September 2025  
**Next Review**: After prototype completion
