# 📚 KB MCP Server Quickstart

Minimal MCP server exposing knowledge base metadata and full raw content for Azure Managed Grafana (AMG) prototype.

🏗️ Architecture context: see `docs/architecture-technologies.md` (this component = Knowledge Base MCP server layer). Future evolution: search, excerpt, vector retrieval (deferred, not shipped now).

---
## 🎯 1. Purpose
Provide two stable MCP tools to downstream agents:
- `get_kb_info` – Metadata and availability
- `get_kb_content` – Full raw text (prototype convenience)

Deferred (not implemented): search, excerpts, segmentation, vector retrieval, dynamic reload.

## 🧱 2. Knowledge Base Content
Source file: `datasets/knowledge-base.txt` loaded once at startup into memory. Replace content to change grounding (restart required; hot reload deferred).

## 🔧 3. Prerequisites
- .NET 9 SDK (works on .NET 10 previews)
- Local clone of repo root

## 🚀 4. Build & Run
```bash
# Build
dotnet build src/mcp-server-kb-content-fetcher/mcp-server-kb-content-fetcher.csproj

# Run (STDIO)
dotnet run --project src/mcp-server-kb-content-fetcher/mcp-server-kb-content-fetcher.csproj
```
Protocol: stdout = MCP JSON-RPC, stderr = logs (never mix).

## ⚙️ 5. Configuration
`appsettings.json` excerpt:
```json
{
  "KnowledgeBase": {
    "FilePath": "./datasets/knowledge-base.txt",
    "MaxResults": 3,
    "MaxContentLength": 3000
  }
}
```
Resolution order for FilePath: absolute → CWD → AppContext.BaseDirectory → project fallback.

## 🛠️ 6. MCP Tools
### 6.1 get_kb_info
Returns knowledge base statistics.
```
{
  "status": "ok" | "unavailable",
  "info": {
    "fileSizeBytes": number,
    "contentLength": number,
    "isAvailable": boolean,
    "description": string,
    "lastModified": string (ISO 8601)
  }
}
```
### 6.2 get_kb_content
Returns full raw content.
```
{
  "status": "ok" | "empty" | "error",
  "contentLength": number?,
  "content": string?,
  "error": string? // only when status = error
}
```

## 🤖 7. GitHub Copilot Chat Integration (Optional)
Add `.vscode/mcp.json`:
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
Example prompts:
- "Call get_kb_info on kb-content-fetcher"
- "Retrieve the full knowledge base content"

## 🧪 8. Testing
```bash
# Unit tests
dotnet test tests/mcp-server-kb-content-fetcher.unit-tests/

# Integration tests (optional)
dotnet test tests/mcp-server-kb-content-fetcher.integration-tests/
```

## 🗂️ 9. Project Structure (Excerpt)
```
src/mcp-server-kb-content-fetcher/
  Program.cs
  appsettings.json
  datasets/knowledge-base.txt
  services/
    IKnowledgeBaseService.cs
    FileKnowledgeBaseService.cs
  tools/
    GetKbInfoTool.cs
    GetKbContentTool.cs
  configuration/
    ServerOptions.cs
```

## 🛟 10. Troubleshooting
| Issue | Check | Action |
|-------|-------|--------|
| Server won't start | Path logs / existence | Ensure `datasets/knowledge-base.txt` present |
| Tool not discovered | Startup log scan | Confirm assembly loaded & tool attribute present |
| Empty content | Initialization success | Verify file non-empty, restart server |
| Protocol errors | stdout pollution | Keep all logging on stderr |
| Stale content | Edited file after start | Restart (reload deferred) |

### Note on Search
Search & excerpt intentionally deferred; full text retrieval acceptable at current small size threshold.

## 🔭 11. Future Enhancements (Deferred)
- Excerpt / bounded-length retrieval tool
- Basic substring/semantic search
- File watcher + live reload
- HTTP/SSE transport option
- Vector/embedding pipeline

## 📌 12. Version & Scope Notes
Two-tool contract stable (implementation plan v1.2). Future additions must be additive; breaking changes require explicit versioning.

---
Last Updated: September 2025 (README content merged into unified quickstart)
