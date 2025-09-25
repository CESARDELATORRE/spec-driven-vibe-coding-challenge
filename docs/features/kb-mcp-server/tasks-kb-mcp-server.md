# Tasks: kb-mcp-server

**Input Sources**: `plan-kb-mcp-server.md`, `research-kb-mcp-server.md`, `data-model-kb-mcp-server.md`, `contracts/`, `quickstart.md` (usage scenarios), existing code (retroactive alignment).
**Feature Goal**: Minimal MCP STDIO server exposing two tools (`get_kb_info`, `get_kb_content`) backed by a single text knowledge base file.

🏗️ **Architecture Reference**: The KB MCP Server design and integration patterns are detailed in [Architecture & Technologies](../../architecture-technologies.md).

> NOTE: Implementation exists; tasks retrofitted for governance, regression hardening, and forward consistency (future incremental work can reference IDs).

## Conventions
- [P] = Can run in parallel (different files / no dependency ordering conflict)
- All test authoring precedes (or validates) implementation changes (TDD when future changes occur)
- File paths are explicit; adjust only if structure changes

## Phase 3.1 Setup / Hardening
| ID | Task | Parallel | Depends On | Description / Acceptance |
|----|------|----------|------------|--------------------------|
| T001 | Verify feature directory integrity |  | — | Ensure `docs/features/kb-mcp-server/` contains plan, research, data model, contracts, quickstart (report missing) **(DONE)** |
| T001a | Confirm legacy prototype artifacts removed |  | T001 | Assert absence of any deprecated search/excerpt tool or DTO/test files; ensure only metadata + content tools exist **(DONE)** |
| T002 | Add anti-drift tool enumeration unit test |  | T001a | New test asserts discovered MCP tool IDs == {`get_kb_info`,`get_kb_content`} **(DONE)** |
| T002a | Implement immutable content cache pattern |  | T002 | Refactor singleton services to use thread-safe immutable cache + scoped stateless services for scaling **(DONE)** |
| T003 | [P] Add test fixture for minimal knowledge base file | ✅ | T001 | Create `tests/fixtures/min-kb.txt` (content length small) |

## Phase 3.2 Contract & Model Tests (Retro + New)
| ID | Task | Parallel | Depends On | Description / Acceptance |
|----|------|----------|------------|--------------------------|
| T005 | Model integrity test: `KnowledgeBaseInfo` field invariants |  | T002 | Asserts non-negative lengths, availability mapping, last modified not default |
| T006 | [P] Edge case test: empty file returns status=empty | ✅ | T003 | Use fixture overriding file path to zero-length file |
| T007 | Error path test: missing file yields info.status=unavailable |  | T003 | Force nonexistent path via configuration override |

## Phase 3.3 Core (Only if refactoring or extending)
| ID | Task | Parallel | Depends On | Description / Acceptance |
|----|------|----------|------------|--------------------------|
| T008 | (Conditional) Introduce `ServerOptions` warning threshold |  | T005,T006 | Add optional `MaxContentWarningLength`; log warning when exceeded |
| T009 | [P] (Conditional) Refactor file load to async method | ✅ | T005 | Replace sync read with async; tests updated; no behavior change |

## Phase 3.4 Integration & Reliability
| ID | Task | Parallel | Depends On | Description / Acceptance |
|----|------|----------|------------|--------------------------|
| T010 | Integration test: startup + tool discovery |  | T005 | Launch server process; list tools; assert both present |
| T011 | [P] Integration test: full content retrieval flow | ✅ | T010 | Start server, invoke `get_kb_content`, compare length vs file size |
| T012 | [P] Integration test: unavailable file path scenario | ✅ | T010 | Misconfigure path; expect `get_kb_info.status=unavailable` |

## Phase 3.5 Polish / Docs / Maintenance
| ID | Task | Parallel | Depends On | Description / Acceptance |
|----|------|----------|------------|--------------------------|
| T015 | Update `plan-kb-mcp-server.md` to high-level variant |  | T001 | Remove verbose executed step list (retro content) & progress noise |
| T016 | [P] Update `memory.md` to register tasks file | ✅ | T015 | Add one-line description of `tasks-kb-mcp-server.md` |
| T017 | [P] Add Quickstart cross-link from orchestrator docs | ✅ | T015 | Insert pointer in `src/orchestrator-agent/README.md` (if exists) |
| T018 | [P] Add README deprecation cleanup commit (remove stub) | ✅ | T015 | Delete deprecated `README.md` if still present or confirm removal |

## Dependency Graph (Simplified)
```
T001 → T002 → (T005) → (T006,T007) → (T008?,T009?)
T003 → (T006,T007)
T005 → T010 → (T011,T012)
T015 → (T016,T017,T018)
```

## Parallel Execution Examples
```
# After T002 completes:
Run in parallel: T006 T007 (after T005 done)

# After T010 completes:
Run in parallel: T011 T012

# After T015 completes:
Run in parallel: T016 T017 T018
```

## Validation Checklist
- [x] Tool enumeration test present & passing (T002)
- [ ] Edge cases (empty, missing file) covered (T006,T007)
- [ ] Integration tests confirm discovery + retrieval (T010–T012)
- [x] Plan simplified (T015) & tasks file referenced in `memory.md` (T016)
- [x] Legacy artifacts removed (T001a)

## Future (Deferred – Not Tasks Yet)
- Introduce excerpt tool (`get_kb_excerpt`) when size trigger hit
- Add basic search (substring or naive ranking) on demand
- Implement file watcher reload logic
- Add HTTP/SSE transport support

---
Document Version: 1.0 (governance tasks retrofitted)  
Last Updated: September 2025
