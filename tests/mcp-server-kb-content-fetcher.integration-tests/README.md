# MCP Server KB Content Fetcher – Integration Tests

End-to-end (E2E) / integration tests for the `mcp-server-kb-content-fetcher` project. These tests exercise the **Model Context Protocol (MCP)** over STDIO exactly like an MCP-enabled client (e.g., GitHub Copilot or Claude Desktop) would: the server process is spawned with `dotnet run`, messages are written to its STDIN, and raw JSON-RPC 2.0 responses are read from STDOUT.

## What These Tests Cover

Current (initial) scope:
- ✅ MCP handshake (`initialize`)
- ✅ Tool discovery (`tools/list`) — asserts presence of `search_knowledge` and `get_kb_info`
- ⏸ Tool invocation (search + info) — temporarily skipped while we standardize the tool return payload format (see TODO notes in test file)

## Project Layout
```
/tests/mcp-server-kb-content-fetcher.integration-tests/
  ├── mcp-server-kb-content-fetcher.integration-tests.csproj
  ├── McpServerProtocolTests.cs          # Handshake + discovery test + skipped placeholders
  └── README.md                          # This file
```

## How the Test Works Internally
1. Starts the server with: `dotnet run` (working directory = `src/mcp-server-kb-content-fetcher`)
2. Sends JSON-RPC initialize message:
   ```json
   {"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"integration-tests","version":"1.0.0"}}}
   ```
3. Waits until a JSON-RPC response appears (substring `"jsonrpc"`).
4. Sends `tools/list` request and asserts discovery of expected tool names.
5. (Future) Will invoke tools and parse structured responses once payload alignment is finalized.

## Running the Integration Tests (CLI)
From repo root:

```bash
# Run only integration tests
 dotnet test tests/mcp-server-kb-content-fetcher.integration-tests/

# Run with normal verbosity
 dotnet test tests/mcp-server-kb-content-fetcher.integration-tests/ --verbosity minimal

# Run all tests (unit + integration)
 dotnet test
```

Filter example (once more tests are active and categorized):
```bash
# (Future) When traits/categories are added
 dotnet test tests/mcp-server-kb-content-fetcher.integration-tests/ --filter Category=McpProtocol
```

## Using VS Code Test Explorer
1. Open the repository in VS Code.
2. Ensure the C# / .NET test extensions are enabled (e.g., C# Dev Kit or built-in test discovery for .NET 9 SDK).
3. Wait for test discovery (Test Explorer panel should list:
   - `Initialize_Then_ListTools_Should_Discover_Expected_Tools`
   - Skipped placeholders for search + info
4. Click the ▶ (Run) icon next to the test or use the inline CodeLens.
5. View output:
   - Open the Test Explorer output channel for logs
   - MCP server runtime logs appear under stderr (we intentionally route logs to stderr to protect the stdout protocol channel)

### Debugging a Test in VS Code
1. In `McpServerProtocolTests.cs`, set a breakpoint inside the test method.
2. Right-click the test in the editor gutter → Run Test / Debug Test.
3. Inspect variables (e.g., captured `stdout` / `stderr`).

## Common Issues & Fixes
| Issue | Symptom | Fix |
|-------|---------|-----|
| Stale server process | Tests hang or fail to start | Kill orphaned `dotnet` processes (`tasklist | findstr dotnet`) |
| Missing dataset file | Tool list OK but future tool call tests fail | Ensure `datasets/knowledge-base.txt` exists in project folder |
| Path resolution mismatch | DirectoryNotFoundException | Confirm relative path logic in `ProjectDirectory` helper |
| JSON not captured | `initialize` assertion fails | Increase timeout or verify no stdout noise from logging |

## Extending the Tests
Planned enhancements:
- Parse JSON-RPC responses with `System.Text.Json` instead of substring matching.
- Add assertions on tool input schema shape.
- Enable tool invocation tests once tool payload format conforms to expected MCP SDK content model.
- Add category/trait attributes (e.g., `[Trait("Category","McpProtocol")]`).

## Design Decisions / Rationale
| Decision | Reason |
|----------|--------|
| Streaming capture via manual read loop | Avoid blocking on full stream close; supports long-running server |
| Substring assertions initially | Fast feedback while payload format stabilizes |
| Skipped tests checked into repo | Visible TODOs encourage future enablement |
| Logs to stderr only | Prevents contamination of MCP STDOUT message channel |

## FAQ
**Q: Why are tool invocation tests skipped?**  
Because current tool delegate return shaping produces wrapped text blocks we are standardizing. Once normalized to the MCP tool content schema, tests will parse and assert structured fields.

**Q: Can I force-run the skipped tests?**  
Yes: remove the `Skip =` argument on the `[Fact]` attribute—but they will currently fail.

**Q: Will this break CI?**  
No. Skipped tests are reported but do not fail the build.

## Next Steps (Suggested)
- Implement structured MCP content responses (array of `{ "type": "text", "text": "..." }`).
- Re-enable `get_kb_info` invocation test.
- Add real search invocation test validating at least one result contains query term.

---
_Last updated: 2025-09-14_
