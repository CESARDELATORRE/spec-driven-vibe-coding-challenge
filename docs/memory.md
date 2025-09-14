# Memory: File Reference Guide

This document provides a brief description of each file's purpose and relevant details for the spec-driven vibe coding challenge project.

## Global Project Documentation

### `/docs/01-original-challenge-definition.md`
**Purpose**: Original challenge definition and requirements document
**Details**: Contains the original problem statement, objectives, and requirements for the spec-driven vibe coding challenge

### `/docs/02-plain-goals-and-approaches.md`
**Purpose**: North-star document defining fundamental project goals and approaches
**Details**: Baseline document for generating detailed idea/vision-scope, architecture definition and implementation code. Contains product goals, technical specifications, and project philosophy

### `/docs/03-idea-vision-scope.md`
**Purpose**: Comprehensive project vision and scope document defining the AMG-specific AI agent concept
**Details**: Contains executive summary, problem definition, functional requirements, and prototype scope with clear horizon planning

### `/docs/04-architecture-technologies.md`  
**Purpose**: Technical architecture and technology stack recommendations with evolution path
**Details**: Defines four architecture variants from prototype to production, technology selections (.NET, MCP, Semantic Kernel), and testing strategies

### `/docs/specs/feature-specs-kb-mcp-server.md`
**Purpose**: Detailed functional specification for the Knowledge Base MCP Server feature
**Details**: Complete feature specification including user journey, functional requirements, MCP tools (search_knowledge, get_kb_info), prototype constraints, and startup loading rationale appendix

### `/docs/specs/feature-specs-chat-agent.md`
**Purpose**: Functional specification for the Chat Agent component providing conversational AI capabilities
**Details**: Defines user journey, functional requirements for prototype/POC scope, extensive out-of-scope items for future development, success metrics, and additional considerations for testing, configuration, and integration


### `/docs/tradeoffs.md`
**Purpose**: Documents all technical and functional tradeoffs made during project development
**Details**: Contains 13 documented tradeoffs covering integration environment, knowledge base complexity, search behavior, and implementation decisions with reasoning and impact analysis

### `/docs/assumptions.md`
**Purpose**: Documents project assumptions about customer feedback and business context
**Details**: Clarifies exercise constraints and assumed validation for prototype development

## Features Documentation

### Specs documentation

#### `/docs/specs/feature-specs-kb-mcp-server.md`
**Purpose**: Detailed functional specification for the Knowledge Base MCP Server feature
**Details**: Complete feature specification including user journey, functional requirements, MCP tools (search_knowledge, get_kb_info), prototype constraints, and startup loading rationale appendix

#### `/docs/specs/feature-specs-chat-agent.md`
**Purpose**: Functional specification for the Chat Agent component providing conversational AI capabilities
**Details**: Defines user journey, functional requirements for prototype/POC scope, extensive out-of-scope items for future development, success metrics, and additional considerations for testing, configuration, and integration

#### `/docs/specs/feature-specs-orchestrator-agent.md`
**Purpose**: Functional specification for the Orchestration Agent MCP server
**Details**: Defines user journey, MCP tools (ask_domain_question, get_orchestrator_status), functional requirements, success criteria, prototype constraints, and future evolution considerations. Scope limited to single-turn coordination of Chat Agent + KB MCP Server.

### Implementation plans documentation

#### `/docs/implementation-plans/feature-implementation-plan-kb-mcp-server.md`
**Purpose**: Detailed implementation plan for the KB MCP Server feature
**Details**: Step-by-step implementation guide with 10 steps covering project setup, services, MCP tools, testing, and configuration. Uses domain-agnostic naming following coding rules for reusability across different knowledge domains

#### `/docs/implementation-plans/feature-implementation-plan-orchestrator-agent.md` 
**Purpose**: Detailed implementation plan for the Orchestration Agent MCP Server feature 
**Details**: Step-by-step implementation guide with 12 steps covering project setup, MCP client integration, Semantic Kernel ChatCompletionAgent coordination, KB server communication via STDIO, and testing. Updated to prioritize environment variables for secrets/config (portable to containers & cloud) with optional local User Secrets layering only in Development; anticipates future Azure Key Vault integration without refactoring tool code. 
 

## Development Guidelines

### `/.github/copilot-instructions.md`
**Purpose**: Global instructions for GitHub Copilot behavior and project rules
**Details**: Defines key must-follow items, memory management, Context7 usage, and commit protocols

### `/.github/instructions/csharp.instructions.md`
**Purpose**: Comprehensive C# and .NET development instructions and coding rules
**Details**: Defines code style, project structure, naming conventions, testing framework guidelines, documentation requirements, and MCP-specific implementation patterns

### `/.github/instructions/tests.instructions.md`
**Purpose**: Testing guidelines and rules for the project
**Details**: Defines rules for creating and running unit tests, UI tests, and maintaining test quality

### `/.github/prompts/feature.specs.kb-mcp-server.prompt.md`
**Purpose**: Template prompt for generating KB MCP Server feature specifications
**Details**: Reusable prompt template with embedded rules for prototype/POC focus and specification generation guidelines

### `/.github/prompts/feature.specs.chat-agent.prompt.md`
**Purpose**: Template prompt for generating Chat Agent feature specifications
**Details**: Reusable prompt template with embedded rules for prototype/POC focus, simple user journey design, and functional requirements structuring
### `/.github/prompts/feature.implementation-plan.prompt.md`
**Purpose**: Template prompt for generating feature implementation plans
**Details**: Generic prompt template for creating detailed implementation plans from feature specifications, with rules for prototype/POC simplicity and references to updated coding rules

### `/.github/prompts/new.idea.vision.scope.prompt.md`
**Purpose**: Template prompt for creating new project idea and vision scope documents
**Details**: Comprehensive prompt template that guides the creation of idea documents with structured sections including executive summary, context, product overview, and risk assessment

### `/.github/prompts/architecture-technology.prompt.md`
**Purpose**: Template prompt for generating architecture and technology decision documents
**Details**: Prompt template for creating comprehensive architecture documents with technology stack recommendations, evolution paths, and integration with Perplexity MCP server for research

### `/AGENTS.md`
**Purpose**: Repository map and development guidelines for agents and workflow
**Details**: Defines style, architecture constraints, PR rules, and contact information

## Journey Documentation

### `/docs-journey-log/Reasoning-Journal-Log.md`
**Purpose**: Complete reasoning and decision-making log throughout the project development
**Details**: Documents all prompts, outputs, decisions, and iterations for transparency and learning

### `/docs-journey-log/approaches-for-reasoning-journal-logging/`
**Purpose**: Research outputs on different approaches for documenting the reasoning process
**Details**: Contains analysis from M365 Copilot, ChatGPT, and Claude on best practices for logging development reasoning and decision-making processes

### `/src/orchestrator-agent/`
**Purpose**: Orchestration MCP server project coordinating ChatCompletionAgent and KB MCP server for single-turn Q&A (prototype).
**Details**: Contains `Program.cs` for MCP host setup, `orchestrator-agent.csproj` with dependencies, and `tools/OrchestratorTools.cs` initial tool implementations (status + placeholder question tool to be expanded in later steps).

### `/src/orchestrator-agent/appsettings.json`
**Purpose**: Non-secret defaults for orchestrator agent (KB MCP server executable path, greeting patterns, logging levels).
**Details**: Consumed at startup (required) and overrideable via environment variables (`KbMcpServer__ExecutablePath`, etc.); supports greeting heuristic configuration.
