---
mode: 'agent'
description: 'Run the test suite'
---

FIRST: (Unit Tests))

- Run all the unit tests testswith at the CLI
- Ensure all test pass successful
- If tests fail, review the error messages and fix the issues in the codebase
- If tests pass, proceed to the next step

THEN:  (Integration Tests)

- Run all the integration tests at the CLI
- Ensure all test pass successful
- If tests fail, review the error messages and fix the issues in the codebase
- If tests pass, proceed to the next step

FINALLY:

- Report back to the user with the results of the test suite