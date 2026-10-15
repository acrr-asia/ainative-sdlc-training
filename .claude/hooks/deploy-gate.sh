#!/usr/bin/env bash
# Approval gate (playbook: "Hooks as approval gates").
# Production deploys require a named release authorization. Claude is told why and where to get one.
input=$(cat)
cmd=$(jq -r '.tool_input.command // empty' <<<"$input")
if grep -qE '(^|\s)(\./)?ops/deploy\.sh\s+prod' <<<"$cmd"; then
  if [ -z "${RELEASE_AUTHORIZED_BY:-}" ]; then
    echo "BLOCKED: production deploy needs a release manager. Ask one to approve the release PR, then rerun with RELEASE_AUTHORIZED_BY=<name>." >&2
    exit 2
  fi
fi
exit 0
