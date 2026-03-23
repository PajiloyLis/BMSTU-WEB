#!/bin/bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(dirname "$SCRIPT_DIR")"
COMPOSE_FILE="$ROOT_DIR/docker-compose.e2e-external.yml"
HOSTNET_COMPOSE_FILE="$ROOT_DIR/docker-compose.e2e-external.hostnet.yml"

E2E_DB_NAME="${E2E_DB_NAME:-e2e_external}"
E2E_DB_USER="${E2E_DB_USER:-postgres}"
E2E_DB_PASSWORD="${E2E_DB_PASSWORD:-postgres}"
E2E_TEST_AUTH_ENABLED="${E2E_TEST_AUTH_ENABLED:-false}"
E2E_HOST_NETWORK="${E2E_HOST_NETWORK:-0}"
KEEP_E2E_APP_UP="${KEEP_E2E_APP_UP:-0}"
E2E_EXTERNAL_MODE="${E2E_EXTERNAL_MODE:-mock}" # mock|real
E2E_EXTERNAL_REAL_BASE_URL="${E2E_EXTERNAL_REAL_BASE_URL:-https://api.agify.io}"
E2E_EXTERNAL_TIMEOUT_SECONDS="${E2E_EXTERNAL_TIMEOUT_SECONDS:-10}"
E2E_EXTERNAL_TEST_NAME="${E2E_EXTERNAL_TEST_NAME:-ivan}"
RUN_ID="$(date +%s)-$$"
COMPOSE_PROJECT_NAME="bmstu-e2e-${RUN_ID}"

COMPOSE_ARGS=(-f "$COMPOSE_FILE")
if [ "$E2E_HOST_NETWORK" = "1" ]; then
    COMPOSE_ARGS+=(-f "$HOSTNET_COMPOSE_FILE")
    echo "[INFO] Host network mode enabled for e2e compose."
fi

cleanup() {
    if [ "$KEEP_E2E_APP_UP" = "1" ]; then
        echo "[INFO] KEEP_E2E_APP_UP=1, app container stays running."
    else
        docker compose -p "$COMPOSE_PROJECT_NAME" "${COMPOSE_ARGS[@]}" down --remove-orphans >/dev/null 2>&1 || true
    fi
}

trap cleanup EXIT INT TERM

echo "[INFO] Compose project: $COMPOSE_PROJECT_NAME"

export E2E_DB_NAME
export E2E_DB_USER
export E2E_DB_PASSWORD
export E2E_TEST_AUTH_ENABLED
export E2E_EXTERNAL_MODE="${E2E_EXTERNAL_MODE:-}"
export E2E_EXTERNAL_REAL_BASE_URL="${E2E_EXTERNAL_REAL_BASE_URL:-}"
export E2E_EXTERNAL_TEST_NAME="${E2E_EXTERNAL_TEST_NAME:-}"

SERVICES_TO_START=(test-db app-under-test)
if [ "$E2E_EXTERNAL_MODE" = "mock" ]; then
    export E2E_EXTERNAL_USE_MOCK="true"
    export E2E_EXTERNAL_EXPECT_MOCK="true"
    SERVICES_TO_START+=(external-age-mock)
elif [ "$E2E_EXTERNAL_MODE" = "real" ]; then
    export E2E_EXTERNAL_USE_MOCK="false"
    export E2E_EXTERNAL_EXPECT_MOCK="false"
else
    echo "[ERROR] E2E_EXTERNAL_MODE must be 'mock' or 'real'"
    exit 1
fi

echo "[INFO] Starting containers for mode '$E2E_EXTERNAL_MODE': ${SERVICES_TO_START[*]}"
docker compose -p "$COMPOSE_PROJECT_NAME" "${COMPOSE_ARGS[@]}" up -d --build --force-recreate "${SERVICES_TO_START[@]}"

echo "[INFO] Building e2e-tests image"
docker compose -p "$COMPOSE_PROJECT_NAME" "${COMPOSE_ARGS[@]}" build e2e-tests

mkdir -p "$ROOT_DIR/src/Tests/Project.E2E.Tests/allure-results" "$ROOT_DIR/TestResults"
rm -rf "$ROOT_DIR/src/Tests/Project.E2E.Tests/allure-results/"* "$ROOT_DIR/TestResults/"* 2>/dev/null || true

echo "[INFO] Running e2e tests in dedicated tests container"
docker compose -p "$COMPOSE_PROJECT_NAME" "${COMPOSE_ARGS[@]}" run --rm e2e-tests dotnet test src/Tests/Project.E2E.Tests/Project.E2E.Tests.csproj --configuration Release --no-build --logger "trx;LogFileName=e2e-tests.trx" --results-directory TestResults "$@" -- xUnit.ReporterSwitch=allure
