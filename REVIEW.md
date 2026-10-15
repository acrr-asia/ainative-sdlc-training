# REVIEW.md

Review policy for Claude's PR review (`.github/workflows/claude-review.yml`) and for human reviewers.
Claude's findings are advice: they do not approve or block. Branch protection still requires a CODEOWNERS approval.

## Passes (in order)
1. **Spec and plan.** Does the diff do what `specs/<n>/spec.md` and `plan.md` say, and nothing else? Flag scope creep.
2. **Bugs.** Logic errors, null handling, wrong status codes vs the spec table, EF queries that load too much.
3. **Security.** Every rule in `.claude/skills/api-security/SKILL.md`. Any new anonymous endpoint is a must-fix unless it is in the allowlist test.
4. **Tests.** Each new behaviour has a test that would fail without the change. No deleted or weakened assertions.

## Output rules
- Severity: `must-fix`, `should-fix`, `nit`.
- At most **3 nits** per PR. Skip style that `dotnet format` already enforces.
- Each finding cites `file:line` and the spec/skill rule it relates to.
- If nothing is wrong, say "No findings" and stop.

## Tuning
The tech lead reviews false positives monthly and adjusts this file. Changes here trigger the eval suite.
