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

# Run integration tests
echo "🔗 Running integration tests..."
dotnet test tests/mcp-server-kb-content-fetcher.integration-tests/mcp-server-kb-content-fetcher.integration-tests.csproj \
  --configuration Release --no-build --verbosity normal --logger trx --results-directory TestResults

dotnet test tests/orchestrator-agent.integration-tests/orchestrator-agent.integration-tests.csproj \
  --configuration Release --no-build --verbosity normal --logger trx --results-directory TestResults

# Run smoke tests
echo "💨 Running smoke tests..."
dotnet test tests/orchestrator-agent.smoke-tests/orchestrator-agent.smoke-tests.csproj \
  --configuration Release --no-build --verbosity normal --logger trx --results-directory TestResults

# Check formatting (optional - might not be configured yet)
echo "📐 Checking code formatting..."
dotnet format spec-driven-vibe-coding-challenge-orchestrator-code.sln --verify-no-changes --verbosity diagnostic || echo "⚠️  Code formatting check failed - consider running 'dotnet format' to fix issues"

# Security scan
echo "🔒 Running security scan..."
dotnet list package --vulnerable --include-transitive 2>&1 | tee vulnerable-packages.log || true

echo "✅ All workflow commands executed successfully!"
echo "📁 Test results saved in TestResults/ directory"
echo "🛡️  Security scan results saved in vulnerable-packages.log"