---
mode: 'agent'
tools: ['perplexity_research', 'perplexity_reason', 'perplexity_ask']
description: 'Research an approach to the architecture and technology stack for the project idea.'
---

Perform an indepth analysis of the provided simplified directions on architecture and stack/technologies provided in the docs as part of the context of the prompt.

The goal is to research and provide a detailed document with multiple alternatives for the Agentic solution architecture and related stack and technologies.

Rules:
- Clarify any details that might be helpful before starting to research my idea.
- The document should not be very long, maximum of around 5 pages, but should be detailed enough to provide a good overview of the architecture and technology stack alternatives.
- Use the provided simplified directions as the main context for your research.
- Start your session co-reasoning with me by doing some research using the #tool:f1e_perplexity_research. 
- Summarize your findings that might be relevant to me before beginning the next step.
- Perform another research loop if asked.

Main TOC or sections:

- Introduction & Objectives
(Introduction & Objectives of this architecture document. Do not get into business requirements or vision/scope since that's defined in another doc)

High-Level Architecture
(Main components, interactions, and system overview)

Technologies, stack & Tools
(Chosen tech stack, frameworks, cloud services, libraries)

Include the following pivots in your research:

- Multiple architecture variants:
    - 1. For initial prototype/POC (Simple MCP servers using MCP STDIO with agents running as processes)
    - 2. For MVP (including an evolved architecture using MCP HTTP towards Dockerized agents and MCP servers using docker-compose.
    - 3. For a scalable version, supporting deployment into Kubernetes clusters in AKS

- Technology stack (incremental evolution per architecture variant):
    - 1. For initial prototype/POC (.NET/C#, MCP STDIO, Semantic Kernel, Azure Foundry OpenAI Service)
    - 2. For local decoupled solution for testing/QA (Plus MCP HTTP, Docker)
    - 3. For MVP (Plus Azure Container Apps)
    - 4. For scalable version (Plus Kubernetes deployment, in AKS)

- Differentiators per each technology alternative

- For the initial prototype/POC, include the diagram docs\simplified-directions\prototype-poc-architecture-diagram.png into the architecture .MD document.

WHEN DONE, output to #file:../../docs/architecture-technologies.md or update the current ../../docs/03-architecture-technologies.md