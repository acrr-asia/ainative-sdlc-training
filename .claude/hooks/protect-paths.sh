#!/usr/bin/env bash
# Build-time guardrail (playbook: "Hooks as build-time guardrails").
# Blocks edits to protected paths, secrets in config, and test files while in fix mode.
# Exit 2 = block; the message on stderr is shown to Claude and should say how to proceed.
set -euo pipefail
input=$(cat)
file=$(jq -r '.tool_input.file_path // empty' <<<"$input")
content=$(jq -r '.tool_input.content // .tool_input.new_string // empty' <<<"$input")
[ -z "$file" ] && exit 0
rel=${file#"${CLAUDE_PROJECT_DIR:-$PWD}/"}

case "$rel" in
  .github/workflows/*|.claude/hooks/*|.claude/settings.json)
    echo "BLOCKED: $rel is protected. Pipeline and guardrail changes need a human-authored PR approved by CODEOWNERS." >&2
    exit 2 ;;
  intent/*)
    if [ -f "$file" ] && grep -q '^- \*\*Status:\*\* Accepted' "$file"; then
      echo "BLOCKED: $rel is an accepted intent. Write a new intent file instead of rewriting history." >&2
      exit 2
    fi ;;
esac

if [ -f "${CLAUDE_PROJECT_DIR:-$PWD}/.claude/FIX_MODE" ] && [[ "$rel" == tests/* ]]; then
  echo "BLOCKED: fix mode is on. The failing test is the target; change src/ until it passes. Remove .claude/FIX_MODE to edit tests." >&2
  exit 2
fi

if [[ "$rel" == *appsettings*.json && "$rel" != *Development* ]] && grep -qiE '"(SigningKey|Password)"\s*:\s*"[^"]+' <<<"$content"; then
  echo "BLOCKED: secrets do not go in $rel. Use environment variables (Jwt__SigningKey) or user-secrets." >&2
  exit 2
fi
exit 0
