---
name: code-reviewer
description: Review proposed or completed code changes for correctness, regressions, validation gaps, and missing tests. Use when the user requests a code or pull-request review, or when another workflow needs an independent review pass before handoff.
---

# Code Reviewer

Review the relevant diff and surrounding code. Report findings; do not modify files unless the user also asks for changes.

1. Establish the review scope from the requested files, diff, or current worktree.
2. Read the changed code and the callers, contracts, and tests needed to verify its behaviour.
3. Check for correctness, regressions, input/error handling, unintended scope changes, and missing or invalid tests.
4. Follow repository conventions and treat user-provided requirements as acceptance criteria.
5. Run existing relevant tests only when useful and permitted; report their exact result.

Report only actionable findings, ordered by severity. Each finding must include the affected file and location, the concrete risk, and why it occurs. If no findings remain, say so and note residual test or manual-verification gaps separately.
