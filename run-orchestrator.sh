#!/usr/bin/env bash
# Simple helper to run the orchestrator MCP server loading env vars from dev.env automatically.
# Usage: ./run-orchestrator.sh [--no-build] [--kb]
#   --no-build  Skip dotnet build (faster if nothing changed)
#   --kb        Also start KB MCP server in a background terminal (if available)
#
# Exits non-zero on failure. Requires bash (Git Bash on Windows ok).
set -euo pipefail
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT="$SCRIPT_DIR"
ENV_FILE="$ROOT/dev.env"
KB_PROJECT="src/mcp-server-kb-content-fetcher/mcp-server-kb-content-fetcher.csproj"
ORCH_PROJECT="src/orchestrator-agent/orchestrator-agent.csproj"

if [[ ! -f "$ENV_FILE" ]]; then
  echo "[run-orchestrator] dev.env not found. Copy dev.env.example to dev.env and populate values." >&2
  exit 1
fi

# Load environment (export all)
set -a
source "$ENV_FILE"
set +a

echo "[run-orchestrator] Environment variables loaded from dev.env" >&2

SKIP_BUILD=false
START_KB=false
for arg in "$@"; do
  case "$arg" in
    --no-build) SKIP_BUILD=true ;;
    --kb) START_KB=true ;;
    *) echo "Unknown argument: $arg" >&2; exit 2 ;;
  esac
  shift || true
end

if ! $SKIP_BUILD; then
  echo "[run-orchestrator] Building orchestrator project..." >&2
  dotnet build "$ORCH_PROJECT" >/dev/null
fi

if $START_KB; then
  if [[ -f "$KB_PROJECT" ]]; then
    echo "[run-orchestrator] Starting KB MCP server in background..." >&2
    (dotnet run --project "$KB_PROJECT" 2>"$ROOT/kb.stderr.log" 1>"$ROOT/kb.stdout.log" &) 
    sleep 2
  else
    echo "[run-orchestrator] KB project not found at $KB_PROJECT (skipping)" >&2
  fi
fi

echo "[run-orchestrator] Starting orchestrator MCP server..." >&2
exec dotnet run --project "$ORCH_PROJECT"