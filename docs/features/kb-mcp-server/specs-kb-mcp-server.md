# KB MCP Server Feature Specification

## KB MCP Server Feature Specification

**Status**: Implemented (Restructured for template alignment)


The KB MCP Server provides AI agents with access to Azure Managed Grafana knowledge through a standardized Model Context Protocol (MCP) interface. This server acts as a bridge between Chat Agents and domain-specific knowledge stored in local text files, enabling precise, context-aware responses about AMG features and capabilities.

🏗️ **Architecture Reference**: This component implements the Knowledge Base layer as detailed in [Architecture & Technologies](../architecture-technologies.md).

## User Journey

1. **AI Agent Request**: Chat Agent receives user question about Azure Managed Grafana
2. **Knowledge Retrieval**: Agent calls KB MCP Server to obtain either knowledge base metadata or full raw content
3. **Response Generation**: Chat Agent uses raw content (or a subset it trims) to formulate a domain-specific response
4. **User Delivery**: Complete response delivered to user through chat interface

## User Scenarios & Testing (Template Alignment)

### Primary User Story
As a Chat/Orchestrator Agent, I need to retrieve either metadata (to decide if a full fetch is warranted) or the full raw knowledge base content so I can generate accurate Azure Managed Grafana answers without embedding duplicate docs.

### Acceptance Scenarios
1. Given a readable non‑empty knowledge base file, when `get_kb_info` is invoked, then size, content length, last modified timestamp, status `available`, and description are returned.
2. Given a readable non‑empty knowledge base file, when `get_kb_content` is invoked, then status `ok`, full content, and correct `contentLength` are returned.
3. Given an empty knowledge base file, when `get_kb_content` is invoked, then status `empty`, `contentLength = 0`, and empty string content are returned.
4. Given the configured file path does not exist, when `get_kb_info` is invoked, then status `unavailable` with minimal safe metadata is returned and the server does not crash.
5. Given a transient read error occurs, when `get_kb_content` is invoked, then status `error` with an error message is returned (exception not propagated beyond tool boundary).

### Edge Cases
- Missing file path configuration
- Zero-length file
- File locked / transient IO error during read
- Large (near 10MB) file still fully returned
- Non-UTF8 / decoding failure -> error path

## Functional Requirements

### FR-1: Knowledge Base Access
**Description**: Provide access to AMG-specific content stored in local plain text file
**Acceptance Criteria**:
- Server can read and access configured local text file at startup
- Content is loaded into memory for fast access *(see Appendix A for rationale)*
- Server handles file access errors gracefully with basic error messages
- Basic console logging records file access operations and status

### FR-2: Knowledge Base Information Retrieval  
**Description**: Provide overview of knowledge base size and availability
**Acceptance Criteria**:
- Implement `get_kb_info` MCP tool
- Return: file size (bytes), content length (characters), availability status, last modified timestamp, description
- Return status `unavailable` with minimal fields if file cannot be read
- Never crash on missing or empty file

### FR-3: Full Raw Content Retrieval
**Description**: Allow agent to retrieve the entire raw text content (prototype convenience)
**Acceptance Criteria**:
- Implement `get_kb_content` MCP tool
- Return full content string plus content length and status (`ok` or `empty`)
- Return `empty` when file loads successfully but has zero length
- Return `error` with message when an exception occurs

### FR-4: MCP Protocol Compliance
**Description**: Implement standard MCP server interface for agent integration
**Acceptance Criteria**:
- Support MCP STDIO transport protocol for prototype simplicity
- Implement required MCP server capabilities and tool discovery
- Handle MCP handshake and protocol negotiation correctly
- Provide proper MCP-compliant error responses
- Enable seamless integration with MCP-compatible agents

### FR-5: Prototype Response Size Handling
**Description**: Prevent runaway payload size issues
**Acceptance Criteria**:
- `get_kb_content` returns entire file unmodified (no internal truncation for prototype)
- Future search / segmentation explicitly deferred (no partial snippet logic now)
- Downstream truncation handled outside this server (explicitly out of scope)

### FR-6: Basic Error Handling and Logging
**Description**: Provide simple error handling and operational visibility
**Acceptance Criteria**:
- Return basic, readable error messages for common failure scenarios
- Implement console logging for debugging and monitoring
- Log server startup, search operations, and basic metrics
- Handle file access errors without server crashes
- Provide clear startup success/failure indicators

## Technical Constraints

### Prototype/POC Limitations
- **Plain Text Only**: Knowledge base must be unstructured plain text (.txt format)
- **Static Content**: Text file content is static; server restart required for updates
- **Startup Loading**: Content loaded once at startup for simple access path
- **Small Scale**: Optimized for knowledge base files under 10MB
- **Basic Logging**: Console-only logging for simplicity and readability
- **STDIO Transport**: MCP communication via STDIO only for prototype
- **No Search**: Query-based or semantic search intentionally deferred

### Design for Future Evolution
- **Data Source Abstraction**: Architecture supports future knowledge base backends
- **Transport Flexibility**: Design enables future HTTP/SSE transport addition
- **Search Enhancement**: Component structure supports advanced search algorithms
- **Scaling Readiness**: Modular design supports distributed deployment patterns

## MCP Tools Specification

### get_kb_info
**Purpose**: Retrieve basic knowledge base information and statistics
**Input**: None
**Output**: Object containing file size, content length, and basic availability status
**Error Handling**: Returns basic status even if knowledge base is unavailable

### get_kb_content
**Purpose**: Retrieve the full raw knowledge base text content (prototype convenience for small/medium files)
**Input**: None
**Output**: Object with:
- `status`: `ok` | `empty` | `error`
- `contentLength`: Integer character length of the content when `ok`/`empty`
- `content`: Full raw text string (present only when `ok` or `empty`)
- `error`: Error message (present only when `status = error`)
**Notes**:
- No pagination, truncation, or segmentation in prototype version
- Downstream agents are responsible for trimming or chunking if needed
**Error Handling**:
- Returns `{ status: "error", error: <message> }` on exceptions
- Returns `{ status: "empty", contentLength: 0, content: "" }` when file successfully read but has zero length

### Tool Naming Conventions
To reduce ambiguity between the wire-level MCP contract and C# implementation details, tool names intentionally exist in two forms:

| Layer | Naming Style | Examples | Purpose |
|-------|--------------|----------|---------|
| Protocol (public) | `snake_case` | `get_kb_info`, `get_kb_content` | Stable identifiers exposed over MCP; used by clients and tests |
| Implementation (C#) | `PascalCase` + `Tool` suffix | `GetKbInfoTool`, `GetKbContentTool` | Internal classes discovered by MCP SDK via reflection/attributes |

Rules:
1. Protocol tool IDs are the authoritative external contract and must remain stable once released.
2. C# class names may be refactored if needed, provided the protocol IDs remain unchanged.
3. New tools MUST define the protocol ID first (snake_case), then implement a matching `PascalCaseTool` class.
4. Logs SHOULD (optional) include both forms for easier traceability when debugging (e.g., `Executing tool get_kb_content (GetKbContentTool)`).

Rationale:
- Cross-language friendliness (snake_case avoids casing interpretation issues in JavaScript/Python clients).
- Adherence to .NET naming conventions internally.
- Allows future automation that could derive protocol IDs from class names via a deterministic transform (PascalCase → snake_case) without requiring it now.

Anti-Drift Safeguard (Future Consideration):
If tool count grows, consider adding a lightweight unit test that enumerates discovered MCP tools and asserts expected protocol IDs (`get_kb_info`, `get_kb_content`) to catch accidental renames.

## Success Criteria

### Functional Success
- Chat Agent can retrieve knowledge base info (`get_kb_info`)
- Chat Agent can retrieve full raw content (`get_kb_content`)
- All tool responses follow expected status field conventions
- Integration works seamlessly with Orchestration Agent via STDIO

### Technical Success
- Server starts and loads knowledge base without errors in under 5 seconds
- MCP protocol communication functions reliably with standard MCP clients
- Console logging provides sufficient debugging information for development
- Error conditions handled gracefully without server crashes or hangs

### User Experience Success
- Retrieved content enables accurate agent responses about AMG topics
- Operators can inspect metadata before deciding to fetch full content
- Server behavior is predictable (no partial or ranked results in prototype)

## Dependencies

- .NET runtime environment (.NET 9)
- MCP SDK for .NET with STDIO transport support
- Local plain text file containing AMG knowledge content
- Compatible MCP client (Chat Agent or test harness)

## Out of Scope

### Current Version Exclusions
- Query-based keyword search (removed for initial scope)
- Semantic search or vector retrieval
- Real-time content updates or file monitoring capabilities
- HTTP transport or containerized deployment options
- Production-scale performance optimization or caching
- Comprehensive monitoring, telemetry, or observability features
- Multi-file knowledge sources or database integration
- Content structure parsing (markdown, sections, headers)

### Future Version Candidates
- Keyword and semantic search (vector or hybrid)
- Dynamic content loading and automatic refresh capabilities
- HTTP/SSE transport support for distributed deployment
- Database or external API knowledge source integration
- Advanced search ranking and relevance scoring
- Content versioning and update management
- Structured content parsing and section-based retrieval

## Implementation Notes

### Knowledge Base Content Format
- Plain text files (.txt) without formatting requirements
- Any content structure acceptable (no assumed headers or sections)
- UTF-8 encoding recommended for broad compatibility
- Content should be focused on Azure Managed Grafana topics for prototype

**Implementation Note**: The prototype uses placeholder AMG content for demonstration purposes. Production deployment would require comprehensive, validated AMG documentation content.

### Deferred Search Behavior (Documented for Future Reference)
- Any form of keyword/semantic query, snippet extraction, or ranking introduced only after justification (KB size growth / user feedback trigger).

### Configuration
- Knowledge base file path configurable via command line argument or config file
- Default search result limits configurable at startup
- Logging level configurable for development vs. demonstration scenarios

## Appendix A: Startup Content Loading Rationale

**Why load content at server startup?**

The requirement to load knowledge base content at startup is a deliberate prototype/POC design choice that prioritizes:

1. **Implementation Simplicity**: Eliminates complexity of file watching, dynamic reload triggers, content caching strategies, and memory management
2. **Consistent Performance**: Ensures immediate search responses without disk I/O latency during queries, providing predictable response times
3. **Prototype Scale**: Aligns with small content files (<10MB) assumption, making in-memory loading feasible for demonstration purposes
4. **Static Content Tradeoff**: Supports the design decision for static text files that require server restart for updates (see Tradeoff #6 in tradeoffs.md)

**Alternative approaches** (deferred for future versions):
- Lazy loading (load on first request)
- Hot reload (monitor file changes)
- Streaming (read portions as needed)
- Intelligent caching with TTL

These alternatives add implementation complexity that contradicts the prototype's rapid development and simple demonstration goals.

---

**Document Version**: 1.1  
**Last Updated**: September 2025  
**Next Review**: After prototype completion

## Review & Acceptance Checklist

### Content Quality
- [x] Focuses on user/business intent (WHAT/WHY)
- [x] Avoids deep implementation specifics beyond necessary protocol/tool naming
- [x] Clearly states deferred scope (search, segmentation)
- [x] Edge cases enumerated

### Requirements & Testability
- [x] Each functional requirement is testable
- [x] Status taxonomy (`ok`, `empty`, `unavailable`, `error`) defined
- [x] Error handling behaviors described (no crashes on IO issues)
- [x] Acceptance scenarios map to FRs (FR-2/3/4/5/6 alignment)

### Scope & Consistency
- [x] Tools documented (`get_kb_info`, `get_kb_content`)
- [x] Tool naming conventions clarified
- [x] Out-of-scope list present and explicit
- [x] Success criteria align with functional requirements

### Outstanding Clarifications
- [x] None (no `[NEEDS CLARIFICATION]` markers present)

## Execution Status (Informational)
- [x] User journey captured
- [x] Scenarios & edge cases added
- [x] Requirements present
- [x] Checklist completed
- [x] Tools defined
- [x] Version bump pending (see below)

---

**Document Version**: 1.2  
**Last Updated**: September 2025  
**Change Note**: Added missing template-aligned sections (User Scenarios & Testing, Review & Acceptance Checklist, Execution Status) without removing legacy section names for continuity.  
**Next Review**: After prototype completion

## Testing Reality

### Implemented Tests
- **Unit Tests**: Core business logic (FileKnowledgeBaseService)
- **Integration Tests**: MCP protocol handshake and tool invocation

### Not Implemented (Future Work)
- **Performance Tests**: Large knowledge base handling
- **Stress Tests**: Concurrent request handling
- **End-to-End Tests**: Full conversation flows
- **Domain Validation**: Answer quality assessment

The current tests validate **technical correctness** not **domain expertise quality**.
