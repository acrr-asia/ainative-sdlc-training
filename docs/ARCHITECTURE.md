# Source structure and key files

This page explains how the repo is laid out and what each important file does. For every file it names the playbook practice the file demonstrates. Start with the tree, then read the sections in the order a request flows through the system.

## 1. Folder tree

```
.
├── intent/                      Stage 1 Plan: one file per idea, in the originator's words
│   ├── 0001-task-api.md         Accepted: the original task API
│   └── 0002-login-throttling.md Accepted: written by Claude from an incident (loop lap 2)
├── specs/                       Stage 2 Design + Stage 3 plan: one folder per intent
│   ├── 0001-task-api/{spec,plan}.md
│   └── 0002-login-throttling/{spec,plan}.md
├── src/TaskApi/                 The .NET 10 minimal API
│   ├── Program.cs               Composition root: services, auth, policies, routes
│   ├── Auth/                    JWT, roles/policies, ownership handler, throttle, endpoints
│   ├── Tasks/  Categories/  Admin/   One feature per folder, one MapGroup per file
│   ├── Data/                    EF Core DbContext and entities
│   └── appsettings*.json        Config (secrets only in Development)
├── tests/TaskApi.Tests/         Stage 4: integration tests over real HTTP
├── postman/                     Manual / newman end-to-end flow (40 requests)
├── evals/                       Stage 4: agent evals (prompt + acceptance check per task)
├── .claude/                     Stage 3 guardrails for Claude Code
│   ├── settings.json            Permissions + hook wiring
│   ├── hooks/                   protect-paths, format-cs, deploy-gate
│   ├── skills/api-security/     Team security policy as a skill
│   └── agents/verifier.md       Read-only verification subagent
├── .github/                     Stage 5: CI, Claude review, evals workflow, CODEOWNERS
├── ops/                         Stage 6: bands, detector, runbooks, deploy script
├── docs/                        This file and the lessons log
├── CLAUDE.md                    Project memory for Claude (read every session)
├── REVIEW.md                    Review policy for Claude and humans
└── README.md                    Practice-to-file map and demo script
```

**Rule of the layout:** process artifacts (`intent/`, `specs/`, `CLAUDE.md`, `REVIEW.md`, `.claude/`, `ops/`) live next to the code and are versioned with it. That is what makes `git log` the audit trail.

## 2. How a request flows

```
HTTP request
  → JwtBearer authentication     (Program.cs: validates issuer, audience, lifetime, key)
  → Authorization middleware     (fallback policy: must be signed in unless AllowAnonymous)
  → Route group policy           (e.g. /admin requires AdminOnly)
  → Endpoint handler             (Tasks/TaskEndpoints.cs …)
      → resource check           (IAuthorizationService + TaskOwnerOrAdminHandler)
      → AppDbContext (SQLite)
  ← Results.Ok / ProblemDetails
```

Authorization happens at three layers. Each layer is visible in one place:

| Layer | Where | Example |
|---|---|---|
| Deny by default | `Program.cs` `SetFallbackPolicy` | Any new route needs a token |
| Role / group | `.RequireAuthorization(Policies.AdminOnly)` on a route or group | `/categories` POST, all of `/admin` |
| Resource | `auth.AuthorizeAsync(user, task, Policies.TaskOwnerOrAdmin)` inside the handler | `/tasks/{id}` |

## 3. Application code (`src/TaskApi`)

### `Program.cs`, the composition root
Everything is wired here, top to bottom:
1. **Data:** `AddDbContext` reads the connection string lazily (through the service provider), so tests can override it with `UseSetting`.
2. **JWT options:** `AddOptions<JwtOptions>().BindConfiguration("Jwt").ValidateDataAnnotations().ValidateOnStart()`. If the signing key is missing or too short, the app **refuses to start** (spec S1).
3. **Services:** `TimeProvider.System` (swappable in tests), `TokenService`, `PasswordHasher<User>`, `LoginThrottle`.
4. **JwtBearer:** `MapInboundClaims = false` keeps the short claim names `sub`, `email` and `role`. `RoleClaimType = "role"` makes `IsInRole` and `RequireRole` work.
5. **Authorization:** the fallback policy (authenticated), `AdminOnly` (role) and `TaskOwnerOrAdmin` (requirement plus handler).
6. **Routes:** `/health` (anonymous), then `MapAuth`, `MapCategories`, `MapTasks` and `MapAdmin`.
7. `AdminSeeder.SeedAsync` creates the DB and the seeded admin. `public partial class Program;` lets `WebApplicationFactory<Program>` boot the app in tests.

### `Auth/`
| File | What it does | Spec / practice |
|---|---|---|
| `JwtOptions.cs` | Config class; `IValidatableObject` rejects keys under 32 bytes | S1, fail fast |
| `TokenService.cs` | Issues HS256 JWTs with `sub`, `email`, `role`, a 60-minute lifetime and the time from `TimeProvider`. `UserId()` extension reads `sub` | S2 |
| `Roles.cs` | `Roles` (`Member`, `Admin`) and `Policies` constants, so names are never typed as strings | skill rule 2 |
| `TaskOwnerOrAdminHandler.cs` | `AuthorizationHandler<Requirement, TaskItem>`: succeeds for admins or the task's owner | resource-based authZ |
| `AuthEndpoints.cs` | `register` (validates input, hashes with PBKDF2, returns `UserResponse` without the hash), `login` (throttle check → verify → record failure or reset → token), `me` | S3, S5, S6, S7 |
| `LoginThrottle.cs` | In-memory failure counter per IP: `IsLockedOut`, `RecordFailure`, `Reset`. Window and lockout come from `LoginThrottle` config | spec 0002 |
| `AdminSeeder.cs` | `EnsureCreated()` plus one admin from `SeedAdmin:*` config | bootstrap |

**Why login checks the lockout first:** if the password were verified first, a locked-out attacker could still tell when a guess was right (S6). The order in `Login` is the security property.

### Feature folders: `Tasks/`, `Categories/`, `Admin/`
Each file follows the same pattern, so a reader (or Claude) learns it once:
```csharp
public record CreateXRequest(...);          // DTOs at the top
public static class XEndpoints {
    public static RouteGroupBuilder MapX(this IEndpointRouteBuilder app) {
        var group = app.MapGroup("/x").WithTags("X");   // policy sits next to the route
        group.MapGet("/", List); ...
    }
    private static async Task<IResult> List(...) { ... }  // handlers are private static
}
```
- `TaskEndpoints.cs`: `FindAuthorized()` loads the task and runs the resource policy. It returns `null` both for "missing" and "not yours", so both become `404` (S4). The list query scopes to `OwnerId` unless the caller is an admin.
- `CategoryEndpoints.cs`: anyone signed in can read; `POST` and `DELETE` carry `.RequireAuthorization(Policies.AdminOnly)`.
- `AdminEndpoints.cs`: the policy is on the **group**, so a new admin route cannot forget it.

### `Data/`
`Entities.cs` holds `User`, `Category` and `TaskItem`. `AppDbContext.cs` holds the unique indexes (email, category name), the max lengths, and `OnDelete(SetNull)` so deleting a category doesn't delete tasks. The schema comes from `EnsureCreated()`; there are no migrations in this demo.

### Config
`appsettings.json` holds non-secret defaults (issuer, audience, throttle limits). `appsettings.Development.json` holds the dev signing key and seeded admin. In production, set `Jwt__SigningKey` as an environment variable. The `protect-paths` hook blocks secrets in the non-Development files.

## 4. Tests (`tests/TaskApi.Tests`)
| File | Covers |
|---|---|
| `ApiFactory.cs` | Boots the real app with a temp SQLite file, test key and seeded admin. Helpers `AdminAsync()` and `NewMemberAsync()` return signed-in `HttpClient`s |
| `AuthTests.cs` | Register, duplicate, validation, login, S3 (same 401), S2 (expired and forged tokens) |
| `StartupTests.cs` | S1: weak key throws `OptionsValidationException` on boot |
| `CategoryTests.cs` / `AdminTests.cs` | Role policies, 403s, S5 |
| `TaskTests.cs` | Ownership, S4 (404 for GET/PUT/DELETE), admin sees all, validation |
| `EndpointSecurityTests.cs` | **Guardrail:** enumerates every endpoint; only `/health`, register and login may be anonymous |
| `LoginThrottleTests.cs` | Spec 0002 with `ThrottleFactory`: `ManualTimeProvider` moves time, `X-Test-Ip` sets the caller IP |

Convention: one class per feature, tests named as sentences, and a `// spec Sx` comment where a test proves a rule. The `verifier` subagent uses those comments to report coverage.

## 5. Claude guardrails (`.claude/`, `CLAUDE.md`, `REVIEW.md`)
| File | Kind | Effect |
|---|---|---|
| `CLAUDE.md` | Memory (advisory) | Commands, workflow, architecture, conventions, **Known mistakes** |
| `.claude/skills/api-security/SKILL.md` | Skill (advisory) | 8 security rules Claude applies when touching endpoints or specs |
| `.claude/settings.json` | Config (enforced) | allow `dotnet build/test/format/run`; ask `git push`, `dotnet add package`; deny `curl`, `.env`, force push; wires the hooks |
| `.claude/hooks/protect-paths.sh` | PreToolUse hook (enforced) | Blocks edits to workflows/hooks/settings, accepted intents, secrets in config, and `tests/` while `.claude/FIX_MODE` exists |
| `.claude/hooks/format-cs.sh` | PostToolUse hook | `dotnet format whitespace` on the one edited file |
| `.claude/hooks/deploy-gate.sh` | PreToolUse hook (approval gate) | `ops/deploy.sh prod` needs `RELEASE_AUTHORIZED_BY` |
| `.claude/agents/verifier.md` | Subagent | Read-only, fresh context; build, test, format, then a coverage table and `VERDICT: READY` or `NOT READY` |
| `REVIEW.md` | Review policy | 4 passes (spec/plan, bugs, security, tests), severities, at most 3 nits |

The split to teach: **advisory** (CLAUDE.md, skill) shapes what Claude tries; **enforced** (hooks, tests, CI, CODEOWNERS) decides what can land.

## 6. CI, evals and ops
- `.github/workflows/ci.yml`: restore, build with `-warnaserror`, `format --verify-no-changes`, test, upload TRX. A read-only `triage` job comments on failures.
- `.github/workflows/claude-review.yml`: Claude reviews each PR against REVIEW.md (needs `id-token: write` and `ANTHROPIC_API_KEY`).
- `.github/workflows/agent-evals.yml` + `evals/run-evals.sh`: each `evals/tasks/<name>/` runs `claude -p prompt.md` in a throwaway worktree, then `check.sh`. Four tasks today, including the regression eval from incident 0002.
- `ops/bands.yaml` + `ops/detect.py`: deterministic sigma bands decide the tier; only tier 2 or higher calls Claude, with tools scoped by tier.
- `ops/runbooks/*.md`, `docs/lessons.md`: what Claude may do in an incident, and what we learned.

## 7. Where to add things
| You want to… | Do this |
|---|---|
| Add a feature | New `intent/NNNN`, then `specs/NNNN/{spec,plan}.md`, then `src/TaskApi/<Feature>/<Feature>Endpoints.cs`, then `Map<Feature>()` in `Program.cs`, then tests |
| Add an anonymous route | `.AllowAnonymous()` **and** add it to `EndpointSecurityTests.AnonymousAllowlist` |
| Fix a bug | Failing test, commit, `touch .claude/FIX_MODE`, fix, `rm .claude/FIX_MODE` |
| Teach Claude something | A repeated mistake goes in `CLAUDE.md` Known mistakes. A policy goes in a skill, plus a hook or test if it must hold |
| After an incident | New intent, a line in `docs/lessons.md`, and an eval in `evals/tasks/` |
