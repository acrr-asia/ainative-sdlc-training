# Intent 0001: A small, secure task API

> Stage 1 (Plan) artifact. Written in the originator's own words, then reviewed and
> committed by the product owner. Accepting this file (merging it) is the gate into Design.

- **Author:** San (product owner)
- **Status:** Accepted
- **Next artifact:** [`specs/0001-task-api/spec.md`](../specs/0001-task-api/spec.md)

## Problem

Our training team needs a tiny but real backend to show how an AI-native SDLC works end to end.
Toy samples skip the parts that matter in real work: who is allowed to do what, and how we prove it.

## Desired outcome

A .NET 10 Web API where people can sign up, sign in, and keep a personal task list.
Admins can manage shared categories and see everyone's tasks.

## Affected users

| Who | What they need |
|---|---|
| Member | Register, log in, create and complete their own tasks |
| Admin | Everything a member can do, plus manage categories, list users, promote users |
| Trainer | A codebase where every playbook stage is visible in a file |

## Constraints

- .NET 10, minimal APIs, EF Core with SQLite (no external services to install).
- Stateless JWT bearer tokens. No secrets in the repo outside Development settings.
- A member must never see or change another member's task.
- Must run with `dotnet test` on a clean machine.

## Open questions (resolved in spec)

1. Should a member get `403` or `404` when touching someone else's task? → **404**, so IDs do not leak.
2. How is the first admin created? → Seeded from configuration at startup.
3. Refresh tokens? → **Out of scope** for this demo.
