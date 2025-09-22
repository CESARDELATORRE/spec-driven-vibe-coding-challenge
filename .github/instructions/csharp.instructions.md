---
applyTo: "**"
---

# C# and .NET Development Instructions

## CODE STYLE RULES

### General Principles
- Make the C# code elegant and following best practices
- Maintain readability and simplicity while ensuring functionality
- When creating new components, always place them at the bottom of the file (not in a C# code block)
- Follow SOLID principles and dependency injection patterns, while minimizing complexity. Do not overengineer.
- Use proper error handling with try-catch blocks where appropriate, while minimizing complexity. Do not overengineer.
- Implement logging using ILogger interface throughout the application, while minimizing complexity. Do not overengineer.

### Code Organization
- Group related classes and interfaces into appropriate namespaces
- Separate business logic, data models, and utility functions into distinct folders


## PROJECT STRUCTURE RULES

### Repository Structure
```
src/
    mcp-server-kb-content-fetcher/         # Data source project
        datasets/                          # Data files
        tools/                             # MCP tools implementation
        services/                          # Business logic services
        models/                            # Data models and DTOs
        configuration/                     # Configuration classes
        extensions/                        # Extension methods
        <other folders in kebab-case>
    orchestrator-agent/                    # Main project (includes in-process Chat Agent)
        tools/                             # MCP tools implementation
        services/                          # Business logic services
        models/                            # Data models and DTOs

    chat-agent/                            # Future project (when moving to containerized architecture)

tests/
    mcp-server-kb-content-fetcher.unit-tests/       # Unit tests
    mcp-server-kb-content-fetcher.integration-tests/ # Integration tests
    orchestrator-agent.unit-tests/                 # Unit tests
    orchestrator-agent.integration-tests/           # Integration tests
    chat-agent.unit-tests/                         # Future unit tests
    chat-agent.integration-tests/                 # Future integration tests
```

## NAMING CONVENTIONS

### Folder Naming
- **RULE**: All folders MUST follow kebab-case convention
- **Examples**:
  - `src/mcp-server-kb-content-fetcher/services`
  - `src/mcp-server-kb-content-fetcher/datasets`
  - `tests/mcp-server-kb-content-fetcher.integration-tests`

### Project File Naming
- **RULE**: Project files follow kebab-case convention
- **Example**: `mcp-server-kb-content-fetcher.csproj`

### C# Code File Naming
- **RULE**: Class files follow PascalCase (aligned with C# class naming conventions)
- **Examples**:
  - `IKnowledgeBaseService.cs`
  - `FileKnowledgeBaseService.cs`
  - (search/excerpt prototype removed; not part of minimal scope)

### Namespace Conventions
- **RULE**: Use PascalCase following .NET conventions
- **Example**: `McpServerKbContentFetcher`
- **Pattern**: Convert kebab-case project names to PascalCase for namespaces

## TESTING RULES

### Testing Framework
- **Primary**: Use xUnit following best practices
- **Structure**: Separate unit tests from integration tests
- **Naming**: Test project names follow `{project-name}.{test-type}-tests` pattern

### Test Organization
```
tests/
├── mcp-server-kb-content-fetcher.unit-tests/         # Fast, isolated unit tests
├── mcp-server-kb-content-fetcher.integration-tests/  # Real protocol tests
└── fixtures/                                         # Shared test data
```

### Test Namespace Conventions
- Use PascalCase for test namespaces based on the project's name.

## DOCUMENTATION RULES

### Required Documentation
- **File-level**: Explain purpose and scope of each file
- **Component-level**: Document inputs, outputs, and behavior for all public methods
- **Inline comments**: Explain complex logic or business rules
- **Type documentation**: Document all interfaces and public types
- **Edge cases**: Note edge cases and error handling approaches
- **Assumptions**: Document any assumptions or limitations

### Documentation Format
```csharp
/// <summary>
/// Brief description of the class/method purpose
/// </summary>
/// <param name="parameter">Description of parameter</param>
/// <returns>Description of return value</returns>
/// <remarks>
/// Additional notes about usage, edge cases, or limitations
/// </remarks>
```

## IMPLEMENTATION PATTERNS

### Dependency Injection
- Do NOT use DI always. Use it only when it makes sense and does not overcomplicate the code.
- Use constructor injection for required dependencies
- Register services with appropriate lifetime (Singleton, Scoped, Transient)
- Implement interface-based abstractions for testability

### Configuration
- Use strongly-typed configuration classes
- Leverage `IOptions<T>` pattern for configuration binding
- Store configuration in `appsettings.json` files

### Error Handling
- For prototype/POC implementations, keep error handling simple and focused on preventing crashes.
- Use structured exception handling with specific exception types
- Log errors appropriately using ILogger
- Return meaningful error messages for debugging

### MCP Server Specific Rules
- Route all logging to stderr to avoid corrupting STDIO transport
- Use MCP SDK fluent builder pattern: `AddMcpServer().WithStdioServerTransport().WithToolsFromAssembly()`
- Implement MCP tools with proper attributes for auto-discovery
- Follow host builder pattern: `Host.CreateApplicationBuilder(args)`

## EXAMPLES

### Correct Project Structure
```
✅ src/mcp-server-kb-content-fetcher/services/IKnowledgeBaseService.cs
✅ src/mcp-server-kb-content-fetcher/models/SearchResult.cs
✅ tests/mcp-server-kb-content-fetcher.unit-tests/services/FileKnowledgeBaseServiceTests.cs
```

### Incorrect Project Structure
```
❌ src/McpServerKbContentFetcher/Services/IKnowledgeBaseService.cs
❌ src/mcp-server-kb-content-fetcher/Services/IKnowledgeBaseService.cs
❌ tests/McpServerKbContentFetcher.UnitTests/Services/FileKnowledgeBaseServiceTests.cs
```

## COMPLIANCE VERIFICATION

When implementing or reviewing code:
1. ✅ Verify all folder names use kebab-case
2. ✅ Confirm project files use kebab-case naming
3. ✅ Check that class files use PascalCase
4. ✅ Validate namespace follows PascalCase conversion
5. ✅ Ensure proper documentation is included
6. ✅ Confirm dependency injection patterns are followed
7. ✅ Verify MCP-specific logging configuration (stderr routing)