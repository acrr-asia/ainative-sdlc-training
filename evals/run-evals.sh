#!/usr/bin/env bash
# Runs each eval task non-interactively in a throwaway git worktree, then runs its acceptance check.
# Usage: ./evals/run-evals.sh [task-name ...]
set -uo pipefail
root=$(git rev-parse --show-toplevel)
cd "$root"
if [ -z "${ANTHROPIC_API_KEY:-}" ]; then
  echo "::error::ANTHROPIC_API_KEY is not set. Add it as a repository secret to run the agent evals." >&2
  exit 1
fi
tasks=("$@"); [ ${#tasks[@]} -eq 0 ] && tasks=($(ls evals/tasks))
pass=0; total=0

for t in "${tasks[@]}"; do
  total=$((total + 1))
  wt=$(mktemp -d)/eval-$t
  git worktree add --detach "$wt" HEAD >/dev/null 2>&1
  echo "::group::eval $t"
  (cd "$wt" && claude -p "$(cat "$root/evals/tasks/$t/prompt.md")" \
      --permission-mode acceptEdits --max-turns 30 >"$wt/.eval-transcript.txt" 2>&1)
  if (cd "$wt" && bash "$root/evals/tasks/$t/check.sh"); then
    echo "PASS $t"; pass=$((pass + 1))
  else
    echo "FAIL $t (transcript below)"; tail -40 "$wt/.eval-transcript.txt"
  fi
  echo "::endgroup::"
  git worktree remove --force "$wt" >/dev/null 2>&1
done

rate=$((100 * pass / total))
echo "Eval pass rate: $pass/$total ($rate%)"
[ "$rate" -ge "${EVAL_MIN_PASS_RATE:-100}" ]
