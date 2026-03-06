#!/bin/bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(dirname "$SCRIPT_DIR")"
COMPOSE_FILE="$ROOT_DIR/docker-compose.integration-external.yml"

INTEGRATION_DB_NAME="${INTEGRATION_DB_NAME:-integration_external}"
INTEGRATION_DB_USER="${INTEGRATION_DB_USER:-postgres}"
INTEGRATION_DB_PASSWORD="${INTEGRATION_DB_PASSWORD:-postgres}"
INTEGRATION_TEST_AUTH_ENABLED="${INTEGRATION_TEST_AUTH_ENABLED:-true}"
KEEP_INTEGRATION_APP_UP="${KEEP_INTEGRATION_APP_UP:-0}"
RUN_ID="$(date +%s)-$$"
COMPOSE_PROJECT_NAME="bmstu-integration-${RUN_ID}"

cleanup() {
    if [ "$KEEP_INTEGRATION_APP_UP" = "1" ]; then
        echo "[INFO] KEEP_INTEGRATION_APP_UP=1, app container stays running."
    else
        docker compose -p "$COMPOSE_PROJECT_NAME" -f "$COMPOSE_FILE" down --remove-orphans >/dev/null 2>&1 || true
    fi
}

trap cleanup EXIT INT TERM

echo "[INFO] Compose project: $COMPOSE_PROJECT_NAME"

export INTEGRATION_DB_NAME
export INTEGRATION_DB_USER
export INTEGRATION_DB_PASSWORD
export INTEGRATION_TEST_AUTH_ENABLED

echo "[INFO] Starting test-db and app-under-test containers"
docker compose -p "$COMPOSE_PROJECT_NAME" -f "$COMPOSE_FILE" up -d --build --force-recreate test-db app-under-test

echo "[INFO] Building integration-tests image"
docker compose -p "$COMPOSE_PROJECT_NAME" -f "$COMPOSE_FILE" build integration-tests

mkdir -p "$ROOT_DIR/src/Tests/Project.Integration.Tests/allure-results" "$ROOT_DIR/TestResults"
rm -rf "$ROOT_DIR/src/Tests/Project.Integration.Tests/allure-results/"* "$ROOT_DIR/TestResults/"* 2>/dev/null || true

echo "[INFO] Running integration tests in dedicated tests container"
docker compose -p "$COMPOSE_PROJECT_NAME" -f "$COMPOSE_FILE" run --rm integration-tests dotnet test src/Tests/Project.Integration.Tests/Project.Integration.Tests.csproj --configuration Release --no-build --logger "trx;LogFileName=integration-tests.trx" --results-directory TestResults "$@" -- xUnit.ReporterSwitch=allure
