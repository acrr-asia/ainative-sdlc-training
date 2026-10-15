# Intent 0002: Throttle repeated failed logins

> Example of the loop closing (Stage 6 back to Stage 1). Written by Claude after `ops/detect.py` reported
> `login_failures_per_min` at tier 3, following `ops/runbooks/login-spike.md`. Triaged by the product owner.

- **Author:** Claude (from incident), triaged by: San
- **Status:** Accepted
- **Next artifact:** [`specs/0002-login-throttling/spec.md`](../specs/0002-login-throttling/spec.md)

## Problem
Failed logins reached 11/min against a baseline of 4 ± 2. 70% came from one IP trying many emails.
Spec 0001 put rate limiting out of scope, so nothing slows this down.

## Desired outcome
After 5 failed logins from one IP in a minute, further attempts get `429` for 1 minute.

## Constraints
- Must keep spec S3: the response must not reveal whether an email exists.
- Use the built-in ASP.NET Core rate limiter; no new infrastructure.

## Open questions (resolved in spec)
1. Per IP, per email, or both? → **Per client IP** (per-email would let an attacker lock out a real user).
2. Should admins be exempt? → **No.** The caller is unknown until login succeeds.
