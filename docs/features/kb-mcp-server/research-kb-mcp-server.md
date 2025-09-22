# Research (Phase 0 + Technology) - kb-mcp-server

Unified research document combining Phase 0 unknowns resolution and detailed technology evaluation (formerly in `tech-research-kb-mcp-server.md`). Prototype scope intentionally minimized; technology choices validated for stability and future evolution.

## 1. Unknowns Resolution
| Item | Initial Status | Resolution | Notes |
|------|----------------|-----------|-------|
| Need for search capability | Questioned | Deferred | Only two retrieval modes needed (metadata + full) |
| File size constraints | Undefined | Soft cap <10MB | Memory & latency trivial at this size |
| Transport choice | Multiple options | STDIO only | Simplest for local agent integration |
| SDK maturity risk | Preview | Accept | Pin version; add anti-drift test later |
| Reload requirement | Possible | Deferred | Low content churn |

No remaining NEEDS CLARIFICATION markers.

## 2. Key Decisions
| Decision | Rationale | Alternatives | Status |
|----------|-----------|-------------|--------|
| Single text file source | Fastest path; no ingestion pipeline | Multi-file, DB, vector store | Accepted |
| Two-tool surface | Minimal stable contract | Add excerpt/search prematurely | Accepted |
| In-memory load at startup | Predictable performance | On-demand streaming | Accepted |
| Separate tool IDs vs class names | Clear protocol boundary | Shared naming | Accepted |
| Skip dynamic reload | Avoid watcher complexity | FileSystemWatcher + invalidation | Accepted |

## 3. Tradeoffs Summary
| Area | Simplicity Gain | Deferred Cost |
|------|-----------------|---------------|
| No search | Fewer classes/tests | Future feature spike later |
| Full-content tool | Zero chunk logic | Potential inefficiency at scale |
| Single file | Minimal config | Migration path to multi-source later |
| No reload | Less state logic | Manual restart on edits |

## 4. Risk Register (Prototype)
| Risk | Impact | Likelihood | Mitigation |
|------|--------|-----------|-----------|
| Preview MCP API changes | Medium | Medium | Version pin + periodic review |
| Content size creep | Performance | Low | Add warning threshold |
| Tool sprawl later | Complexity | Medium | Enforce naming & anti-drift test |

## 5. Deferred Topics
- Semantic / vector retrieval
- Excerpt pagination & summarization
- Multi-source domain aggregation
- Hot reload / file watch
- HTTP transport & auth

## 6. Exit Criteria (Satisfied)
- All unknowns addressed or explicitly deferred
- Prototype decisions documented with rationale
- No blocking research items for Phase 1/implementation

---
Generated retroactively (Phase 0) for alignment with plan template.

---

## 7. MCP Protocol & Ecosystem Overview
Model Context Protocol standardizes tool exposure between agents/IDEs and external capability providers. Prototype uses STDIO (lowest friction, supported by GitHub Copilot, Claude Desktop, Cursor). Future transports: HTTP/SSE (remote), WebSocket multiplexing.

## 8. Candidate SDKs & Approaches
| Option | Language | Maturity | Pros | Cons | Decision |
|--------|----------|----------|------|------|----------|
| ModelContextProtocol .NET SDK (official preview) | C# | Preview (0.3.x) | Native language, fluent builder, reflection discovery | Preview churn risk | Selected |
| Node.js MCP SDK | TypeScript | Active | Large ecosystem & examples | Polyglot overhead | Rejected (not needed) |
| Python early SDK | Python | Experimental | Rapid scripting | Immature, adds runtime | Deferred |
| Manual implementation | Any | N/A | Full control | High effort, risk | Rejected |

## 9. Selected Stack & Versions
| Component | Version | Rationale | Upgrade Watch |
|-----------|---------|-----------|---------------|
| .NET Runtime | net9.0 | Stable; aligns with orchestrator | Revisit at .NET 10 GA |
| ModelContextProtocol | 0.3.0-preview.4 | Meets plan floor; stable enough | Track 0.4.x breaks |
| Microsoft.Extensions.Hosting | 9.0.9 | Standard hosting alignment | Minor auto-updates OK |

## 10. Dependency Risk Assessment
| Risk | Impact | Likelihood | Mitigation |
|------|--------|-----------|------------|
| Preview API churn | Build breaks | Medium | Pin version; review release notes |
| Future transport need | Refactor cost | Medium | Isolate builder config in Program.cs |
| stdout logging leak | Protocol corruption | Low | Forced stderr threshold + tests (optional) |
| File size growth | Memory / latency | Low | Soft cap + future excerpt tool |

## 11. Tool Surface Stability Strategy
Principles: stable protocol IDs (`get_kb_info`, `get_kb_content`), internal class refactor freedom, optional anti-drift enumeration test, semantic version rules (breaking = remove/rename tool; additive = add tool; patch = field addition w/out removal).

## 12. Configuration Strategy
Strongly-typed `ServerOptions` with nested `KnowledgeBaseOptions`; deterministic path resolution sequence (absolute → CWD → AppContext.BaseDirectory → repo variants) reduces environment variance; no dynamic reload to avoid file race complexity.

## 13. Performance & Scale (Prototype Thresholds)
| Aspect | Current | Adequate While | Trigger For Change |
|--------|---------|----------------|--------------------|
| Load | Single read at startup | <10MB file | >2s startup or >50MB memory |
| Retrieval | In-memory return | <50 req/sec local | Multi-user or remote deployment |
| Partial access | Not supported | File small | >200KB semantic segmentation need |
| Search | Deferred | Agent embeddings viable | KB > 30KB dense or multi-file |

## 14. Security & Safety (Prototype Local)
No secrets; future remote considerations: payload truncation, file allow-list, PII redaction, rate limiting.

## 15. Evolution Roadmap
| Milestone | Change | Precondition |
|-----------|--------|--------------|
| Excerpt tool | `get_kb_excerpt` with length param | Size inefficiency evidence |
| Basic search | Substring / naive ranking | Agent context inefficiency |
| HTTP/SSE transport | Add server transport | Multi-process orchestration |
| Multi-source | Composite service | Multiple domain files appear |
| Vector retrieval | Chunk + embed pipeline | Accuracy gap observed |

## 16. Alternatives Revisited
Vector-first, dynamic reload, multi-format ingestion all deferred due to complexity without clear ROI at current scale.

## 17. Open Questions (Non-blocking)
| Question | Status | Planned Trigger |
|----------|--------|-----------------|
| Schema version field now? | Deferred | First additive change |
| Output truncation guard | Deferred | Content > threshold |

## 18. Technology Decision Summary
| Decision | Outcome |
|----------|---------|
| SDK | .NET MCP preview 0.3.x pinned |
| Transport | STDIO only |
| Content model | Single in-memory string |
| Tools | Exactly two (info + full content) |
| Logging | stderr only |
| Hardening | Anti-drift test (future optional) |

## 19. References
- Spec: `docs/features/kb-mcp-server/specs-kb-mcp-server.md`
- Implementation Plan v1.2
- Architecture: `docs/architecture-technologies.md`
- MCP SDK NuGet: https://www.nuget.org/packages/ModelContextProtocol/

---
Merged content (September 2025) – replaces separate technology research file.
