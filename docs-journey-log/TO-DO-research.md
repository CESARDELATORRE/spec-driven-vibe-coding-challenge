

---> Repo name for GitHub MCP server prompts: CESARDELATORRE/spec-driven-vibe-coding-challenge





- Review ARCHITECTURE doc.

- ADD to JOURNAL LOG DAY 2:
    - Create initial feature specs docs (tbd)
        --> One PR per feature specs doc

- HOW TO create and test assumptions or hypothesis?
        Usually with CUSTOMERS, but could we validate/invalidate with LLMs? That's an interesting but a bit risky option...

- (*) Why [McpServerTool] attributes only in ORCHESTRATOR but not in KB-MCP-Server?


- (*) Add more metadata to MCP Server and AGENTS so questions are better redirected by the LLM

- (*) Do versions as GH VERSIONS? --> iterations



- Update the "ARCHITECTURE DOC FILE NAME when created into the copilot-instructions.md:
<key must-follow items>
        - Use the exact build/test commands from AGENTS.md "Build & Test".
        - (*********) Obey architecture constraints in the Architecture document within /docs folder.
        - PRs: respect "PR Rules" (conventional commits, CI gates).
        - Never commit secrets; see "Security".
</key>

- ## Build & Test instructions at the AGENTS.MD file for Agents to take into account.

- Include the .vscode/tasks.json file when creating TESTS.

- Evaluate the creationg of a "decision log template" as suggested by GHCP.



ADDITIONAL DETAILED TO DOs:
===========================


- MCP SERVER evolve:
Next (Optional) Follow-ups
Consider adding a unit test for GetKbContentTool.
Add a test ensuring search tool still functions when KB unavailable (edge case).
Remove double-serialization remnants on the server side (currently acceptable, but could streamline).
Introduce dynamic query parameters for search tool when ready.


DONE:

- (DONE) - Add Coding-Rules.md file before generating code.
        ---> DONE with csharp.instructions.md
- (DONE) Add a summary of the SPECS-DRIVEN VIBE CODING at the begining of the repo README.md.
- (DONE) Add a visual about SPECS-DRIVEN VIBE CODING at the begining of the repo README.md.
- (DONE) Try it with Claude.
- (DONE) Add "measurement planning".
- (DONE) What **metrics** to use for **decision making**.
-----------> CESAR: Specs docs should be validated when building the features and code, as a metric and measurement.
- (DONE) Add research.prompt.md before creating the Architecture file and coding so I create research info. (DONE for technical and idea)
- (DONE) Add technology.research.prompt.md before creating the Architecture file and coding.
FEATURE:
        - (*) feature.prompt.md for the specs creation for each FEATURE
        - (*) plan.prompt.md for the implementation plan per each FEATURE




- List of topics to cover:
        [X] Vibe coding
        
        [X] Operate as 'native PM/developer' / E2E PM craft work
        
        [X] Clear storytelling on "Why this solution"
        
        [X] Define idea / vision & scope
        
        [X] From hypothesis to prototype
        
        [X] Problem to be solved framing/articulation
        
        [X] Discovery/Research --> Research docs using GHCP with Perplexity MCP server
        
        [X] Scoping
        
        [X] Iterate prototyping
        
        [X] User segments
        
        [X] Measurement planning
        
        [X] Metrics for decission making 
        
        [X] Success metrics
        
        [X] Make and Test assumptions
       
        [X] Iterate quickly based on evidence
        
        [X] Tradeoffs - Make and explain
        
        [X] Develop prototype (Spec-driven vibe approach)
        
        [X] Create/save journal log with all prompts, co-reasoning and actions