---
name: api-security
description: Apply the team's API security policy. Use when writing a spec, adding or changing an endpoint, touching authentication/authorization, or reviewing a PR in this repo.
---

# API security policy

Apply every rule. When a rule does not hold, say which one and why, in the spec or PR.

1. **Deny by default.** New endpoints inherit the fallback "authenticated" policy. Anonymous access needs `.AllowAnonymous()` *and* an entry in `EndpointSecurityTests.AnonymousAllowlist`.
2. **Name the policy.** Role checks use a named policy from `Auth/Roles.cs` (`AdminOnly`), never `IsInRole` scattered in handlers, except inside a policy handler or to scope a query.
3. **Ownership is resource-based.** Load the entity, then `IAuthorizationService.AuthorizeAsync(user, entity, policy)`.
4. **Do not leak existence.** If the caller may not see a resource, return `404`.
5. **No secrets in the repo.** Signing keys and passwords come from env vars / user-secrets. Only `appsettings.Development.json` may hold dev values.
6. **Never return credentials.** Map entities to response records; no `PasswordHash` in any response.
7. **Same error for auth failures.** Login must not reveal whether the email exists.
8. **Every rule gets a test.** Add or update a test that would fail if the rule were broken.

This skill is advisory. Rules 1, 5 and 8 are also enforced by tests, the `protect-paths` hook and CI.
