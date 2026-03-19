#!/bin/bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(dirname "$SCRIPT_DIR")"

MODE="${1:-mock}" # mock|real

if [ "$MODE" != "mock" ] && [ "$MODE" != "real" ]; then
  echo "Usage: $0 [mock|real]"
  exit 1
fi

export E2E_EXTERNAL_MODE="$MODE"
export E2E_EXTERNAL_TEST_NAME="${E2E_EXTERNAL_TEST_NAME:-ivan}"

if [ "$MODE" = "real" ]; then
  export E2E_EXTERNAL_REAL_BASE_URL="${E2E_EXTERNAL_REAL_BASE_URL:-https://api.agify.io}"
fi

echo "[INFO] Running external service E2E demo in mode: $MODE"
bash "$ROOT_DIR/scripts/run-e2e-external.sh" --filter "FullyQualifiedName~ExternalAgeServiceE2E"
