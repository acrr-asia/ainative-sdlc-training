# Stage 6: Maintain

```
metrics ──> detect.py (no model) ──tier 1──> log
                     │
                     ├──tier 2──> claude -p (read-only) ──> intent/NNNN.md ──> back to Plan
                     └──tier 3──> claude -p + runbook ────> PR through the normal review gate
```

Try it: `python3 ops/detect.py ops/sample-metrics.json` prints `login_failures_per_min ... tier=3`.
Add `--invoke` to actually call Claude. [`intent/0002-login-throttling.md`](../intent/0002-login-throttling.md)
is an example of the intent Claude writes back, and `evals/` gets a regression task for each incident.
