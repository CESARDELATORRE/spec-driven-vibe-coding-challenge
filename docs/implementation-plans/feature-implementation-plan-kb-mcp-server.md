# Implementation Plan for KB MCP Server

## Overview
This implementation plan outlines the steps to build a Knowledge Base MCP Server that provides Azure Managed Grafana (AMG) content to AI agents via the Model Context Protocol using STDIO transport. The implementation uses domain-agnostic code structure to enable reusability across different knowledge domains while targeting AMG as the specific content domain for this prototype.

## Architecture Approach
- .NET/C# console application using Host.CreateApplicationBuilder pattern
- MCP SDK for .NET with fluent configuration (AddMcpServer().WithStdioServerTransport().WithToolsFromAssembly())
- File-based knowledge storage with in-memory search
- Configuration via appsettings.json for simplicity
- Domain-agnostic code structure and naming for cross-domain reusability
- Logging configured to stderr to avoid MCP STDIO conflicts

## Implementation Steps

- [ ] Step 1: Create Project Structure and Configuration
  - **Task**: Set up .NET console application with proper project structure and basic configuration
  - **Files**:
    - `src/mcp-server-kb-content-fetcher/mcp-server-kb-content-fetcher.csproj`: Console app project file with MCP SDK dependency
    - `src/mcp-server-kb-content-fetcher/Program.cs`: Main entry point using Host.CreateApplicationBuilder with MCP SDK fluent configuration and stderr logging
    - `src/mcp-server-kb-content-fetcher/appsettings.json`: Configuration for knowledge base file path and basic settings
  - **Dependencies**: .NET 10 Preview or .NET 9 as backup plan, Microsoft MCP SDK for .NET (latest popular/stable version, even if in preview state, since MCP is evolving very fast)
  - **Configuration Reasoning**: Using appsettings.json is simplest because it follows standard .NET configuration patterns, requires no command-line parsing logic, and automatically binds to strongly-typed options classes

- [ ] Step 2: Create Knowledge Base Service
  - **Task**: Implement service to load Azure Managed Grafana content from text file at startup and provide search functionality
  - **Files**:
    - `src/mcp-server-kb-content-fetcher/services/IKnowledgeBaseService.cs`: Interface defining search and info operations
    - `src/mcp-server-kb-content-fetcher/services/FileKnowledgeBaseService.cs`: Implementation that loads text file content at startup, performs case-insensitive partial search with context
    - `src/mcp-server-kb-content-fetcher/models/SearchResult.cs`: Simple model containing matched content, surrounding context, and basic metadata
  - **Dependencies**: System.IO for file operations
  - **Interface Pattern Reasoning**: Using `IKnowledgeBaseService` interface enables dependency injection and future knowledge base implementations without changing consuming code. Examples of future potential implementations:
    - `DatabaseKnowledgeBaseService`: Query SQL databases or document stores
    - `ApiKnowledgeBaseService`: Fetch content from REST APIs or GraphQL endpoints
    - `VectorKnowledgeBaseService`: Use vector databases like Azure Cognitive Search or Pinecone
    - `HybridKnowledgeBaseService`: Combine multiple knowledge sources

- [ ] Step 3: Create Sample Azure Managed Grafana Knowledge Base Content
  - **Task**: Generate sample Azure Managed Grafana content file for the knowledge base (~5,000 characters)
  - **Files**:
    - `src/mcp-server-kb-content-fetcher/datasets/knowledge-base.txt`: Sample text file with AMG information covering key features, pricing, getting started guide, integration options, and common use cases
  - **Dependencies**: Research current AMG capabilities and documentation for realistic sample content
  - **User Intervention**: Review sample content for accuracy and completeness within size constraints

- [ ] Step 4: Implement MCP Tools Using Attributes
  - **Task**: Create MCP tool implementations using MCP SDK attributes for auto-discovery by WithToolsFromAssembly()
  - **Files**:
    - `src/mcp-server-kb-content-fetcher/tools/SearchKnowledgeTool.cs`: MCP tool class with [McpTool] attribute implementing search_knowledge functionality
    - `src/mcp-server-kb-content-fetcher/tools/GetKbInfoTool.cs`: MCP tool class with [McpTool] attribute implementing get_kb_info functionality
    - `src/mcp-server-kb-content-fetcher/models/ToolModels.cs`: Request/response models for MCP tool parameters and results
  - **Dependencies**: Microsoft MCP SDK tool attributes and interfaces

- [ ] Step 5: Configure MCP Server with Fluent Builder
  - **Task**: Configure MCP server using fluent builder pattern with STDIO transport and auto-tool discovery
  - **Files**:
    - Update `src/mcp-server-kb-content-fetcher/Program.cs`: Add MCP server configuration using builder.Services.AddMcpServer().WithStdioServerTransport().WithToolsFromAssembly()
    - `src/mcp-server-kb-content-fetcher/configuration/ServerOptions.cs`: Configuration options model for knowledge base file path and server settings
  - **Dependencies**: Microsoft MCP SDK server components, dependency injection registration for knowledge base service

- [ ] Step 6: Configure Logging and Error Handling for MCP STDIO Compatibility
  - **Task**: Configure logging to route to stderr and implement basic error handling focused on preventing crashes
  - **Files**:
    - Update `src/mcp-server-kb-content-fetcher/Program.cs`: Configure logging with `builder.Logging.AddConsole(options => { options.LogToStandardErrorThreshold = LogLevel.Trace; })`
    - Update all existing service and tool files with basic try-catch blocks and ILogger usage
    - `src/mcp-server-kb-content-fetcher/extensions/LoggingExtensions.cs`: Simple logging helper methods for common scenarios
  - **Dependencies**: Built-in .NET logging abstractions (ILogger, ILoggingBuilder) configured through Host.CreateApplicationBuilder
  - **Critical Note**: All logging must go to stderr to avoid corrupting MCP STDIO communication on stdout

- [ ] Step 7: Build and Run Application
  - **Task**: Ensure application builds correctly and runs as MCP server with proper STDIO communication
  - **Files**: No new files, verification and testing step
  - **Dependencies**: .NET SDK, sample knowledge base file
  - **User Intervention**: Manually test with MCP client or compatible tool to verify STDIO communication and tool discovery works correctly

- [ ] Step 8: Write Unit Tests
  - **Task**: Create unit tests for core search functionality, file loading, and MCP tool logic
  - **Files**:
    - `tests/mcp-server-kb-content-fetcher.unit-tests/mcp-server-kb-content-fetcher.unit-tests.csproj`: Test project file with xUnit and testing utilities
    - `tests/mcp-server-kb-content-fetcher.unit-tests/services/FileKnowledgeBaseServiceTests.cs`: Tests for search functionality, file loading, and edge cases
    - `tests/mcp-server-kb-content-fetcher.unit-tests/tools/SearchKnowledgeToolTests.cs`: Tests for search tool parameter validation and result formatting
    - `tests/mcp-server-kb-content-fetcher.unit-tests/tools/GetKbInfoToolTests.cs`: Tests for info tool functionality
  - **Dependencies**: xUnit framework, test data files

- [ ] Step 9: Write Integration Tests
  - **Task**: Create basic integration tests for MCP protocol compliance via STDIO transport
  - **Files**:
    - `tests/mcp-server-kb-content-fetcher.integration-tests/mcp-server-kb-content-fetcher.integration-tests.csproj`: Integration test project file
    - `tests/mcp-server-kb-content-fetcher.integration-tests/McpServerIntegrationTests.cs`: End-to-end tests simulating MCP client communication, tool discovery, and basic request/response cycles
    - `tests/fixtures/test-knowledge-content.txt`: Smaller test content file for integration testing with Azure Managed Grafana sample data
  - **Dependencies**: Microsoft MCP SDK client components, test process hosting utilities

- [ ] Step 10: Run All Tests
  - **Task**: Execute complete test suite to verify functionality and identify any issues
  - **Files**: No new files, verification step
  - **Dependencies**: .NET test runner, all test projects
  - **User Intervention**: Review test results, fix any failing tests, and ensure coverage of critical functionality

## Key Design Principles

### Simplicity First
- MCP SDK handles server hosting and tool discovery automatically
- Direct file I/O without complex caching mechanisms
- Basic string search without advanced indexing or ranking
- Minimal error handling focused on crash prevention

### Interface-Based Architecture
**Decision**: Use `IKnowledgeBaseService` interface pattern with dependency injection
**Reasoning**:
- **Dependency Inversion**: High-level MCP tools depend on abstraction, not concrete implementation
- **Open/Closed Principle**: Easy to extend with new knowledge base types without modifying existing code
- **Testability**: Simple to mock interface for unit testing MCP tools
- **Future Flexibility**: Seamless swapping between file-based, database, API, or vector-based knowledge sources
- **Configuration-Driven**: Can switch implementations via dependency injection configuration

### Future Evolution Readiness
- Service abstractions enable future data source swapping
- MCP SDK supports future HTTP transport through configuration changes
- Tool attribute-based discovery supports easy addition of new tools
- Domain-agnostic code structure supports reuse across different knowledge domains

## Technical Implementation Guidelines

### MCP SDK Integration
**Decision**: Use MCP SDK fluent builder pattern with auto-discovery
**Reasoning**: 
- Leverages SDK's built-in hosting and transport management
- Auto-discovery via WithToolsFromAssembly() eliminates manual tool registration
- Follows SDK conventions and best practices
- Reduces boilerplate code and potential configuration errors

### MCP STDIO Logging Configuration
**Critical Requirement**: Configure logging to stderr to prevent MCP protocol corruption
**Implementation**: 
```csharp
builder.Logging.AddConsole(options =>
{
    // Route all log levels to stderr to avoid corrupting MCP stdio (stdout) channel
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});
```
**Reasoning**: MCP protocol uses stdout for communication; any logging to stdout will corrupt the protocol stream

### Configuration Strategy
**Decision**: Use appsettings.json configuration approach
**Reasoning**: 
- Follows standard .NET configuration patterns
- No custom command-line parsing logic required
- Automatic binding to strongly-typed configuration classes
- Easy to extend for future configuration needs
- Integrates seamlessly with Host.CreateApplicationBuilder

### .NET Dependencies Summary
- .NET 10 Preview runtime or .NET 9 as backup plan
- Microsoft MCP SDK for .NET (ModelContextProtocol package) - Latest stable/popular version, even if in preview state
- Built-in .NET hosting and configuration abstractions
- xUnit for testing framework
- Sample Azure Managed Grafana knowledge base content file

## Success Criteria
- Application starts successfully using MCP SDK hosting and loads Azure Managed Grafana knowledge base content
- MCP tools are auto-discovered and respond correctly via STDIO transport
- Search functionality returns relevant AMG-related results with proper context
- Basic error scenarios handled gracefully without crashes
- Unit and integration tests pass reliably
- Code structure remains domain-agnostic for future reusability
- Logging properly routes to stderr without corrupting MCP STDIO communication

## Architecture Alignment Notes
This implementation plan is aligned with the MCP SDK for .NET patterns:
- Uses `Host.CreateApplicationBuilder(args)` as the foundation
- Implements fluent configuration with `AddMcpServer().WithStdioServerTransport().WithToolsFromAssembly()`
- Leverages automatic tool discovery instead of manual registration
- Integrates logging through the standard .NET hosting model with stderr routing for MCP compatibility
- Follows `await app.RunAsync()` execution pattern
