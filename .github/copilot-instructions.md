<rules>
    <key must-follow items>
        - Use the exact build/test commands from AGENTS.md "Build & Test".
        - Obey architecture constraints in the Architecture document within /docs folder.
        - PRs: respect "PR Rules" (conventional commits, CI gates).
        - Never commit secrets; see "Security".
    </key>

    <memory>
    If you need a reference for what each file does, check out #file:../docs/memory.md.

    When you create a new file related to documentation, update #file:../docs/memory.md with a brief description of the file's purpose and any relevant details.
    Do NOT update the #file:../docs/memory.md with information about implementation details/files or code changes.
    </memory>

    <context7>
    If you lack context on how to solve the user's request:

    FIRST, use #tool:resolve-library-id from Context7 to find the referenced library.

    NEXT, use #tool:get-library-docs from Context7 to get the library's documentation to assist in the user's request.
    </context7>

    <commit>
    WHEN file changes are COMPLETE:
    - Stage your changes with git add .
    - Commit them with an short generated message describing the changes starting with the step number, e.g. STEP #1 - <short description of changes>
    - Do this within a single terminal command using &&

    ONLY do this if you create or edit a file during the turn.
    </commit>
</rules>
