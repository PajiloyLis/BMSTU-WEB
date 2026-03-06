# Benchmark: indexes vs no-indexes

This benchmark compares two DB profiles for your API:

- `without-index` - target indexes dropped
- `with-index` - target indexes created

Both profiles run the same workload (`scores-flow`) against the same seeded dataset.

## What is measured

- API latency (`med`, `p95`, `p99`) from k6 summary
- request rate and failed request rate
- resource usage snapshots for app/db containers (`CPU%`, `RAM MiB`)

## Dataset strategy

- Baseline scripts are reused:
  - `DB_data_scripts/integration/create.sql`
  - `DB_data_scripts/integration/truncate.sql`
  - `DB_data_scripts/integration/copy_all.sql`
- Dataset is then expanded with:
  - `benchmark/sql/generate_large_dataset.sql` (`+100000` rows in `score_story`)

## Run

From repository root:

```bash
BENCH_RUNS=100 bash ./scripts/run-benchmark-indexes.sh all
```

Quick smoke test:

```bash
BENCH_RUNS=2 bash ./scripts/run-benchmark-indexes.sh all
```

Host network mode (Linux, useful for bridge/DNS issues):

```bash
BENCH_HOST_NETWORK=1 BENCH_RUNS=2 bash ./scripts/run-benchmark-indexes.sh all
```

Optional host ports in host-network mode:

```bash
BENCH_HOST_NETWORK=1 BENCH_HOST_APP_PORT=58082 BENCH_HOST_DB_PORT=55434 BENCH_RUNS=2 bash ./scripts/run-benchmark-indexes.sh all
```

Single profile:

```bash
BENCH_RUNS=20 bash ./scripts/run-benchmark-indexes.sh with-index
BENCH_RUNS=20 bash ./scripts/run-benchmark-indexes.sh without-index
```

## Output

- Raw run artifacts:
  - `benchmark/results/with-index/run-*/`
  - `benchmark/results/without-index/run-*/`
- Aggregated report:
  - `benchmark/results/summary/summary.json`
  - `benchmark/results/summary/summary.csv`

## Notes

- In `BENCH_HOST_NETWORK=1` mode, avoid parallel benchmark runs with the same host ports.
