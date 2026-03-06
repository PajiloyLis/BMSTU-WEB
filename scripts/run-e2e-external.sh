#!/bin/bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(dirname "$SCRIPT_DIR")"
COMPOSE_FILE="$ROOT_DIR/docker-compose.e2e-external.yml"

E2E_DB_NAME="${E2E_DB_NAME:-e2e_external}"
E2E_DB_USER="${E2E_DB_USER:-postgres}"
E2E_DB_PASSWORD="${E2E_DB_PASSWORD:-postgres}"
E2E_TEST_AUTH_ENABLED="${E2E_TEST_AUTH_ENABLED:-false}"
KEEP_E2E_APP_UP="${KEEP_E2E_APP_UP:-0}"
RUN_ID="$(date +%s)-$$"
COMPOSE_PROJECT_NAME="bmstu-e2e-${RUN_ID}"

cleanup() {
    if [ "$KEEP_E2E_APP_UP" = "1" ]; then
        echo "[INFO] KEEP_E2E_APP_UP=1, app container stays running."
    else
        docker compose -p "$COMPOSE_PROJECT_NAME" -f "$COMPOSE_FILE" down --remove-orphans >/dev/null 2>&1 || true
    fi
}

trap cleanup EXIT INT TERM

echo "[INFO] Compose project: $COMPOSE_PROJECT_NAME"

export E2E_DB_NAME
export E2E_DB_USER
export E2E_DB_PASSWORD
export E2E_TEST_AUTH_ENABLED

echo "[INFO] Starting test-db and app-under-test containers"
docker compose -p "$COMPOSE_PROJECT_NAME" -f "$COMPOSE_FILE" up -d --build --force-recreate test-db app-under-test

echo "[INFO] Building e2e-tests image"
docker compose -p "$COMPOSE_PROJECT_NAME" -f "$COMPOSE_FILE" build e2e-tests

mkdir -p "$ROOT_DIR/src/Tests/Project.E2E.Tests/allure-results" "$ROOT_DIR/TestResults"
rm -rf "$ROOT_DIR/src/Tests/Project.E2E.Tests/allure-results/"* "$ROOT_DIR/TestResults/"* 2>/dev/null || true

echo "[INFO] Running e2e tests in dedicated tests container"
docker compose -p "$COMPOSE_PROJECT_NAME" -f "$COMPOSE_FILE" run --rm e2e-tests dotnet test src/Tests/Project.E2E.Tests/Project.E2E.Tests.csproj --configuration Release --no-build --logger "trx;LogFileName=e2e-tests.trx" --results-directory TestResults "$@" -- xUnit.ReporterSwitch=allure
