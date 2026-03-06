#!/bin/bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(dirname "$SCRIPT_DIR")"
BENCH_DIR="$ROOT_DIR/benchmark"
COMPOSE_FILE="$BENCH_DIR/docker-compose.benchmark.yml"
HOSTNET_COMPOSE_FILE="$BENCH_DIR/docker-compose.benchmark.hostnet.yml"

RUNS="${BENCH_RUNS:-100}"
BENCH_DB_NAME="${BENCH_DB_NAME:-bench_external}"
BENCH_DB_USER="${BENCH_DB_USER:-postgres}"
BENCH_DB_PASSWORD="${BENCH_DB_PASSWORD:-postgres}"
BENCH_USER_EMAIL="${BENCH_USER_EMAIL:-fedorova@example.com}"
BENCH_USER_PASSWORD="${BENCH_USER_PASSWORD:-fedorova}"
BENCH_HOST_NETWORK="${BENCH_HOST_NETWORK:-0}"
BENCH_HOST_APP_PORT="${BENCH_HOST_APP_PORT:-58082}"

SCENARIO="${1:-all}" # all|with-index|without-index

if ! [[ "$RUNS" =~ ^[0-9]+$ ]] || [ "$RUNS" -lt 1 ]; then
  echo "[ERROR] BENCH_RUNS must be a positive integer"
  exit 1
fi

mkdir -p "$BENCH_DIR/results"

COMPOSE_ARGS=(-f "$COMPOSE_FILE")
if [ "$BENCH_HOST_NETWORK" = "1" ]; then
  COMPOSE_ARGS+=(-f "$HOSTNET_COMPOSE_FILE")
  echo "[INFO] Host network mode enabled for benchmark compose."
fi

run_profile() {
  local profile="$1"
  local index_sql="$2"
  local profile_dir="$BENCH_DIR/results/$profile"
  mkdir -p "$profile_dir"

  echo "[INFO] Profile '$profile' started, runs: $RUNS"

  for run in $(seq 1 "$RUNS"); do
    local run_dir="$profile_dir/run-$(printf "%03d" "$run")"
    local compose_project="bench-${profile}-$(date +%s)-${run}"

    mkdir -p "$run_dir"
    echo "[INFO] [$profile][$run/$RUNS] compose project: $compose_project"

    export BENCH_DB_NAME BENCH_DB_USER BENCH_DB_PASSWORD

    cleanup() {
      docker compose -p "$compose_project" "${COMPOSE_ARGS[@]}" down --remove-orphans -v >/dev/null 2>&1 || true
    }
    trap cleanup RETURN

    docker compose -p "$compose_project" "${COMPOSE_ARGS[@]}" up -d --build --force-recreate test-db app-under-test

    local db_id
    db_id="$(docker compose -p "$compose_project" "${COMPOSE_ARGS[@]}" ps -q test-db)"
    local app_id
    app_id="$(docker compose -p "$compose_project" "${COMPOSE_ARGS[@]}" ps -q app-under-test)"

    # Baseline seed + benchmark expansion + index profile.
    docker exec "$db_id" psql -U "$BENCH_DB_USER" -d "$BENCH_DB_NAME" -f /db-data/integration/create.sql >/dev/null
    docker exec "$db_id" psql -U "$BENCH_DB_USER" -d "$BENCH_DB_NAME" -f /db-data/integration/truncate.sql >/dev/null
    docker exec "$db_id" psql -U "$BENCH_DB_USER" -d "$BENCH_DB_NAME" -f /db-data/integration/copy_all.sql >/dev/null
    docker exec "$db_id" psql -U "$BENCH_DB_USER" -d "$BENCH_DB_NAME" -f /benchmark-sql/generate_large_dataset.sql >/dev/null
    docker exec "$db_id" psql -U "$BENCH_DB_USER" -d "$BENCH_DB_NAME" -f "$index_sql" >/dev/null

    # k6 run in background to sample docker stats while test is running.
    local bench_base_url="http://app-under-test:8080/api/v1"
    if [ "$BENCH_HOST_NETWORK" = "1" ]; then
      bench_base_url="http://127.0.0.1:${BENCH_HOST_APP_PORT}/api/v1"
    fi

    (
      docker compose -p "$compose_project" "${COMPOSE_ARGS[@]}" run --rm \
        -e BENCH_BASE_URL="$bench_base_url" \
        -e BENCH_USER_EMAIL="$BENCH_USER_EMAIL" \
        -e BENCH_USER_PASSWORD="$BENCH_USER_PASSWORD" \
        loadgen k6 run \
        --summary-export "/benchmark/results/$profile/run-$(printf "%03d" "$run")/k6-summary.json" \
        /benchmark/k6/scores-flow.js
    ) >"$run_dir/k6-output.log" 2>&1 &
    local k6_pid=$!

    echo "timestamp,container,cpu_perc,mem_usage,mem_perc,net_io,block_io" >"$run_dir/docker-stats.csv"
    while kill -0 "$k6_pid" 2>/dev/null; do
      local now
      now="$(date -Iseconds)"
      docker stats --no-stream --format "{{.Container}},{{.CPUPerc}},{{.MemUsage}},{{.MemPerc}},{{.NetIO}},{{.BlockIO}}" \
        "$app_id" "$db_id" | sed "s/^/$now,/" >>"$run_dir/docker-stats.csv" || true
      sleep 2
    done
    wait "$k6_pid"

    trap - RETURN
    cleanup
    echo "[INFO] [$profile][$run/$RUNS] done"
  done
}

case "$SCENARIO" in
  all)
    run_profile "without-index" "/benchmark-sql/drop_indexes.sql"
    run_profile "with-index" "/benchmark-sql/create_indexes.sql"
    ;;
  without-index)
    run_profile "without-index" "/benchmark-sql/drop_indexes.sql"
    ;;
  with-index)
    run_profile "with-index" "/benchmark-sql/create_indexes.sql"
    ;;
  *)
    echo "Usage: $0 [all|with-index|without-index]"
    exit 1
    ;;
esac

python3 "$SCRIPT_DIR/aggregate-benchmark-results.py" "$BENCH_DIR/results"
echo "[INFO] Aggregated report: $BENCH_DIR/results/summary"
