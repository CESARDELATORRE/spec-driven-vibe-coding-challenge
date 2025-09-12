# Goals and Approaches for the agentic chat system

This is basically the north-star document to be taken into account always, while generating the detailed idea/vision-scope, architecture definition and implementation code.

## Assumption: Problem to be solved and customer feedback on the current generic chat-bot for AMG was "hypothetically" provided

- Since this is an exercise and we don't have real users to ask about the "problem to be solved", feedback about the current chat-bot and the reasons why we want to do this project/idea, we assume that we already have that negative feedback and current customer's pain-points about the generic chat-bot for AMG and therefore we created the following hypothesis based on that.

- **Problem to be solved:** The simplified problem is that current users might not be satisfied with the current generic chat-bot because it doesn't provide skilled insights about the target domain (AMG in this case). Its knowledge about AMG is very limited and generic, plus it seems it might not be able to grow to additional KBs. 

## Challenge hypothesis

The current Azure Managed Grafana (AMG) marketing web site, a.k.a. front door has a generic agent/chat bot. Our hypothesis is that customers and users will be better served by a dedicated agent specifically for AMG.

## Assumption: Hypothesis validation

Again, since this is an exercise, we assume that the main hypothesis was validated by hypothetical customers/users.


## Product goals

### Product overview
A simple Agent for customers or potential customers interested in AMG, capable of providing answers with precise insights about it. 

- Core value proposition: Instantly generate real insights about the domain by answering any question from the customer about that domain (AMG).

- Target audience: Customers or potential customers who want an effective chat-bot.

### Functional specifications

**Build Domain-Specific AI Agent**: Create a dedicated AMG agent that better serves users than generic alternatives.

The agent should be able to answer any question about AMG and could be integrated into Azure AMG marketing page or any other chat UI such as Copilot, GitHub Copilot or any other popular AI-powered chat UI.

**Approach**:
  - Use configurable knowledge bases and prompt templates
  - Focus on conversational UI patterns that scale

### Technical specifications

**Architecture approach:** Implement modular, domain-agnostic architecture while keeping it as simple as possible, taking into account the following technical requirements.

**Platform:** .NET C#, Semantic Kernel and MCP (Model-Context Protocol)

**Design requirements:**

- Agentic system designed to be pluggable into any chat-bot environment thanks to standards such as MCP (Model-Context Protocol).

- Agentic system to be designed with a flexible framework/SDK capable to orchestrate multi-agent workflows if/when  complexity grows, such as Semantic Kernel.

- KB MCP server: A Knowledge Base MCP Server designed in a way that can grow or swap its source data KB transparently, from a basic data source in the prototype/POC to "any KB data source thing" in the future. This probably can be done with a data source implemented as an MCP server.

- Agent should use an LLM model in AI Foundry.

- Design for reusability across comparable KB domains, while maintaining simplicity in the development.


### Prototype/POC scope and tradeoffs

- Single agent using an LLM model in AI Foundry.
- Single and simple custom MCP server with example data initially coming from a text file.
- If using a Semantic Kernel orchestrator agent, it should be simplified with a single agent plus the needed data source/KB such as a custom MCP server. 


## Project goals and approaches

### 1. **Demonstrate Product Maker Mindset**
- **Goal**: Move from hypothesis → prototype, showcasing product development craft
- **General approach**: 
  - Start with user research and problem validation
  - Create lean hypothesis-driven development cycles
  - Document decision-making rationale at each step
  - Focus on evidence-based iterations

- **Approach for the exercise/challenge**: 

    As stated above, we assume that the hypothesis was created based on user's feedback and it was also already validated by users/customers. 
    

### 2. **Problem Framing & User Segmentation**
- **Goal**: Articulate the problem and define user segments clearly
- **Approach**:
  - Analyze current generic chatbot limitations
  - Define specific AMG user personas and pain points


### 3. **Define & Measure Success**
- **Goal**: Establish clear success metrics and measurement framework
- **Approach**:
  - Define baseline metrics from current generic agent
  - Implement testing
  - (Tradeoff - Out of scope for exercise) Track domain-specific query resolution rates
  - (Tradeoff - Out of scope for exercise) Measure user satisfaction and task completion

### 4. **Rapid Prototyping & Iteration**
- **Goal**: Create clickable prototype with iterative improvements
- **Approach**:
  - Build prototype/POC with core conversational flow
  - Start "small" with the simplest working POC, then iterate to add additional functionalities.

### 5. **Capture Reasoning Process**
- **Goal**: Document all prompts, artifacts, and decision-making
- **Approach**:
  - Maintain reasoning journal (at docs-journey-log/Reasoning-Journal-Log.md file) throughout development
  - Version control all AI prompts and responses as well as code and all documents in a source control system (GitHub).
  
## Philosophy

**Agile & Reusable**: Design for quick end-to-end prototype while maintaining extensibility for other technical product domains beyond AMG.

**Evidence-Driven**: Every feature and design decision backed by a hypothesis (in real products backed with user research) or measurable outcomes.

**Simple to Understand**: Clear separation of concerns, minimal complexity, well-documented codebase.

**Documentation:** Document architecture decisions and tradeoffs.
