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
BENCH_WORKLOADS="${BENCH_WORKLOADS:-scores-flow}"
BENCH_APP_CPUS="${BENCH_APP_CPUS:-1.5}"
BENCH_APP_MEM_LIMIT="${BENCH_APP_MEM_LIMIT:-1536m}"
BENCH_DB_CPUS="${BENCH_DB_CPUS:-1.0}"
BENCH_DB_MEM_LIMIT="${BENCH_DB_MEM_LIMIT:-1024m}"
BENCH_LOADGEN_CPUS="${BENCH_LOADGEN_CPUS:-1.0}"
BENCH_LOADGEN_MEM_LIMIT="${BENCH_LOADGEN_MEM_LIMIT:-1024m}"
BENCH_RUNS_MODE="${BENCH_RUNS_MODE:-per-profile-total}" # per-profile-total|per-workload

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

IFS=',' read -r -a WORKLOADS <<< "$BENCH_WORKLOADS"
for workload in "${WORKLOADS[@]}"; do
  workload="${workload// /}"
  if [ -z "$workload" ]; then
    echo "[ERROR] BENCH_WORKLOADS contains empty workload name"
    exit 1
  fi
  if [ ! -f "$BENCH_DIR/k6/${workload}.js" ]; then
    echo "[ERROR] Workload script not found: $BENCH_DIR/k6/${workload}.js"
    exit 1
  fi
done

run_profile() {
  local profile="$1"
  local index_sql="$2"
  local profile_dir="$BENCH_DIR/results/$profile"
  mkdir -p "$profile_dir"

  local workload_count="${#WORKLOADS[@]}"
  echo "[INFO] Profile '$profile' started, runs: $RUNS, workloads: ${BENCH_WORKLOADS}, mode: $BENCH_RUNS_MODE"

  for idx in "${!WORKLOADS[@]}"; do
    local workload="${WORKLOADS[$idx]}"
    local workload_dir="$profile_dir/$workload"
    mkdir -p "$workload_dir"
    local runs_for_workload="$RUNS"
    if [ "$BENCH_RUNS_MODE" = "per-profile-total" ]; then
      local base_runs=$((RUNS / workload_count))
      local extra_runs=$((RUNS % workload_count))
      if [ "$idx" -lt "$extra_runs" ]; then
        runs_for_workload=$((base_runs + 1))
      else
        runs_for_workload=$base_runs
      fi
      if [ "$runs_for_workload" -lt 1 ]; then
        runs_for_workload=1
      fi
    elif [ "$BENCH_RUNS_MODE" != "per-workload" ]; then
      echo "[ERROR] BENCH_RUNS_MODE must be 'per-profile-total' or 'per-workload'"
      exit 1
    fi
    echo "[INFO] Profile '$profile', workload '$workload' started, runs: $runs_for_workload"

    local compose_project="bench-${profile}-${workload}-$(date +%s)"
    export BENCH_DB_NAME BENCH_DB_USER BENCH_DB_PASSWORD
    export BENCH_APP_CPUS BENCH_APP_MEM_LIMIT
    export BENCH_DB_CPUS BENCH_DB_MEM_LIMIT
    export BENCH_LOADGEN_CPUS BENCH_LOADGEN_MEM_LIMIT

    cleanup_workload() {
      docker compose -p "$compose_project" "${COMPOSE_ARGS[@]}" down --remove-orphans -v >/dev/null 2>&1 || true
    }
    trap cleanup_workload RETURN

    echo "[INFO] [$profile][$workload] compose project: $compose_project"
    docker compose -p "$compose_project" "${COMPOSE_ARGS[@]}" up -d --build --force-recreate test-db app-under-test

    local db_id
    db_id="$(docker compose -p "$compose_project" "${COMPOSE_ARGS[@]}" ps -q test-db)"
    local app_id
    app_id="$(docker compose -p "$compose_project" "${COMPOSE_ARGS[@]}" ps -q app-under-test)"
    local db_name
    db_name="$(docker inspect --format '{{.Name}}' "$db_id" 2>/dev/null | sed 's#^/##')"
    local app_name
    app_name="$(docker inspect --format '{{.Name}}' "$app_id" 2>/dev/null | sed 's#^/##')"

    # Baseline seed + benchmark expansion + index profile once per workload.
    docker exec "$db_id" psql -U "$BENCH_DB_USER" -d "$BENCH_DB_NAME" -f /db-data/integration/create.sql >/dev/null
    docker exec "$db_id" psql -U "$BENCH_DB_USER" -d "$BENCH_DB_NAME" -f /db-data/integration/truncate.sql >/dev/null
    docker exec "$db_id" psql -U "$BENCH_DB_USER" -d "$BENCH_DB_NAME" -f /db-data/integration/copy_all.sql >/dev/null
    docker exec "$db_id" psql -U "$BENCH_DB_USER" -d "$BENCH_DB_NAME" -f /benchmark-sql/generate_large_dataset.sql >/dev/null
    docker exec "$db_id" psql -U "$BENCH_DB_USER" -d "$BENCH_DB_NAME" -f "$index_sql" >/dev/null

    local bench_base_url="http://app-under-test:8080/api/v1"
    if [ "$BENCH_HOST_NETWORK" = "1" ]; then
      bench_base_url="http://127.0.0.1:${BENCH_HOST_APP_PORT}/api/v1"
    fi

    for run in $(seq 1 "$runs_for_workload"); do
      local run_dir="$workload_dir/run-$(printf "%03d" "$run")"
      mkdir -p "$run_dir"
      echo "[INFO] [$profile][$workload][$run/$runs_for_workload] started"

      (
        docker compose -p "$compose_project" "${COMPOSE_ARGS[@]}" run --rm --user root \
          -e BENCH_BASE_URL="$bench_base_url" \
          -e BENCH_USER_EMAIL="$BENCH_USER_EMAIL" \
          -e BENCH_USER_PASSWORD="$BENCH_USER_PASSWORD" \
          loadgen run \
          --summary-export "/benchmark/results/$profile/$workload/run-$(printf "%03d" "$run")/k6-summary.json" \
          "/benchmark/k6/${workload}.js"
      ) >"$run_dir/k6-output.log" 2>&1 &
      local k6_pid=$!

      echo "timestamp,container,cpu_perc,mem_usage,mem_perc,net_io,block_io" >"$run_dir/docker-stats.csv"
      while kill -0 "$k6_pid" 2>/dev/null; do
        local now
        now="$(date -Iseconds)"
        docker stats --no-stream --format "{{.Name}},{{.CPUPerc}},{{.MemUsage}},{{.MemPerc}},{{.NetIO}},{{.BlockIO}}" \
          "$app_name" "$db_name" | sed "s/^/$now,/" >>"$run_dir/docker-stats.csv" || true
        sleep 2
      done
      wait "$k6_pid"

      echo "[INFO] [$profile][$workload][$run/$runs_for_workload] done"
    done

    trap - RETURN
    cleanup_workload
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
