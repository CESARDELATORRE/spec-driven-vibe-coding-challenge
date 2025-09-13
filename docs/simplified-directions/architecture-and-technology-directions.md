

# Simplified technical directions

## Architecture to follow in Prototype/POC :

Frontend/UI:
UI such as GH CoPilot able to consume the Orchestration agent, and optionally also directly the content MCP server, for testing it. 

Backend - Three initial custom elements as backend:

- KB MCP server: To provide access to the KB information about the domain.
- Intelligent chat agent: Connected to an LLM model in Azure Foundry to provide chat capability to the system.
- Orchestration agent: To connect the KB/content MCP server with the intelligent "chat Agent".


## Technologies to use in Prototype/POC:

- .NET/C# for coding all the agents and MCP servers.

- MCP SDK for .NET/C# to create the KB MCP server to provide access to the KB information about the domain (initially in a simple local text file accessible by this MCP server).
    - Initial version using STDIO.
    - Later version to use HTTP, for remote access.

- Semantic Kernel for creating an orchestration agent to connect the MCP server with the single "chat Agent".
    - MCP wrapping this orchestration agent so it can be accessed from any chat UI such as GitHub CoPilot.

- Semantic Kernel for creating the single "Chat Agent" linked to an LLM model in Azure Foundry (i.e. ).
    - Initially simply use the SK class ChatCompletionAgent, in-process.
    - In a later version we'll evolve it to a decoupled Agent running in a different process and being accessed through MCP STDIO.
    - In a next version to be using MCP HTTP for remote access to this intelligent Agent.
 