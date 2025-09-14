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

### `/docs/specs/feature-specs-chat-agent.md`
**Purpose**: Functional specification for the Chat Agent component providing conversational AI capabilities
**Details**: Defines user journey, functional requirements for prototype/POC scope, extensive out-of-scope items for future development, success metrics, and additional considerations for testing, configuration, and integration

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

### `/.github/prompts/feature.specs.kb-mcp-server.prompt.md`
**Purpose**: Template prompt for generating KB MCP Server feature specifications
**Details**: Reusable prompt template with embedded rules for prototype/POC focus and specification generation guidelines

### `/.github/prompts/feature.specs.chat-agent.prompt.md`
**Purpose**: Template prompt for generating Chat Agent feature specifications
**Details**: Reusable prompt template with embedded rules for prototype/POC focus, simple user journey design, and functional requirements structuring

### `/AGENTS.md`
**Purpose**: Repository map and development guidelines for agents and workflow
**Details**: Defines style, architecture constraints, PR rules, and contact information

## Journey Documentation

### `/docs-journey-log/Reasoning-Journal-Log.md`
**Purpose**: Complete reasoning and decision-making log throughout the project development
**Details**: Documents all prompts, outputs, decisions, and iterations for transparency and learning
