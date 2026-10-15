# Runbook: elevated 5xx or latency

1. Check `/health`. If it fails, roll back: `ops/deploy.sh prod --rollback` (needs RELEASE_AUTHORIZED_BY).
2. Read recent exceptions in the logs; find the endpoint and the commit that introduced it (`git log -S`).
3. Tier 2: write the diagnosis as `intent/NNNN-*.md`. Tier 3: open a PR with a failing test and the fix.
