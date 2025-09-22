# Architecture and Technologies Document

## Introduction

This document provides architecture guidance and technology stack recommendations for a domain‑specific AI agent system centered on Azure Managed Grafana (AMG) knowledge. It defines a progressive evolution path from a lightweight prototype to an enterprise‑grade, scalable deployment while preserving early development velocity.

### Objectives
1. Define three incremental architecture variants (Prototype / POC → Dockerized Decoupled → Cloud-Native) with clear evolution criteria.
2. Establish a coherent technology stack (.NET version strategy, MCP, Semantic Kernel, Azure services) with risk-managed runtime adoption.
3. Document core design principles (single orchestration, separation of concerns) and a consistent component naming standard.
4. Clarify transport strategy (STDIO for inner loop; HTTP/SSE/Streaming for decoupled & remote scenarios) without premature abstraction.
5. Outline minimal Prototype (Variant 1) testing scope plus a deferred, trigger-based future testing roadmap.
6. Ensure terminology consistency ("Chat Agent") and eliminate redundant or unsubstantiated claims.

### Scope
The scope includes functional interaction patterns between Orchestration, Chat, and Knowledge Base components; runtime and transport recommendations; deployment progression; and testing strategy. Broader production concerns (formal security reviews, advanced semantic evaluation harnesses, multi-region failover drills) are intentionally deferred until later variants when justified by adoption signals.

### Deployment Scenarios Covered
- Prototype / POC (Local Desktop): Fast iteration via MCP STDIO and in-process simplicity.
- Dockerized Decoupled (Local): Containerized components using HTTP/SSE for integration testing.
- Cloud-Native (Managed ACA or AKS): Single cloud-hosted deployment path (platform decision deferred until justified).

Each variant leverages the complementary strengths of Model Context Protocol (MCP), Semantic Kernel, and Azure platform services to create modular, maintainable AI agent capabilities while minimizing migration friction between stages.

## High-Level Architecture

### System Overview

The proposed architecture implements a modular AI agent system built around four core components that interact through standardized protocols to provide domain-specific conversational capabilities. The Knowledge Base MCP Server manages access to AMG-specific information through MCP-compliant interfaces, enabling flexible data source integration while maintaining security boundaries. The Chat Agent provides conversational AI capabilities through Azure AI Foundry integration, implementing sophisticated natural language processing for user interaction. The Orchestration Agent coordinates between knowledge sources and chat capabilities, implementing complex workflow management through Semantic Kernel's advanced orchestration patterns. In the Prototype (Variant 1) the Chat Agent is not a separate deployable service but an in-process Semantic Kernel `ChatCompletionAgent`; externalization is intentionally deferred to Variant 2+.

The architectural foundation leverages Model Context Protocol as the primary integration mechanism, providing standardized interfaces that enable loose coupling between system components while maintaining consistent communication patterns. This approach facilitates independent component development, testing, and deployment while ensuring reliable inter-component communication across different transport mechanisms and deployment environments.

### Component Interactions

The system implements a layered interaction model where user requests flow through the Orchestration Agent, which coordinates knowledge retrieval through the KB MCP Server and response generation through the Chat Agent. This design enables sophisticated conversation management while maintaining clear separation of concerns between knowledge access, conversation logic, and user interface integration.

The Orchestration Agent serves as the primary coordination point. In the Prototype (Variant 1) it performs explicit, linear logic: gather optional KB snippet(s), construct instructions, invoke the in‑process `ChatCompletionAgent`, and assemble the response. Conversation state persistence, context window trimming, and multi-tool sequencing are NOT automated; they are consciously deferred. Future variants may layer Semantic Kernel orchestration patterns (sequential / concurrent / group chat) plus memory plugins to introduce managed conversation history, tool arbitration, and adaptive context handling.

### Core Components

The system consists of four primary components that work together to deliver conversational AI capabilities:

| Component | Responsibility | Interface |
|-----------|----------------|-----------|
| **Chat UI** | Human interaction interface (CLI, MCP client, web UI) | Submits user requests and displays responses |
| **Orchestration Agent** | Request routing & response synthesis (Prototype: single-turn, manual KB + LLM invocation) | Receives requests, invokes KB + in-process LLM, assembles answer |
| **Chat Agent** | LLM interaction, prompt construction, response processing (Prototype: in-process Semantic Kernel `ChatCompletionAgent`; externalizes Variant 2+) | Handles Azure AI Foundry calls and response formatting |
| **KB MCP Server** | Domain knowledge access via MCP protocol | Provides AMG-specific information through MCP tools |

### Component Interaction Flow
1. **Chat UI** submits user utterance to **Orchestration Agent**
2. **Orchestration Agent** decides: direct LLM request, knowledge lookup, or multi-step plan
3. **Chat Agent** handles LLM interactions (Azure AI Foundry)
4. **Orchestration Agent** synthesizes results (no multi-turn context persisted in Variant 1)
5. Response delivered back to **Chat UI**

### Design Principles
- **Single Coordination:** Only Orchestration Agent manages multi-step workflows and can call multiple agents/tools in one logical user turn
- **Clear Separation:** Chat Agent = LLM specifics; Orchestration = business logic  
- **Flexible Deployment:** Components co-located (prototype) or distributed (production)

#### Naming Standard
- Use **Chat Agent** as the consistent label going forward. If legacy references exist (e.g., “Intelligent Chat Agent”), treat them as aliases with a planned cleanup pass.
- Orchestration Agent is the *only* component permitted to call multiple agents/tools in one logical user turn (single coordination locus).
 - **Domain Extensibility Principle:** Adding a second domain MUST NOT require refactoring orchestration core logic—only new KB assets + configuration.

## Architecture Variants & Evolution Path

### Overview
We implement three architectural variants that evolve capability while preserving early velocity. Former separate "MVP" and "Scalable Production" stages were collapsed into a single Cloud-Native variant to reduce premature branching. A hypothetical Enterprise Expansion (multi‑region, advanced compliance) could become a future Variant 4 if ever justified—explicitly out of scope now.

**Implementation Status (September 2025)**: Only Variant 1 (Local Desktop Prototype) has been implemented. Variants 2-3 are planned for future iterations based on prototype validation results.

### Variant Naming Canonical Mapping
| Variant | Canonical Label | Version Tag | Purpose |
|---------|-----------------|-------------|---------|
| Variant 1 | Prototype / POC (Local Desktop) | v0.1 | Fast inner‑loop validation (in‑process LLM + KB) |
| Variant 2 | Dockerized Decoupled (Local) | v2.0 | Process & transport decoupling (HTTP between components) |
| Variant 3 | Cloud-Native (Managed ACA or AKS) | v3.0 | Single cloud deployment path (platform choice TBD) |

All documents must reference variants using the pattern: "Variant N: Canonical Label (vX.Y)" on first mention.

### Variant 1: Prototype / POC (Local Desktop) (v0.1)

![Prototype / POC Architecture Diagram](./_images/v0.1-prototype-poc-architecture-diagram.png "Variant 1 – Local Desktop Prototype (STDIO, in‑process Chat Agent)")

**Technology Stack:**
- .NET 9 / C# (single target framework)
- MCP SDK for .NET (STDIO transport)
- Semantic Kernel for agent orchestration
- Azure AI Foundry for LLM capabilities
- File-based knowledge storage

**Characteristics:**
Console applications communicating via STDIO transport. KB MCP Server reads from local files; Chat Agent runs in-process with Semantic Kernel; Orchestration Agent coordinates via STDIO. Prioritizes rapid development and validation over scalability.

**Decoupling Path (Planned Variant 2+):**
- Introduce a thin internal interface around the in-process `ChatCompletionAgent` only when beginning Variant 2—avoid premature layering now.
- Externalize Chat Agent into a separate process/container using MCP over HTTP/SSE once transport introduction is justified (containerization trigger).
- Add provenance & latency instrumentation and optional transport negotiation before multi-process rollout.
- Preserve identical functional contract so Variant 1 tests remain valid regression assets.

**Testing:** Unit tests for MCP tools, integration tests for coordination, manual conversation validation.

**Deployment:** Simple executables with config files; requires only .NET runtime and Azure AI Foundry API credentials.

### Variant 2: Dockerized Decoupled (Local) (v2.0)

![Dockerized Decoupled Architecture Diagram](./_images/dockerized-architecture-diagram.png "Variant 2 – Dockerized Decoupled (HTTP/SSE + containers)")

**Technology Stack:**
- Previous stack (continues targeting .NET 9)
- MCP HTTP/SSE transport + STDIO for local tooling
- Docker + Docker Compose for orchestration
- Enhanced configuration management

**Characteristics:**
Components run in Docker containers with HTTP-based MCP communication. Enables distributed deployment while maintaining local development simplicity.

> **STDIO vs HTTP**: Docker isolation requires HTTP transport—process boundaries block STDIO sharing; HTTP provides native port exposure and service discovery.

**Testing:** Prototype tests continue unchanged. Additional container networking and transport parity tests TBD.

### Variant 3: Cloud-Native (Managed Azure Container Apps OR AKS) (v3.0)

![Cloud-Native Architecture Diagram](./_images/cloud-native-architecture-diagram.png "Variant 3 – Cloud-Native (ACA or AKS, secrets & observability)")

**Decision Frame:** A single cloud-native variant captures both managed PaaS (Azure Container Apps) and Kubernetes (AKS) options. The platform choice is deferred until a scaling or operational governance trigger demands capabilities beyond local Docker.

**Technology Stack (baseline irrespective of platform):**
- Previous stack (.NET 9, MCP over HTTP/SSE)
- Secrets: Azure Key Vault (or ACA secrets if interim) + managed identity
- Observability: Azure Monitor / Application Insights (minimal traces + metrics)
- Deployment: CI/CD pipeline (build, scan, deploy) + environment promotion

**If ACA Chosen:** Lower operational overhead, faster iteration, built-in scaling; limited deep cluster-level control.

**If AKS Chosen:** Greater customization (ingress, network policies, multi-namespace isolation), higher operational burden. Introduce only if required by multi-team tenancy, advanced networking, or custom scaling strategies.

**Deferred (Hypothetical Future Variant 4 – Enterprise Expansion):** Multi-region failover, geo-redundant storage, policy compliance automation, chaos and DR drills. Not part of current scope.


## Technologies, Stack & Tools

### Core Technology Stack

The architecture centers on .NET/C# with Microsoft's AI development ecosystem:

- **Model Context Protocol**: Production-ready .NET SDK with ASP.NET Core patterns
- **Semantic Kernel**: AI agent orchestration with planning and memory management  
- **Azure AI Foundry**: Enterprise-grade OpenAI access with security and compliance

#### .NET Version Strategy

All components target **.NET 9** for the Prototype (Variant 1) and subsequent planned variants until **.NET 10 reaches General Availability (GA)**. We intentionally avoid preview multi‑targeting to reduce churn and eliminate risk from breaking changes in evolving preview SDKs.

Key points:
- Stability First: Aligns with the Constitution simplicity principle—no speculative preview adoption.
- Upgrade Trigger: Reassess after .NET 10 GA AND prior to starting Variant 2 (Dockerized Decoupled) or earlier only if a critical dependency mandates it.
- Migration Expectation: Straightforward retarget (net9.0 → net10.0) followed by full build + unit + integration test run; no architectural adjustments anticipated.
- Deferred: No evaluation of .NET 10 preview-only features (no conditional compilation, no dual TFMs).

> Decision: Treat .NET 9 as the authoritative runtime baseline until explicit GA-triggered review.

#### MCP SDK Version Guidance
To ensure consistency across environments while allowing rapid evolution with the MCP ecosystem:

- Use the official Microsoft MCP SDK for .NET: https://www.nuget.org/packages/ModelContextProtocol/
- Target: Latest popular/stable preview (MCP is evolving rapidly)
- Minimum recommended version: 0.3.0-preview.4 (or newer if available)
- Add to the project with:
  ```bash
  dotnet add package ModelContextProtocol --version 0.3.0-preview.4
  ```
- Rationale:
  - Aligns with current feature set (tool discovery, fluent server builder, STDIO transport)
  - Reduces breaking change risk by pinning to a tested preview
  - Keeps upgrade path simple: bump version, validate build, run integration tests

> Upgrade Policy: Re-evaluate MCP SDK version after successful prototype completion and before starting Variant 2 (Dockerized Decoupled) to benefit from transport and stability improvements.

#### Transport Strategy

| Aspect | Current | Emerging | Action |
|--------|---------|----------|--------|
| Local Dev | STDIO | Continues | Primary for inner-loop development |
| Remote | SSE supported | HTTP Streaming preferred | Add HTTP/SSE when needed |
| Future | — | WebSocket potential | Track MCP spec updates |

> Prototype Decision: No transport abstraction layer; a single STDIO path keeps code surface minimal. Introduce an interface only when a second transport (HTTP/SSE) is actually integrated (earliest: Variant 2).

### Prototype/POC Testing (Variant 1)

Testing in the prototype is intentionally limited to what already exists under `tests/` to keep focus on core functionality:

- Unit Tests: Validate tool input validation, basic knowledge base loading/path resolution logic, and simple success/error paths.
- Integration Tests: Exercise real MCP STDIO round‑trips between Orchestrator and KB MCP Server using the actual server processes (no mocks).

Deferred Until Variant 2+:
- Prompt / semantic evaluation harness or automated answer quality scoring.
- Transport parity testing (HTTP/SSE vs STDIO).
- Performance/load, structured observability pipelines, container/network simulations.

Principle: Only test what changes now. Sophisticated evaluation and non-functional suites emerge with new architecture variants (trigger: adding second transport or externalized Chat Agent).

## Technology Differentiators

### Key Benefits

- **MCP Protocol**: Standardized integration layer enabling consistent tool/agent interoperability and dual transport support (STDIO & HTTP/SSE/Streaming trajectories)
- **Semantic Kernel**: Rich orchestration patterns (Sequential, Concurrent, Handoff, Group Chat) with unified prompt, memory, and planning abstractions
- **Azure Integration**: Managed services for compliance, observability, security, scaling, and operational governance
- **.NET Ecosystem**: Mature tooling, productive developer ergonomics, strong testing & diagnostics support

## Future Testing Roadmap (Post-Prototype)

The following capabilities are *intentionally deferred* until after the Prototype (Variant 1) delivers validated core value (target activation earliest Variant 2 unless otherwise noted):

| Future Capability | Target Variant | Trigger to Implement | Success Metric |
|-------------------|---------------|----------------------|----------------|
| Transport parity tests (STDIO vs HTTP/SSE) | 2 | HTTP transport adoption | <5% behavioral drift across transports |
| Container integration tests | 2 | First Docker Compose baseline | Green build under Compose matrix |
| Performance baseline & regression guard | 2 → 3 | Latency concerns / scaling goals | p95 latency stable ±10% over 5 builds |
| Structured observability (traces, Langfuse) | 3 | Variant 3 readiness review | Key spans captured & searchable |
| Load & resilience (chaos, restart, network loss) | 2 → 3 | Pre-production gate | Automated scenario pass rate ≥95% |
| Security scanning & secret handling tests | 3 | Introduction of Key Vault / managed identity | Zero critical findings |
| Multi-region failover simulation | (Future) | Enterprise expansion trigger | RPO/RTO targets validated |
| Advanced semantic evaluation harness | 3 | Prompt complexity growth | Drift alerts actionable (<15% unacceptable variance) |
| Human evaluation rubric & inter-rater scoring | 3 | Pilot user feedback cycle | Agreement coefficient ≥ target threshold |

**Principle**: Defer sophistication until signal justifies cost. Each capability includes clear activation triggers.

## Implementation Status

**Current Status (September 2025)**: Variant 1 (Prototype / Local Desktop) implemented. Variant 2 (Dockerized Decoupled) and Variant 3 (Cloud-Native) planned.

## Production Readiness Snapshot (Prototype vs Forward Needs)

| Aspect | Prototype Status | Forward Requirement (Variants 2–3) |
|--------|------------------|-------------------------------------|
| Architecture | Single-process (Orchestrator + in-process Chat Agent; separate KB server) | Process separation + HTTP/SSE transport + containerization |
| Performance | Ad-hoc manual observation only | Baseline latency metrics + regression guardrail (p95 tracking) |
| Security / Secrets | Env vars (local) | Managed identity + Key Vault (Variant 3) |
| Monitoring / Observability | Console/stderr logs only | Structured traces + minimal metrics (Variant 3) |
| Knowledge Base | Flat curated text file | Structured segmentation + update workflow → ingestion pipeline |
| Testing | Unit + STDIO integration | Transport parity, container integration, performance baseline |
| Operations | Manual run (`dotnet run`) | CI/CD build, scan, promote |
| Resilience | Not addressed | Basic restart/network disruption tests (late Variant 2 / early 3) |
| Multi-Domain Support | Not implemented | Second domain onboarding without orchestration refactor |

## Knowledge Base Evolution Path
1. Prototype (V1): Single flat text file(s) – transparent & low ceremony.
2. Variant 2: Segmented files + lightweight curation checklist (manual freshness review).
3. Variant 3: Ingestion workflow (script/pipeline) + change detection signals (modified/added sections).
4. Future Expansion: Automated relevance scoring + drift/freshness alerts (deferred beyond scope).

## Domain Extensibility Readiness (Second Domain Future-On Ramp)
Adding a second business domain is NOT required for upcoming variants (2–3) but MUST be low‑friction when a qualified domain candidate is approved. We explicitly design for *readiness without premature implementation*.

### Readiness Objectives
| Objective | Prototype Status | What Must Already Exist Before Adding Domain 2 |
|-----------|------------------|-----------------------------------------------|
| Isolation of domain artifacts | AMG content isolated under `datasets/` | Folder convention supports sibling domain root (`datasets/<domain-key>/`) |
| Orchestrator neutrality | Hard-coded single domain logic (acceptable) | Single switch point (DomainResolver) to replace constant with lookup |
| Knowledge retrieval coupling | Direct file read | Introduce minimal `IKnowledgeSource` interface ONLY when 2nd domain begins |
| Testing strategy | Single-domain tests | Add parametrized integration tests looping domains (same prompt set adapted) |
| Configuration | Implicit domain | Add optional `DOMAIN_KEY` env/appsetting (default = `amg`) |

### Minimal Contract (Introduced ONLY When Needed)
```
// Pseudocode contract (do not implement until trigger)
record DomainMetadata(string Key, string DisplayName, string Description, string DefaultSystemPreamble);

interface IDomainRegistry {
    DomainMetadata Get(string key);            // Throws if unknown
    IEnumerable<DomainMetadata> List();        // For diagnostics tool
}

interface IDomainResolver {                    // Prototype: returns constant "amg"
    string Resolve(string? userHint);          // Future: parse prompt / explicit selector
}
```
Prototype deliberately inlines constants rather than shipping empty abstractions. The registry + resolver appear only when the second domain implementation begins (trigger below).

### File & Folder Conventions (Pre-Decided Now)
```
datasets/
  amg/
    knowledge-base.txt
    metadata.json          # (Added when domain 2 starts)
  <new-domain-key>/        # Parallel structure
    knowledge-base.txt
    metadata.json
```
`metadata.json` (future) example:
```jsonc
{
  "key": "amg",
  "displayName": "Azure Managed Grafana",
  "description": "Managed Grafana service in Azure for observability dashboards.",
  "defaultSystemPreamble": "You are an assistant specializing in Azure Managed Grafana..."
}
```

### Trigger to Activate Multi-Domain Work
| Trigger | Validation Gate | Action Bundle |
|---------|-----------------|---------------|
| Approved candidate second domain (documented spec + success criteria) | Architecture review confirms scope & KB curation plan | Introduce registry + resolver + second dataset folder |

### Non-Goals (Until Trigger Fires)
- No dynamic domain detection heuristics.
- No cross-domain blended answers.
- No domain weighting / scoring logic.

### Adding the Second Domain – Expected Steps (Day 1 Playbook)
1. Create `datasets/<domain-key>/knowledge-base.txt` (curated minimal slice).
2. Add `metadata.json` for both domains.
3. Add lightweight `DomainRegistry` (in-memory, loads metadata files).
4. Replace constant domain key in orchestrator with `IDomainResolver` (returns default unless explicit override parameter/tool option added).
5. Extend existing MCP tool (e.g., `get_orchestrator_status`) to list available domains.
6. Duplicate current integration prompt set → parameterize over domain key; add failing cases if domain not recognized.
7. Update docs (`architecture-technologies.md` + feature spec for new domain) & `memory.md` registration.

### Testing Adjustments on Second Domain Introduction
- Reuse existing integration tests: run once per domain.
- Add a negative test: requesting unknown domain returns clear error (not fallback guess).
- Ensure no code path conditionally changes orchestration logic aside from domain-specific KB retrieval + system preamble.

### Quality Bar for Domain 2 Acceptance
| Dimension | Metric (Qualitative Allowed) |
|-----------|------------------------------|
| Prompt relevance parity | ≥ 80% of AMG baseline relevance score |
| Orchestrator code diff size | Minimal & localized (no widespread refactors) |
| Added abstractions | Only registry + resolver + interface for KB source |
| Test expansion cost | < 10% increase in test runtime (initial) |

> Principle Reinforced: We architect *for* multi-domain evolution, we do not *implement* it prematurely.

## Variant Advancement Triggers (Readiness Gates)
| Transition | Trigger Signals | Gate Outcome |
|-----------|-----------------|--------------|
| V1 → V2 | Need for process boundary (container test matrix) OR introduction of second domain candidate | Introduce HTTP/SSE transport + container compose baseline |
| V2 → V3 | Need for managed secrets + external accessibility + scaling signals | Adopt ACA or AKS + secrets + minimal observability |
| V3 → Future Expansion (Hypothetical) | Compliance / multi-region resilience requirement | Add geo redundancy + advanced DR & policy automation |

## Consolidated Deferred Capabilities (Prototype Explicitly Out of Scope)
Functional:
- Multi-turn conversational memory & context window management.
- Multi-domain orchestration logic & dynamic domain registry.
- Automated content freshness / ingestion pipeline.
- Autonomous multi-agent planning / multi-step hierarchical workflows.
- Human escalation or handoff pathways.

Non-Functional:
- Structured distributed tracing & semantic observability platform.
- Performance/load & resilience (chaos, network partition) test suites.
- Multi-region failover & disaster recovery drills.
- Compliance posture automation (policy, audit, attestation flows).
- Advanced semantic evaluation harness (similarity/drift scoring) & rubric-driven human evaluation.

> These items appear only when their associated variant trigger fires—preventing premature complexity.

## Conclusion

This architecture provides a systematic progression from simple STDIO-based prototypes to enterprise-grade Kubernetes deployments. The .NET/C#, MCP, and Semantic Kernel technology stack offers robust AI agent capabilities while leveraging proven Microsoft technologies for enterprise compatibility.

The modular design ensures prototype investments translate to production capabilities while providing flexibility for changing requirements. Organizations can start with minimal infrastructure and evolve toward enterprise-scale deployments along clear migration paths.

---

**Document Version**: 1.6  
**Last Updated**: September 2025  
**Next Review**: After prototype validation