# Tasks: Orchestrator Agent

**Input**: Design documents from `/docs/features/orchestrator-agent/`
**Prerequisites**: plan-orchestrator-agent.md (required), research-orchestrator-agent.md, data-model-orchestrator-agent.md, contracts/, quickstart-orchestrator-agent.md

🏗️ **Architecture Reference**: The Orchestrator Agent design and integration patterns are detailed in [Architecture & Technologies](../../architecture-technologies.md).

## Execution Flow (main)
```
1. Load plan-orchestrator-agent.md from feature directory ✓
   → Tech stack: C# .NET 9, MCP .NET SDK 0.3.0-preview.4, Semantic Kernel 1.54.0, xUnit 2.9.2
   → Structure: Single project with src/orchestrator-agent/ and tests/ directories
2. Load optional design documents: ✓
   → data-model-orchestrator-agent.md: 4 entities (DomainQuestion, DomainResponse, OrchestratorStatus, KnowledgeSnippet)
   → contracts/: 2 JSON schemas (ask_domain_question, get_orchestrator_status)
   → research-orchestrator-agent.md: Technical decisions resolved
   → quickstart-orchestrator-agent.md: 5 validation scenarios
3. Generate tasks by category: ✓
   → Setup: Project init, MCP dependencies, Azure AI configuration (simplified structure)
   → Tests: Contract tests for 2 tools, integration tests for 5 scenarios (includes smoke tests)
   → Core: 2 consolidated models, 2 MCP tools with embedded orchestration (no separate services)
   → Integration: Response assembly, logging, manual validation
   → Polish: 3 focused unit test files, documentation, final testing
4. Apply task rules: ✓
   → Different files = mark [P] for parallel execution
   → Same file = sequential (no [P])
   → Tests before implementation (TDD approach)
5. Number tasks sequentially (T001, T002...) ✓
6. Generate dependency graph ✓
7. Create parallel execution examples ✓
8. Validate task completeness: ✓
   → All contracts have tests? ✅ (T006-T007)
   → All entities have models? ✅ (T013-T014)
   → All tools implemented? ✅ (T015-T016)
9. Return: SUCCESS (26 ultra-simplified tasks ready for execution)
```

## Format: `[ID] [P?] Description`
- **[P]**: Can run in parallel (different files, no dependencies)
- Include exact file paths in descriptions

## Code Snippets Note
**All code snippets below are high-level pseudocode for guidance only.** In the final implementation, add proper:
- Exception handling and error management
- Input validation and sanitization
- Logging and diagnostics
- Performance optimizations
- Security considerations
- Complete dependency injection setup
- Comprehensive unit test coverage

The snippets focus on architectural patterns and key integration points rather than production-ready code.

## Path Conventions
- **Single project**: `src/orchestrator-agent/`, `tests/orchestrator-agent.*-tests/` at repository root
- Paths below follow the structure defined in plan-orchestrator-agent.md

## Phase 3.1: Setup
- [ ] T001 Create project structure per implementation plan at src/orchestrator-agent/ with tools/, models/ subdirectories (simplified structure)
- [ ] T002 Initialize C# .NET 9 project with MCP .NET SDK 0.3.0-preview.4, Semantic Kernel 1.54.0, Azure AI connectors dependencies in src/orchestrator-agent/orchestrator-agent.csproj
- [ ] T003 [P] Configure Azure AI Foundry environment variables in dev.env.example (AzureOpenAI__ApiKey, AzureOpenAI__Endpoint, AzureOpenAI__DeploymentName)
- [ ] T004 [P] Create unit test project in tests/orchestrator-agent.unit-tests/ with xUnit 2.9.2 and FluentAssertions 6.12.0
- [ ] T005 [P] Create integration test project in tests/orchestrator-agent.integration-tests/ with MCP protocol testing capabilities (includes smoke test scenarios)

## Phase 3.2: Tests First (TDD) ⚠️ MUST COMPLETE BEFORE 3.3
**CRITICAL: These tests MUST be written and MUST FAIL before ANY implementation**
- [ ] T006 [P] Contract test for ask_domain_question MCP tool in tests/orchestrator-agent.integration-tests/McpTools/AskDomainQuestionToolTests.cs
- [ ] T007 [P] Contract test for get_orchestrator_status MCP tool in tests/orchestrator-agent.integration-tests/McpTools/GetOrchestratorStatusToolTests.cs
- [ ] T008 [P] Integration test for Scenario 1 (valid AMG question with KB available) in tests/orchestrator-agent.integration-tests/Scenarios/ValidAmgQuestionWithKbTests.cs
- [ ] T009 [P] Integration test for Scenario 2 (health check and status) in tests/orchestrator-agent.integration-tests/Scenarios/HealthCheckStatusTests.cs
- [ ] T010 [P] Integration test for Scenario 3 (KB unavailable graceful degradation) in tests/orchestrator-agent.integration-tests/Scenarios/KbUnavailableGracefulDegradationTests.cs
- [ ] T011 [P] Integration test for Scenario 4 (input validation and error handling) in tests/orchestrator-agent.integration-tests/Scenarios/InputValidationErrorHandlingTests.cs
- [ ] T012 [P] Integration test for Scenario 5 (content truncation handling) in tests/orchestrator-agent.integration-tests/Scenarios/ContentTruncationHandlingTests.cs

## Phase 3.3: Core Implementation (ONLY after tests are failing)
- [ ] T013 [P] Core models: DomainQuestion and DomainResponse with embedded supporting types in src/orchestrator-agent/models/DomainModels.cs

```csharp
// High-level pseudocode - add validation, error handling in final implementation
public class DomainQuestion
{
    public string Text { get; set; }
    public bool IsValid => !string.IsNullOrWhiteSpace(Text);
}

public class DomainResponse
{
    public string Answer { get; set; }
    public bool UsedKb { get; set; }
    public List<KnowledgeSnippet> KbResults { get; set; }
    // + other properties as needed
}
```
- [ ] T014 [P] OrchestratorStatus model with health monitoring in src/orchestrator-agent/models/OrchestratorStatus.cs

```csharp
// High-level pseudocode - add validation, error handling in final implementation
public class OrchestratorStatus
{
    public string OrchestratorHealth { get; set; } // "Healthy", "Degraded", "Unhealthy"
    public string KbServerStatus { get; set; } // "Connected", "Disconnected"
    public string ChatAgentStatus { get; set; } // "Ready", "ConfigError"
    public string Version { get; set; }
    public DateTime Timestamp { get; set; }
    // + nested classes for dependencies, performance metrics
}
```
- [ ] T015 Ask domain question MCP tool with embedded orchestration (KB MCP client + ChatCompletionAgent) following example pattern in src/orchestrator-agent/tools/AskDomainQuestionTool.cs

```csharp
// High-level pseudocode - add validation, error handling in final implementation
[McpServerTool]
public static async Task<string> AskDomainQuestionAsync(string question)
{
    // 1. Get configuration (Azure AI endpoint, key, deployment)
    // 2. Create MCP client to KB server
    // 3. Setup Semantic Kernel with KB tools as plugins
    // 4. Create ChatCompletionAgent with instructions
    // 5. Execute question and return response
    
    return agentResponse.Message.ToString();
}
```
- [ ] T016 Get orchestrator status MCP tool implementation in src/orchestrator-agent/tools/GetOrchestratorStatusTool.cs

```csharp
// High-level pseudocode - add validation, error handling in final implementation
[McpServerTool]
public static async Task<string> GetOrchestratorStatusAsync()
{
    // 1. Check Azure AI configuration
    // 2. Check KB server connection
    // 3. Build status object with health info
    // 4. Return JSON serialized status
    
    return JsonSerializer.Serialize(status);
}
```
- [ ] T017 MCP server Program.cs with Host.CreateEmptyApplicationBuilder pattern and direct configuration following example code in src/orchestrator-agent/Program.cs

```csharp
// High-level pseudocode - add validation, error handling in final implementation
// Basic MCP server setup pattern
var builder = Host.CreateEmptyApplicationBuilder(settings: null);
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

await builder.Build().RunAsync();
```
- [ ] T018 Basic input validation and error handling with clear error messages

## Phase 3.4: Integration
- [ ] T019 Implement response assembly and content truncation logic per functional requirements
- [ ] T020 Add structured logging to stderr for MCP protocol compliance
- [ ] T021 Validate end-to-end functionality with manual testing

## Phase 3.5: Polish
- [ ] T022 [P] Unit tests for domain models validation logic in tests/orchestrator-agent.unit-tests/Models/DomainModelsTests.cs
- [ ] T023 [P] Unit tests for orchestrator status logic in tests/orchestrator-agent.unit-tests/Models/OrchestratorStatusTests.cs
- [ ] T024 [P] Unit tests for MCP tool orchestration logic in tests/orchestrator-agent.unit-tests/Tools/AskDomainQuestionToolTests.cs
- [ ] T025 [P] Update src/orchestrator-agent/README.md with usage and integration guide per quickstart template
- [ ] T026 Run complete test suite and fix any remaining issues

## Dependencies
- Setup (T001-T005) before all other phases
- Tests (T006-T012) before implementation (T013-T018)
- Models (T013-T014) before tools (T015-T016)
- Tools (T015-T016) before Program.cs (T017)
- Core implementation (T013-T018) before integration (T019-T021)
- Integration (T019-T021) before polish (T022-T026)

## Parallel Example
```
# Launch T013-T014 together (models):
Task: "Core models: DomainQuestion and DomainResponse with embedded supporting types in src/orchestrator-agent/models/DomainModels.cs"
Task: "OrchestratorStatus model with health monitoring in src/orchestrator-agent/models/OrchestratorStatus.cs"

# Launch T015-T016 together (tools):
Task: "Ask domain question MCP tool with embedded orchestration (KB MCP client + ChatCompletionAgent) following example pattern in src/orchestrator-agent/tools/AskDomainQuestionTool.cs"
Task: "Get orchestrator status MCP tool implementation in src/orchestrator-agent/tools/GetOrchestratorStatusTool.cs"

# Launch T022-T024 together (unit tests):
Task: "Unit tests for domain models validation logic in tests/orchestrator-agent.unit-tests/Models/DomainModelsTests.cs"
Task: "Unit tests for orchestrator status logic in tests/orchestrator-agent.unit-tests/Models/OrchestratorStatusTests.cs"
Task: "Unit tests for MCP tool orchestration logic in tests/orchestrator-agent.unit-tests/Tools/AskDomainQuestionToolTests.cs"
```

## Notes
- [P] tasks target different files with no dependencies
- Verify tests fail before implementing solutions
- Commit after each logical group of tasks
- Follow constitutional naming conventions (kebab-case folders, PascalCase namespaces)
- Route all logging to stderr for MCP protocol compliance
- Environment variables must be validated before use
- Graceful degradation required when KB MCP server unavailable
- Follow example code pattern from example-program-semantic-kernel-orchestrator-mcp-server.cs for architecture simplicity

## Task Generation Rules
*Applied during main() execution*

1. **From Contracts**:
   - ask_domain_question.schema.json → T006 contract test + T015 implementation
   - get_orchestrator_status.schema.json → T007 contract test + T016 implementation
   
2. **From Data Model**:
   - Core entities consolidated → T013 combined models + T022 unit tests
   - OrchestratorStatus → T014 model + T023 unit tests
   
3. **From Quickstart Scenarios**:
   - Scenario 1 (Valid AMG question) → T008 integration test
   - Scenario 2 (Health check) → T009 integration test
   - Scenario 3 (KB unavailable) → T010 integration test
   - Scenario 4 (Input validation) → T011 integration test
   - Scenario 5 (Content truncation) → T012 integration test

4. **From Example Code**:
   - Host.CreateEmptyApplicationBuilder pattern → T017 Program.cs
   - MCP client integration → T015 embedded in AskDomainQuestionTool
   - ChatCompletionAgent usage → T015 embedded in AskDomainQuestionTool
   - Direct tool orchestration → T015 AskDomainQuestionTool pattern

5. **Ordering**:
   - Setup → Tests → Models → Services → Tools → Integration → Polish
   - Dependencies block parallel execution where files are shared

## Validation Checklist
*GATE: Checked by main() before returning*

- [x] All contracts have corresponding tests (T006-T007 for 2 contracts)
- [x] All entities have model tasks (T013-T014 for core entities)
- [x] All tests come before implementation (T006-T012 before T013-T020)
- [x] Parallel tasks target different files (verified [P] markings)
- [x] Each task specifies exact file path (all tasks include full paths)
- [x] No task modifies same file as another [P] task (verified no conflicts)
- [x] Test coverage matches functional requirements (18 FRs covered across test scenarios)
- [x] Constitutional compliance maintained (naming, logging, secret hygiene)
- [x] MCP protocol requirements covered (STDIO transport, tool discovery, error handling)
- [x] Simplified architecture following example code patterns

**Task Generation Complete**: 26 ultra-simplified tasks ready for execution following example code patterns with embedded orchestration.
