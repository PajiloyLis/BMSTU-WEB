#!/bin/bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(dirname "$SCRIPT_DIR")"
COMPOSE_FILE="$ROOT_DIR/docker-compose.integration-external.yml"
HOSTNET_COMPOSE_FILE="$ROOT_DIR/docker-compose.integration-external.hostnet.yml"

INTEGRATION_DB_NAME="${INTEGRATION_DB_NAME:-integration_external}"
INTEGRATION_DB_USER="${INTEGRATION_DB_USER:-postgres}"
INTEGRATION_DB_PASSWORD="${INTEGRATION_DB_PASSWORD:-postgres}"
INTEGRATION_TEST_AUTH_ENABLED="${INTEGRATION_TEST_AUTH_ENABLED:-true}"
INTEGRATION_HOST_NETWORK="${INTEGRATION_HOST_NETWORK:-0}"
COMPOSE_PROJECT_NAME="${INTEGRATION_COMPOSE_PROJECT_NAME:-bmstu-integration-external}"
RUN_ID="$(date +%s)-$$"
INTEGRATION_TESTS_CONTAINER_NAME="integration-tests-${RUN_ID}"
RUN_ALLURE_RESULTS_DIR="$ROOT_DIR/src/Tests/Project.Integration.Tests/allure-results/${RUN_ID}"
RUN_TEST_RESULTS_DIR="$ROOT_DIR/TestResults/${RUN_ID}"
RUN_ALLURE_CONFIG_FILE="$ROOT_DIR/TestResults/${RUN_ID}/allureConfig.${RUN_ID}.json"

COMPOSE_ARGS=(-f "$COMPOSE_FILE")
if [ "$INTEGRATION_HOST_NETWORK" = "1" ]; then
    COMPOSE_ARGS+=(-f "$HOSTNET_COMPOSE_FILE")
    COMPOSE_PROJECT_NAME="${INTEGRATION_COMPOSE_PROJECT_NAME:-bmstu-integration-external-hostnet}"
    echo "[INFO] Host network mode enabled for integration compose."
fi

echo "[INFO] Compose project: $COMPOSE_PROJECT_NAME"

export INTEGRATION_DB_NAME
export INTEGRATION_DB_USER
export INTEGRATION_DB_PASSWORD
export INTEGRATION_TEST_AUTH_ENABLED

echo "[INFO] Ensuring test-db and app-under-test containers are available"
is_service_running() {
    local service="$1"
    local container_id
    container_id="$(docker compose -p "$COMPOSE_PROJECT_NAME" "${COMPOSE_ARGS[@]}" ps -q "$service" 2>/dev/null || true)"
    if [ -z "$container_id" ]; then
        return 1
    fi

    [ "$(docker inspect -f '{{.State.Running}}' "$container_id" 2>/dev/null || true)" = "true" ]
}

if is_service_running test-db && is_service_running app-under-test; then
    echo "[INFO] test-db and app-under-test are already running. Reusing existing containers."
else
    echo "[INFO] Starting (or updating) test-db and app-under-test containers."
    docker compose -p "$COMPOSE_PROJECT_NAME" "${COMPOSE_ARGS[@]}" up -d --build test-db app-under-test
fi

echo "[INFO] Building integration-tests image"
docker compose -p "$COMPOSE_PROJECT_NAME" "${COMPOSE_ARGS[@]}" build integration-tests

mkdir -p "$RUN_ALLURE_RESULTS_DIR" "$RUN_TEST_RESULTS_DIR"

cat > "$RUN_ALLURE_CONFIG_FILE" <<EOF
{
  "allure": {
    "directory": "/workspace/src/Tests/Project.Integration.Tests/allure-results",
    "links": [
      "https://github.com/allure-framework/allure-csharp/{link}"
    ]
  }
}
EOF

echo "[INFO] Running integration tests in dedicated tests container"
docker compose -p "$COMPOSE_PROJECT_NAME" "${COMPOSE_ARGS[@]}" run --rm --name "$INTEGRATION_TESTS_CONTAINER_NAME" \
    -e ALLURE_CONFIG="/workspace/TestResults/allureConfig.${RUN_ID}.json" \
    -v "$RUN_ALLURE_RESULTS_DIR:/workspace/src/Tests/Project.Integration.Tests/allure-results" \
    -v "$RUN_TEST_RESULTS_DIR:/workspace/TestResults" \
    integration-tests dotnet test src/Tests/Project.Integration.Tests/Project.Integration.Tests.csproj --configuration Release --no-build --logger "trx;LogFileName=integration-tests-${RUN_ID}.trx" --results-directory TestResults "$@" -- xUnit.ReporterSwitch=allure
