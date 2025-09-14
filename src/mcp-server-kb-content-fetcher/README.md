# MCP Server KB Content Fetcher

A Knowledge Base MCP (Model Context Protocol) Server that provides AI agents with access to Azure Managed Grafana knowledge through a standardized MCP interface. This server acts as a bridge between Chat Agents and domain-specific knowledge stored in local text files.

## Table of Contents

- [Overview](#overview)
- [Features](#features)
- [Prerequisites](#prerequisites)
- [Installation](#installation)
- [Building the Project](#building-the-project)
- [Configuration](#configuration)
- [Running the Server](#running-the-server)
- [Testing](#testing)
 - [Diagnostic CLI Mode](#diagnostic-cli-mode)
- [GitHub Copilot Integration](#github-copilot-integration)
- [MCP Tools](#mcp-tools)
- [Troubleshooting](#troubleshooting)
- [Project Structure](#project-structure)
- [Contributing](#contributing)

## Overview

The KB MCP Server enables AI agents to access structured knowledge about Azure Managed Grafana through the Model Context Protocol. It provides:

- **Knowledge Base Access**: Reads AMG-specific content from local plain text files
- **Content Search**: Case-insensitive keyword search with partial matching
- **Content Discovery**: Overview of knowledge base size and statistics
- **MCP Protocol Compliance**: Standard MCP server interface for agent integration

## Features

- 📁 **File-based Knowledge Storage**: Loads content from plain text files
- 🔍 **Fast Search**: In-memory search with case-insensitive partial matching
- 🔌 **MCP Integration**: Standard Model Context Protocol STDIO transport
- 📊 **Knowledge Base Info**: Statistics and metadata about available content
- 🪵 **Structured Logging**: Console logging routed to stderr for MCP compatibility
- ⚡ **Quick Response**: Content loaded at startup for immediate search responses

## Prerequisites

### Required Software

- **.NET Runtime**: .NET 9
- **Operating System**: Windows, macOS, or Linux

### Development Prerequisites

- **.NET SDK**: .NET 9
- **Git**: For source code management
- **Text Editor**: VS Code, or any text editor

### MCP Client

To interact with this server, you'll need an MCP-compatible client such as:
- GitHub CoPilot in VS Code
- Claude Desktop with MCP support
- Custom MCP clients
- MCP testing tools

## Installation

### 1. Clone the Repository

```bash
git clone https://github.com/CESARDELATORRE/spec-driven-vibe-coding-challenge.git
cd <YOUR-PATH>/src/mcp-server-kb-content-fetcher
```

### 2. Install Dependencies

```bash
# Restore NuGet packages
dotnet restore
```

## Building the Project

### Command Line Build

```bash
# Clean previous builds
dotnet clean

# Build the project
dotnet build

# Build for release
dotnet build --configuration Release

# Build for specific runtime (optional)
dotnet build --runtime win-x64 --configuration Release
dotnet build --runtime linux-x64 --configuration Release
dotnet build --runtime osx-x64 --configuration Release
```

### Build Verification

```bash
# Check build output
ls -la bin/Debug/net*/ || dir bin\Debug\net*\

# Verify executable exists
ls -la bin/Debug/net*/mcp-server-kb-content-fetcher* || dir bin\Debug\net*\mcp-server-kb-content-fetcher*
```

## Configuration

### 1. Knowledge Base File

Create or use an existing knowledge base file:

```bash
# Create sample knowledge base (if not exists)
mkdir -p datasets
cat > datasets/knowledge-base.txt << 'EOF'
Azure Managed Grafana Overview
Azure Managed Grafana is a fully managed service that provides powerful data visualization and monitoring capabilities.

Key Features:
- Built-in high availability and scalability
- Integration with Azure Monitor and other data sources
- Support for custom dashboards and alerts
- Role-based access control (RBAC)
- Pre-configured data source connections

Getting Started:
1. Create an Azure Managed Grafana instance in the Azure portal
2. Configure data sources (Azure Monitor, Application Insights, etc.)
3. Import or create custom dashboards
4. Set up alerts and notifications
5. Configure user access and permissions

Pricing:
Azure Managed Grafana uses a pay-as-you-go pricing model based on active users and data retention.
EOF
```

### 2. Application Configuration

Update `appsettings.json`:

```json
{
  "KnowledgeBase": {
    "FilePath": "./datasets/knowledge-base.txt",
    "MaxResults": 3,
    "MaxContentLength": 3000
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "System": "Warning"
    }
  }
}
```

## Running the Server

### 1. Direct Execution

```bash
# Run from project directory
dotnet run

# Run with specific configuration
dotnet run --configuration Release

# Run with custom knowledge base file
dotnet run -- --knowledgebase-path="/path/to/custom/knowledge-base.txt"
```

### 2. Using Built Executable

```bash
# After building, run the executable
./bin/Debug/net*/mcp-server-kb-content-fetcher

# Windows
bin\Debug\net*\mcp-server-kb-content-fetcher.exe
```

### 3. Server Startup Verification

When the server starts successfully, you should see:

```
info: McpServerKbContentFetcher.Program[0]
      Starting MCP Server KB Content Fetcher...
info: McpServerKbContentFetcher.Services.FileKnowledgeBaseService[0]
      Loading knowledge base from: ./datasets/knowledge-base.txt
info: McpServerKbContentFetcher.Services.FileKnowledgeBaseService[0]
      Knowledge base loaded: 5247 characters, 42 lines
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
```

## Diagnostic CLI Mode

An optional diagnostics-only mode allows you to query the knowledge base without initiating the MCP server handshake. This is helpful for quick validation of content loading or search logic. It is intentionally gated behind a required `--cli-mode` flag to ensure the default behavior stays MCP-pure.

Warning:
- Do NOT add `--cli-mode` to your `.vscode/mcp.json` or any MCP client configuration. MCP clients expect STDIO protocol messages (initialize, tools/list, etc.).
- CLI output is raw JSON intended for human inspection or simple scripting, not MCP tooling.

Examples:
```bash
# Get knowledge base info (size, availability, metadata)
dotnet run --project src/mcp-server-kb-content-fetcher -- --cli-mode --get-kb-info

# Perform a search (default max results = 3)
dotnet run --project src/mcp-server-kb-content-fetcher -- --cli-mode --search "Azure Managed Grafana pricing"

# Perform a search with explicit max results
dotnet run --project src/mcp-server-kb-content-fetcher -- --cli-mode --search "dashboard" --max-results 5
```

Sample output (`--get-kb-info`):
```json
{"info":{"fileSizeBytes":7421,"contentLength":7421,"isAvailable":true,"description":"Azure Managed Grafana knowledge base content (file loaded in memory)","lastModified":"2025-09-14T14:21:07.1234567Z"},"status":"available"}
```

Sample output (`--search`):
```json
{"query":"pricing","total":1,"results":[{"matchStrength":1,"position":1234,"length":220,"content":"Pricing:\nAzure Managed Grafana uses a pay-as-you-go pricing model...","context":"... preceding context ... Pricing: Azure Managed Grafana uses a pay-as-you-go pricing model ... following context ..."}]}
```

Exit Codes:
- 0: Successful execution of CLI action
- 1: Knowledge base initialization failure

Internals:
- CLI flags are stripped before host building so they do not leak into generic host configuration binding.
- The same `IKnowledgeBaseService` implementation powers both CLI and MCP tool execution paths.

If you need to debug MCP behavior itself, do not use CLI mode—run normally and send proper JSON-RPC messages to stdin.

## Testing

### Unit Tests

```bash
# Run all unit tests
dotnet test tests/mcp-server-kb-content-fetcher.unit-tests/

# Run with detailed output
dotnet test tests/mcp-server-kb-content-fetcher.unit-tests/ --verbosity normal

# Run specific test class
dotnet test tests/mcp-server-kb-content-fetcher.unit-tests/ --filter ClassName=FileKnowledgeBaseServiceTests

# Run with code coverage
dotnet test tests/mcp-server-kb-content-fetcher.unit-tests/ --collect:"XPlat Code Coverage"
```

### Integration Tests (TBD)

```bash
# Run integration tests
dotnet test tests/mcp-server-kb-content-fetcher.integration-tests/

# Run integration tests with real MCP protocol testing
dotnet test tests/mcp-server-kb-content-fetcher.integration-tests/ --filter TestCategory=McpProtocol

# Run all tests
dotnet test
```

### Test Categories

- **Unit Tests**: Test individual components in isolation
  - `FileKnowledgeBaseServiceTests`: Knowledge base loading and searching
  - `SearchKnowledgeToolTests`: MCP tool parameter validation
  - `GetKbInfoToolTests`: Knowledge base info functionality

- **Integration Tests**: Test MCP protocol compliance (**IMPLEMENTATION OF INTEGRATION TESTS is still TBD**)
  - `McpServerIntegrationTests`: End-to-end MCP communication
  - STDIO transport verification
  - Tool discovery and execution

### Manual Testing with MCP Client

```bash
# Test server communication manually
echo '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"test-client","version":"1.0.0"}}}' | dotnet run

# Test tool discovery
echo '{"jsonrpc":"2.0","id":2,"method":"tools/list","params":{}}' | dotnet run
```

## GitHub Copilot Integration

### Recommended: Configure Via `.vscode/mcp.json`

As of the current MCP-enabled Copilot previews, the preferred, shareable, version-controlled approach is to define MCP servers in a workspace-level file: `.vscode/mcp.json`.

Benefits:
- Keeps configuration in the repo (onboard collaborators instantly)
- Supports multiple servers + secure prompted inputs
- Avoids cluttering global/user `settings.json`
- Single source of truth for tooling (matches existing `mcp.json` already in this repo)

Example entry to add (or confirm) inside `.vscode/mcp.json` under `servers`:

```jsonc
{
  "servers": {
    "kb-content-fetcher": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "./src/mcp-server-kb-content-fetcher"
      ]
    }
  }
}
```

If you also define other MCP servers (e.g., `github`, `perplexity-ask`, `context7`), keep them in the same `servers` object—do not duplicate root keys.

### (Alternative / Legacy) User Settings JSON Method
You can still configure via user/workspace `settings.json`, but this is less portable:

```jsonc
{
  "github.copilot.advanced": {
    "mcp.servers": {
      "kb-content-fetcher": {
        "command": "dotnet",
        "args": ["run", "--project", "./src/mcp-server-kb-content-fetcher"]
      }
    }
  }
}
```

Use this only if your editor build does not yet pick up the `.vscode/mcp.json` manifest.

### Using Copilot With the KB Server
1. Ensure the server definition exists in `.vscode/mcp.json`.
2. Open a Copilot Chat panel; Copilot should auto-start the server on first tool invocation.
3. Invoke tools with natural language or explicit commands:

Prompts you can try:
- "Search the knowledge base for Azure Managed Grafana pricing"
- "What are the key features of Azure Managed Grafana?"
- "Give me the knowledge base status"

Explicit tool calls in chat (syntax may vary slightly by build):
```bash
@workspace /mcp search_knowledge "Azure Monitor integration"
@workspace /mcp get_kb_info
```

If tools are not discovered, open the command palette and reload the window, or verify there are no launch errors in the MCP output / logs.

### Claude Desktop Integration

1. **Configure Claude Desktop** (`~/.config/claude-desktop/claude_desktop_config.json`):
   ```json
   {
     "mcpServers": {
       "kb-content-fetcher": {
         "command": "dotnet",
         "args": ["run", "--project", "/path/to/mcp-server-kb-content-fetcher"]
       }
     }
   }
   ```

2. **Usage in Claude**:
   - Restart Claude Desktop
   - Use commands like: "Search the knowledge base for dashboard creation"
   - Claude will automatically use the MCP server for Azure Managed Grafana queries

## MCP Tools

### search_knowledge

Search the knowledge base for keyword matches.

**Parameters:**
- `query` (string): Search keywords or phrases
- `max_results` (optional int): Maximum results to return (default: 3, max: 5)

**Example:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "tools/call",
  "params": {
    "name": "search_knowledge",
    "arguments": {
      "query": "Azure Monitor integration",
      "max_results": 3
    }
  }
}
```

### get_kb_info

Retrieve knowledge base information and statistics.

**Parameters:** None

**Example:**
```json
{
  "jsonrpc": "2.0",
  "id": 2,
  "method": "tools/call",
  "params": {
    "name": "get_kb_info",
    "arguments": {}
  }
}
```

## Troubleshooting

### Common Issues

#### 1. Server Won't Start

**Problem**: Application fails to start with file access errors.

**Solution:**
```bash
# Check file permissions
ls -la datasets/knowledge-base.txt

# Verify file exists
test -f datasets/knowledge-base.txt && echo "File exists" || echo "File missing"

# Check configuration
cat appsettings.json | grep -A5 "KnowledgeBase"
```

#### 2. MCP Client Connection Issues

**Problem**: MCP client cannot connect to server.

**Solution:**
```bash
# Verify server is running and accepting STDIO
echo '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"test","version":"1.0"}}}' | dotnet run

# Check for port conflicts (if using HTTP transport)
netstat -tlnp | grep :8080
```

#### 3. Search Returns No Results

**Problem**: Knowledge base searches return empty results.

**Solution:**
```bash
# Verify knowledge base content
head -20 datasets/knowledge-base.txt

# Test with simple search terms
echo "Testing search..." | dotnet run -- --test-search "Azure"

# Check case sensitivity
grep -i "azure" datasets/knowledge-base.txt
```

#### 4. Build Failures

**Problem**: Compilation errors or missing dependencies.

**Solution:**
```bash
# Clean and restore
dotnet clean
dotnet restore

# Check .NET version
dotnet --version

# Verify package references
dotnet list package

# Install specific MCP SDK version
dotnet add package ModelContextProtocol --version 0.3.0-preview.4
```

### Logging and Diagnostics

#### Enable Detailed Logging

Update `appsettings.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "McpServerKbContentFetcher": "Trace"
    }
  }
}
```

#### Check Log Output

```bash
# Run with verbose logging
dotnet run --verbosity diagnostic

# Redirect stderr to file for analysis
dotnet run 2>server.log

# Monitor logs in real-time
tail -f server.log
```

### Performance Issues

#### Large Knowledge Base Files

For files >10MB, consider:
1. Splitting content into smaller files
2. Using database storage instead of file-based
3. Implementing pagination for search results

#### Memory Usage

```bash
# Monitor memory usage
dotnet-monitor --urls http://localhost:8080

# Profile memory usage
dotnet run --logger "memory"
```

## Project Structure

```
src/mcp-server-kb-content-fetcher/
├── README.md                              # This documentation
├── mcp-server-kb-content-fetcher.csproj   # Project file
├── Program.cs                             # Main entry point
├── appsettings.json                       # Configuration
├── datasets/                              # Knowledge base files
│   └── knowledge-base.txt                 # Sample AMG content
├── services/                              # Business logic
│   ├── IKnowledgeBaseService.cs          # Service interface
│   └── FileKnowledgeBaseService.cs       # File-based implementation
├── tools/                                 # MCP tools
│   ├── SearchKnowledgeTool.cs            # Search functionality
│   └── GetKbInfoTool.cs                  # Info functionality
├── models/                                # Data models
│   ├── SearchResult.cs                   # Search result model
│   └── ToolModels.cs                     # Tool request/response models
├── configuration/                         # Configuration classes
│   └── ServerOptions.cs                  # Server configuration
└── extensions/                            # Extension methods
    └── LoggingExtensions.cs              # Logging helpers

tests/
├── mcp-server-kb-content-fetcher.unit-tests/
│   ├── services/
│   │   └── FileKnowledgeBaseServiceTests.cs
│   └── tools/
│       ├── SearchKnowledgeToolTests.cs
│       └── GetKbInfoToolTests.cs
├── mcp-server-kb-content-fetcher.integration-tests/
│   └── McpServerIntegrationTests.cs
└── fixtures/
    └── test-knowledge-content.txt
```

## Contributing

### Development Workflow

1. **Fork and Clone**:
   ```bash
   git clone https://github.com/your-username/spec-driven-vibe-coding-challenge.git
   cd spec-driven-vibe-coding-challenge/src/mcp-server-kb-content-fetcher
   ```

2. **Create Feature Branch**:
   ```bash
   git checkout -b feature/your-feature-name
   ```

3. **Make Changes**:
   - Follow C# coding conventions
   - Add unit tests for new functionality
   - Update documentation as needed

4. **Test Changes**:
   ```bash
   dotnet test
   dotnet build --configuration Release
   ```

5. **Submit Pull Request**:
   - Ensure all tests pass
   - Include description of changes
   - Reference related issues

### Code Style

- **Naming**: Use PascalCase for classes, camelCase for variables
- **Folders**: Use kebab-case (e.g., `mcp-server-kb-content-fetcher`)
- **Interfaces**: Prefix with `I` (e.g., `IKnowledgeBaseService`)
- **Documentation**: Include XML comments for public APIs

### Testing Guidelines

- Write unit tests for all public methods
- Use descriptive test method names
- Include integration tests for MCP protocol compliance
- Maintain >80% code coverage

---

## License

This project is licensed under the MIT License - see the [LICENSE](../../LICENSE) file for details.

## Support

For issues and questions:
- Create an issue in the GitHub repository
- Review the [troubleshooting section](#troubleshooting)
- Check existing documentation in the `docs/` folder

---

*Last updated: September 2025*