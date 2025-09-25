# GitHub Actions Workflows

This directory contains GitHub Actions workflows for the Spec-Driven Development challenge project.

## Workflows

### CI (Continuous Integration) - `ci.yml`

**Triggers:**
- Push to `main`, `develop`, or any `features/*` branch
- Pull requests targeting `main` or `develop`

**Jobs:**

#### 1. Build and Test
- **Platform:** Ubuntu Latest
- **.NET Version:** 9.0.x
- **Steps:**
  1. **Checkout code** - Gets the latest source code
  2. **Setup .NET** - Installs .NET 9.0 SDK
  3. **Restore dependencies** - Downloads NuGet packages
  4. **Build solution** - Compiles the entire solution in Release mode
  5. **Run unit tests** - Executes fast unit tests first
     - `mcp-server-kb-content-fetcher.unit-tests`
     - `orchestrator-agent.unit-tests`
  6. **Publish test results** - Creates test report summaries
  7. **Upload test artifacts** - Saves test results for download

#### 2. Code Quality Checks
- **Platform:** Ubuntu Latest
- **Steps:**
  1. **Checkout code** - Gets the latest source code
  2. **Setup .NET** - Installs .NET 9.0 SDK
  3. **Restore dependencies** - Downloads NuGet packages
  4. **Security scan** - Checks for vulnerable NuGet packages
  5. **Upload security scan results** - Saves security scan results

## Test Execution Order

Following the recommendations from `AGENTS.md`, tests are executed in this order:

1. **Unit Tests** (fast feedback)
   - Knowledge Base unit tests
   - Orchestrator Agent unit tests

**Note:** Integration tests and smoke tests are currently excluded from the CI workflow and should be run manually when needed.

## Artifacts

The workflow generates the following artifacts:

- **test-results**: Test result files (.trx format) available for 7 days
- **security-scan**: Security vulnerability scan results available for 7 days

## Environment Variables

- `DOTNET_VERSION: '9.0.x'` - Specifies .NET version to use
- `DOTNET_SKIP_FIRST_TIME_EXPERIENCE: true` - Skips .NET welcome messages
- `DOTNET_NOLOGO: true` - Suppresses .NET logo output
- `DOTNET_CLI_TELEMETRY_OPTOUT: true` - Disables telemetry collection

## Security Considerations

- Uses latest GitHub Actions (v4 for checkout/upload, v4 for .NET setup)
- Runs security scans on NuGet packages
- No secrets are exposed in the workflow
- Tests run in isolated containers

## Monitoring

- Test results are published as GitHub Check annotations
- Failed tests will cause the workflow to fail
- Code formatting violations will cause the workflow to fail
- Security scan results are uploaded but don't fail the build

## Local Validation

Use the provided validation script to test workflow commands locally before pushing:

```bash
# From repository root
./.github/workflows/validate-local.sh
```

This script mirrors the exact commands used in the GitHub Actions workflow, helping you catch issues before they reach CI.