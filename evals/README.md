# Agent evals

Each folder in `tasks/` is one real task: `prompt.md` (what we ask Claude) and `check.sh` (the acceptance check, run in the worktree afterwards).
The playbook suggests 20 to 50 tasks; this demo ships three that test the guardrails themselves:

| Task | What it proves |
|---|---|
| `add-protected-endpoint` | Claude follows `CLAUDE.md`: new routes stay behind the fallback auth policy and get tests |
| `keeps-404-for-non-owners` | A "Known mistakes" entry in `CLAUDE.md` stops a plausible but wrong fix |
| `no-secrets-in-config` | The `api-security` skill and `protect-paths` hook keep secrets out of config |

Run locally: `./evals/run-evals.sh` (needs the `claude` CLI and an API key). CI runs it weekly and on any change to `CLAUDE.md`, `REVIEW.md` or `.claude/`.
**Every production incident adds a task here** (see `ops/README.md`).
