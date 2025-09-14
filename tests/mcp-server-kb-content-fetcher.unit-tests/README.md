# Tests for MCP Server KB Content Fetcher

This document explains the testing strategy, rationale, and structure for the `mcp-server-kb-content-fetcher` solution. It focuses on why we have **separate unit tests for the service layer and the MCP tool layer**, what each test validates, and how to extend the suite safely. A placeholder section for future integration tests is also provided.

---

## 1. UNIT TESTS

### 1.1 Purpose & Philosophy
The unit test layer provides fast, deterministic validation of:

- Core knowledge base mechanics (file loading, search behavior, metadata exposure)
- MCP tool adapter behavior (input normalization, parameter clamping, formatting, error shielding)
- Separation of concerns between business logic (service) and protocol/contract shaping (tools)

These tests run in-memory (except for one controlled file-read scenario) and avoid network, STDIO protocol interactions, or end-to-end orchestration concerns (those will move to the Integration Test layer).

### 1.2 Architectural Layers Under Test
| Layer | Component(s) | Responsibility | Test Focus |
|-------|--------------|----------------|------------|
| Service | `FileKnowledgeBaseService` | Load & hold KB content, execute searches, provide metadata | File existence handling, initialization states, search result constraints, availability info |
| MCP Tools | `SearchKnowledgeTool`, `GetKbInfoTool` | Expose service functionality via MCP tool contract | Input validation, result shaping, defensive behavior, error translation |

### 1.3 Why Separate Tests for Service vs Tools?
- **Isolation of Failure Domains**: A failure in file parsing or search algorithm should not break tests concerned only with formatting tool responses.
- **Mocking Strategy**: Tool tests mock `IKnowledgeBaseService` to simulate edge cases instantly (errors, partial data, empty results) without file I/O overhead.
- **Specification Traceability**: The feature spec requires: startup loading, search with bounded results, and knowledge base info reporting. Each requirement maps cleanly to one or more test groups.
- **Refactoring Safety**: Internal changes to search logic won’t break tool tests unless the tool contract changes.

### 1.4 Current Unit Test Files
| File | Scope | Key Behaviors Validated |
|------|-------|-------------------------|
| `services/FileKnowledgeBaseServiceTests.cs` | Service initialization & search | Valid + invalid file path, pre/post initialization behavior, empty/null query handling, case-insensitive search, max results limiting, metadata correctness |
| `tools/SearchKnowledgeToolTests.cs` | Search MCP tool adapter | Input sanitation (null/empty), max results clamping, delegation correctness, error wrapping, formatting (Match strength / Position), unchanged service call count |
| `tools/GetKbInfoToolTests.cs` | Info MCP tool adapter | Availability status mapping, error shielding, partial info resilience, single invocation guarantee |
| `tools/GetKbContentToolTests.cs` | Raw content dump tool | Full content retrieval, empty content handling, error propagation (status=error) |

### 1.5 Failure Modes & Coverage
| Category | Examples Covered |
|----------|------------------|
| File system issues | Missing file => initialization returns false (service) |
| Uninitialized usage | Search / Info returns safe defaults (service tests) |
| Input anomalies | Null / empty query => no service call (tool tests) |
| Parameter bounds | Max results clamped (tool tests) |
| Exception handling | Service throws => graceful error response (tool tests) |
| Data formatting | Context vs content placement, metadata shaping (tool tests) |
| Case insensitivity | Queries like `GRAFANA` still match (service) |

### 1.6 Testing Techniques Applied
- **Direct file-backed test** (single controlled text file) for realistic search surface.
- **Mock-driven tests** using NSubstitute for tool logic edge cases.
- **Clamping & Guard Assertions** verify that user-supplied values never propagate unsafely.
- **Behavioral Assertions** (e.g., `DidNotReceive`) ensure we don’t over-call services on invalid input.

### 1.7 Design Principles Reinforced
1. **Separation of Concerns** – Tool layer doesn’t reimplement search; service layer doesn’t perform presentation shaping.
2. **Fail Safe** – On error, return structured, minimal, diagnostic-friendly responses instead of throwing.
3. **Determinism First** – All unit tests produce stable results regardless of environment.
4. **Specification Alignment** – Each test case maps to an explicit or implicit requirement from the feature spec.

### 1.8 Adding New Unit Tests (Guidelines)
When introducing new functionality:
1. Decide whether logic belongs in the service (core behavior) or tool (protocol adaptation).
2. Service logic ⇒ prefer concrete tests with real data if behavior depends on text patterns; otherwise synthetic inputs are fine.
3. Tool logic ⇒ mock `IKnowledgeBaseService` and focus on:
   - Input normalization
   - Parameter shaping (clamps/defaults)
   - Error resilience
   - Output contract stability
4. Prefer one behavioral assertion per conceptual rule; avoid over-coupling to implementation details.
5. For new edge cases, add both a positive and a defensive test where meaningful.

### 1.9 Potential Future Unit Test Enhancements
- Unicode / multi-byte content indexing validation
- Very large file performance boundary (synthetic generation)
- Search snippet truncation once implemented (`MaxContentLengthPerResult` enforcement)
- Overlapping match ordering logic (if ranking evolves)

### 1.10 Quick Reference: What NOT to Put in Unit Tests
- STDIO / MCP transport interaction (belongs in integration tests)
- Multi-process orchestration
- Performance benchmarking (use profiling / perf tests if needed)
- Real network or external storage dependencies

---

## 2. INTEGRATION TESTS (Implemented)

See `../mcp-server-kb-content-fetcher.integration-tests/` for four protocol-level tests:
- Initialize + tool discovery
- `search_knowledge` (fixed query prototype)
- `get_kb_info`
- `get_kb_content` (raw full text)

They launch the built DLL (not `dotnet run`) to avoid file locking and validate real STDIO JSON-RPC behavior.

---

## 3. Traceability Matrix (Current Snapshot)
| Requirement Theme | Representative Spec Item | Test(s) Covering It |
|-------------------|---------------------------|---------------------|
| Startup loading | Knowledge base must load file at startup | `FileKnowledgeBaseServiceTests.InitializeAsync_WithValidFile_ReturnsTrue` |
| Availability reporting | Provide KB info state | `GetKbInfoToolTests.*` (available/unavailable/error) |
| Raw content retrieval | Provide full content for downstream embedding | `GetKbContentToolTests.*` |
| Search capability | Case-insensitive partial search | Service search tests (valid query, case variant) |
| Result limiting | Enforce max results | Service (max results limit) + Tool clamping tests |
| Defensive input | Null/empty queries safe | Tool tests for empty/null query |
| Error resilience | Graceful on exceptions | Tool tests: service throws → error response |

---

## 4. How to Run the Tests
From repository root:

```bash
dotnet test tests/mcp-server-kb-content-fetcher.unit-tests/
```

Run a single test class:

```bash
dotnet test tests/mcp-server-kb-content-fetcher.unit-tests/ --filter ClassName=SearchKnowledgeToolTests
```

Collect coverage:

```bash
dotnet test tests/mcp-server-kb-content-fetcher.unit-tests/ --collect:"XPlat Code Coverage"
```

---

## 5. Maintenance Notes
- When adding new test files, keep folder names in kebab-case and namespaces aligned (`UnitTests.Services`, `UnitTests.Tools`).
- Update this README and `docs/memory.md` if you introduce new categories.
- Prefer **clear intent** over **DRY** in test naming—duplication is acceptable when it improves readability.

---

*Last updated: September 2025 (includes raw content tool + integration test implementation)*
