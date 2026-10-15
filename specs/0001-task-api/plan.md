# Plan 0001: Implementation plan

> Stage 3 (Build) artifact. Produced in Claude Code **plan mode** (read-only), iterated with the
> engineer until it was implementable from this document alone, then committed before any code.
> Reviewers compare the diff against this plan.

## Steps

1. **Data** (`src/TaskApi/Data`): `AppDbContext` with `User`, `TaskItem`, `Category`.
   Unique indexes on `User.Email` and `Category.Name`. Database created with `EnsureCreated()` (no migrations in this demo).
2. **Auth** (`src/TaskApi/Auth`):
   - `JwtOptions` bound from `Jwt` section, validated at startup (spec S1).
   - `TokenService` issues tokens with `sub`, `email`, `role` claims (S2).
   - `Roles` and `Policies` constants. `TaskOwnerOrAdminHandler` for the resource policy.
   - `AuthEndpoints`: register, login, me.
   - `AdminSeeder` creates the admin from `SeedAdmin:Email` / `SeedAdmin:Password` if set.
3. **Features**: `Categories/CategoryEndpoints.cs`, `Tasks/TaskEndpoints.cs`, `Admin/AdminEndpoints.cs`.
   Each file is one `MapGroup` with its policy, so the authorization rule is visible next to the route.
4. **Program.cs**: wire services, set `AuthenticatedUser` as the fallback policy, map groups.
5. **Tests** (`tests/TaskApi.Tests`): `ApiFactory` (`WebApplicationFactory` with a temp SQLite file,
   test signing key, seeded admin). One test class per feature. Write failing tests first for each endpoint.
6. **Guardrails**: `CLAUDE.md`, `.claude/settings.json` hooks, `api-security` skill, `verifier` subagent, CI.

## Risks

- Leaking task existence through `403`. Mitigated by S4 tests.
- Forgetting to protect a new endpoint. Mitigated by the fallback policy plus a test that enumerates every endpoint.

## Done when

`dotnet build -warnaserror` and `dotnet test` pass, `dotnet format --verify-no-changes` is clean,
and the verifier subagent reports every spec row covered.
