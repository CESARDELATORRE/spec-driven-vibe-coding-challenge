# 📚 KB MCP Server

## 🎯 Overview
MCP server that provides knowledge base access through standardized MCP tools. Part of the Architecture Variant 1 (Local Desktop) implementation, this server acts as a bridge between AI agents and domain-specific knowledge stored in text files.

## 📄 Knowledge Base Content
**Current Status**: The prototype uses placeholder content about Azure Managed Grafana (AMG) for demonstration purposes. The text-based knowledge store approach aligns with the prototype scope, focusing on rapid development and demonstration capabilities.

**📍 Location**: `/datasets/knowledge-base.txt`

## 🚀 Setup

```bash
# Install dependencies
dotnet restore

# Build the project
dotnet build
```

## 🛠️ MCP Tools

### 1. 🔍 search_knowledge
Search knowledge base for keyword matches using intelligent substring matching.

**Parameters**:
- `query` (string, required): Search keywords
- `max_results` (int, optional): Maximum results (default: 3, max: 5)

**Returns**: Array of search results with content snippets and match context.

### 2. 📊 get_kb_info
Retrieve knowledge base statistics and metadata for understanding available content.

**Parameters**: None

**Returns**: Knowledge base size, availability status, and metadata.

## ⚙️ Configuration

### appsettings.json
```json
{
  "KnowledgeBase": {
    "FilePath": "./datasets/knowledge-base.txt",
    "MaxResults": 3,
    "MaxContentLength": 3000
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

## 🎮 Usage Scenarios

### 1. 🤖 GitHub Copilot Integration 

**NOTE: This is optional, only in case you want to do a direct testing/trying the KB-MCP Server from GitHub CoPilot.**

Add to `.vscode/mcp.json`:
```json
{
  "servers": {
    "kb-content-fetcher": {
      "command": "dotnet",
      "args": ["run", "--project", "./src/mcp-server-kb-content-fetcher"]
    }
  }
}
```

**💡 Example prompts in Copilot Chat:**
- "Search the knowledge base for Azure Managed Grafana pricing"
- "What are the key features of Azure Managed Grafana?"
- "Get information about the knowledge base status"

### 2. 🎭 Orchestrator Agent Integration

The KB MCP Server is automatically started by the Orchestrator Agent when processing domain questions. The orchestrator coordinates between this server and the in-process Chat Agent to provide comprehensive answers about Azure Managed Grafana.

## 🧪 Testing

```bash
# Unit tests - Fast feedback on business logic
dotnet test tests/mcp-server-kb-content-fetcher.unit-tests/

# Integration tests - MCP protocol validation
dotnet test tests/mcp-server-kb-content-fetcher.integration-tests/
```

## 📁 Project Structure

```
mcp-server-kb-content-fetcher/
├── 📄 Program.cs                     # Entry point with MCP server setup
├── ⚙️ appsettings.json               # Configuration
├── 📂 datasets/                      
│   └── 📚 knowledge-base.txt         # Knowledge base content
├── 🏢 services/                      
│   ├── 📋 IKnowledgeBaseService.cs   # Service interface
│   └── 🔧 FileKnowledgeBaseService.cs # File-based implementation
├── 🛠️ tools/                         
│   ├── 🔍 SearchKnowledgeTool.cs     # Search functionality
│   └── 📊 GetKbInfoTool.cs           # KB metadata tool
└── 📦 models/                        
    ├── 🎯 SearchResult.cs            # Search result model
    └── 📈 KnowledgeBaseInfo.cs       # KB info model
```

## 🔧 Troubleshooting

### ❌ Server Won't Start
- Check if `datasets/knowledge-base.txt` exists
- Verify file permissions: `ls -la datasets/knowledge-base.txt`

### 🔍 Search Returns No Results
- Verify content exists: `cat datasets/knowledge-base.txt | grep -i "your-search-term"`
- Search is case-insensitive substring matching

### 🔌 MCP Client Issues
- Ensure server outputs to stderr for logging (MCP uses stdio)
- Check initialization response format matches MCP spec

---

**📅 Last Updated**: September 2025