#!/bin/bash

# Local validation script for CI workflow commands
# This script mirrors the GitHub Actions workflow steps for local testing

set -e

echo "🔧 Validating CI Workflow Commands Locally"
echo "=========================================="

# Check .NET version
echo "📋 Checking .NET version..."
dotnet --version

# Restore dependencies
echo "📦 Restoring dependencies..."
dotnet restore spec-driven-vibe-coding-challenge-orchestrator-code.sln

# Build solution
echo "🔨 Building solution..."
dotnet build spec-driven-vibe-coding-challenge-orchestrator-code.sln --configuration Release --no-restore

# Create test results directory
mkdir -p TestResults

# Run unit tests
echo "🧪 Running unit tests..."
dotnet test tests/mcp-server-kb-content-fetcher.unit-tests/mcp-server-kb-content-fetcher.unit-tests.csproj \
  --configuration Release --no-build --verbosity normal --logger trx --results-directory TestResults

dotnet test tests/orchestrator-agent.unit-tests/orchestrator-agent.unit-tests.csproj \
  --configuration Release --no-build --verbosity normal --logger trx --results-directory TestResults

# Integration tests and smoke tests skipped in CI workflow (run manually if needed)
echo "⏭️ Skipping integration tests and smoke tests (excluded from CI workflow)..."

# Check formatting (optional - might not be configured yet)
echo "📐 Code formatting can be checked with: dotnet format --verify-no-changes"

# Security scan
echo "🔒 Running security scan..."
dotnet list package --vulnerable --include-transitive 2>&1 | tee vulnerable-packages.log || true

echo "✅ All workflow commands executed successfully!"
echo "📁 Test results saved in TestResults/ directory (unit tests only)"
echo "🛡️  Security scan results saved in vulnerable-packages.log"
echo "💡 Integration tests and smoke tests can be run manually if needed"