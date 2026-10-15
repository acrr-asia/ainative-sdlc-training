# Runbook: login failure spike

Pre-approved for tier 3. Claude may run steps 1 to 3 without asking.

1. Read the last hour of logs for `POST /auth/login` 401s; group by client IP and by email.
2. If one IP is > 50% of failures: it is a credential-stuffing attempt. Record it in the incident thread.
3. Write `intent/NNNN-*.md` describing the attack and the proposed mitigation. Do **not** change code.
4. Human step: the on-call engineer decides whether to block the IP at the gateway.

After the incident: add an eval in `evals/tasks/` and a line in `docs/lessons.md`.
