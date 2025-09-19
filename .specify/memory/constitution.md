# Spec-Driven Vibe Coding Challenge Constitution

This document establishes the minimal non-negotiable principles and governance rules for the current prototype / POC phase. It is intentionally lean: it defines WHAT must stay true; secondary documents (e.g. `AGENTS.md`, `/docs/*`, `.github/instructions/*`) define HOW. When tension exists, this Constitution prevails unless explicitly amended.

## Core Principles

### 1. Spec-Driven Development First
Every meaningful feature starts from (or updates) a written spec in `docs/specs` or an implementation plan in `docs/implementation-plans` before significant code is merged. Code without an aligning spec or recorded tradeoff is subject to rework or removal.

### 2. Deliberate Simplicity (Prototype Scope Discipline)
Prefer the smallest implementation that satisfies the current prototype goals. Avoid anticipatory abstractions. Defer scaling, performance tuning, and multi-agent generalization unless a spec or tradeoff explicitly justifies it.

### 3. Test Coverage Where It Matters
Fast unit tests protect core logic (services, deterministic transformations). Integration tests cover MCP tool surfaces and cross-process orchestration. New public contracts require at least one test (unit or integration) demonstrating expected behavior and one edge / failure path when practical.

### 4. Documentation & Traceability
Key decisions, assumptions, and tradeoffs are logged in `/docs/tradeoffs.md` or the relevant spec. New durable documentation files are registered in `docs/memory.md`. Architectural or behavioral changes that affect users or contributors must update the corresponding spec/plan.

### 5. Security & Secret Hygiene
No secrets committed. Environment variables (or user secrets in local dev) are the mechanism for sensitive configuration. Reviews block if a secret or credential-like artifact appears in Git history.

### 6. Consistent Naming & Structure
Folder naming: kebab-case. C# namespaces: PascalCase transformation of project name. Tests separated by type (`*.unit-tests`, `*.integration-tests`). Deviation requires explicit justification in a tradeoff entry.

### 7. Observability via Simplicity
Console / stderr logging (structured where helpful) is sufficient for the prototype. Instrumentation beyond logging is postponed unless a spec mandates it for learning value.

## Implementation Constraints (Current Phase)
- Language / Runtime: .NET (C#) for MCP servers & orchestration; Semantic Kernel for in-process chat orchestration.
- Protocol: MCP stdio for local agent-to-agent communication.
- Scope Guardrails: Single-turn orchestrator (no multi-step autonomous loops yet); KB search is file/dataset backed, not vector DB.
- Allowed Expansion Paths MUST be pre-declared in docs (e.g. future multi-agent evolution doc) before starting structural refactors.

## Workflow & Quality Gates
1. Branching & Commits: Conventional commit messages; feature/fix/chore branch prefixes (see `AGENTS.md`).
2. PR Acceptance Gates (all mandatory):
	- Builds succeed using documented commands.
	- Tests relevant to changed areas are added/updated and pass.
	- No secrets / accidental credentials.
	- If behavior or structure changed: related spec / implementation plan or tradeoff updated.
	- New persistent doc files added to `docs/memory.md`.
3. Fast Feedback: Prefer adding a minimal failing test before refactoring contract logic.
4. Tooling Guidance for AI Assistants: Honor `.github/instructions/*`; if conflicts arise those files adapt—Constitution remains stable unless formally amended.

## Governance
- Precedence Order (highest → lowest): Constitution → Explicit Tradeoff Entry → Feature Spec / Implementation Plan → `.github/instructions/*` → `AGENTS.md` / README.
- Amendment Process: (a) Draft change PR updating this file; (b) Include rationale + impacted docs; (c) Version bump (PATCH for clarifications, MINOR for new principle/constraint, MAJOR for removals or philosophical shifts).
- Enforcement: Reviewers (human or AI) flag non-compliance; unresolved violations block merge or require documenting an explicit tradeoff and (if systemic) a Constitution amendment.
- Temporary Exceptions: Must include an inline `// TEMP-CONSTITUTION-EXCEPTION: <issue-link or rationale>` plus a dated follow-up note in the relevant spec or tradeoff.

**Version**: 0.1.0 | **Ratified**: 2025-09-19 | **Last Amended**: 2025-09-19

---
Minimal by design; expand only when a repeated friction or risk justifies hardening.