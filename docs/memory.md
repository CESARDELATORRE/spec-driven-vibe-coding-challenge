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

### Implementation plans documentation

#### `/docs/implementation-plans/feature-implementation-plan-kb-mcp-server.md`
**Purpose**: Detailed implementation plan for the KB MCP Server feature
**Details**: Step-by-step implementation guide with 10 steps covering project setup, services, MCP tools, testing, and configuration. Uses domain-agnostic naming following coding rules for reusability across different knowledge domains

### `/tests/mcp-server-kb-content-fetcher.integration-tests/mcp-server-kb-content-fetcher.integration-tests.csproj`
**Purpose**: Project file for integration tests exercising real MCP STDIO protocol
**Details**: References main project; includes xUnit + coverlet packages

### `/tests/mcp-server-kb-content-fetcher.integration-tests/McpServerProtocolTests.cs`
**Purpose**: End-to-end MCP protocol tests (initialize, tools/list, tools/call)
**Details**: Launches server with `dotnet run`, sends JSON-RPC over redirected STDIN/STDOUT, asserts tool discovery & search results

### `/src/mcp-server-kb-content-fetcher/tools/GetKbContentTool.cs`
**Purpose**: MCP tool exposing full raw knowledge base text content for prototype agent consumption
**Details**: Zero-argument tool returning status, contentLength, and full content string (loaded at startup via `FileKnowledgeBaseService`)

### `/tests/mcp-server-kb-content-fetcher.integration-tests/README.md`
**Purpose**: Documentation for running and extending MCP integration tests
**Details**: Explains handshake test scope, skipped tool tests rationale, CLI & VS Code Test Explorer usage, and future enhancements

### `/tests/mcp-server-kb-content-fetcher.unit-tests/tools/GetKbContentToolTests.cs`
**Purpose**: Unit tests for `GetKbContentTool` covering success, empty content, and error scenarios
**Details**: Mocks `IKnowledgeBaseService` to validate tool behavior without requiring file IO

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
