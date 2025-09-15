# Future Multi-Agent Workflow Approach

## 1. Purpose
This document captures forward-looking architectural options, decision drivers, and an incremental roadmap for evolving the current Orchestrator MCP Server (prototype) into a resilient, explainable, and extensible multi‑agent coordination layer. It is intentionally pragmatic: optimized for progressive hardening rather than a disruptive rewrite.

## 2. Current Prototype Snapshot
| Dimension | Current State (Prototype) |
|-----------|---------------------------|
| Orchestration Mode | Single-step: (Optional KB snippet) → ChatCompletionAgent (Azure OpenAI) |
| Knowledge Base | External KB MCP server launched via stdio child process |
| Reasoning Agent | In-process Semantic Kernel ChatCompletionAgent + fallback direct prompt |
| Provenance | Basic object: provider, serviceId, deployment, temperature, mode, kbGrounded |
| Heuristics | Greeting skip, length validation, simple KB inclusion toggle |
| Isolation | Chat logic & orchestration coupled inside same process |
| Observability | Diagnostics tool + inline provenance (no step timeline yet) |
| State / Memory | Stateless per request (no long-term conversation tracking) |

## 3. Core Separation of Concerns (Target Model)
Orchestrator (Control Plane) SHOULD own:
- Request correlation, step timeline, provenance aggregation.
- Tool / agent selection & ordering heuristics or planning.
- Policy controls (token budget, allowed models, fallback logic).
- Multi-source result adjudication & answer synthesis strategy.
- Graceful degradation & fallback routing.

Capability / Domain Agents SHOULD own:
- Domain prompts, embedding / retrieval specifics, summarization transforms.
- Model choice & tuning per capability (cost/perf autonomy).
- Stateful memory where domain-specific (e.g., user intent model refinement).
- Independent deployment, scaling, and secret scopes.

## 4. Architectural Option Set
### Option A: Thin (Pure Logic) Orchestrator
The Orchestrator performs deterministic sequencing only (no embedded LLM reasoning). All semantic reasoning lives in remote agent MCP servers.
Pros: Strong isolation, easy horizontal scaling, minimal secret spread.  
Cons: Less adaptive; coordination logic evolves slower (hand-coded branching).

### Option B: SK-Embedded (Meta-Agent) Orchestrator
The Orchestrator embeds Semantic Kernel (SK) planning / reasoning and treats remote agents as tools.
Pros: Rapid iteration of dynamic multi-step plans; centralized optimization.  
Cons: Larger blast radius, secret concentration, potential “god process” anti-pattern.

### Option C: Hybrid (Recommended Path)
Incrementally externalize domain reasoning (Chat Agent, KB) while preserving a small adaptive layer (light planning, classification) in the Orchestrator only when justified. Avoid premature over-consolidation.

## 5. Decision Drivers Matrix
| Driver | Weight | A: Thin | B: Embedded | C: Hybrid (Score Rationale) |
|--------|--------|---------|-------------|-----------------------------|
| Iteration Speed (planning tweaks) | High | Medium | High | High (planning added only when needed) |
| Failure Isolation | High | High | Low | Medium-High |
| Secret Minimization | Medium | High | Low | Medium |
| Token Cost Governance | Medium | Medium | High | High (central view + remote enforcement) |
| Complexity (Operational) | Medium | Low | High | Medium |
| Portability / Polyglot Agents | Medium | High | Medium | High |
| Progressive Migration Effort | High | High | Low | High |
Outcome: Hybrid yields balanced trajectory while reducing upfront complexity.

## 6. Evolution Roadmap
| Stage | Goal | Key Additions | Exit Criteria |
|-------|------|---------------|---------------|
| 1 (Now) | Working single-turn orchestrator | KB + ChatCompletionAgent + provenance | Stable answer + diagnostics |
| 2 | Externalize Chat | Chat Agent MCP server (generate_chat_answer) + orchestrator wrapper | Orchestrator no longer holds model secret for chat path |
| 3 | Step Timeline & Provenance Detail | steps[] array (kb_fetch, answer_generation, fallback) | Deterministic replay & audit |
| 4 | Policy & Budgeting | Token ceilings, retry budget, model selection guard | Prevent runaway cost / loops |
| 5 | Adaptive Selection (Light) | Intent classifier (cheap model) → dynamic tool inclusion | Reduced unnecessary KB calls |
| 6 | Structured Planning (Optional) | Planner or chain-of-thought gating | Measurable answer quality uplift |
| 7 | Multi-Agent Arbitration | Compare candidate answers (e.g., fast vs accurate model) | Quality > threshold metrics |
| 8 | Memory Layer (Optional) | External conversation store (Redis/Postgres) | Consistent multi-turn context |
| 9 | Policy Service Split | Out-of-process safety/policy adjudicator | Independent policy upgrade cadence |

## 7. Data Contracts (Future-Friendly)
Suggested stable Orchestrator response envelope (extensions additive):
```jsonc
{
  "answer": "...",
  "steps": [
    { "id": "kb_fetch", "status": "success", "latencyMs": 120, "tokensIn": 0, "tokensOut": 0, "notes": "1 snippet" },
    { "id": "answer_generation", "agent": "chat", "model": "gpt-4o", "mode": "agent", "tokensIn": 900, "tokensOut": 180 }
  ],
  "provenance": { /* aggregated */ },
  "diagnostics": { /* non-breaking */ },
  "warnings": ["Answer generated without knowledge base grounding"],
  "version": "1.0"
}
```
Contract Principles:
- Additive fields only (preserve backward compatibility).
- Separate human-facing disclaimers from machine-readable step data.
- Provide per-step latency & token metrics when available.

## 8. Provenance & Observability Expansion
Planned Additions:
- steps[] timeline with chronological order.
- policyDecisions[] (e.g., { rule: "MaxTokenBudget", action: "allow", estimatedTokens: 1400 }).
- arbitration block (when multiple candidate answers exist).
- costEstimate (if pricing metadata available per model/provider).

## 9. Security & Deployment Considerations
| Concern | Mitigation Path |
|---------|-----------------|
| Secret sprawl | Move chat model key into Chat Agent server at Stage 2 |
| Process crashes | Supervisory launcher or container orchestration (restart policy) |
| Over-permissioned agent | Define allow-list of callable tools per agent id |
| Data leakage in logs | Central structured logging w/ redaction layer |
| Model provider diversification | serviceId tagging + provenance for auditing |

## 10. Failure & Fallback Strategy (Target)
| Failure Class | Primary Fallback | Secondary | Tertiary |
|---------------|------------------|----------|----------|
| KB launch fail | Skip KB & continue | Retry (1) with backoff | Mark step failed, continue answer |
| Agent invocation fail | Direct prompt fallback | Alternate model (smaller) | Return partial answer + failure step |
| Planner loop/diffusion | Hard iteration cap | Switch to deterministic template | Abort with policy explanation |
| Token ceiling exceeded | Summarize KB snippet(s) | Reduce snippet window | Abort with quota notice |

## 11. Lightweight Interface Abstractions (Preparation)
Introduce two internal interfaces before extraction:
```csharp
public interface IChatAnswerInvoker {
  Task<ChatAnswerResult> GenerateAsync(ChatAnswerRequest request, CancellationToken ct = default);
}

public interface IKnowledgeProvider {
  Task<IReadOnlyList<KbSnippet>> FetchAsync(KbQuery query, CancellationToken ct = default);
}
```
These become adapters: (a) in-process implementation (current) → (b) remote MCP tool proxy.

## 12. Planning Strategy (If Adopted)
Start with deterministic rule-based selection (pattern / classifier) before introducing an LLM planner to avoid premature unpredictability. Only add planning if: (1) tool count > 3, or (2) conditional sequences materially impact quality or cost.

## 13. Metrics for Advancement Between Stages
| Stage Gate | Metric Example |
|------------|----------------|
| 2 → 3 | 95% success of chat calls post-extraction over 50 requests |
| 3 → 4 | Token cost variance reduced ≥15% via KB skip heuristic |
| 4 → 5 | KB calls reduced ≥25% with no answer quality regression |
| 5 → 6 | Planner improves first-pass answer acceptance by ≥8% |
| 6 → 7 | Arbitration lifts factual accuracy score ≥5% |

## 14. Anti-Patterns to Avoid
- Monolithic “mega agent” doing retrieval + reasoning + arbitration implicitly.
- Embedding secret rotation logic inside orchestration paths (centralize infra concerns).
- Expanding response schema without version tagging once clients integrate.
- Silent fallbacks (always record mode = agent | fallback-direct | degraded).

## 15. Immediate Actionable Next Steps (Low-Risk Wins)
1. Add kbReachable boolean (separate from executableResolved).  
2. Emit minimal steps array (even if synthetic) for (kb_fetch, answer_generation).  
3. Introduce IChatAnswerInvoker abstraction (no external changes).  
4. Prepare Chat Agent project scaffold (tool: generate_chat_answer).  
5. Add tokenBudgetCeiling config; record when near >80%.  

## 16. Glossary
- serviceId: Stable logical identifier for a registered model service in SK Kernel.
- deployment: Azure OpenAI deployment name (maps to model & capacity).
- grounding: Use of retrieved, domain-authoritative snippets in composing the final answer.
- arbitration: Comparing multiple candidate answers and selecting one (or merging).
- planner: Component (rule-based or LLM) that decides tool invocation order.

## 17. Summary
Adopting a Hybrid evolution lets the system mature through additive, observable increments. Each stage delivers standalone value while laying infrastructural hooks (provenance, abstractions, step logging) that minimize refactor risk when introducing higher-order capabilities (planning, arbitration, memory, policy services).

---
Document Status: Draft (Prototype Roadmap)  
Intended Audience: Implementers & reviewers guiding multi-agent evolution  
Last Updated: {{LAST_UPDATED}}
