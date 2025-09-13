# KB MCP Server Feature Specification

## Overview

The KB MCP Server provides AI agents with access to Azure Managed Grafana knowledge through a standardized Model Context Protocol (MCP) interface. This server acts as a bridge between Chat Agents and domain-specific knowledge stored in local text files, enabling precise, context-aware responses about AMG features and capabilities.

## User Journey

1. **AI Agent Request**: Chat Agent receives user question about Azure Managed Grafana
2. **Knowledge Lookup**: Agent calls KB MCP Server to search for relevant information  
3. **Content Retrieval**: KB MCP Server searches local knowledge base and returns matching content
4. **Response Generation**: Chat Agent uses retrieved content to formulate accurate, domain-specific response
5. **User Delivery**: Complete response delivered to user through chat interface

## Functional Requirements

### FR-1: Knowledge Base Access
**Description**: Provide access to AMG-specific content stored in local plain text file
**Acceptance Criteria**:
- Server can read and access configured local text file at startup
- Content is loaded into memory for fast access *(see Appendix A for rationale)*
- Server handles file access errors gracefully with basic error messages
- Basic console logging records file access operations and status

### FR-2: Content Search Capability  
**Description**: Enable case-insensitive keyword search with partial matching across knowledge base content
**Acceptance Criteria**:
- Implement `search_knowledge` MCP tool with case-insensitive search
- Support partial keyword matching within text content
- Return relevant content snippets with surrounding context
- Handle empty search results with appropriate "no results found" response
- Log search requests with query terms and result counts

### FR-3: Content Discovery
**Description**: Provide overview of knowledge base size and basic statistics
**Acceptance Criteria**:
- Implement `get_kb_info` MCP tool
- Return basic knowledge base statistics (file size, content length, etc.)
- Enable agents to understand scope of available knowledge
- Handle requests gracefully even if file is unavailable

### FR-4: MCP Protocol Compliance
**Description**: Implement standard MCP server interface for agent integration
**Acceptance Criteria**:
- Support MCP STDIO transport protocol for prototype simplicity
- Implement required MCP server capabilities and tool discovery
- Handle MCP handshake and protocol negotiation correctly
- Provide proper MCP-compliant error responses
- Enable seamless integration with MCP-compatible agents

### FR-5: Response Management and Limits
**Description**: Manage response sizes to prevent overwhelming downstream agents
**Acceptance Criteria**:
- Limit individual search results to reasonable content length (max 3000 characters per result)
- Return maximum of 3 most relevant matches per search query
- Preserve original text formatting without normalization
- Truncate long content gracefully with indication of truncation

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
- **Startup Loading**: Content loaded once at startup for fast query response and implementation simplicity
- **Small Scale**: Optimized for knowledge base files under 10MB
- **Basic Logging**: Console-only logging for simplicity and readability
- **STDIO Transport**: MCP communication via STDIO only for prototype
- **Simple Search**: Basic string matching without semantic search or ranking

### Future Evolution Design
- **Data Source Abstraction**: Architecture supports future knowledge base backends
- **Transport Flexibility**: Design enables future HTTP/SSE transport addition
- **Search Enhancement**: Component structure supports advanced search algorithms
- **Scaling Readiness**: Modular design supports distributed deployment patterns

## MCP Tools Specification

### search_knowledge
**Purpose**: Search knowledge base for keyword matches with partial matching
**Input**: 
- `query` (string): Search keywords or phrases
- `max_results` (optional int): Maximum results to return (default: 3, max: 5)
**Output**: Array of content snippets (max 3000 chars each) with basic relevance context
**Error Handling**: Returns "No results found" for empty results, "Search error" for failures

### get_kb_info
**Purpose**: Retrieve basic knowledge base information and statistics
**Input**: None
**Output**: Object containing file size, content length, and basic availability status
**Error Handling**: Returns basic status even if knowledge base is unavailable

## Success Criteria

### Functional Success
- Chat Agent can successfully retrieve AMG-specific information through MCP protocol
- Search operations return relevant content within 2 seconds for prototype files
- All MCP tools function correctly with appropriate error handling
- Integration works seamlessly with Orchestration Agent via STDIO

### Technical Success
- Server starts and loads knowledge base without errors in under 5 seconds
- MCP protocol communication functions reliably with standard MCP clients
- Console logging provides sufficient debugging information for development
- Error conditions handled gracefully without server crashes or hangs

### User Experience Success
- Retrieved content enables accurate agent responses about AMG topics
- Search results provide sufficient context for meaningful question answering
- Response times feel immediate for typical prototype use cases
- Overall interaction supports natural conversation flow

## Dependencies

- .NET runtime environment (.NET 10 Preview or .NET 8 LTS)
- MCP SDK for .NET with STDIO transport support
- Local plain text file containing AMG knowledge content
- Compatible MCP client (Chat Agent or test harness)

## Out of Scope

### Current Version Exclusions
- Real-time content updates or file monitoring capabilities
- Advanced search features (semantic search, relevance ranking, indexing)
- HTTP transport or containerized deployment options
- Production-scale performance optimization or caching
- Comprehensive monitoring, telemetry, or observability features
- Multi-file knowledge sources or database integration
- Content structure parsing (markdown, sections, headers)

### Future Version Candidates
- Vector search and semantic similarity matching
- Dynamic content loading and automatic refresh capabilities
- HTTP/SSE transport support for distributed deployment
- Database and external API knowledge source integration
- Advanced search ranking and relevance scoring
- Content versioning and update management
- Structured content parsing and section-based retrieval

## Implementation Notes

### Knowledge Base Content Format
- Plain text files (.txt) without formatting requirements
- Any content structure acceptable (no assumed headers or sections)
- UTF-8 encoding recommended for broad compatibility
- Content should be focused on Azure Managed Grafana topics for prototype

### Search Behavior
- Case-insensitive matching across entire file content
- Partial keyword matching (substring search)
- Results include surrounding context (±100 characters around match)
- No relevance ranking - returns results in order found

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
