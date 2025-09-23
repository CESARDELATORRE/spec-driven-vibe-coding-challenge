# Implementation Plan: Orchestrator Agent

**Branch**: `features/gh-spec-kit-support` | **Date**: September 23, 2025 | **Spec**: [specs-orchestrator-agent.md](./specs-orchestrator-agent.md)
**Input**: Feature specification from `docs/features/orchestrator-agent/specs-orchestrator-agent.md`

## Execution Flow (/plan command scope)
```
1. Load feature spec from Input path ✓
   → Feature: Orchestrator Agent MCP Server for AMG domain questions
2. Fill Technical Context ✓
   → Project Type: single (MCP server application)
   → Structure Decision: Default Option 1 (single project)
3. Fill Constitution Check section ✓
   → Evaluated against constitution requirements
4. Evaluate Constitution Check section ✓
   → No violations identified
   → Update Progress Tracking: Initial Constitution Check ✓
5. Execute Phase 0 → research.md ✓
   → All technical unknowns resolved from architecture docs
6. Execute Phase 1 → contracts, data-model.md, quickstart.md ✓
   → Generated design artifacts based on functional requirements
7. Re-evaluate Constitution Check section ✓
   → No new violations after design
   → Update Progress Tracking: Post-Design Constitution Check ✓
8. Plan Phase 2 → Task generation approach described ✓
9. STOP - Ready for /tasks command ✓
```

**IMPORTANT**: The /plan command STOPS at step 8. Phase 2 tasks.md creation is handled by the /tasks command.

## Summary
The Orchestrator Agent is an MCP server that coordinates between a Knowledge Base MCP Server and an in-process Chat Agent (Semantic Kernel ChatCompletionAgent) to provide domain-grounded responses about Azure Managed Grafana. The system accepts natural language questions, optionally retrieves relevant knowledge base content, synthesizes responses using Azure AI Foundry, and returns structured answers with transparency indicators. The prototype focuses on single-turn interactions with stateless operation and graceful degradation when dependencies are unavailable.

## Technical Context
**Language/Version**: C# .NET 9  
**Primary Dependencies**: MCP .NET SDK 0.3.0-preview.4, Semantic Kernel 1.54.0, Azure AI connectors  
**Storage**: File-based knowledge base via KB MCP Server (no direct storage)  
**Testing**: xUnit 2.9.2 with FluentAssertions 6.12.0  
**Target Platform**: Windows/Linux (local execution, MCP STDIO transport)  
**Project Type**: single (MCP server application)  
**Performance Goals**: <10s response time, <3s startup time, <100ms status queries  
**Constraints**: Prototype scope, single-turn only, no persistence, environment-based secrets  
**Scale/Scope**: Local prototype, 2 MCP tools, AMG domain focus

## Constitution Check
*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

✅ **Spec-Driven Development First**: Implementation plan derived from specs-orchestrator-agent.md  
✅ **Deliberate Simplicity**: Single project, no anticipatory abstractions, prototype-focused  
✅ **Test Coverage Where It Matters**: Unit tests for tool logic, integration tests for MCP protocol  
✅ **Documentation & Traceability**: Plan follows constitution naming (plan-orchestrator-agent.md)  
✅ **Security & Secret Hygiene**: Environment variables for Azure AI credentials  
✅ **Consistent Naming & Structure**: kebab-case folders, PascalCase namespaces  
✅ **Observability via Simplicity**: Console/stderr logging, MCP protocol compliance

**Assessment**: No constitutional violations identified. Design aligns with prototype constraints and simplicity principles.

## Project Structure

### Documentation (this feature)
```
docs/features/orchestrator-agent/
├── plan-orchestrator-agent.md          # This file (/plan command output)
├── research-orchestrator-agent.md      # Phase 0 output (/plan command)
├── data-model-orchestrator-agent.md    # Phase 1 output (/plan command)
├── quickstart-orchestrator-agent.md    # Phase 1 output (/plan command)
├── contracts/                          # Phase 1 output (/plan command)
│   ├── ask_domain_question.schema.json
│   └── get_orchestrator_status.schema.json
└── tasks-orchestrator-agent.md         # Phase 2 output (/tasks command - NOT created by /plan)
```

### Source Code (repository root)
```
# Option 1: Single project (DEFAULT - matches existing structure)
src/
├── orchestrator-agent/              # Main MCP server project
│   ├── tools/                       # MCP tool implementations
│   ├── services/                    # Business logic services
│   ├── models/                      # Data models and DTOs
│   ├── configuration/               # Configuration classes
│   └── extensions/                  # Extension methods

tests/
├── orchestrator-agent.unit-tests/          # Fast unit tests
├── orchestrator-agent.integration-tests/   # MCP protocol tests
└── orchestrator-agent.smoke-tests/         # End-to-end validation
```

## Phase 0: Outline & Research

### Technical Unknowns Analysis
Based on the feature specification and attached architecture documents, all major technical decisions have been pre-resolved:

1. **MCP Integration Pattern**: ✅ Resolved in architecture-technologies.md
   - Decision: MCP .NET SDK with STDIO transport for prototype
   - Rationale: Simplest approach for local execution and MCP client testing
   - Implementation: Host.CreateEmptyApplicationBuilder + AddMcpServer + WithStdioServerTransport

2. **Chat Agent Integration**: ✅ Resolved in architecture-technologies.md
   - Decision: In-process Semantic Kernel ChatCompletionAgent (not external service)
   - Rationale: Prototype simplicity, avoid premature distribution
   - Implementation: Direct ChatCompletionAgent instantiation with Azure AI configuration

3. **Knowledge Base Integration**: ✅ Resolved from existing KB MCP Server
   - Decision: MCP client connection to existing KB MCP Server
   - Rationale: Leverage existing domain knowledge infrastructure
   - Implementation: McpClientFactory with StdioClientTransport

4. **Configuration Strategy**: ✅ Resolved in constitution and architecture docs
   - Decision: Environment variables for secrets, appsettings.json for non-secrets
   - Rationale: Constitutional requirement for secret hygiene
   - Implementation: ConfigurationBuilder with AddEnvironmentVariables

**Output**: research-orchestrator-agent.md documenting these pre-resolved decisions with rationale and implementation approach.

## Phase 1: Design & Contracts

### Data Model Extraction (FR-015: Structured Responses)
From functional requirements, key entities identified:

1. **Domain Question**: Input natural language query with validation
2. **Domain Response**: Comprehensive response structure with transparency
3. **Orchestrator Status**: Health and dependency status information  
4. **Knowledge Snippet**: Retrieved content with truncation handling

### API Contracts (FR-012: Tool Discovery)
From functional requirements FR-001, FR-003, and FR-012:

1. **ask_domain_question Tool**:
   - Input: `{ "question": "string (required)" }`
   - Output: Structured response with answer, usage indicators, source info
   - Validation: Empty/invalid question rejection (FR-006)

2. **get_orchestrator_status Tool**:
   - Input: No parameters
   - Output: Health status, KB connectivity, Chat Agent availability
   - Performance: <100ms response time (extrapolated from OLD spec)

### Contract Test Strategy
- JSON Schema validation for tool inputs/outputs
- MCP protocol compliance verification
- Error handling validation for edge cases
- Performance constraint verification

### Quickstart Validation
End-to-end scenario testing based on acceptance scenarios:
1. Valid AMG question with KB available → grounded response
2. Question with KB unavailable → graceful degradation  
3. Empty question → validation error
4. Status check → health information

**Output**: data-model-orchestrator-agent.md, contracts/ JSON schemas, failing contract tests, quickstart-orchestrator-agent.md

## Phase 2: Task Planning Approach
*This section describes what the /tasks command will do - DO NOT execute during /plan*

**Task Generation Strategy**:
- Load functional requirements FR-001 through FR-018 from specification
- Generate implementation tasks following TDD approach
- Each contract → contract test task [P] (parallel-safe)
- Each entity → model creation task [P]
- Each functional requirement → implementation task
- Integration scenarios → integration test tasks

**Ordering Strategy**:
1. **Foundation** (Parallel): Project setup, configuration, models
2. **Tool Contracts** (Sequential): Contract tests before implementations  
3. **Business Logic** (Sequential): Services to make contract tests pass
4. **Integration** (Sequential): MCP server setup, KB client, Chat Agent integration
5. **Validation** (Parallel): Edge cases, error handling, performance verification

**Estimated Output**: 20-25 numbered tasks in tasks-orchestrator-agent.md covering:
- Project infrastructure (csproj, configuration)
- Data models and DTOs (4-5 tasks)
- Contract tests (2-3 tasks) 
- Tool implementations (4-6 tasks)
- Integration setup (3-4 tasks)
- Error handling and validation (3-4 tasks)
- Testing and documentation (2-3 tasks)

**IMPORTANT**: This phase is executed by the /tasks command, NOT by /plan

## Phase 3+: Future Implementation
*These phases are beyond the scope of the /plan command*

**Phase 3**: Task execution (/tasks command creates tasks-orchestrator-agent.md)  
**Phase 4**: Implementation following tasks in priority order  
**Phase 5**: Validation via quickstart scenarios and integration tests

## Complexity Tracking
*No constitutional violations requiring justification*

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| None | N/A | N/A |

**Assessment**: Design maintains constitutional simplicity while meeting all functional requirements. In-process Chat Agent avoids premature distribution. Single project structure avoids unnecessary complexity.

## Progress Tracking
*This checklist is updated during execution flow*

**Phase Status**:
- [x] Phase 0: Research complete (/plan command)
- [x] Phase 1: Design complete (/plan command)  
- [x] Phase 2: Task planning complete (/plan command - describe approach only)
- [ ] Phase 3: Tasks generated (/tasks command)
- [ ] Phase 4: Implementation complete
- [ ] Phase 5: Validation passed

**Gate Status**:
- [x] Initial Constitution Check: PASS
- [x] Post-Design Constitution Check: PASS  
- [x] All NEEDS CLARIFICATION resolved
- [x] Complexity deviations documented (None required)

---
*Based on Constitution v0.1.3 - See `.specify/memory/constitution.md`*