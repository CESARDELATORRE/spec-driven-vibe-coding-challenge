# MCP Server KB Content Fetcher – Integration Tests

End-to-end integration tests for the `mcp-server-kb-content-fetcher` project. These tests exercise the **Model Context Protocol (MCP)** over STDIO exactly like an MCP-enabled client would: the server process is spawned (`dotnet run --project <csproj>`), JSON-RPC 2.0 requests are written to its STDIN (one line per JSON object), and responses are read line-by-line from STDOUT. All server logging is routed to STDERR to keep the protocol channel clean.

## Current Coverage
All implemented tests are active (no skips):
- ✅ MCP handshake (`initialize`)
- ✅ Tool discovery (`tools/list` – asserts presence of `search_knowledge` + `get_kb_info`)
- ✅ `get_kb_info` tool invocation (validates structured MCP content payload with status + info fields)
- ✅ `search_knowledge` tool invocation (validates query echo + positive `totalMatches`)

## Project Layout
```
/tests/mcp-server-kb-content-fetcher.integration-tests/
   ├── mcp-server-kb-content-fetcher.integration-tests.csproj
   ├── McpServerProtocolTests.cs          # Three protocol-level test cases
   ├── StdioMcpClient.cs                  # Reusable lightweight JSON-RPC over stdio client
   └── README.md                          # This file
```

## Execution Flow (Per Test)
1. Resolve the server project path using layered fallbacks (repo root, relative, AppContext-based).
2. Launch server: `dotnet run --project <csproj>` with redirected STDIN / STDOUT / STDERR.
3. Send `initialize` request (JSON-RPC id auto-assigned by `StdioMcpClient`).
4. Await matching response line whose `id` equals the request id.
5. Send follow-up (`tools/list` or `tools/call`).
6. Parse response with `System.Text.Json` and assert shape/content using `FluentAssertions`.

### Example Initialize Request
```json
{"jsonrpc":"2.0","method":"initialize","id":1,"params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"integration-tests","version":"1.0"}}}
```

### Example Tool Call Request (`search_knowledge`)
```json
{"jsonrpc":"2.0","method":"tools/call","id":2,"params":{"name":"search_knowledge","arguments":{"query":"pricing"}}}
```

### MCP Tool Response (Shape Excerpt)
```json
{
   "jsonrpc": "2.0",
   "id": 2,
   "result": {
      "content": [
         { "type": "text", "text": "{\"query\":\"pricing\", ...}" }
      ]
   }
}
```

The inner `text` field itself contains JSON we re-parse (payload includes `query`, `totalMatches` or `total`, result set metadata, etc.).

## Running (CLI)
From repository root:

```bash
# Run only integration tests
dotnet test tests/mcp-server-kb-content-fetcher.integration-tests/

# Minimal verbosity
dotnet test tests/mcp-server-kb-content-fetcher.integration-tests/ -v minimal

# Run entire solution tests (unit + integration)
dotnet test
```

## VS Code Test Explorer
You should see three tests:
- Initialize_Then_ListTools_Should_Discover_Expected_Tools
- GetKbInfo_Tool_Should_Return_Knowledge_Base_Status
- SearchKnowledge_Tool_Should_Return_Pricing_Results

All are green when the server starts and datasets are present.

### Debugging
1. Set a breakpoint inside a test method.
2. Use the inline Run / Debug CodeLens or Test Explorer panel.
3. Observe server STDERR in the test output pane for diagnostics.

## Core Abstraction: `StdioMcpClient`
Responsibilities:
- Launch and dispose server process.
- Assign sequential request ids when absent.
- Write newline-delimited JSON.
- Read stdout lines with a short polling timeout (250 ms slice) until matching `id` is found.
- Drain STDERR asynchronously (mirrors real client behavior while preserving protocol purity on STDOUT).

## Design Decisions
| Decision | Reason |
|----------|--------|
| Dedicated `StdioMcpClient` | Encapsulates process + JSON-RPC mechanics; keeps tests focused on assertions |
| Line-by-line response matching by `id` | Deterministic; ignores notifications noise |
| `FluentAssertions` adoption | More readable, intention-revealing assertions |
| Layered project path resolution | Resilient across IDEs, CI runners, and different working directories |
| Re-parsing inner tool JSON payload | Tools currently embed structured JSON inside MCP text content element |
| Uniform timeouts (30s per test) | Prevents indefinite hang while giving cold start headroom |
| Logs to STDERR only | Avoids corrupting MCP STDOUT channel |

## Common Issues & Fixes
| Issue | Symptom | Resolution |
|-------|---------|------------|
| Path resolution failure | FileNotFoundException on startup | Verify repo root; run from solution root; inspect fallback logic prints |
| No initialize response | Timeout in first test | Ensure no stdout logging; confirm server builds locally (`dotnet run`) |
| Empty search results | Assertion fails on `totalMatches > 0` | Confirm dataset file content includes the query term (pricing) |
| Hanging process after cancel | Tests slow to finish | Ensure no external debugger attached; client Dispose kills tree |

## Extending Tests (Next Candidates)
- Add negative tool call (nonexistent tool → error).
- Validate tool schema listing fields (names + descriptions not empty).
- Add a max_results bounded search test.
- Add concurrency test (sequential rapid calls) to confirm server stability.
- Introduce `[Trait("Category","McpProtocol")]` for selective filtering.

## FAQ
**Why parse JSON inside `content[0].text`?**  
Current tools return a single MCP text item embedding a serialized JSON payload; we re-parse to assert semantic fields. This keeps server implementation minimal for the prototype.

**Why not stream partial responses?**  
Prototype scope keeps responses atomic; streaming can be explored later for large result sets.

**Why a custom client vs existing MCP SDK test harness?**  
Keeps dependencies light and surfaces raw protocol traffic for early-phase debugging.

## Future Enhancements (Roadmap Thoughts)
- Switch tool outputs to structured MCP content objects (multiple text items or richer types) instead of embedding JSON strings.
- Add coverage reporting integration for protocol paths.
- Introduce snapshot tests for response shapes (with redaction of volatile fields).

---
_Last updated: 2025-09-14_
