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
E2E_AUTH_SECURITY_ENABLED="${E2E_AUTH_SECURITY_ENABLED:-true}"
E2E_AUTH_REQUIRE_2FA="${E2E_AUTH_REQUIRE_2FA:-true}"
E2E_AUTH_EXPOSE_CODES_FOR_TESTS="${E2E_AUTH_EXPOSE_CODES_FOR_TESTS:-true}"
E2E_AUTH_MAX_FAILED_ATTEMPTS="${E2E_AUTH_MAX_FAILED_ATTEMPTS:-5}"
E2E_AUTH_LOCKOUT_MINUTES="${E2E_AUTH_LOCKOUT_MINUTES:-15}"
E2E_AUTH_OTP_LIFETIME_MINUTES="${E2E_AUTH_OTP_LIFETIME_MINUTES:-5}"
E2E_AUTH_RECOVERY_TOKEN_LIFETIME_MINUTES="${E2E_AUTH_RECOVERY_TOKEN_LIFETIME_MINUTES:-30}"
E2E_AUTH_PASSWORD_MAX_AGE_DAYS="${E2E_AUTH_PASSWORD_MAX_AGE_DAYS:-90}"
RUN_ID="$(date +%s)-$$"
COMPOSE_PROJECT_NAME="bmstu-bdd-e2e-${RUN_ID}"

COMPOSE_ARGS=(-f "$COMPOSE_FILE")
if [ "$E2E_HOST_NETWORK" = "1" ]; then
    COMPOSE_ARGS+=(-f "$HOSTNET_COMPOSE_FILE")
    echo "[INFO] Host network mode enabled for bdd e2e compose."
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
export E2E_AUTH_SECURITY_ENABLED
export E2E_AUTH_REQUIRE_2FA
export E2E_AUTH_EXPOSE_CODES_FOR_TESTS
export E2E_AUTH_MAX_FAILED_ATTEMPTS
export E2E_AUTH_LOCKOUT_MINUTES
export E2E_AUTH_OTP_LIFETIME_MINUTES
export E2E_AUTH_RECOVERY_TOKEN_LIFETIME_MINUTES
export E2E_AUTH_PASSWORD_MAX_AGE_DAYS

echo "[INFO] Starting test-db and app-under-test containers"
docker compose -p "$COMPOSE_PROJECT_NAME" "${COMPOSE_ARGS[@]}" up -d --build --force-recreate test-db app-under-test

echo "[INFO] Building e2e-tests image"
docker compose -p "$COMPOSE_PROJECT_NAME" "${COMPOSE_ARGS[@]}" build e2e-tests

mkdir -p "$ROOT_DIR/src/Tests/Project.BDD.E2E.Tests/allure-results" "$ROOT_DIR/TestResults"
rm -rf "$ROOT_DIR/src/Tests/Project.BDD.E2E.Tests/allure-results/"* "$ROOT_DIR/TestResults/"* 2>/dev/null || true

echo "[INFO] Running bdd e2e tests in dedicated tests container"
docker compose -p "$COMPOSE_PROJECT_NAME" "${COMPOSE_ARGS[@]}" run --rm e2e-tests dotnet test src/Tests/Project.BDD.E2E.Tests/Project.BDD.E2E.Tests.csproj --configuration Release --no-build --logger "trx;LogFileName=bdd-e2e-tests.trx" --results-directory TestResults "$@" -- xUnit.ReporterSwitch=allure
