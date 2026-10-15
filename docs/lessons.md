# Lessons learned

Version-controlled post-mortems. One line per lesson: what happened, what we changed, where.

| Date | Lesson | Change |
|---|---|---|
| 2026-10-15 | Claude read JWT roles with `ClaimTypes.Role` twice; they are short names because `MapInboundClaims = false` | Added to `CLAUDE.md` Known mistakes |
| 2026-10-15 | A "fix" to return 403 for other users' tasks would leak which IDs exist | Spec S4, test `Other_member_gets_404_not_403...`, eval `keeps-404-for-non-owners` |
| 2026-10-15 | Login failure spike (11/min, tier 3) had no defence because spec 0001 left rate limiting out of scope | Intent 0002, spec 0002 (S6-S9), `LoginThrottle`, eval `throttle-keeps-s3` |
