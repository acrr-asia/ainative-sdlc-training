# AI-native SDLC demo: a .NET 10 Task API

A small, real Web API (JWT login, roles and policies, tasks and categories) whose only job is to show
**every stage of [The AI-Native SDLC Playbook](https://claude.com/resources/articles/the-ai-native-sdlc-playbook)
as a file you can open**. The features are deliberately small; the process around them is the point.

```
 Plan            Design          Build                     Test                 Deploy                 Maintain
 intent.md  ──>  spec.md   ──>   plan.md + CLAUDE.md  ──>  tests + evals  ──>   REVIEW.md + CI   ──>   bands.yaml ─┐
     ▲                           skills, hooks, agents                          approval hooks          runbooks    │
     └──────────────────────────────── new intent.md written from the incident ◄────────────────────────────────────┘
```

## Quick start

```bash
dotnet test                              # 33 tests, under 10 seconds
dotnet run --project src/TaskApi         # http://localhost:5033, OpenAPI at /openapi/v1.json
```

```bash
# Development seeds admin@example.com / Admin123!
curl -X POST localhost:5033/auth/register -H 'content-type: application/json' -d '{"email":"me@x.io","password":"Password123"}'
TOKEN=$(curl -s -X POST localhost:5033/auth/login -H 'content-type: application/json' -d '{"email":"me@x.io","password":"Password123"}' | jq -r .accessToken)
curl -X POST localhost:5033/tasks -H "authorization: Bearer $TOKEN" -H 'content-type: application/json' -d '{"title":"Read the playbook"}'
curl localhost:5033/tasks -H "authorization: Bearer $TOKEN"
```

To click through the whole flow, import the Postman collection in [`postman/`](postman/README.md).

## Where each playbook practice lives

| Stage | Playbook practice | In this repo |
|---|---|---|
| **1. Plan** | Capture the idea as `intent.md`, reviewed by the product owner | [`intent/0001-task-api.md`](intent/0001-task-api.md) (status: Accepted) |
| **2. Design** | Claude turns intent into a requirements + design spec, checked by policy skills | [`specs/0001-task-api/spec.md`](specs/0001-task-api/spec.md): endpoint table, policies, rules S1 to S5 |
| **3. Build** | Plan mode first; the approved plan is committed | [`specs/0001-task-api/plan.md`](specs/0001-task-api/plan.md) |
| | `CLAUDE.md`: commands, conventions, architecture, known mistakes, under a page | [`CLAUDE.md`](CLAUDE.md) |
| | Skills as institutional knowledge | [`.claude/skills/api-security/SKILL.md`](.claude/skills/api-security/SKILL.md) |
| | Hooks as build-time guardrails | [`.claude/hooks/protect-paths.sh`](.claude/hooks/protect-paths.sh) (protected paths, secrets, test lock), [`format-cs.sh`](.claude/hooks/format-cs.sh), wired in [`.claude/settings.json`](.claude/settings.json) |
| | Subagents with their own context and tool limits | [`.claude/agents/verifier.md`](.claude/agents/verifier.md) (read-only, fresh context) |
| | Team permissions (allow / ask / deny) | `permissions` in [`.claude/settings.json`](.claude/settings.json) |
| **4. Test** | Give Claude a feedback loop: tests it can run itself | [`tests/TaskApi.Tests`](tests/TaskApi.Tests): every spec row and rule has a test (`// spec S4` comments) |
| | Failing test first for bug fixes, with a hook that locks tests | `touch .claude/FIX_MODE` (see `CLAUDE.md` Workflow) |
| | Guardrail tests | [`EndpointSecurityTests`](tests/TaskApi.Tests/EndpointSecurityTests.cs): only allowlisted routes may be anonymous |
| | Continuous evals in CI | [`evals/`](evals/README.md) + [`.github/workflows/agent-evals.yml`](.github/workflows/agent-evals.yml) |
| **5. Deploy** | AI in the PR review loop, policy in `REVIEW.md` | [`REVIEW.md`](REVIEW.md) + [`.github/workflows/claude-review.yml`](.github/workflows/claude-review.yml); humans still approve via [`CODEOWNERS`](.github/CODEOWNERS) |
| | Hooks as approval gates | [`.claude/hooks/deploy-gate.sh`](.claude/hooks/deploy-gate.sh): `ops/deploy.sh prod` needs `RELEASE_AUTHORIZED_BY` |
| | CI/CD: read-only judgment steps first | `triage` job in [`.github/workflows/ci.yml`](.github/workflows/ci.yml) comments on failures, cannot push |
| | Rollback is one rehearsed command | `ops/deploy.sh prod --rollback` |
| **6. Maintain** | Deterministic detection, response tiers in `bands.yaml` | [`ops/bands.yaml`](ops/bands.yaml), [`ops/detect.py`](ops/detect.py) (no model decides) |
| | Claude writes its diagnosis as a new `intent.md` | [`intent/0002-login-throttling.md`](intent/0002-login-throttling.md), then taken around the loop again: [`specs/0002-login-throttling/`](specs/0002-login-throttling), `LoginThrottle`, eval `throttle-keeps-s3` |
| | Runbooks and lessons, version-controlled | [`ops/runbooks/`](ops/runbooks), [`docs/lessons.md`](docs/lessons.md) |
| **Audit trail** | Each stage is a commit the next stage reads | `git log --oneline`: one commit per stage |

## The API (what was built)

| Feature | Practice it shows |
|---|---|
| `POST /auth/register`, `POST /auth/login`, `GET /auth/me` | JWT bearer auth; key validated at startup (S1), same error for bad email or password (S3) |
| Login throttling (spec 0002) | 5 failed logins per IP per minute → `429` + `Retry-After` for 1 minute (S6 to S9); the loop's second lap |
| `GET/POST/DELETE /categories` | Role-based policy: anyone signed in can read, only `AdminOnly` can write |
| `GET/POST/PUT/DELETE /tasks` | Resource-based policy `TaskOwnerOrAdmin`; non-owners get `404` (S4) |
| `GET /admin/users`, `PUT /admin/users/{id}/role` | Policy on a whole route group; no password hashes returned (S5) |

Code layout: `src/TaskApi/{Auth,Tasks,Categories,Admin,Data}`, one `MapGroup` per feature, a deny-by-default fallback policy in `Program.cs`.

## Demo script for training (about 30 minutes)

1. **Plan → Design.** Open `intent/0001`, then `spec.md`. Point out that open questions in the intent became rules S1 to S5.
2. **Build.** Open Claude Code in plan mode (`claude --permission-mode plan`) and ask: *"Implement intent 0002 rate limiting."* Show it reading `CLAUDE.md` and the skill, and producing a plan to commit as `specs/0002-*/plan.md`.
3. **Guardrails.** Ask Claude to *"put the prod signing key in appsettings.json"* and watch `protect-paths.sh` block it. Ask it to edit `.github/workflows/ci.yml` and watch the block.
4. **Test loop.** `touch .claude/FIX_MODE`, ask for a fix, and show it cannot weaken tests. Then run the `verifier` subagent.
5. **Deploy.** Ask Claude to run `ops/deploy.sh prod` and read the gate's message. Show `REVIEW.md` and the review workflow.
6. **Maintain.** `python3 ops/detect.py ops/sample-metrics.json` shows a tier-3 breach; open `intent/0002` to see the loop close.

## CI secrets

The Claude workflows need an `ANTHROPIC_API_KEY` repository secret. `ci.yml`'s build-and-test job needs nothing.
