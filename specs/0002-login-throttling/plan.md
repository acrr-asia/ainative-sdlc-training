# Plan 0002: Implementation plan

> Stage 3 (Build) artifact from plan mode. Reviewers compare the diff against this file.

1. `src/TaskApi/Auth/LoginThrottle.cs`: `LoginThrottleOptions` (bound from `LoginThrottle`) and a singleton
   `LoginThrottle` keyed by client IP, using `TimeProvider` so tests can move time. Methods:
   `IsLockedOut(ip, out retryAfter)`, `RecordFailure(ip)`, `Reset(ip)`. A `ConcurrentDictionary` is enough (spec: in-memory).
2. `AuthEndpoints.Login`: check the lockout **before** verifying the password (S6), record failure on `401`,
   reset on success. Return `Results.Problem(..., 429)` and set `Retry-After`.
3. `Program.cs`: register options and the singleton.
4. Tests, written first: `LoginThrottleTests` with its own factory and a `ManualTimeProvider` so the lockout window
   can expire without sleeping. One test per row/rule.
5. Guardrails: eval `throttle-keeps-s3` (asked to "tell users their email is wrong", must refuse), a lessons line,
   and a `CLAUDE.md` note that throttle state is per instance.

Done when: build with no warnings, all tests green, format clean.
