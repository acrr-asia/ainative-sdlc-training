# CLAUDE.md

Project memory for Claude Code. Keep it under one page. **Rule: when Claude makes the same mistake twice, add the correction under "Known mistakes".**

## Commands
- Build: `dotnet build -warnaserror`
- Test: `dotnet test` (25+ integration tests, about 5 s)
- Format: `dotnet format` (CI runs `dotnet format --verify-no-changes`)
- Run: `dotnet run --project src/TaskApi` then `http://localhost:5033/openapi/v1.json`
  (Development seeds `admin@example.com` / `Admin123!`)

## Workflow
1. Every change starts from an `intent/NNNN-*.md`, then `specs/NNNN-*/spec.md`, then `plan.md` (plan mode).
2. Bug fix: write the failing test first and commit it. Then `touch .claude/FIX_MODE` so the hook locks test files while you make it pass. Delete the file when done.
3. Before saying "done", run the `verifier` subagent.

## Architecture
- Minimal APIs, one file per feature: `src/TaskApi/<Feature>/<Feature>Endpoints.cs`, each a `MapGroup`.
- `Program.cs` sets a **fallback policy that requires authentication**. Anonymous routes must call `.AllowAnonymous()` and be added to the allowlist in `EndpointSecurityTests`.
- Policies live in `Auth/Roles.cs`. Task ownership is resource-based (`TaskOwnerOrAdminHandler`), checked after loading the entity.
- EF Core + SQLite, `EnsureCreated()` at startup, no migrations.
- Errors are `ProblemDetails` / `ValidationProblem`.

## Conventions
- Records for request/response DTOs, declared at the top of the endpoint file.
- Never return the `User` entity. Map to `UserResponse`.
- Tests: one class per feature, `IClassFixture<ApiFactory>`, unique emails via `api.NewMemberAsync()`. Name tests as sentences.
- Reference the spec rule in a comment when code exists because of it, e.g. `// Spec S4`.

## Known mistakes
- JWT claims use short names (`sub`, `email`, `role`) because `MapInboundClaims = false`. Do not use `ClaimTypes.Email` / `ClaimTypes.Role` to read them.
- Non-owners get `404`, not `403`, on `/tasks/{id}` (spec S4). Do not "fix" this.
- Comparing whole ProblemDetails bodies in tests fails because of `traceId`. Compare `Detail` instead.
