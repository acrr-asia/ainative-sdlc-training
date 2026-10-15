#!/usr/bin/env bash
# Runs the formatter on the edited C# file only (fast and file-scoped, per the playbook).
input=$(cat)
file=$(jq -r '.tool_input.file_path // empty' <<<"$input")
[[ "$file" == *.cs ]] || exit 0
cd "${CLAUDE_PROJECT_DIR:-$PWD}" && dotnet format whitespace TaskApi.slnx --include "$file" >/dev/null 2>&1 || true
exit 0
