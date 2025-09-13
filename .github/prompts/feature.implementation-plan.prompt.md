---
mode: 'edit'
description: 'Plan a feature implementation/coding'
---

Your goal is to generate an implementation/coding plan for a specification document provided to you.

RULES:
- Keep implementations simple, do not over architect
- Do not generate real code for your plan, pseudocode is OK
- For each step in your plan, include the objective of the step, the steps to achieve that objective, and any necessary pseudocode.
- Call out any necessary user intervention required for each step
- Consider accessibility part of each step and not a separate step
- Follow the rules in #file:../../.github/copilot-instructions.md
- ALWAYS follow the coding rules in #file:../../.github/instructions/csharp.instructions.md (like project names and folder's structure), but do not create real code in this implementation/coding plan document but simplify the code-snippets to the maximum extent possible. Always make sure the code snippets you provide in your plan follow the coding rules.
- (Special rule for prototy/POC) Focus on simplest possible implementation that meets the requirements
- (Special rule for prototy/POC) Avoid over engineering or over architecting
- (Special rule for prototy/POC) Avoid production grade implementations or optimizations
- (Special rule for prototy/POC) Avoid advanced features or capabilities that are not in scope
- (Special rule for prototy/POC) The most important rule for this implementation plan is to keep things (approaches, design and related code) as simple as possible while still meeting the requirements of the specification document provided to you.

FIRST:

- Review the global docs such as #file:../../docs/03-idea-vision-scope.md and #file:../../docs/04-architecture-technologies.md to understand an overview of the project, but focus mostly on the prototype/POC scope (functional, architecture and technologies for the prototype).
- Review the attached specs document (as context file) to understand the requirements and objectives.

THEN:
- Create a detailed implementation plan that outlines the steps needed to achieve the objectives of the specification document.
- The plan should be structured, clear, and easy to follow.
- Structure your plan as follows, and output as Markdown code block

```markdown
# Implementation Plan for [Spec Name]

- [ ] Step 1: [Brief title]
  - **Task**: [Detailed explanation of what needs to be implemented]
  - **Files**: [Maximum of 10 files, ideally less]
    - `path/to/file1.cs`: [Description of changes], [Pseudocode for implementation]
  - **Dependencies**: [Dependencies for step]

[Additional steps...]
```

- After the steps to implement/code the feature, add a step to build and run the app
- Add a step to write unit tests, integration tests and UI tests for the feature
- Add a step to run all tests as last step 

NEXT:

- Iterate with me until I am satisfied with the plan

FINALLY: 

- Output your plan in #folder:../../docs/implementation-plans/feature-implementation-plan-name.md
- DO NOT start implementation without my permission.