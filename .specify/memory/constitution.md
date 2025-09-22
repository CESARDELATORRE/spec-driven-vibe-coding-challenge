# Constitution of Spec-Driven Vibe Coding Challenge 

This document establishes the minimal non-negotiable principles and governance rules for the current prototype / POC phase. It is intentionally lean: it defines WHAT must stay true; secondary documents (e.g. `AGENTS.md`, `.github/copilot-instructions.md`, `.github/instructions/*`) define HOW. When tension exists, this Constitution prevails unless explicitly amended.

## Core Principles

### 1. Spec-Driven Development First
Every meaningful feature starts from (or updates) a written spec in `docs/specs` or an implementation plan in `docs/implementation-plans` before significant code is merged. Code without an aligning spec or recorded tradeoff is subject to rework or removal.

### 2. Deliberate Simplicity (Prototype Scope Discipline)
Prefer the smallest implementation that satisfies the current prototype goals. Avoid anticipatory abstractions. Defer scaling, performance tuning, and multi-agent generalization unless a spec or tradeoff explicitly justifies it.

### 3. Test Coverage Where It Matters
Fast unit tests protect core logic (services, deterministic transformations). Integration tests cover MCP tool surfaces and cross-process orchestration. New public contracts require at least one test (unit or integration) demonstrating expected behavior and one edge / failure path when practical.

### 4. Documentation & Traceability

#### 4.1 Global documents

- The global project/product's idea and global vision and scope, should be defined in a document named `/docs/idea-vision-scope.md`
- The global project/product's architecture and technologies selection, should be defined in a document named `/docs/architecture-technologies.md`
- All project's assumptions are logged in `/docs/assumptions.md`
- All project's tradeoffs are logged in `/docs/tradeoffs.md` 
- New durable documentation files are registered/logged in `docs/memory.md`. 
 
#### 4.2 Features' documents

- What's specific for particular features (i.e. product's modules or smaller features) should be logged on the relevant specs and plan docs docs within the `/docs/features/` folder, such as:

```
/docs/features/
/docs/features/kb-mcp-server
/docs/features/orchestrator-agent
/docs/features/chat-agent
```

Then, within each particular feature's folder (i.e. /docs/features/kb-mcp-server), it should have several document types such as:
- **Specs doc:**
	- With the name format as `specs-<FEATURE-NAME>.md` (Example: `specs-kb-mcp-server.md`)
- **Tasks doc:**
	- With the name format as `tasks-<FEATURE-NAME>.md` (Example: `tasks-kb-mcp-server.md`)
- **Technology research:**
	- With the name format as `tech-research-<FEATURE-NAME>.md`
- **Implementation plan doc:**
	- With the name format as `implementation-plan-<FEATURE-NAME>.md` (Example: `implementation-plan-kb-mcp-server.md`)
- **Code examples (Optional):
 I	- Free file name, for now.

 Example of feature's files in its folder:

 ```
/docs/features/kb-mcp-server/specs-orchestrator-agent.md
/docs/features/kb-mcp-server/tasks-orchestrator-agent
/docs/features/kb-mcp-server/tech-research-<FEATURE-NAME>.md
/docs/features/kb-mcp-server/implementation-plan-orchestrator-agent
/docs/features/kb-mcp-server/example-program-semantic-kernel-orchestrator-mcp-server.cs
```
	


### 5. Security & Secret Hygiene
No secrets committed. Environment variables (or user secrets in local dev) are the mechanism for sensitive configuration. Reviews block if a secret or credential-like artifact appears in Git history.

### 6. Consistent Naming & Structure

- Folder naming: kebab-case. 
- C# namespaces: PascalCase transformation of project name. 
- Tests separated by type (`*.unit-tests`, `*.integration-tests`). 

Deviation requires explicit justification in a tradeoff entry.

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
	- Build succeeds using documented commands.
	- Tests (unit / integration / smoke if applicable) updated & pass for changed surfaces.
	- No secrets / accidental credentials.
	- Behavior or structure change → related spec and/or implementation plan updated OR new tradeoff entry.
	- Any new durable doc registered in `docs/memory.md`.
3. Fast Feedback: Prefer adding a minimal failing test before refactoring contract logic.
4. Tooling Guidance for AI Assistants: Honor `.github/instructions/*`; if conflicts arise those files adapt—Constitution remains stable unless formally amended.

## Governance
- Precedence Order (highest → lowest): Constitution → Explicit Tradeoff Entry → Feature Spec / Implementation Plan → `.github/instructions/*` → `AGENTS.md` / README.
- Amendment Process: (a) Draft change PR updating this file; (b) Include rationale + impacted docs; (c) Version bump (PATCH for clarifications, MINOR for new principle/constraint, MAJOR for removals or philosophical shifts).
- Enforcement: Reviewers (human or AI) flag non-compliance; unresolved violations block merge or require documenting an explicit tradeoff and (if systemic) a Constitution amendment.
- Temporary Exceptions: Must include an inline `// TEMP-CONSTITUTION-EXCEPTION: <issue-link or rationale>` plus a dated follow-up note in the relevant spec or tradeoff.

**Version**: 0.1.3 | **Ratified**: 2025-09-19 | **Last Amended**: 2025-09-22

---
Minimal by design; expand only when a repeated friction or risk justifies hardening.