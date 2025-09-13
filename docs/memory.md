# Memory: File Reference Guide

This document provides a brief description of each file's purpose and relevant details for the spec-driven vibe coding challenge project.

## Project Documentation

### `/docs/03-idea-vision-scope.md`
**Purpose**: Comprehensive project vision and scope document defining the AMG-specific AI agent concept
**Details**: Contains executive summary, problem definition, functional requirements, and prototype scope with clear horizon planning

### `/docs/04-architecture-technologies.md`  
**Purpose**: Technical architecture and technology stack recommendations with evolution path
**Details**: Defines four architecture variants from prototype to production, technology selections (.NET, MCP, Semantic Kernel), and testing strategies

### `/docs/specs/feature-specs-kb-mcp-server.md`
**Purpose**: Detailed functional specification for the Knowledge Base MCP Server feature
**Details**: Complete feature specification including user journey, functional requirements, MCP tools (search_knowledge, get_kb_info), prototype constraints, and startup loading rationale appendix

### `/docs/implementation-plans/feature-implementation-plan-kb-mcp-server.md`
**Purpose**: Detailed implementation plan for the KB MCP Server feature
**Details**: Step-by-step implementation guide with 10 steps covering project setup, services, MCP tools, testing, and configuration. Uses domain-agnostic naming following coding rules for reusability across different knowledge domains

### `/docs/tradeoffs.md`
**Purpose**: Documents all technical and functional tradeoffs made during project development
**Details**: Contains 13 documented tradeoffs covering integration environment, knowledge base complexity, search behavior, and implementation decisions with reasoning and impact analysis

### `/docs/assumptions.md`
**Purpose**: Documents project assumptions about customer feedback and business context
**Details**: Clarifies exercise constraints and assumed validation for prototype development

## Development Guidelines

### `/.github/copilot-instructions.md`
**Purpose**: Global instructions for GitHub Copilot behavior and project rules
**Details**: Defines key must-follow items, memory management, Context7 usage, and commit protocols

### `/.github/instructions/csharp.instructions.md`
**Purpose**: Comprehensive C# and .NET development instructions and coding rules
**Details**: Defines code style, project structure, naming conventions, testing framework guidelines, documentation requirements, and MCP-specific implementation patterns

### `/.github/prompts/feature.specs.kb-mcp-server.prompt.md`
**Purpose**: Template prompt for generating KB MCP Server feature specifications
**Details**: Reusable prompt template with embedded rules for prototype/POC focus and specification generation guidelines

### `/.github/prompts/feature.implementation-plan.prompt.md`
**Purpose**: Template prompt for generating feature implementation plans
**Details**: Generic prompt template for creating detailed implementation plans from feature specifications, with rules for prototype/POC simplicity and references to updated coding rules

### `/AGENTS.md`
**Purpose**: Repository map and development guidelines for agents and workflow
**Details**: Defines style, architecture constraints, PR rules, and contact information

### `/docs/coding-rules.md`
**Purpose**: Coding conventions and project structure guidelines
**Details**: Defines kebab-case folder naming, .NET project structure, namespace conventions, and testing framework guidelines

## Journey Documentation

### `/docs-journey-log/Reasoning-Journal-Log.md`
**Purpose**: Complete reasoning and decision-making log throughout the project development
**Details**: Documents all prompts, outputs, decisions, and iterations for transparency and learning
