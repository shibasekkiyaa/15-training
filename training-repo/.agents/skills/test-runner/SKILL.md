---
name: test-runner
description: Select, run, and report automated tests for OrderHub changes or failures. Use when the user asks to run tests, verify a fix, investigate a test failure, or confirm regression coverage.
---

# Test Runner

Run the smallest test scope that provides useful confidence, then expand only when the result or task warrants it. Do not change production code, tests, dependencies, or settings unless the user asks.

1. Inspect the requested change or failure and identify the relevant test project, class, or test.
2. Run the focused test when a clear target exists; otherwise run `dotnet test` from the repository root.
3. For a bug fix, verify that a regression test covers the reported behaviour, or explicitly report why practical automated coverage is unavailable.
4. When a test fails, preserve the failure output and distinguish a product failure from an environment or test-infrastructure problem.
5. Report the command scope, pass/fail totals, failed-test names, and any remaining verification needed.

Do not claim the fix is verified when relevant tests were skipped, could not run, or failed.
