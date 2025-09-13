# Architecture and Technologies Document

## Introduction & Objectives

This document presents a comprehensive analysis of architecture alternatives and technology stack recommendations for building a domain-specific AI agent system focused on Azure Managed Grafana (AMG). The architectural approach emphasizes modular, scalable design patterns that enable rapid prototyping while providing clear evolution paths toward production-ready deployments.

The primary objective is to establish multiple architecture variants that support systematic progression from simple prototype implementations through enterprise-scale deployments. Each variant builds incrementally upon previous implementations, enabling development teams to validate concepts quickly while maintaining architectural flexibility for future scaling requirements.

The architectural analysis addresses three critical deployment scenarios: initial prototype/POC using MCP STDIO for rapid development iteration, MVP implementation incorporating HTTP transport and containerization for distributed deployment, and scalable production architecture supporting Kubernetes orchestration in Azure environments. Each variant leverages the complementary strengths of Model Context Protocol (MCP), Semantic Kernel, and Azure services to create robust, maintainable AI agent systems.

## High-Level Architecture

### System Overview

The proposed architecture implements a modular AI agent system built around three core components that interact through standardized protocols to provide domain-specific conversational capabilities. The Knowledge Base MCP Server manages access to AMG-specific information through MCP-compliant interfaces, enabling flexible data source integration while maintaining security boundaries. The Chat Agent (previously described as the "Intelligent Chat Agent"—single consolidated naming used from here forward) provides conversational AI capabilities through Azure Foundry OpenAI integration, implementing sophisticated natural language processing for user interaction. The Orchestration Agent coordinates between knowledge sources and chat capabilities, implementing complex workflow management through Semantic Kernel's advanced orchestration patterns.

The architectural foundation leverages Model Context Protocol as the primary integration mechanism, providing standardized interfaces that enable loose coupling between system components while maintaining consistent communication patterns. This approach facilitates independent component development, testing, and deployment while ensuring reliable inter-component communication across different transport mechanisms and deployment environments.

### Component Interactions

The system implements a layered interaction model where user requests flow through the Orchestration Agent, which coordinates knowledge retrieval through the KB MCP Server and response generation through the Chat Agent. This design enables sophisticated conversation management while maintaining clear separation of concerns between knowledge access, conversation logic, and user interface integration.

The Orchestration Agent serves as the primary coordination point, implementing Semantic Kernel's orchestration patterns to manage complex multi-step interactions that might require multiple knowledge lookups, context analysis, and response synthesis. The agent maintains conversation state, manages context windows, and coordinates tool usage to provide coherent, helpful responses that leverage the full capabilities of the knowledge base and chat system.

### Component Inventory & Variant Mapping

The following table clarifies each logical component, its responsibility, and how its deployment / interaction pattern evolves across architecture variants. This also resolves the earlier naming ambiguity: **"Intelligent Chat Agent" and "Chat Agent" are the same component.** The canonical name is now **Chat Agent**.

| Component | Core Responsibility | Variant 1 (Prototype) | Variant 2 (Local Decoupled) | Variant 3 (MVP - ACA) | Variant 4 (Prod - AKS) | Notes / Interaction Nuances |
|-----------|---------------------|-----------------------|-----------------------------|-----------------------|------------------------|-----------------------------|
| Chat UI (Client) | Human interaction surface (could be CLI, MCP-compatible editor, or lightweight web UI) | Direct STDIO session to Orchestration Agent | HTTP/SSE (client ↔ gateway or directly to orchestrator) | Public/Private ingress via ACA front endpoint | API Gateway / Ingress Controller (mTLS / OAuth) | Not an internal agent—treat as external consumer; auth hardening added in later variants. |
| Orchestration Agent | Conversation state, tool routing, multi-step planning, context assembly | Process-host + in-process Chat Agent; STDIO to KB Server | Separate process/container; HTTP/SSE to Chat Agent & KB Server | Independent container app (horizontal scale) | Dedicated deployment / scaled pods with autoscaling (CPU + custom metrics) | Implements SK planners; owns retry logic & response synthesis. |
| Chat Agent (aka former Intelligent Chat Agent) | LLM prompt construction, response post-processing, safety filtering | In-process library inside Orchestration Agent | Separate container (optional) or still co-located depending on latency needs | Separate container app to scale independently (token throughput) | Dedicated microservice pod with model-specific tuning / caching | Can be merged back with Orchestration for cost or early simplification; separation enables independent scaling. |
| KB MCP Server | Domain knowledge retrieval (files, future API connectors) via MCP tools | Separate process via STDIO (simple file-based) | Container exposing HTTP/SSE MCP endpoints | Container app with secrets + telemetry | AKS deployment with sharding / caching layers | May later integrate vector store / embedding pipeline; contract remains MCP tool interface. |
| Transport Layer Abstraction | Uniform interface (STDIO/SSE/HTTP Streaming) for agent & KB calls | Minimal (direct STDIO) | Introduced abstraction layer for protocol switching | Formalized library / shared package | Stable internal SDK w/ resiliency (circuit breakers, retries) | Keeps higher layers unaware of protocol transition roadmap. |
| Observability / Telemetry (future) | Metrics, traces, conversation analytics | Console logs only | Basic structured logs (JSON) | App Insights / tracing + correlation IDs | Full distributed tracing + AI usage analytics | Added progressively; NOT part of functional core in Variant 1. |
| Secrets & Config | Secure handling of API keys, endpoints | .env / user-secrets (developer machine) | Docker secrets / mounted config | Azure Key Vault + managed identity | Centralized secret & policy management (Key Vault / CSI driver) | Hardened only from MVP onward. |

#### Interaction Flow (Canonical Naming)
1. Chat UI submits user utterance to Orchestration Agent.
2. Orchestration Agent decides: (a) direct LLM request via Chat Agent, (b) tool invocation via KB MCP Server, or (c) multi-step plan (tool(s) then LLM synthesis).
3. Chat Agent formats prompts, performs response shaping (e.g., trimming, safety heuristics), and returns candidate responses.
4. Orchestration Agent integrates knowledge payloads + model output, applies conversation state rules, and returns final response to UI.
5. (Future) Telemetry pipeline captures trace spans for each decision node.

#### Naming Standard
- Use **Chat Agent** as the consistent label going forward. If legacy references exist (e.g., “Intelligent Chat Agent”), treat them as aliases with a planned cleanup pass.
- Orchestration Agent is the *only* component permitted to call multiple agents/tools in one logical user turn (single coordination locus).

#### Rationale for Separation (Chat Agent vs Orchestration Agent)
- Keeps planning / tool routing (stateful, multi-turn) separate from model interaction (stateless per call except for injected history/context).
- Enables future experimentation: swap Chat Agent implementation (different LLM backend or prompt templating strategy) without destabilizing orchestration.
- Scaling: High token throughput pressure sits in Chat Agent; cognitive orchestration logic scales differently.

#### Variant Simplification Option
For extremely constrained prototypes, Chat Agent + Orchestration Agent can be collapsed **without** altering downstream evolution path—public contracts (interfaces / message schemas) should still be shaped as if separation exists to avoid refactors.

### Prototype/POC Architecture

![Prototype/POC Architecture Diagram](simplified-directions/prototype-poc-architecture-diagram.png)

The initial prototype architecture emphasizes simplicity and rapid development iteration through STDIO-based MCP communication and in-process component integration. The Chat UI connects directly to the Orchestration Agent through MCP STDIO, enabling immediate testing and validation through compatible interfaces like GitHub Copilot or Claude Desktop. The Orchestration Agent coordinates with both the Chat Agent and KB MCP Server using STDIO transport, maintaining simple process-based communication that eliminates network complexity during development phases.

This architecture enables rapid prototyping by minimizing infrastructure requirements while providing complete functionality for validation and demonstration purposes. The STDIO transport ensures reliable communication with minimal configuration overhead, allowing development teams to focus on agent logic and conversation quality rather than deployment complexity.

## Technologies, Stack & Tools

### Core Technology Stack Foundation

The technology foundation centers on .NET/C# as the primary development platform, leveraging Microsoft's comprehensive ecosystem for AI agent development. The Model Context Protocol .NET SDK provides production-ready capabilities for building MCP-compliant servers with familiar ASP.NET Core patterns and robust dependency injection support. Semantic Kernel enables sophisticated AI agent orchestration through its mature framework for planning, memory management, and multi-agent coordination. Azure Foundry OpenAI Service provides enterprise-grade language model access with built-in security, compliance, and monitoring capabilities essential for production deployments.

While earlier iterations referenced **.NET 8** as the baseline, current strategic guidance (Microsoft Learn previews, .NET Blog MCP posts, and evolving SK/MCP samples) encourages evaluating **.NET 10 (Preview 6 or higher)** for *new* MCP servers and Semantic Kernel agent projects **where preview risk is acceptable**. Current previews improve developer ergonomics (incremental hosting + packaging experience and community scaffolding patterns) but there is **no officially published dedicated `dotnet new` MCP server template yet**—teams still scaffold from existing console / minimal API templates and add MCP packages manually.[^templates] Any perceived “alignment improvements” with Semantic Kernel are incremental and derive mainly from staying current with SK releases rather than .NET 10–specific runtime capabilities. Teams already running stable workloads on .NET 8 (LTS) may remain there until .NET 10 reaches GA; experimental or greenfield efforts can target .NET 10 to reduce forward migration overhead if governance permits.[^preview]

> Version Strategy Summary (Strategic – not an official mandate):
> - New development (innovation track): .NET 10 previews – when accepting preview risk & needing latest MCP/SK iteration velocity
> - Existing production (stability track): Remain on .NET 8 LTS until .NET 10 GA unless a blocking feature gap justifies earlier move
> - Transitional prototypes on .NET 9: Upgrade directly to .NET 10 (expected low breaking delta for typical MCP/SK usage—still regression test)

> Preview Use Disclaimer: Previews are **non-LTS** and can introduce breaking changes between drops. Apply ring-based rollout (dev → integration → staging) with explicit rollback points and freeze adoption N weeks prior to major demos or releases.[^preview]

The architectural approach emphasizes leveraging proven Microsoft technologies that provide strong integration capabilities, comprehensive documentation, and robust tooling support. This technology selection enables rapid development while ensuring compatibility with enterprise requirements including security, monitoring, and operational management that become critical as systems scale toward production deployment.

### Runtime & SDK Version Recommendations

| Area | Recommended | Alternate (Stable / LTS) | Notes |
|------|-------------|--------------------------|-------|
| .NET Runtime | **.NET 10 Preview 6+** (innovation track) | .NET 8 LTS (stability track) | Previews optional; choose based on risk appetite & feature need. Plan cut-over post GA. |
| MCP Server SDK (C#) | Latest prerelease (ModelContextProtocol.*) | N/A | Manual scaffolding (no official `dotnet new mcp` template yet). Monitor for template emergence. |
| Semantic Kernel | Latest released (keep within 1–2 versions of head) | Pinned prior minor | Frequent updates reduce integration drift; test for behavior shifts in planners. |
| Transport | STDIO (inner loop), HTTP Streaming (future-aligned), SSE (transitional) | — | SSE usable but moving toward HTTP Streaming; abstract transport to ease swap. |
| Packaging | NuGet (.NET tool / library) | Local project refs | Enables discovery & reuse; private feeds (Azure Artifacts) for internal servers. |

Rationale:
- **Forward Compatibility:** Evaluating on .NET 10 previews early reduces migration friction at GA if no blocking regressions surface.
- **Iteration Velocity:** MCP & SK enhancements surface first in prereleases; early adoption accelerates feedback loops.
- **Packaging & Distribution:** Streamlined NuGet practices (not runtime-bound) shorten path from prototype to shareable tool/server.
- **Risk Containment:** Dual-track branching (e.g., `main`= net8.0, `develop`= net10.0) isolates preview volatility from production.

#### Transport Evolution
| Aspect | Current | Emerging | Action |
|--------|---------|----------|--------|
| Local Dev | STDIO dominant | Continues | Keep as primary for inner-loop speed. |
| Remote Streaming | SSE supported | HTTP Streaming (SSE backward compatible) | Wrap transport behind interface; plan migration tests. |
| Future Options | — | Potential WebSocket or hybrid patterns | Track MCP spec updates; avoid premature optimization. |
| Risk | SSE deprecation path | Minor migration overhead | Provide adapter layer & integration tests now. |

Design Recommendation: implement a transport abstraction (e.g., `IMcpTransportClient`) with dependency injection to enable a compile-time + runtime switch (STDIO vs SSE vs HTTP Streaming) and facilitate progressive rollout experiments.

Migration Guidance (if currently on .NET 8/9):
1. Update `global.json` (if present) with roll-forward settings (`rollForward": "latestFeature"`) to allow preview SDK usage in controlled environments.
2. Increment `TargetFramework` to `net10.0` **OR** multi-target (`<TargetFrameworks>net8.0;net10.0</TargetFrameworks>`) for libraries consumed by mixed environments.
3. Bump Semantic Kernel & MCP SDK prereleases; document version pins in a `/docs/versions.md` manifest for reproducibility.
4. Diff new minimal-host patterns (Program.cs) manually—apply only additive improvements (logging filters, DI patterns). Avoid wholesale file replacement to preserve custom logic.
5. Run regression suite: (a) unit tests, (b) transport integration tests (STDIO/SSE), (c) prompt evaluation harness (semantic similarity thresholds), (d) performance smoke (latency & allocation).
6. Capture deltas; if semantic drift > agreed tolerance (e.g., cosine sim < 0.85 vs baseline), open investigation before promoting.

Fallback Strategy:
- Blocking dependency / perf regression: retain dual-targeting and publish only `net8.0` assets; keep preview path gated behind feature flag.
- Security advisory affecting preview only: suspend preview track CI; rebase patches onto LTS branch; resume once patched.

Planned Evolution:
- GA adoption checkpoint (criteria: green perf benchmarks, no Sev-1 regressions, security review sign-off).
- Introduce automated perf harness (latency p95, tokens/sec, memory footprint, CPU %) comparing `net8.0` vs `net10.0` nightly.
- Remove dual-targeting after 2 consecutive stable releases post GA (or earlier if dependency graph fully compatible).

#### Dual Targeting Considerations
| Dimension | Benefit | Cost/Risk | Mitigation |
|-----------|---------|-----------|------------|
| Compatibility | Supports mixed runtime estate | Larger test matrix | Matrix pruning (focus on critical paths) |
| Adoption Velocity | Early preview feedback | Potential churn | Version pin + weekly upgrade cadence |
| Build Time | Parallel TFMs slow CI | Longer pipelines | Incremental build caching |
| Package Size | Larger NuGet artifacts | Storage / bandwidth | Drop older TFM once ≥90% consumers upgraded |
| Cognitive Load | More conditions in code | Complexity | Encapsulate runtime-specific code behind services |

#### Adoption Matrix (Strategic Tracks)
| Track | Runtime | Primary Goal | When to Promote | Exit Criteria |
|-------|---------|--------------|-----------------|---------------|
| Stability | .NET 8 LTS | Predictable ops | Security / perf parity proven in preview | GA + validated SLO adherence |
| Innovation | .NET 10 Preview | Feature velocity & early MCP/SK changes | Passing regression + acceptable drift | GA + sign-off, then merged into stability |
| Sunset | .NET 9 (if present) | Temporary bridge | Complete by first .NET 10 RC | No consuming services remain |

Key References (public examples & articles – verify latest versions when implementing):
- Microsoft Learn quickstarts (MCP + .NET previews)
- .NET Blog posts on MCP server building & NuGet distribution
- Semantic Kernel multi-agent + Azure AI Foundry integration samples
- NuGet MCP packaging guidance (official docs)

[^templates]: As of the referenced previews, MCP servers are scaffolded from existing `console` / `web` templates plus adding ModelContextProtocol packages; no dedicated first-party `dotnet new mcpserver` template publicly documented.
[^preview]: Preview adoption should follow an engineering RFC with explicit rollback triggers (e.g., perf regression >20%, security advisory, breaking protocol change) and isolation from production release branches.


### Prototype Testing Infrastructure (Variant 1 Scope Only)

For the **Prototype/POC (Variant 1)** we adopt a deliberately *minimal* testing strategy optimized for speed of iteration over exhaustive coverage:

| Layer | Scope (Variant 1) | Tooling | Deferral Notes |
|-------|-------------------|---------|----------------|
| Unit | MCP tool parameter validation & simple success path | xUnit / MSTest (light) | Edge/error branches largely deferred |
| Integration | Orchestrator ↔ KB Server (STDIO) basic round‑trip | In-process harness | Multi-transport & failure simulation deferred |
| Prompt / Semantic | Golden prompt snapshots + manual qualitative review | Promptfoo (optional), manual | Automated semantic similarity scoring deferred |
| Observability | Console logging only | Built-in logging | Structured & telemetry pipelines deferred |
| Performance | Ad-hoc manual timing (single run) | Stopwatch / simple script | Load, soak, stress deferred |

Design Principle: **Ship a working conversational loop first; harden later.** Any additional frameworks (Langfuse, full prompt evaluation matrices, property-based tests) are *explicitly out of scope* for Variant 1 to avoid premature optimization.

Future variants will layer in broader testing (integration across transports, performance baselines, resilience/chaos scenarios, security scanning) once architectural seams stabilize.

## Architecture Variants

### Variant 1: Initial Prototype/POC

**Technology Stack:**
- .NET 10 (Preview 6+) / C# (preferred for new work) — fallback: .NET 8 LTS
- MCP SDK for .NET (STDIO transport in preview templates)
- Semantic Kernel for agent orchestration
- Azure Foundry OpenAI Service for LLM capabilities
- Simple file-based knowledge storage

**Architecture Characteristics:**
The prototype architecture prioritizes rapid development and validation over scalability, implementing all components as console applications that communicate through MCP STDIO transport. The KB MCP Server reads AMG information from local text files, providing immediate knowledge access without external dependencies. The Chat Agent operates as an in-process Semantic Kernel component, eliminating deployment complexity while maintaining full conversational capabilities. The Orchestration Agent coordinates all interactions through STDIO-based MCP communication, ensuring reliable message passing with minimal configuration requirements.

**Development and Testing Approach:**
Testing focuses on validating core functionality through unit tests for individual MCP tools, integration tests for agent coordination, and conversation flow validation through manual testing with compatible chat interfaces. The testing strategy emphasizes rapid feedback cycles and immediate issue identification rather than comprehensive automated validation, enabling quick iteration on agent behavior and conversation quality.

**Deployment Model:**
Deployment involves simple executable distribution with configuration files, requiring only .NET runtime installation and Azure Foundry API credentials. The self-contained deployment model eliminates infrastructure dependencies while providing complete functionality for demonstration and validation purposes.

### Variant 2: Local Decoupled Solution (Testing/QA)

**Technology Stack:**
- Previous stack (aligned to .NET 10 Preview where feasible; .NET 8 acceptable for stability)
- MCP HTTP/SSE transport (in addition to STDIO for local tooling)
- Docker for component containerization
- Docker Compose for orchestration
- Enhanced configuration management

**Architecture Characteristics:**
The decoupled architecture evolves STDIO transport to HTTP-based communication, enabling distributed component deployment while maintaining local development simplicity. Each component operates in dedicated Docker containers with HTTP-based MCP communication, providing realistic production communication patterns while eliminating external infrastructure dependencies. The containerized approach enables comprehensive integration testing across different deployment configurations while maintaining development environment consistency.

> STDIO vs Containers (Why shift to HTTP): In Docker-based isolation, cross-container STDIO is impractical—process boundaries block direct file descriptor sharing, Compose would require fragile linkage, and any workaround would not map to Kubernetes. HTTP is the natural fit: native port exposure, built-in service discovery, network tooling for debugging, and a seamless path to scalable production.

**Testing (Evolution Placeholder):** Prototype test set continues to run unchanged. Additional suites (container networking, HTTP/SSE transport parity, basic latency baseline) are **TBD** and will be defined in a separate "Variant 2 Test Expansion" document prior to implementation.

**Development Benefits:**
The decoupled architecture enables independent component development and testing while providing realistic production behavior patterns. Development teams can iterate on individual components without affecting others, while integration testing validates complete system behavior through production-representative communication patterns.

### Variant 3: MVP (Azure Container Apps)

**Technology Stack:**
- Previous stack plus Azure Container Apps
- Azure Application Insights for monitoring
- Azure Key Vault for secrets management
- CI/CD pipeline integration
- (Optional) Dual-targeting net8.0 + net10.0 during transitional MVP phase

**Architecture Characteristics:**
The MVP architecture leverages Azure Container Apps for managed container orchestration, providing enterprise-grade deployment capabilities with minimal infrastructure management overhead. Components deploy as independent container apps with automatic scaling, built-in monitoring, and integrated security features. The architecture maintains HTTP-based MCP communication while adding production monitoring, secrets management, and automated deployment capabilities essential for business-critical applications.

**Production Readiness Features:** (Architecture view only at this stage) Monitoring, scaling, and secrets integration planned; *formal test expansion (resilience, load, security scanning) remains TBD* and will be chartered before MVP hardening.

**Operational Capabilities:** (Planned) Real-time monitoring & alerting are *not* implemented in Variant 1; instrumentation stories will accompany the MVP testing charter.

### Variant 4: Scalable Production (Azure Kubernetes Service)

**Technology Stack:**
- Previous stack plus Azure Kubernetes Service (AKS)
- AKS Fleet Manager for multi-cluster scaling
- Azure Monitor and Azure AI Foundry tracing
- Enterprise security and compliance features
- Standardize on net10.0 (post-GA) with deprecation plan for net8.0 artifacts

**Architecture Characteristics:**
The production architecture implements comprehensive Kubernetes orchestration through AKS, providing unlimited scaling capabilities and enterprise-grade operational features. The architecture supports multi-cluster deployments through AKS Fleet Manager, enabling global distribution and massive scale requirements that exceed single-cluster limitations. Advanced monitoring includes Azure AI Foundry tracing for agent behavior analysis and comprehensive operational metrics for performance optimization.

**Enterprise Features:**
Production deployment includes comprehensive security controls, compliance monitoring, audit trail management, and integration with enterprise identity management systems. The architecture supports multi-region deployment for global availability, disaster recovery capabilities, and comprehensive backup and restore procedures essential for business-critical AI applications.

**Scalability and Performance:**
The Kubernetes architecture supports horizontal scaling across thousands of nodes, enabling AI agent systems that can handle massive concurrent user loads while maintaining performance and reliability standards. Advanced features include automatic resource management, intelligent load balancing, and performance optimization strategies that ensure cost-effective operation while meeting demanding performance requirements.

## Technology Differentiators

### MCP Protocol Advantages

Model Context Protocol provides standardized integration capabilities that eliminate the fragmented approaches typically associated with AI system integration. Research indicates up to 30% improvement in response accuracy compared to traditional model serving methods, stemming from MCP's ability to provide live, enterprise-specific data rather than relying on static embeddings. The protocol's dual transport support (STDIO and HTTP) enables flexible deployment strategies that can evolve from local development to distributed production environments without architectural changes.

### Semantic Kernel Orchestration Benefits

Semantic Kernel offers sophisticated multi-agent orchestration patterns including Sequential, Concurrent, Handoff, Group Chat, and Magentic coordination that enable complex problem decomposition and collaborative solution development. The framework's native Azure integration provides optimal performance when combined with Azure AI services while maintaining compatibility with other LLM providers. The unified developer experience across orchestration patterns enables rapid experimentation with different coordination strategies without significant code changes.

### Azure Service Integration

Azure Foundry OpenAI Service provides enterprise-grade language model access with built-in compliance, monitoring, and security features that are essential for production AI deployments. The service includes comprehensive token usage tracking, cost management capabilities, and integration with Azure security and compliance frameworks. Azure Container Apps and AKS provide managed infrastructure that eliminates operational overhead while providing enterprise-grade scaling, security, and monitoring capabilities.

### .NET Ecosystem Advantages

The .NET platform provides mature tooling, comprehensive package management, and robust deployment options that accelerate AI agent development. The official MCP SDK for .NET includes familiar ASP.NET Core patterns and dependency injection support that leverage existing .NET expertise. Strong Visual Studio integration, comprehensive debugging capabilities, and extensive testing frameworks provide development productivity advantages that reduce time-to-market for AI agent implementations.

## Future Testing Roadmap (Post-Prototype)

The following capabilities are *intentionally deferred* until after the Prototype (Variant 1) delivers validated core value:

| Future Capability | Target Variant | Trigger to Implement | Success Metric |
|-------------------|---------------|----------------------|----------------|
| Transport parity tests (STDIO vs HTTP/SSE) | 2 | HTTP transport adoption | <5% behavioral drift across transports |
| Container integration tests | 2 | First Docker Compose baseline | Green build under Compose matrix |
| Performance baseline & regression guard | 2 → 3 | Latency concerns / scaling goals | p95 latency stable ±10% over 5 builds |
| Structured observability (traces, Langfuse) | 3 | MVP readiness review | Key spans captured & searchable |
| Load & resilience (chaos, restart, network loss) | 3 → 4 | Pre-production gate | Automated scenario pass rate ≥95% |
| Security scanning & secret handling tests | 3 | Introduction of Key Vault / managed identity | Zero critical findings |
| Multi-region failover simulation | 4 | AKS multi-cluster planning | RPO/RTO targets validated |
| Advanced semantic evaluation harness | 3 | Prompt complexity growth | Drift alerts actionable (<15% unacceptable variance) |
| Human evaluation rubric & inter-rater scoring | 3 | Pilot user feedback cycle | Agreement coefficient ≥ target threshold |

Human-in-the-loop review, semantic similarity scoring, property-based generation fuzzing, and full failure injection are deferred to prevent over-investment before conversational core value is confirmed.

Principle: **Defer sophistication until signal justifies cost.** Each postponed layer includes a clear activation trigger to avoid indefinite deferral.

## Conclusion

The architectural analysis demonstrates that successful AI agent implementations require systematic progression through well-defined evolutionary stages that balance rapid prototyping needs with production scalability requirements. The proposed architecture variants provide clear migration paths from simple STDIO-based prototypes through enterprise-grade Kubernetes deployments while maintaining consistent development patterns and technology foundations.

The technology stack centered on .NET/C#, MCP, and Semantic Kernel provides robust capabilities for building sophisticated AI agents while leveraging proven Microsoft technologies that ensure enterprise compatibility and operational maturity. The comprehensive testing strategy addresses the unique challenges of AI system validation while providing practical approaches for maintaining quality and reliability as systems scale and evolve.

This architectural foundation enables organizations to begin AI agent development with minimal infrastructure requirements while maintaining clear evolution paths toward production deployments that can support enterprise-scale requirements. The modular design ensures that investments in initial prototype development translate directly to production capabilities while providing flexibility to adapt to changing business requirements and technological advances.