## Repo Map
- docs (Documents on idea/vision-scope, functionality and architecture)
- setup (How to setup this repo woring in VS Code and MCP servers)

## Build 

```bash
# Build (entire solution)
dotnet build spec-driven-vibe-coding-challenge-orchestrator-code.sln

# Or build individual production projects
dotnet build src/mcp-server-kb-content-fetcher/mcp-server-kb-content-fetcher.csproj
dotnet build src/orchestrator-agent/orchestrator-agent.csproj
```

## Test

```bash
# Run ALL tests (solution-wide)
dotnet test spec-driven-vibe-coding-challenge-orchestrator-code.sln

# Recommended granular execution order:
# 1. Unit tests (fast feedback)
dotnet test tests/mcp-server-kb-content-fetcher.unit-tests/mcp-server-kb-content-fetcher.unit-tests.csproj
dotnet test tests/orchestrator-agent.unit-tests/orchestrator-agent.unit-tests.csproj

# 2. Smoke tests (basic end-to-end sanity)
dotnet test tests/orchestrator-agent.smoke-tests/orchestrator-agent.smoke-tests.csproj

# 3. Integration tests (slower, external interactions / protocol)
dotnet test tests/mcp-server-kb-content-fetcher.integration-tests/mcp-server-kb-content-fetcher.integration-tests.csproj
dotnet test tests/orchestrator-agent.integration-tests/orchestrator-agent.integration-tests.csproj
```


## Run (Local)

The following are the commands. But an end-user would use the UI of an Agentic UI such as GH CoPilot or Claude to use the custom agents.
```bash
# Run KB Content Fetcher MCP Server
dotnet run --project src/mcp-server-kb-content-fetcher/mcp-server-kb-content-fetcher.csproj

# Run Orchestrator Agent MCP Server
dotnet run --project src/orchestrator-agent/orchestrator-agent.csproj
```

## Style & Architecture
- Agents and Workflow: C# (maybe adding Python Agents in the future) using Semantic Kernel SDKs for .NET and Python, with MCP protocol for cross-agent communication.
- MCP servers: C# (and Python in the future) using MCP SDKs.
- Cross-language communication: MCP stdio (local) and HTTP+SSE (containerized/cloud).

## PR Rules
- Branch: feat|fix|chore/{scope}-{short}
- Commits: Conventional Commits

## Security
- Never commit secrets.
- Do not run scripts/db:reset in CI.

## Contacts
- Code owners: @cesardelatorre