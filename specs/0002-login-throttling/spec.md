# Spec 0002: Login throttling

> Stage 2 (Design) artifact, from [`intent/0002-login-throttling.md`](../../intent/0002-login-throttling.md),
> checked against the `api-security` skill. Extends [spec 0001](../0001-task-api/spec.md); rules S1 to S5 still hold.

## 1. Behaviour

| Situation | Response |
|---|---|
| Fewer than `MaxFailures` failed logins from this IP in the last `WindowSeconds` | Normal login (`200` or `401`) |
| `MaxFailures` failures reached | Next attempts get `429` with a `Retry-After` header, for `LockoutSeconds` |
| Lockout over | Counter reset, logins allowed again |
| Successful login | Clears this IP's failure count |

Defaults (config section `LoginThrottle`): `MaxFailures` 5, `WindowSeconds` 60, `LockoutSeconds` 60.

## 2. Security requirements

- S6. A locked-out IP gets `429` even with the correct password (otherwise the lockout leaks which guess was right).
- S7. The `429` body is the same whether or not the email exists (keeps S3).
- S8. Only failed logins count. Registering, `/auth/me` and other routes are never throttled.
- S9. One IP's lockout does not affect another IP.

## 3. Out of scope

Distributed counters (state is in-memory per instance), CAPTCHA, per-email lockout.

## 4. Acceptance

Every rule S6 to S9 and every row in section 1 has a test in `tests/TaskApi.Tests/LoginThrottleTests.cs`.
