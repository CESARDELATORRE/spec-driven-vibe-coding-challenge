## Repo Map
- docs (Documents on idea/vision-scope, functionality and architecture)
- setup (How to setup this repo woring in VS Code and MCP servers)

## Build & Test


## Run (Local)

## Style & Architecture
- Agents and Workflow: C# and Python using Semantic Kernel SDKs for .NET and Python, with MCP protocol for cross-agent communication.
- MCP servers: C# or Python using MCP SDKs.
- Cross-language communication: MCP stdio (local) and HTTP+SSE (containerized/cloud).

## PR Rules
- Branch: feat|fix|chore/{scope}-{short}
- Commits: Conventional Commits

## Security
- Never commit secrets.
- Do not run scripts/db:reset in CI.

## Contacts
- Code owners: @cesardelatorre