## Implementation Plan: kb-mcp-server

**Branch**: `features/gh-spec-kit-support` | **Date**: September 2025 | **Spec**: `docs/features/kb-mcp-server/specs-kb-mcp-server.md`
**Input**: Feature specification (v1.2) providing functional requirements for metadata + full raw content retrieval (no search).

## Summary
Provide a minimal MCP STDIO server that exposes two tools: `get_kb_info` (metadata) and `get_kb_content` (full text) backed by a single plain text file loaded at startup. Scope explicitly defers search, segmentation, ranking, and multi-source aggregation to future iterations—prioritizing simplicity, fast turnaround, and clear external contract stability. Implementation uses .NET Host builder + MCP SDK fluent configuration with stderr-only logging to protect STDIO protocol stream.

🏗️ **Architecture Reference**: Implements Knowledge Base layer from [Architecture & Technologies](../architecture-technologies.md) prototype variant.

## Technical Context
**Language/Version**: .NET 9 (compatible with .NET 10 Preview)  
**Primary Dependencies**: MCP SDK for .NET (`ModelContextProtocol` >= 0.3.0-preview.4), Microsoft.Extensions.Hosting/Logging  
**Storage**: Single plain text file (`datasets/knowledge-base.txt`) loaded into memory at startup  
**Testing**: xUnit (unit + light integration), NSubstitute for mocking  
**Target Platform**: Local development (console MCP server over STDIO)  
**Project Type**: Single backend service (console)  
**Performance Goals**: Startup < 5s; content retrieval O(1) in-memory; no explicit latency SLA (perf evaluation deferred)  
**Constraints**: All logging to stderr; file size guidance < 10MB; no dynamic reload  
**Scale/Scope**: Prototype single knowledge domain (Azure Managed Grafana)  

## Constitution Check
Principles adherence (Spec-First, Simplicity, Test Focus, Traceability, Naming Consistency, Lightweight Observability):
- Spec alignment: Tools & statuses match spec v1.2.
- Simplicity: No premature search abstraction or vector infra.
- Tests: Unit tests cover service + tools; integration optional/minimal.
- Traceability: FRs mapped to implementation classes (`GetKbInfoTool`, `GetKbContentTool`).
- Naming: Folder + project naming follow kebab-case / PascalCase per instructions.
- Observability: Basic logging only; no over-engineering.
Result: PASS (no violations requiring Complexity Tracking).

## Project Structure (Extract)
```
src/mcp-server-kb-content-fetcher/
  datasets/knowledge-base.txt
  services/IKnowledgeBaseService.cs
  services/FileKnowledgeBaseService.cs
  services/IKnowledgeBaseContentCache.cs
  services/FileKnowledgeBaseContentCache.cs
  tools/GetKbInfoTool.cs
  tools/GetKbContentTool.cs
  models/KnowledgeBaseContent.cs (immutable record)
  configuration/ServerOptions.cs
  extensions/LoggingExtensions.cs (helper)
tests/
  mcp-server-kb-content-fetcher.unit-tests/
  mcp-server-kb-content-fetcher.integration-tests/ 
```

Deprecated/Removed: prior prototype search/excerpt tool and related DTO/test files have been fully retired. Only two tools remain (`get_kb_info`, `get_kb_content`).

**Structure Decision**: Single-project console server (matches template Option 1). Web/mobile split not applicable.

## Execution Flow (Retroactive Summary)
```
1. Loaded feature spec (v1.2) – OK
2. Technical Context filled – no NEEDS CLARIFICATION items identified
3. Constitution principles reviewed – PASS (no violations)
4. Phase 0 research (implicit) – minimal; decisions captured via tradeoffs/spec
5. Phase 1 design executed directly in code (entities + tools) – complete
6. Post-design constitution re-check – PASS
7. Phase 2 task planning skipped (prototype already implemented)
8. Implementation & tests executed (Phases 3-4 collapsed) – complete
9. Validation via unit tests – PASS (Phase 5)
```

## Phase 0: Outline & Research (Retroactive)
Lightweight reasoning only (no separate `research.md` created due to prototype velocity goal):
- Decision: Single plain text file (no segmentation/index) → Simplicity
- Decision: Defer search / excerpt tooling → Avoid premature abstraction
- Decision: In-memory load at startup → Predictable responses, small file size
- Decision: Two tools only (`get_kb_info`, `get_kb_content`) → Minimal surface
- Alternatives Rejected: dynamic reload (adds complexity), vector store (over-scope), streaming partials (unnecessary for file size)
All potential unknowns either explicitly deferred or rendered non-issues by constrained scope; zero active NEEDS CLARIFICATION markers.

## Architecture Approach
- Host.CreateApplicationBuilder → add MCP server via fluent builder.
- `WithStdioServerTransport()` for prototype transport simplicity.
- Explicit manual tool registration using `McpServerTool.Create(...)` (chosen to avoid unintended discovery of deprecated or experimental tools and to keep the surface deterministic for anti-drift tests). Reflection-based auto discovery intentionally avoided for prototype to reduce surprise and simplify contract enforcement.
- **Immutable Content Cache Pattern**: Thread-safe singleton cache (`IKnowledgeBaseContentCache`) loads and holds immutable content at startup.
- **Scoped Service Layer**: Stateless scoped services (`IKnowledgeBaseService`) delegate to cache for thread safety and performance.
- **Service Lifetimes**: Cache is singleton (shared immutable state), services are scoped (no shared mutable state).
- File-based knowledge base loaded once at startup into memory via cache.
- No search/indexing layer: raw text only.
- Strongly-typed options + appsettings.json for file path.
- All logging to stderr to avoid protocol contamination.

## Phase 1: Design & Contracts (Applied)
Key design artifacts implemented directly in code (no external API contract files required since MCP tool surface minimal and SDK-driven):
- Entities: Knowledge Base (in-memory content), Tool Responses (anonymous projections returned).
- Contracts: Implicit via tool response shape (documented in spec + tests).
- Quickstart: Usage described in project README + spec; could be formalized as `quickstart.md` (optional future doc).
Rationale: Overhead of generating OpenAPI/GraphQL or separate contracts not justified for two zero-input tools.

## Tasks Reference
Detailed execution steps have been migrated to `tasks-kb-mcp-server.md` for governance and future incremental work. The plan now remains high-level (intent, architecture, principles, evolution path) while the tasks file owns day-to-day actionable breakdown.

## Phase 2: Task Planning Approach (Template Alignment)
Since implementation is already complete for prototype scope, Phase 2 (task generation) is moot. If retrofitting tasks for governance:
- One task per FR (FR-001..FR-008) mapping to service/tool implementation & tests.
- Add cleanup task: validate removal of deprecated search artifacts (DONE).
- Add future candidate tasks (deferred): search introduction trigger conditions; content reload; segmentation.
No `tasks.md` generated (out of scope per template until /tasks command).

## Tool Naming Conventions

This implementation uses a deliberate two-level naming scheme for MCP tools:

| Concern | External MCP Tool ID | C# Implementation Class |
|---------|----------------------|-------------------------|
| Metadata retrieval | `get_kb_info` | `GetKbInfoTool` |
| Full raw content retrieval | `get_kb_content` | `GetKbContentTool` |

Guidelines:
1. Always document protocol (snake_case) IDs in specs—these are what MCP clients invoke.
2. Implementation classes follow .NET PascalCase conventions with a `Tool` suffix for discoverability.
3. Adding a new tool: choose the protocol ID first, then create the corresponding class.
4. Keep protocol IDs stable; class names may be refactored if necessary without breaking clients.
5. (Optional) Include both identifiers in logs for clearer traceability.

Why not unify now? Keeping a separation lets us refactor internal structure or introduce decorators/adapters later (e.g., caching, authorization) without changing the protocol boundary. It also avoids accidental client breakage from internal renames.

Future Hardening (Optional): Add a unit test asserting the discovered MCP tool set exactly matches the expected protocol IDs. This will detect accidental removal or renaming early.

## Key Design Principles

### Simplicity First
- MCP SDK handles server hosting and tool discovery automatically
- Direct file I/O without complex caching mechanisms
- Basic string search without advanced indexing or ranking
- Minimal error handling focused on crash prevention

### Interface-Based Architecture
**Decision**: Use `IKnowledgeBaseService` interface pattern with dependency injection
**Reasoning**:
- **Dependency Inversion**: High-level MCP tools depend on abstraction, not concrete implementation
- **Open/Closed Principle**: Easy to extend with new knowledge base types without modifying existing code
- **Testability**: Simple to mock interface for unit testing MCP tools
- **Future Flexibility**: Seamless swapping between file-based, database, API, or vector-based knowledge sources
- **Configuration-Driven**: Can switch implementations via dependency injection configuration

### Future Evolution Readiness
- Service abstractions enable future data source swapping
- MCP SDK supports future HTTP transport through configuration changes
- Tool attribute-based discovery supports easy addition of new tools
- Domain-agnostic code structure supports reuse across different knowledge domains

## Technical Implementation Guidelines

### MCP SDK Integration
**Decision**: Use MCP SDK fluent builder pattern with auto-discovery
**Reasoning**: 
- Leverages SDK's built-in hosting and transport management
- Auto-discovery via WithToolsFromAssembly() eliminates manual tool registration
- Follows SDK conventions and best practices
- Reduces boilerplate code and potential configuration errors

### MCP STDIO Logging Configuration
**Critical Requirement**: Configure logging to stderr to prevent MCP protocol corruption
**Implementation**: 
```csharp
builder.Logging.AddConsole(options =>
{
    // Route all log levels to stderr to avoid corrupting MCP stdio (stdout) channel
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});
```
**Reasoning**: MCP protocol uses stdout for communication; any logging to stdout will corrupt the protocol stream

### Configuration Strategy
**Decision**: Use appsettings.json configuration approach
**Reasoning**: 
- Follows standard .NET configuration patterns
- No custom command-line parsing logic required
- Automatic binding to strongly-typed configuration classes
- Easy to extend for future configuration needs
- Integrates seamlessly with Host.CreateApplicationBuilder

### .NET Dependencies Summary
- .NET 10 Preview 6+ runtime or .NET 9 as fallback
- Microsoft MCP SDK for .NET (ModelContextProtocol package) - Latest stable/popular version, even if in preview state. Available here: https://www.nuget.org/packages/ModelContextProtocol/
- Built-in .NET hosting and configuration abstractions
- xUnit for testing framework
- Sample Azure Managed Grafana knowledge base content file

## Success Criteria

## Phase 3+: Future Implementation (Informational)
Future enhancements conditional on validated need:
- Introduce `get_kb_excerpt` or search tools when KB size or latency justifies.
- Add dynamic reload (file watcher) if content update frequency increases.
- Evaluate segmentation/chunking pipeline before integrating vector/semantic retrieval.
- Optional transport expansion (HTTP/SSE) when multi-process orchestration emerges.
- KB MCP Server starts successfully and loads text file
- MCP tools are discoverable and callable
- Info and content tools return expected payload shapes
- Integration with orchestrator works via STDIO transport

## Complexity Tracking
| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|---------------------------------------|
| (none) | — | — |

## Progress Tracking
*This checklist is updated during execution flow*

**Phase Status**:
- [x] Phase 0: Research documentation captured
- [x] Phase 1: High-level design documented
- [x] Phase 2: Task planning approach documented
- [ ] Phase 3: Tasks implemented (code) 
- [ ] Phase 4: Implementation complete (code) 
- [ ] Phase 5: Validation passed (tests) 

**Gate Status**:
- [x] Initial Constitution Check documented
- [x] Post-Design Constitution Check documented
- [x] All NEEDS CLARIFICATION resolved (documentation)
- [ ] Complexity deviations documented (none yet)

### Extended Gates & Hardening
Additional governance / quality items (mapped to `tasks-kb-mcp-server.md`):
- [ ] Anti-drift tool enumeration test (T002)
- [ ] Model invariants test (T005)
- [ ] Empty file edge test (T006)
- [ ] Missing file edge test (T007)
- [ ] Integration discovery test (T010)
- [ ] Integration full content retrieval test (T011)
- [ ] Integration unavailable path test (T012)
- [x] Plan high-level simplification (T015 documentation)
- [x] Tasks file registered in memory (T016 documentation)
- [ ] Orchestrator quickstart cross-link (T017 doc link)
- [ ] Deprecated README removal (T018 doc cleanup)

Current focus: Execute T002–T009 before moving to integration hardening (T012–T014). Phase 3 checkbox above will flip once initial hardening tasks are merged.

## Version & Change Log
**Document Version**: 1.2  
**Last Updated**: September 2025  
**Status**: Implemented (Prototype Complete)  
**Change Note**: Added missing template sections (Execution Flow, Phase 0, Phase 3+), corrected progress tracking & gates, removed outdated test description reference to search, clarified retroactive nature of phases.
