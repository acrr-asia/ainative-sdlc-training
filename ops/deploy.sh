#!/usr/bin/env bash
# Demo deploy script. The deploy-gate hook blocks `ops/deploy.sh prod` unless RELEASE_AUTHORIZED_BY is set.
# Rollback is one rehearsed command: ops/deploy.sh <env> --rollback
set -euo pipefail
env=${1:?usage: deploy.sh <dev|prod> [--rollback]}
if [ "${2:-}" = "--rollback" ]; then echo "Rolling back $env to previous release"; exit 0; fi
dotnet publish src/TaskApi -c Release -o "out/$env"
echo "Published to out/$env (authorized by: ${RELEASE_AUTHORIZED_BY:-n/a})"
