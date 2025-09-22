# Analysis: Constitution vs Copilot Instructions vs AGENTS.md

Date: 2025-09-22 (updated for Constitution v0.1.3)  
Scope: Clarifies roles, precedence, and coordinated usage of three governance / guidance artifacts:  
- `/.specify/memory/constitution.md` ("Constitution")  
- `/.github/copilot-instructions.md` ("Copilot Instructions")  
- `/AGENTS.md` ("Agents Guide")

---
## 0. Priority Note (Authoritative Source)
The Constitution (current version: v0.1.3, amended 2025-09-22) is the single authoritative governance layer. Any conflict in this analysis, `AGENTS.md`, or `.github/*` documents must defer to the Constitution until those documents are updated. This file is descriptive, not normative.

## 1. Purpose & Audience Summary
| Document | Core Purpose | Primary Audience | Stability | Enforcement Style |
|----------|--------------|------------------|----------|-------------------|
| Constitution | Non‑negotiable principles & governance (WHY + guardrails) | Everyone (humans + AI) | Low change (versioned) | Hard precedence gates |
| AGENTS.md | Practical contributor handbook (HOW to build/test/contribute) | Human contributors | Moderate | Procedural / PR review |
| Copilot Instructions | Operational behavioral rules for AI assistants inside repo | Automated coding agents | Higher | Behavioral (soft, but expected) |

---
## 2. Conceptual Layering (Mental Model)
Think in three concentric layers:
1. Charter Layer (Immutable Intent): Constitution defines the *philosophy* and *constraints* – spec-first, simplicity, traceability, testing discipline, secret hygiene.
2. Practice Layer (Codified Workflow): AGENTS.md turns principles into daily repeatable actions (commands, branch naming, test invocation sequence).
3. Execution Layer (Automated Mediation): Copilot Instructions ensure AI actions respect both the Charter and Practice layers (e.g., run tests after edits, don’t leak secrets, update memory docs).

---
## 3. Precedence Chain
Highest → Lowest:
1. Constitution  
2. Explicit Tradeoff Entries (`/docs/tradeoffs.md`) when they justify deviations  
3. Feature Specs (`/docs/specs/*`) & Implementation Plans (`/docs/implementation-plans/*`)  
4. Language / Testing Rules (`.github/instructions/*.md`)  
5. AGENTS.md  
6. Copilot Instructions  
7. README / Setup docs  

If two layers conflict, escalate upward until resolved; only amend the Constitution if the principle itself needs evolution.

---
## 4. Division of Responsibility
| Concern | Constitution | AGENTS.md | Copilot Instructions |
|---------|--------------|-----------|----------------------|
| Non-negotiable principles | YES | Reference only | Must honor |
| Build / test commands | NO | YES | Executes them |
| AI tool usage patterns | NO | Mention indirectly | YES |
| Spec-first enforcement rule | YES | Mirrors | Must operationalize |
| Naming conventions & structure | YES (principle-level) | Summarizes | Enforces in actions |
| Amendment/governance process | YES | NO | Obeys outcomes |
| Memory doc update requirement | YES (traceability) | Reiterates | Explicit behavior |

---
## 5. Typical Change Triggers
| Scenario | Update Constitution? | Update AGENTS.md? | Update Copilot Instructions? |
|----------|----------------------|------------------|------------------------------|
| Add new principle (e.g., performance budgets) | YES | Maybe (if operational fallout) | Maybe |
| Introduce new build command | NO | YES | YES |
| Adjust AI workflow (e.g., run lint before test) | NO | Maybe | YES |
| Expand scope to multi-step orchestration | Maybe (scope guardrail) | YES | YES |
| Add new test category (e.g., smoke tests) | NO | YES | YES |

---
## 6. Example Conflict Resolutions
| Conflict | Resolution | Root Cause Category |
|----------|-----------|---------------------|
| AI adds premature abstraction (over-engineering) | Constitution principle “Deliberate Simplicity” prevails → refactor | Philosophy violation |
| Missing spec for new MCP tool | Block merge; Constitution spec-first principle | Process gap |
| Secret-like token committed | Hard block; Security principle + AGENTS.md | Hygiene failure |
| Folder named `PascalCaseFolder` | Rename to kebab-case; Constitution + naming rule | Structural inconsistency |

---
## 7. Workflow Integration (End-to-End)
1. Ideate → Write/extend spec in `docs/specs` (Constitution: Spec-Driven).  
2. Plan implementation → optionally add `/docs/implementation-plans/...`.  
3. Code with AI aid → AI follows Copilot Instructions (tests, memory updates).  
4. Add/modify tests (unit + integration as appropriate).  
5. Record decisions in `/docs/tradeoffs.md` if non-obvious or deferring complexity.  
6. Add any new durable doc to `docs/memory.md`.  
7. Open PR using branch + commit semantics (AGENTS.md).  
8. Reviewer / AI verifies Constitution gates before merge.  

---
## 8. Governance Mechanics (From Constitution)
- Amendment Path: PR → rationale + version bump (PATCH: clarification; MINOR: new principle; MAJOR: removal or philosophy shift).  
- Temporary Exceptions: Inline `// TEMP-CONSTITUTION-EXCEPTION:` with rationale + dated note + tradeoff entry.  
- Enforcement Vector: Human reviewers + automated assistant checks.  

---
## 9. What NOT to Put Where
| Content Type | Belongs In | Not In |
|--------------|-----------|--------|
| Principle (e.g., “traceability before refactor”) | Constitution | AGENTS.md only |
| Command (`dotnet test …`) | AGENTS.md | Constitution |
| AI operational pattern | Copilot Instructions | Constitution |
| Technology evolution path | Architecture doc / future plan | Constitution |
| One-off decision rationale | `/docs/tradeoffs.md` | Constitution |

---
## 10. Expansion Readiness Indicators
You should consider amending or expanding governance if any of these recur more than twice:
- Repeated PR friction over ambiguous scope boundaries.  
- Divergent test quality expectations.  
- Onboarding confusion about precedence order.  
- AI assistants misapplying instructions due to granularity gap.  

---
## 11. Recommended Light Future Enhancements
| Enhancement | Rationale | Effort |
|-------------|-----------|--------|
| Add PR checklist referencing Constitution gates | Reduce review cognitive load | Low |
| Add simple script to scan for TEMP-CONSTITUTION-EXCEPTION | Ensure follow-up hygiene | Low |
| Add CI job verifying `docs/memory.md` entries for new docs | Maintain traceability | Low |
| Tag specs with status (Draft/Active/Deprecated) | Clarify lifecycle | Medium |

---
## 12. Quick Reference Cheat Sheet
- Start → Spec.  
- Implement → Minimal viable structure.  
- Test → Behavior + one edge path.  
- Document → Tradeoffs + memory registration.  
- Review → Gates (build, tests, docs, no secrets).  
- Merge → Aligns with Constitution or amend it.

---
## 13. Summary Statement
The Constitution defines the *why* and non-negotiables; AGENTS.md operationalizes the *how* for humans; Copilot Instructions operationalize the *how* for AI. Together they create a layered governance system: stable intent, adaptable practice, responsive automation.

---
## 14. Source Traceability
Original draft: 2025-09-19. Updated 2025-09-22 to align with Constitution v0.1.3 (restored explicit `/docs/features/<feature>` organizational vision for feature docs). Branch: `features/gh-spec-kit-support`. No external network research used in this revision.  

---
## 15. Maintenance Note
Update this analysis file only when any of: (a) Constitution version changes; (b) Precedence order changes; (c) New governance doc class introduced.
