# 📝 Memory: File Reference Guide

This document provides a brief description of each file's purpose and relevant details for the spec-driven vibe coding challenge project.

## 📚 Global Project Documentation

### `/.specify/memory/constitution.md`
**Purpose**: Project Constitution (minimal non-negotiable principles & governance)
**Details**: Defines precedence order, core principles (spec-first, simplicity, test focus, traceability, security, naming consistency, lightweight observability), workflow gates, and amendment process. Overrides other guidance when conflicts arise unless amended.

### `/docs/01-original-challenge-definition.md`
**Purpose**: Original challenge definition and requirements document
**Details**: Contains the original problem statement, objectives, and requirements for the spec-driven vibe coding challenge

### `/docs/02-plain-goals-and-approaches.md`
**Purpose**: North-star document defining fundamental project goals and approaches
**Details**: Baseline document for generating detailed idea/vision-scope, architecture definition and implementation code. Contains product goals, technical specifications, and project philosophy

### `/docs/idea-vision-scope.md`
**Purpose**: Comprehensive project vision and scope document defining the AMG-specific AI agent concept
**Details**: Contains executive summary, problem definition, functional requirements, and prototype scope with clear horizon planning

### `/docs/architecture-technologies.md`  
**Purpose**: Technical architecture and technology stack recommendations with evolution path
**Details**: Defines three architecture variants (Prototype, Dockerized Decoupled, Cloud-Native) after consolidation of prior four-stage model, plus technology selections (.NET 9 baseline, MCP, Semantic Kernel) and testing strategies with deferred triggers

### `/analisys-constitution-ghcp-instructions-agents.md`
**Purpose**: Comparative analysis of Constitution, Copilot instructions, and AGENTS.md
**Details**: Defines roles, precedence chain, usage workflow, conflict resolution patterns, and maintenance triggers for governance artifacts


### `/docs/tradeoffs.md`
**Purpose**: Documents all technical and functional tradeoffs made during project development
**Details**: Contains 13 documented tradeoffs covering integration environment, knowledge base complexity, search behavior, and implementation decisions with reasoning and impact analysis

### `/docs/assumptions.md`
**Purpose**: Documents project assumptions about customer feedback and business context
**Details**: Clarifies exercise constraints and assumed validation for prototype development

## 🎯 Features Documentation

Features follow the Constitution's `/docs/features/<feature-name>/` structure.

### Knowledge Base MCP Server Feature (`/docs/features/kb-mcp-server/`)
- `specs-kb-mcp-server.md`: Functional specification (journey, requirements, MCP tools: get_kb_info, get_kb_content; search deferred)
- `plan-kb-mcp-server.md`: High-level implementation plan (detailed steps migrated out to tasks file)
- `research-kb-mcp-server.md`: Unified Phase 0 unknowns + technology research (SDK selection, risks, roadmap)
- `data-model-kb-mcp-server.md`: Minimal entity & payload schema documentation for tool responses
- `contracts/`: JSON Schemas for `get_kb_info` and `get_kb_content` responses (prototype contract reference)
- `quickstart.md` (project root `src/mcp-server-kb-content-fetcher/quickstart.md`): Developer run & integration guide (merged former README)
- `tasks-kb-mcp-server.md`: Governance task list (retrofit) with anti-drift, contract tests, integration hardening tasks

### Orchestrator Agent Feature (`/docs/features/orchestrator-agent/`)
- `specs-orchestrator-agent.md`: Functional specification (tools: ask_domain_question, get_orchestrator_status; coordination scope)
- `plan-orchestrator-agent.md`: Implementation plan following plan.prompt.md template (MCP client wiring, Semantic Kernel orchestration, testing strategy)
- `research-orchestrator-agent.md`: Phase 0 technical decision consolidation from architecture documents  
- `data-model-orchestrator-agent.md`: Phase 1 entity and data structure design (DomainQuestion, DomainResponse, OrchestratorStatus, KnowledgeSnippet)
- `quickstart-orchestrator-agent.md`: Phase 1 end-to-end validation scenarios with 5 comprehensive test cases
- `contracts/`: JSON Schema definitions for MCP tool validation (ask_domain_question, get_orchestrator_status)
- `tasks-orchestrator-agent.md`: Phase 2 implementation task breakdown (26 ultra-simplified tasks following TDD approach with dependency ordering and parallel execution optimization)
- `example-program-semantic-kernel-orchestrator-mcp-server.cs`: Example code illustrating orchestration pattern

### Chat Agent Feature (`/docs/features/chat-agent/`)
- `specs-chat-agent.md`: Functional specification (single-turn scope, out-of-scope futures, success metrics)
- `implementation-plan-chat-agent.md`: Implementation path for in-process prototype with future extraction strategy
- (Planned) tasks / tech research docs as feature evolves

All newly added durable feature docs must be registered here and referenced in `docs/memory.md` upon creation.

## 🔧 Development Guidelines

### `/.github/copilot-instructions.md`
**Purpose**: Global instructions for GitHub Copilot behavior and project rules
**Details**: Defines key must-follow items, memory management, Context7 usage, and commit protocols

### `/.github/instructions/csharp.instructions.md`
**Purpose**: Comprehensive C# and .NET development instructions and coding rules
**Details**: Defines code style, project structure, naming conventions, testing framework guidelines, documentation requirements, and MCP-specific implementation patterns

### `/.github/instructions/tests.instructions.md`
**Purpose**: Testing guidelines and rules for the project
**Details**: Defines rules for creating and running unit tests, UI tests, and maintaining test quality

### `/.github/workflows/ci.yml`
**Purpose**: GitHub Actions continuous integration workflow
**Details**: Automated CI/CD pipeline that runs on push/PR to main, dev, and features/* branches. Executes unit tests and code quality checks. Uses .NET 9.0. Integration tests and smoke tests excluded for faster CI runs

### `/.github/workflows/README.md`
**Purpose**: Documentation for GitHub Actions workflows
**Details**: Comprehensive guide explaining CI workflow jobs, test execution order, artifacts, environment variables, security considerations, and monitoring capabilities

### `/AGENTS.md`
**Purpose**: Repository map and development guidelines for agents and workflow
**Details**: Defines style, architecture constraints, PR rules, and contact information

### `/README.md`
**Purpose**: Main project readme file
**Details**: Entry point documentation for the repository

### `/setup/`
**Purpose**: Setup documentation directory
**Details**: Contains instructions for setting up the repository in VS Code and configuring MCP servers

### `/setup/README.md`
**Purpose**: Comprehensive setup guide for development environment
**Details**: Lists prerequisites, required software, environment setup steps, and links to project-specific MCP configuration. Provides clear onboarding path for new developers

### `/dev.env.example`
**Purpose**: Template of environment variables for local development
**Details**: Non-secret example values for Azure OpenAI and orchestrator flags; users copy to `dev.env` (ignored) to load variables easily. Note: Currently, only the Orchestrator Agent project uses environment variables; the KB MCP Server does not require any environment configuration in the prototype implementation

## 💻 Getting started Documentation

### `/src/orchestrator-agent/README.md`
**Purpose**: Usage and integration guide for the Orchestrator MCP Server
**Details**: Documents tools (`get_orchestrator_status`, `ask_domain_question`), configuration via environment variables & appsettings, GitHub Copilot MCP client setup snippet, example prompts, troubleshooting, and security notes. Added post Step 11 for discoverability and onboarding

### `/src/orchestrator-agent/FUTURE-MULTI-AGENT-WORKFLOW-APPROACH.md`
**Purpose**: Forward-looking architecture & roadmap for evolving from single-step Q&A to a multi-agent orchestration layer
**Details**: Compares Thin vs SK-Embedded vs Hybrid orchestrator patterns, defines staged migration plan, data contract evolution (steps, provenance), security & failure strategy, decision drivers, and actionable near-term steps (interfaces + extraction of Chat Agent). Serves as a living guide for incremental, low-risk scaling

## 📊 Journey Documentation

### `/docs-journey-log/Reasoning-Journal-Log.md`
**Purpose**: Complete reasoning and decision-making log throughout the project development
**Details**: Documents all prompts, outputs, decisions, and iterations for transparency and learning

### `/docs-journey-log/approaches-for-reasoning-journal-logging/`
**Purpose**: Research outputs on different approaches for documenting the reasoning process
**Details**: Contains analysis from M365 Copilot, ChatGPT, and Claude on best practices for logging development reasoning and decision-making processes

### `/docs-journey-log/temp-word-docs/`
**Purpose**: Temporary working Word documents captured during live reasoning/journaling and prompt crafting sessions
**Details**: Holds ad-hoc draft journals, research prompt experiments, and transient planning notes migrated from former `/docs/temp-word-docs/` (directory relocated for better alignment with journey log artifacts). Treated as non-authoritative scratch sources; canonical distilled content is moved into Markdown specs/architecture docs when stabilized

## 🗑️ Removed Files

### (Removed) `/run-orchestrator.sh`
**Status**: Removed
**Reason**: Simplified workflow now relies on direct `dotnet run` and MCP client auto-launch; script created duplicate execution path and added maintenance overhead

### (Removed) `/.vscode/tasks.json`
**Status**: Removed
**Reason**: Project standardized on direct terminal execution with `dev.env` rather than VS Code-specific configurations. Build commands are documented in AGENTS.md for portability across different IDEs

### (Removed) `/.vscode/launch.local.example.json`
**Status**: Removed
**Reason**: Debug launch indirection replaced by explicit terminal instructions using `dev.env` for portability across editors and CI
**Reason**: Simplified workflow now relies on direct `dotnet run` and MCP client auto-launch; script created duplicate execution path and added maintenance overhead.

### (Removed) `/.vscode/tasks.json`
**Status**: Removed.
**Reason**: Project standardized on direct terminal execution with `dev.env` rather than VS Code-specific configurations. Build commands are documented in AGENTS.md for portability across different IDEs.

### (Removed) `/.vscode/launch.local.example.json`
**Status**: Removed.
**Reason**: Debug launch indirection replaced by explicit terminal instructions using `dev.env` for portability across editors and CI.
