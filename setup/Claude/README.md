# 🏠 Claude Desktop Integration

Add to your Claude Desktop configuration file:

**Windows**: `%APPDATA%\Claude\claude_desktop_config.json`

**macOS**: `~/Library/Application Support/Claude/claude_desktop_config.json`

**Linux**: `~/.config/Claude/claude_desktop_config.json`


```json
{
  "mcpServers": {
    "kb-content-fetcher": {
      "command": "dotnet",
      "args": ["run", "--project", "/absolute/path/to/your/project/src/mcp-server-kb-content-fetcher"]
    },
    "orchestrator-agent": {
      "command": "dotnet",
      "args": ["run", "--project", "/absolute/path/to/your/project/src/orchestrator-agent"]
    }
  }
}
```