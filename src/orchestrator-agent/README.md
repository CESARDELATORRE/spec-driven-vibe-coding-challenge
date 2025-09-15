# Orchestrator Agent MCP Server (Prototype)

Single-turn orchestration MCP server that:
- Optionally launches and queries the Knowledge Base MCP Server (KB) for grounding
- Uses Azure OpenAI (when configured) through Semantic Kernel to synthesize an answer
- Gracefully degrades if KB or LLM configuration is missing (returns disclaimers)

> Prototype scope: No multi-turn memory; no persistence; minimal heuristics (greeting skip, validation, result clamping).

## Tools Exposed
| Tool Name | Description | Status Output Field(s) |
|-----------|-------------|------------------------|
| `get_orchestrator_status` | Basic health/status flags | `status`, `kbConnected` (future), `chatAgentReady` |
| `ask_domain_question` | Validates question, optional KB lookup, attempts LLM answer, returns structured JSON | `answer`, `usedKb`, `kbResults`, `disclaimers`, `diagnostics`, `status` |

Example partial JSON response (degraded path – no Azure OpenAI vars):
```json
{
  "answer": "Azure OpenAI configuration missing; cannot generate answer.",
  "usedKb": false,
  "kbResults": [],
  "disclaimers": [
    "Missing Azure OpenAI configuration (Endpoint / Deployment / ApiKey)",
    "Answer generated without knowledge base grounding"
  ],
  "tokensEstimate": 76,
  "diagnostics": {
    "environment": "Production",
    "endpointConfigured": false,
    "deploymentConfigured": false,
    "apiKeyConfigured": false,
    "attemptedKb": false,
    "heuristicSkipKb": false,
    "chatAgentReady": false,
    "requestedMaxKbResults": 2,
    "effectiveMaxKbResults": 2,
    "kbResultsClamped": false
  },
  "status": "scaffold"
}
```

## Configuration
All runtime configuration is driven by environment variables (portable) + `appsettings.json` (non-secret defaults).

### Required (for LLM positive path)
| Purpose | Env Var | Notes |
|---------|---------|-------|
| Azure OpenAI Endpoint | `AzureOpenAI__Endpoint` | e.g. `https://your-resource.openai.azure.com/` |
| Azure OpenAI Deployment Name | `AzureOpenAI__DeploymentName` | Name of deployed GPT model (e.g. `gpt-4o-mini`) |
| Azure OpenAI API Key | `AzureOpenAI__ApiKey` | Omit only if later switching to AAD + managed identity |

### Optional
| Purpose | Env Var | Notes |
|---------|---------|-------|
 | KB Server Executable Path | `KbMcpServer__ExecutablePath` | Overrides value in `appsettings.json`; may point to base name (auto-detects .exe/.dll) or explicit file |
| .NET Environment | `DOTNET_ENVIRONMENT` | `Development` enables optional User Secrets lookup |

### appsettings.json (checked-in, non-secret)
```json
{
  "KbMcpServer": {
  "ExecutablePath": "../mcp-server-kb-content-fetcher/bin/Debug/net9.0/mcp-server-kb-content-fetcher"
  },
  "GreetingPatterns": ["hi", "hello", "hey", "greetings"],
  "Logging": { "LogLevel": { "Default": "Information" } }
}
```
Override path or patterns via environment variables if needed.

## Running Locally
```bash
# (Optional) Export Azure OpenAI vars for full LLM path
export AzureOpenAI__Endpoint="https://your-resource.openai.azure.com/"
export AzureOpenAI__DeploymentName="gpt-4o-mini"
export AzureOpenAI__ApiKey="YOUR_KEY_VALUE"

# (Optional) Override KB path if different build folder
export KbMcpServer__ExecutablePath="../mcp-server-kb-content-fetcher/bin/Debug/net9.0/mcp-server-kb-content-fetcher"

# Build & run
dotnet build src/orchestrator-agent/orchestrator-agent.csproj
dotnet run --project src/orchestrator-agent/orchestrator-agent.csproj
```
The server speaks MCP over stdio. Keep stdout clean; logs go to stderr.


### To check the ENV VARS available

In PowerShell:

```bash
$env:AzureOpenAI__DeploymentName

$env:AzureOpenAI__Endpoint

$env:AzureOpenAI__ApiKey

$env:KbMcpServer__ExecutablePath

$env:

```

### Using a dev.env file (Recommended Local Pattern)
Maintain all non-secret configuration + secrets (API key) in a single git-ignored `dev.env` file for fast reloads across shells.

1. Create file (first time only):
  ```bash
  cp dev.env.example dev.env
  # edit dev.env with real values (do NOT quote unless value contains spaces)
  ```
2. Load into your current shell session:
  * Bash / Git Bash / WSL:
    ```bash
    set -a
    source dev.env
    set +a
    ```
  * Zsh (mac/Linux): same as above.
  * PowerShell:
    ```powershell
    Get-Content dev.env | ForEach-Object { if ($_ -match '^(.*?)=(.*)$') { $n=$matches[1]; $v=$matches[2]; [Environment]::SetEnvironmentVariable($n,$v) } }
    ```
  * Windows CMD (creates a throwaway batch wrapper):
    ```cmd
    for /f "usebackq tokens=1,2 delims==" %a in (dev.env) do set %a=%b
    ```
    > NOTE: This `for /f` syntax is a classic **cmd.exe** construct. If you paste it into **PowerShell** you will get a parser error like *"Missing opening '(' after keyword 'for'"*. Use one of the PowerShell examples instead (see below) when in a PowerShell terminal.

  * PowerShell (single-line robust loader alternative – ignores comments/blank lines):
    ```powershell
    Get-Content dev.env | Where-Object {$_ -notmatch '^(#|$)'} | ForEach-Object {
      $k,$v = $_ -split '=',2; if ($k) { $env:$k = $v }
    }
    ```
    Or a slightly more explicit version handling trimming:
    ```powershell
    foreach ($line in Get-Content dev.env) {
      if ($line -match '^(#|\s*$)') { continue }
      if ($line -match '^(.*?)=(.*)$') {
        $name = $matches[1].Trim(); $value = $matches[2].Trim()
        $env:$name = $value
      }
    }
    ```
    Verify afterwards:
    ```powershell
    $env:AzureOpenAI__Endpoint; $env:AzureOpenAI__DeploymentName; if ($env:AzureOpenAI__ApiKey) { 'API key present' } else { 'API key missing' }
    ```
3. Verify key variables loaded:
  ```bash
  echo $AzureOpenAI__Endpoint
  echo $AzureOpenAI__DeploymentName
  test -n "$AzureOpenAI__ApiKey" && echo "API key present" || echo "API key missing"
  ```
4. Run the server (same terminal so vars stay in scope):
  ```bash
  dotnet run --project src/orchestrator-agent/orchestrator-agent.csproj
  ```

Notes:
* Lines beginning with `#` in `dev.env` are ignored; keep comments for clarity.
* Avoid surrounding values with quotes unless necessary; the loader above does not trim them.
* Never commit `dev.env`; only the `dev.env.example` template lives in git.

#### Running Both MCP Servers Manually
In separate terminals (after loading `dev.env` in each if needed):
```bash
dotnet run --project src/mcp-server-kb-content-fetcher/mcp-server-kb-content-fetcher.csproj
```
```bash
dotnet run --project src/orchestrator-agent/orchestrator-agent.csproj
```
When using an MCP client (e.g., GitHub Copilot) configured to launch them automatically, you typically only need to ensure the environment variables are set beforehand; this README no longer recommends a VS Code launch configuration approach.

#### One-Step Startup Script
A helper script at the repository root automates loading `dev.env` and launching the orchestrator (and optionally the KB server):
```bash
./run-orchestrator.sh            # load env + build + run orchestrator
./run-orchestrator.sh --no-build # skip build if already built
./run-orchestrator.sh --kb       # also start KB server in background (logs to kb.*.log)
```
If `dev.env` is missing, the script exits with instructions. Works in Git Bash / WSL / Linux / macOS. (On Windows PowerShell, invoke via `bash ./run-orchestrator.sh`).

### Executable vs .dll Launching
Both servers now generate a native apphost executable (e.g., `mcp-server-kb-content-fetcher.exe`) as well as the `.dll`.
The orchestrator launch logic prefers:
1. Exact configured path
2. `<base>.exe`
3. `<base>.dll` (launched via `dotnet <dll>`)
This means you can keep using the extensionless base path in configuration while having flexibility across platforms.

### Environment Variable Propagation (VS Code MCP Launches)
Servers launched from the VS Code MCP Servers panel inherit only the environment present when VS Code started. Sourcing `dev.env` later in a different terminal does not update those already running processes.

Implication: A variable visible during manual `dotnet run` can appear missing in MCP tool diagnostics if VS Code was opened earlier without it.

#### Strategies to Provide Environment Variables
| Strategy | How | Pros | Cons |
|----------|-----|------|------|
| Start VS Code from prepared shell | Source `dev.env` then run `code .` | Mirrors manual runs; no file edits | Must remember each time |
| `.vscode/mcp.json` env block (non-secrets) | Add keys under `servers.orchestrator-agent.env` | Reproducible per workspace | Risk of accidental secret commit |
| User / system env vars | Windows Environment UI / `setx` | Persistent across sessions | Harder to toggle quickly |
| External run script | `./run-orchestrator.sh --kb` | Deterministic, explicit | VS Code UI not managing lifecycle |
| Hybrid | Secrets in user env; flags in `mcp.json` | Minimizes exposure | Slightly more setup |

#### What DOES work (inherit-from-shell flow)
1. In a terminal (Git Bash / WSL / PowerShell) load variables:
   * Bash / Git Bash:
     ```bash
     set -a; source dev.env; set +a
     ```
   * PowerShell:
     ```powershell
     Get-Content dev.env | ForEach-Object { if ($_ -match '^(.*?)=(.*)$') { $env:$($matches[1]) = $matches[2] } }
     ```
2. (Optional) Echo a variable to confirm (e.g., `echo $AzureOpenAI__DeploymentName` / `$env:AzureOpenAI__DeploymentName`).
3. From that SAME terminal launch VS Code: `code .`
4. Start the orchestrator (and KB) from the MCP panel.
5. Call `get_orchestrator_status` or `ask_domain_question`; diagnostics should show expected flags (e.g. `endpointConfigured=true`).
6. If not, fully close all VS Code windows and repeat (a lingering window reused the old environment).

#### Optional `.vscode/mcp.json` Snippet (non-secrets only)
```jsonc
"orchestrator-agent": {
  "command": "dotnet",
  "args": [
    "run",
    "--project",
    "${workspaceFolder}/src/orchestrator-agent/orchestrator-agent.csproj"
  ],
  "env": { "Orchestrator__UseFakeLlm": "true" }
}
```
Avoid putting `AzureOpenAI__ApiKey` in a committed file.

#### Verification Checklist
* `endpointConfigured`, `deploymentConfigured`, `apiKeyConfigured` reflect presence
* `fakeLlmMode=true` if fake flag set
* KB path disclaimers disappear once resolved

#### Quick Troubleshooting
| Symptom | Cause | Fix |
|---------|-------|-----|
| Missing Azure OpenAI config (UI) | VS Code started before sourcing env | Close VS Code, source env, relaunch |
| `fakeLlmMode` false | Typo / missing env entry | Correct; reload window |
| KB executable not found | Relative path mismatch | Use absolute `KbMcpServer__ExecutablePath` |
| Works manually, fails via UI | Environment mismatch | Compare diagnostics outputs |

> Rule: VS Code only knows the environment it was born with.

## Using with GitHub Copilot (MCP Client)
1. Ensure Copilot supports MCP configuration (Insiders / feature flag).
2. Add an entry to your Copilot MCP configuration (example pseudo JSON):
```jsonc
{
  "mcpServers": {
    "orchestrator-agent": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "${workspaceFolder}/src/orchestrator-agent/orchestrator-agent.csproj"
      ]
    },
    "kb-mcp-server": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "${workspaceFolder}/src/mcp-server-kb-content-fetcher/mcp-server-kb-content-fetcher.csproj"
      ]
    }
  }
}
```
3. Restart Copilot so it launches both servers.
4. Ask Copilot to invoke the tool implicitly via natural language (examples below) or explicitly if UI allows selecting tools.

### Example Prompts to Copilot
Natural language (will route to `ask_domain_question`):
- "Using the orchestrator, answer: What is the purpose of the knowledge base server in this repo?"
- "Ask the orchestrator: How does the greeting heuristic work?"
- "Orchestrator: explain how Azure OpenAI config impacts answer generation."

Explicit / structured (if client UI allows specifying the tool):
- Tool: `ask_domain_question` with arguments `{ "question": "Describe the configuration layering strategy", "includeKb": true, "maxKbResults": 2 }`

Status / health:
- "Check orchestrator status"
- "Call get_orchestrator_status"

### Interpreting Responses
- `status` = `scaffold` (prototype) or future `ok` once expanded.
- `disclaimers` array explains any degradation (missing LLM config, no KB, heuristic skip).
- `diagnostics` contains booleans describing which config pieces were detected.

## Validation & Safeguards
- Input validation rejects: empty, <5 chars, punctuation-only.
- `maxKbResults` clamped to range 1..3 (diagnostics include clamp flag).
- Greeting heuristic (configurable patterns) short-circuits KB usage to save startup cost.
- Graceful fallback returns structured JSON even when KB or LLM not available.

## Fake LLM Mode (Deterministic Testing Aid)
Set `Orchestrator__UseFakeLlm=true` plus provide any (non-secret) placeholder Azure OpenAI env vars to force a simulated success path without calling a real model. The response will:
* Start answer with `FAKE_LLM_ANSWER:`
* Include disclaimer `Simulated LLM answer (fake mode)`
* Set `diagnostics.fakeLlmMode=true` and `chatAgentReady=true`

Example:
```bash
export AzureOpenAI__Endpoint="https://fake-endpoint.example.com/"
export AzureOpenAI__DeploymentName="gpt-fake"
export AzureOpenAI__ApiKey="FAKE_KEY_VALUE"
export Orchestrator__UseFakeLlm=true
dotnet run --project src/orchestrator-agent/orchestrator-agent.csproj
```

Use cases:
| Scenario | Benefit |
|----------|---------|
| CI without real model credentials | Deterministic answer string & stable diagnostics |
| Local debugging of downstream consumers | No external latency or quota usage |
| Contract testing | Ensures JSON schema stability separate from model variability |

Disable by unsetting or setting `Orchestrator__UseFakeLlm` to any value other than `true`.

## Extending
| Area | Suggested Next Step |
|------|---------------------|
| LLM Abstraction | Migrate to `Microsoft.Extensions.AI` interfaces (already referenced) |
| Positive-path Test | Add integration test with env vars set (mock or real) |
| KB Search | Invoke `search_knowledge` with query slices instead of static `get_kb_content` |
| Retry Logic | Add limited retry for KB process start failures |
| Monitoring | Emit lightweight metrics (counts of heuristic skips, degradations) to stderr JSON |

## Troubleshooting
| Symptom | Likely Cause | Action |
|---------|--------------|--------|
| Answer contains "Azure OpenAI configuration missing" | Env vars not set | Export `AzureOpenAI__*` values |
| Disclaimers include "KB executable not found" | Path mismatch / not built | Ensure project built; set `KbMcpServer__ExecutablePath` (base path only or explicit .dll/.exe) |
| Tool list missing `ask_domain_question` | Build failed / stale binary | Rebuild project; ensure Copilot restarted |
| Long startup delay on first question | KB server cold start | Pre-run KB MCP server independently to warm it |

## Security Notes
- Never commit secrets: only use environment variables or (local only) User Secrets.
- Logs route to stderr; stdout reserved strictly for MCP protocol messages.
- API key is never logged; only boolean `apiKeyConfigured` appears in diagnostics.

## License
Apache 2.0 (see root `LICENSE`).
