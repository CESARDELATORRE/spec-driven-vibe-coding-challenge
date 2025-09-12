# Reasoning Journal Log

## Day 1

### Goals for Day 1 

- Create the repo structure, initial co-pilot instruction files, helper MCP servers config and the reasoning journal log approach and basic "north-star" goals and approaches baseline document":

    --> PR #1

- Create initial projects docs:
    - Create project's vision & scope document (idea)

        --> PR #2

    - Create project's Architecture and technology decissions document

        --> PR #3

    - Create initial feature specs docs (tbd)

        --> One PR per feature specs doc

### Global setup

#### GH repo

I want to work driven by issues and branches w/ related PRs, like when working with a dev team, so everything **can be trackable**.

In some cases I'll create the issues in github.com repo, in other cases directly using the GH MCP server.

About PRs, some branches w/ PRs will be created by me, in other cases branches w/ PRs will be created by GHCP **CODING AGENT** for me.

I created this GitHub repo and started working from this initial issue and related PR:

Issue #1:
![alt text](images-journey/issue-01.png)

Related branch:
![alt text](images-journey/branch-01.png)

This is just a visual example, to showcase the approach, but I won't be putting additional examples in the log, for efficiency's sake.

#### GHCP and MCP servers

Before starting using GHCP alter, I set up my usual approach which is using the following MCP servers as tools for GHCP plus some extensions for VS Code:

**MCP servers at .vscode/mcp.json:**

- **GitHub MCP server:** For directly operating against my GitHub repo from GHCP in VS code. 
- **Perplexity MCP server:** For better research and reasoning for approches and documents by using content from the Internet (I'm using my own subscription).
- **Context7 MCP server:** For better research and reasoning for coding while having access to their SDK documentations thorugh this MCP server.

See my **.vscode/mcp.json** file.

### Thoughts and own brainstorming

The main goal is to *demonstrate end‑to‑end PM + developer craft: problem framing, discovery, scoping, iterative prototyping, measurement planning, and clear storytelling about why this  solution is the right next step,* ** *not just that it “works.”* **.

Therefore, this is not just about YOLO VIBE CODING, but having a solid approach starting from the requirements, idea and specs docs, moving later to the coding/implementation. 

So, I know how I'll probably tackle the challenge following practices I've been using in the last months which is basically having a new mindset as "AI-Native PM/developer" where the PM component is important, so no YOLO vide coding but I believe engineering excellence must be based on a "Spec-driven Vibes", but having a single "living/evolving" repo with idea, specs docs and implementation code.

For the "Spec-driven Vibes I have a good idea on how to move based on written instructions in files in the repo for GHCP (.github/copilot-instructions.md, AGENTS.MD, .github/instructions folder, and pre-defined templates for well defined prompts within the .github/prompts folder), then generating my idea/architecture docs, specs docs and finally the implementation code, by iterating with CoPilot. I've done that approach for several projects in the last months (Multi-Agent workflow POC and a VB6 application migration/modernization research for a customer) and I think that's the right way to go. 

This is going to be my approach, all within the same GitHub repo with the code and created by co-reasoning with iterations with GH CoPilot prompts:

- Functional spec for flow and architecture
- Implementation plan drives iteration tasks
- Custom prompts/tools formalize workflows

However, related to how to record/write a reasoning journal log...,let's start with an open mind and ask CoPilot and other tools what could be a good approach for this "reasoning journal log".



### Prompts & Results 

Prompt -> Copilot output -> My action

#### Prompt 1 - What approach to go for reasoning process logging

For most of the project and specs-driven vibes, I'll be using GitHub CoPilot, but for this initial brainstorming about approaches for journal logging, I'm starting to ask to several tools:
- M365 CoPilot with Researcher Agent
- ChatGPT
- Claude

I did the same prompt for all of them:

```
// Context: I’m going to be working with VS Code and GitHub Copilot on creating a feature, from the idea and docs, specs, to the implementation in C#. 

// Objective: I also want to record/save somewhere, in some kind of documentation, the whole process I’m doing, including what I think and I’m going to do and also every prompt I create and use with GHCP, so later I can showcase the process and reasoning I did. What would be the best way to save/write my whole process and reasoning including my own prompts?

// Requirements: Initially open to multiple approaches
// Format: Initially open to multiple approaches
// Constraints: I don't want to over-engineer, I need to be agile.
```

#### Copilot/ChatGPT/Claude outputs from Prompt 1

Since these outputs are long, I put each in a different WORD doc that I read and researched. 

They are here in the repo:

/docs-journey-log/approaches-for-reasoning-journal-logging/

M365-CoPilot-Researcher-approaches-Reasoning-Journal.docx
ChatGPT-approaches-Reasoning-Journal.docx
Claude-approaches-Reasoning-Journal.docx

#### My action after prompt 1

I got many insights from those answers, such as:

- “Everything-in-GitHub-repo” and simply using .MD docs for logging the reasoning, process and prompts. Also, using GitHub Issues and PRs to track advances in a clean way.

- Using additional tools/platforms such as **Notion** and **Obsidian**. 

I reviewed all the suggestions and will keep doing it while advancing.

My decission, given that time is limited and I want to make it agile and not overengineering for a simple exercise, I chose to simply write everything in .MD files within this repo.

Basically, this is the initial approach and repo structure related to documents:

**/docs** --> Project's documents such as idea/vision-scope, architecture, etc.

**/docs-journey-log** --> Documents to log the co-reasoning when working with GHCP CoPilot (or additional tools such as ChatGPC and Clause, in some cases) and the decissions taken after each prompt execution with answers.

Files for GHCP instructions and prompts templates:

**.github/**

**.github/instructions/**

**.github/prompts/**



#### Prompt 2 - Indentify main goals and best approaches to take based from the challenge definition doc

Additionally, before getting to work on the idea, vision, scope and requirements, I also want to give a quick pass with CoPilot about what are the important goals to achieve and what should be the potential approaches to take, while keeping the process agile and light. 

This is important so I won't focus on what I already have in my mind but I'll take into account additional points of view or approaches I didn't think about, like if I'm in a branstorming session with someone or a dev team.

PROMPT - Using GHCP in AGENT MODE and Claude Sonnet 4, for better reasoning on documentation.

```
// Context: I have the attached challenge definition (/docs/01-original-challenge-definition.md). Review it deeply.

// Objective: Extract the most important goals as different bullets an per each goal propose a good approach for tackling it:
- Goals of the challenge
   - Best approaches: Per goal, how to tackle it, best approaches to take

// Requirements: Even when the challenge is targeting a particular domain context related to Azure Managed Grafana (AMG) marketing web site, I'd like to take approaches and architecture decissions that would make possible to use this solution for other comparable use cases, not just for AMG.

// Format/Output: Create a new .MD doc named "02-plain-goals-and-approaches.md" with it, very simple and going to the mentioned points. It probably shouldn't be longer than a single page.
We'll use this document as the baseline to later develop the idea, vision-scope, architecture, etc. but that will come in later iterations.

// Constraints: I don't want to over-engineer, I want to be agile and create and end to end prototype quikcly, which should be simple to understand.
```

![alt text](images-journey/prompt-02.png)


#### GHCP Copilot outputs from Prompt 2

GHCP:

**--> Generated Document: 02-plain-goals-and-approaches.md **

*Based on my review of the challenge definition, I'll extract the key goals and create a concise document with approaches for tackling each one. Let me create the new document now.

I've created the 02-plain-goals-and-approaches.md document that extracts the key goals from the challenge definition and provides practical approaches for each one.

The document focuses on:

Product maker mindset - Moving from hypothesis to prototype with evidence-based decisions
Problem framing - Understanding users and their specific needs beyond generic solutions
Domain-specific AI agent - Building a specialized solution with reusable architecture
Success measurement - Defining metrics and A/B testing capabilities
Rapid prototyping - Agile development with iterative improvements
Reasoning documentation - Capturing the entire decision-making process
Each goal includes specific approaches that balance the need for a quick AMG prototype while designing for reusability across other technical product domains. The document stays concise (single page) and emphasizes agility and simplicity as requested.

This will serve as your baseline for developing the vision, scope, and architecture in future iterations while keeping the focus on creating an end-to-end prototype quickly.


#### My actions after prompt 2

I reviewed the **02-plain-goals-and-approaches.md** and made some manual updates:

I made a few changes such as tradeoff I need to maek due to the short time and the fact that this is an exercise, not a real feature for customers. For example:

- **Approach for the exercise/challenge**: 
    - Same as above but related to user research needs to be limited to our own hypothesis and validation, not with real users, still, creating the hypothesis-based approach.

Since we won't have real users and satisfaction measurement, as tradeoff of te exercise I'm specifying some out of scope points that normally should have been included in a real project/feature:

**Define & Measure Success**
- **Goal**: Establish clear success metrics and measurement framework
- **Approach**:
  - Define baseline metrics from current generic agent
  - Implement testing
  - (Tradeoff - Out of scope for exercise) Track domain-specific query resolution rates
  - (Tradeoff - Out of scope for exercise) Measure user satisfaction and task completion

I also added this comment at the begining of this doc:

The **02-plain-goals-and-approaches.md** is 
*basically the north-star document to be taken into account always, while generating the detailed idea/vision-scope, architecture definition and implementation code.*




### Create PR for repo structure and merge into main branch

At this point we have the foundational repo structure, initial co-pilot instruction files, helper MCP servers config (Perplexity, Context7 and GitHub MCP servers) and this reasoning journal log approach plus the simplified "north-star" goals and approaches baseline document, all ready to start hacking with spec-driven vibing approaches.

Before moving forward, let's merge this branch content with a PR into the main branch.

![alt text](images-journey/task-repo-structure-branch.png)




#### Prompt 3 - Idea / Vision-Scope document

Now that we have the initial north-star defined, let's deep dive into the details, starting with the definition of the idea with an  "idea-vision-scope" document.

For this, I'm starting to use a pre-defined template that I'm putting into the **.github/prompts** folder, named **new.idea.vision.scope.prompt.md**.

Also, before running this prompt, I'm manually adding some global instructions for GHCP at the **.github/copilot-instructions.md** file and other instructions files within **.github** folder.

















### Global decisions made in Day 1



### Next Steps for Day 2. 