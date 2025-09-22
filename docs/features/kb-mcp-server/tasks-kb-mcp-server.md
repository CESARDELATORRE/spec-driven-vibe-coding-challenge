# Tasks: kb-mcp-server

**Input Sources**: `plan-kb-mcp-server.md`, `research-kb-mcp-server.md`, `data-model-kb-mcp-server.md`, `contracts/`, `quickstart.md` (usage scenarios), existing code (retroactive alignment).
**Feature Goal**: Minimal MCP STDIO server exposing two tools (`get_kb_info`, `get_kb_content`) backed by a single text knowledge base file.

> NOTE: Implementation exists; tasks retrofitted for governance, regression hardening, and forward consistency (future incremental work can reference IDs).

## Conventions
- [P] = Can run in parallel (different files / no dependency ordering conflict)
- All test authoring precedes (or validates) implementation changes (TDD when future changes occur)
- File paths are explicit; adjust only if structure changes

## Phase 3.1 Setup / Hardening
| ID | Task | Parallel | Depends On | Description / Acceptance |
|----|------|----------|------------|--------------------------|
| T001 | Verify feature directory integrity |  | — | Ensure `docs/features/kb-mcp-server/` contains plan, research, data model, contracts, quickstart (report missing) |
| T001a | Confirm legacy prototype artifacts removed |  | T001 | Assert absence of any deprecated search/excerpt tool or DTO/test files; ensure only metadata + content tools exist |
| T002 | Add anti-drift tool enumeration unit test |  | T001a | New test asserts discovered MCP tool IDs == {`get_kb_info`,`get_kb_content`} |
| T003 | [P] Add schema validation helper (internal) | ✅ | T001 | Introduce lightweight JSON Schema validation utility (optional dependency) for contract tests |
| T004 | [P] Add test fixture for minimal knowledge base file | ✅ | T001 | Create `tests/fixtures/min-kb.txt` (content length small) |

## Phase 3.2 Contract & Model Tests (Retro + New)
| ID | Task | Parallel | Depends On | Description / Acceptance |
|----|------|----------|------------|--------------------------|
| T005 | Contract test: `get_kb_info` response schema |  | T002 | Test builds tool response, validates against `contracts/get_kb_info.schema.json` |
| T006 | [P] Contract test: `get_kb_content` response schema | ✅ | T002 | Validate required fields per status branches (ok/empty/error) |
| T007 | [P] Model integrity test: `KnowledgeBaseInfo` field invariants | ✅ | T002 | Asserts non-negative lengths, availability mapping, last modified not default |
| T008 | [P] Edge case test: empty file returns status=empty | ✅ | T004 | Use fixture overriding file path to zero-length file |
| T009 | Error path test: missing file yields info.status=unavailable |  | T004 | Force nonexistent path via configuration override |

## Phase 3.3 Core (Only if refactoring or extending)
| ID | Task | Parallel | Depends On | Description / Acceptance |
|----|------|----------|------------|--------------------------|
| T010 | (Conditional) Introduce `ServerOptions` warning threshold |  | T005,T006 | Add optional `MaxContentWarningLength`; log warning when exceeded |
| T011 | [P] (Conditional) Refactor file load to async method | ✅ | T007 | Replace sync read with async; tests updated; no behavior change |

## Phase 3.4 Integration & Reliability
| ID | Task | Parallel | Depends On | Description / Acceptance |
|----|------|----------|------------|--------------------------|
| T012 | Integration test: startup + tool discovery |  | T005,T006 | Launch server process; list tools; assert both present |
| T013 | [P] Integration test: full content retrieval flow | ✅ | T012 | Start server, invoke `get_kb_content`, compare length vs file size |
| T014 | [P] Integration test: unavailable file path scenario | ✅ | T012 | Misconfigure path; expect `get_kb_info.status=unavailable` |

## Phase 3.5 Polish / Docs / Maintenance
| ID | Task | Parallel | Depends On | Description / Acceptance |
|----|------|----------|------------|--------------------------|
| T015 | Update `plan-kb-mcp-server.md` to high-level variant |  | T001 | Remove verbose executed step list (retro content) & progress noise |
| T016 | [P] Update `memory.md` to register tasks file | ✅ | T015 | Add one-line description of `tasks-kb-mcp-server.md` |
| T017 | [P] Add Quickstart cross-link from orchestrator docs | ✅ | T015 | Insert pointer in `src/orchestrator-agent/README.md` (if exists) |
| T018 | [P] Add README deprecation cleanup commit (remove stub) | ✅ | T015 | Delete deprecated `README.md` if still present or confirm removal |

## Dependency Graph (Simplified)
```
T001 → T002 → (T005,T006,T007) → (T008,T009) → (T010?,T011?)
T004 → (T008,T009)
T005,T006 → T012 → (T013,T014)
T015 → (T016,T017,T018)
```

## Parallel Execution Examples
```
# After T002 completes:
Run in parallel: T006 T007 T008

# After T012 completes:
Run in parallel: T013 T014

# After T015 completes:
Run in parallel: T016 T017 T018
```

## Validation Checklist
- [ ] Tool enumeration test present & passing (T002)
- [ ] Both JSON Schemas have contract tests (T005,T006)
- [ ] Edge cases (empty, missing file) covered (T008,T009)
- [ ] Integration tests confirm discovery + retrieval (T012–T014)
- [x] Plan simplified (T015) & tasks file referenced in `memory.md` (T016)

## Future (Deferred – Not Tasks Yet)
- Introduce excerpt tool (`get_kb_excerpt`) when size trigger hit
- Add basic search (substring or naive ranking) on demand
- Implement file watcher reload logic
- Add HTTP/SSE transport support

---
Document Version: 1.0 (governance tasks retrofitted)  
Last Updated: September 2025
