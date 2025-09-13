# Implementation Plan for KB MCP Server

## Overview
This implementation plan outlines the steps to build a Knowledge Base MCP Server that provides Azure Managed Grafana (AMG) content to AI agents via the Model Context Protocol using STDIO transport.

## Architecture Approach
- Console application using Host.CreateApplicationBuilder pattern
- Background service hosting MCP server with STDIO transport
- File-based knowledge storage with in-memory search
- Configuration via appsettings.json for simplicity

## Implementation Steps

- [ ] Step 1: Create Project Structure and Configuration
  - **Task**: Set up .NET console application with proper project structure and basic configuration
  - **Files**:
    - `src/AmgKnowledgeBase.McpServer/AmgKnowledgeBase.McpServer.csproj`: Console app project file with MCP SDK dependency
    - `src/AmgKnowledgeBase.McpServer/Program.cs`: Main entry point with Host.CreateApplicationBuilder setup
    - `src/AmgKnowledgeBase.McpServer/appsettings.json`: Configuration for knowledge base file path and basic settings
  - **Dependencies**: .NET 8 LTS or .NET 10 Preview, Microsoft MCP SDK for .NET
  - **Configuration Reasoning**: Using appsettings.json is simplest because it follows standard .NET configuration patterns, requires no command-line parsing logic, and automatically binds to strongly-typed options classes

- [ ] Step 2: Create Knowledge Base Service
  - **Task**: Implement service to load AMG content from text file at startup and provide search functionality
  - **Files**:
    - `src/AmgKnowledgeBase.McpServer/Services/IKnowledgeBaseService.cs`: Interface defining search and info operations
    - `src/AmgKnowledgeBase.McpServer/Services/FileKnowledgeBaseService.cs`: Implementation that loads text file content at startup, performs case-insensitive partial search with context
    - `src/AmgKnowledgeBase.McpServer/Models/SearchResult.cs`: Simple model containing matched content, surrounding context, and basic metadata
  - **Dependencies**: System.IO for file operations

- [ ] Step 3: Create Sample AMG Knowledge Base Content
  - **Task**: Generate sample Azure Managed Grafana content file for the knowledge base (~5,000 characters)
  - **Files**:
    - `data/amg-knowledge-base.txt`: Sample text file with AMG information covering key features, pricing, getting started guide, integration options, and common use cases
  - **Dependencies**: Research current AMG capabilities and documentation for realistic sample content
  - **User Intervention**: Review sample content for accuracy and completeness within size constraints

- [ ] Step 4: Implement MCP Tools
  - **Task**: Create MCP tool implementations for search and knowledge base information retrieval
  - **Files**:
    - `src/AmgKnowledgeBase.McpServer/Tools/SearchKnowledgeTool.cs`: MCP tool implementing search_knowledge with query parameter and max_results option
    - `src/AmgKnowledgeBase.McpServer/Tools/GetKbInfoTool.cs`: MCP tool implementing get_kb_info returning file size, content length, and availability status
    - `src/AmgKnowledgeBase.McpServer/Models/ToolModels.cs`: Request/response models for MCP tool parameters and results
  - **Dependencies**: Microsoft MCP SDK tool interfaces and attributes

- [ ] Step 5: Implement MCP Server Host Service
  - **Task**: Create background service that hosts the MCP server with STDIO transport and tool registration
  - **Files**:
    - `src/AmgKnowledgeBase.McpServer/Services/McpServerHostService.cs`: Background service implementing IHostedService, registers MCP tools, handles STDIO communication
    - `src/AmgKnowledgeBase.McpServer/Configuration/ServerOptions.cs`: Configuration options model for knowledge base file path and server settings
  - **Dependencies**: Microsoft MCP SDK server components, IHostedService interface

- [ ] Step 6: Add Basic Logging and Error Handling
  - **Task**: Implement basic console logging throughout the application with simple error handling focused on preventing crashes
  - **Files**:
    - Update all existing service and tool files with basic try-catch blocks and ILogger usage
    - `src/AmgKnowledgeBase.McpServer/Extensions/LoggingExtensions.cs`: Simple logging helper methods for common scenarios
  - **Dependencies**: Built-in .NET logging abstractions (ILogger, ILoggingBuilder)

- [ ] Step 7: Build and Run Application
  - **Task**: Ensure application builds correctly and runs as MCP server with proper STDIO communication
  - **Files**: No new files, verification and testing step
  - **Dependencies**: .NET SDK, sample knowledge base file
  - **User Intervention**: Manually test with MCP client or compatible tool to verify STDIO communication and tool discovery works correctly

- [ ] Step 8: Write Unit Tests
  - **Task**: Create unit tests for core search functionality, file loading, and MCP tool logic
  - **Files**:
    - `tests/AmgKnowledgeBase.McpServer.Tests/AmgKnowledgeBase.McpServer.Tests.csproj`: Test project file with xUnit and testing utilities
    - `tests/AmgKnowledgeBase.McpServer.Tests/Services/FileKnowledgeBaseServiceTests.cs`: Tests for search functionality, file loading, and edge cases
    - `tests/AmgKnowledgeBase.McpServer.Tests/Tools/SearchKnowledgeToolTests.cs`: Tests for search tool parameter validation and result formatting
    - `tests/AmgKnowledgeBase.McpServer.Tests/Tools/GetKbInfoToolTests.cs`: Tests for info tool functionality
  - **Dependencies**: xUnit framework, test data files

- [ ] Step 9: Write Integration Tests
  - **Task**: Create basic integration tests for MCP protocol compliance via STDIO transport
  - **Files**:
    - `tests/AmgKnowledgeBase.McpServer.IntegrationTests/McpServerIntegrationTests.cs`: End-to-end tests simulating MCP client communication, tool discovery, and basic request/response cycles
    - `tests/AmgKnowledgeBase.McpServer.IntegrationTests/TestData/test-amg-content.txt`: Smaller test content file for integration testing
  - **Dependencies**: Microsoft MCP SDK client components, test process hosting utilities

- [ ] Step 10: Run All Tests
  - **Task**: Execute complete test suite to verify functionality and identify any issues
  - **Files**: No new files, verification step
  - **Dependencies**: .NET test runner, all test projects
  - **User Intervention**: Review test results, fix any failing tests, and ensure coverage of critical functionality

## Key Design Principles

### Simplicity First
- Single background service hosting all MCP functionality
- Direct file I/O without complex caching mechanisms
- Basic string search without advanced indexing or ranking
- Minimal error handling focused on crash prevention

### Configuration Strategy
**Decision**: Use appsettings.json configuration approach
**Reasoning**: 
- Follows standard .NET configuration patterns
- No custom command-line parsing logic required
- Automatic binding to strongly-typed configuration classes
- Easy to extend for future configuration needs
- Reduces complexity compared to command-line argument handling

### Future Evolution Readiness
- Service abstractions enable future data source swapping
- MCP tool interface supports future HTTP transport
- Modular design allows independent component testing and modification

## Dependencies Summary
- .NET 8 LTS or .NET 10 Preview runtime
- Microsoft MCP SDK for .NET (ModelContextProtocol package)
- Built-in .NET hosting and configuration abstractions
- xUnit for testing framework
- Sample AMG knowledge base content file

## Success Criteria
- Application starts successfully and loads knowledge base content
- MCP tools respond correctly via STDIO transport
- Search functionality returns relevant results with proper context
- Basic error scenarios handled gracefully without crashes
- Unit and integration tests pass reliably
