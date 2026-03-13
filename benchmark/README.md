# Бенчмарк: индексы vs без индексов

Этот набор бенчмарков сравнивает два профиля БД для вашего API:

- `without-index` — целевые индексы удалены
- `with-index` — целевые индексы созданы

Оба профиля запускают одинаковый набор сценариев на одной и той же стартовой выборке данных.

Доступные сценарии нагрузки:

- `scores-flow` — базовый смешанный сценарий.
- `scores-degradation` — ступенчатое повышение нагрузки для поиска точки деградации.
- `scores-max-load` — длительная работа на высокой нагрузке.
- `scores-recovery` — перегрузка и восстановление.

## Что измеряется

- задержка API (`med`, `p95`, `p99`) из summary k6;
- скорость запросов и доля ошибочных запросов;
- срезы утилизации ресурсов для контейнеров app/db (`CPU%`, `RAM MiB`, `Net I/O MiB`, `Block I/O MiB`).

## Стратегия данных

- Переиспользуются базовые скрипты:
  - `DB_data_scripts/integration/create.sql`
  - `DB_data_scripts/integration/truncate.sql`
  - `DB_data_scripts/integration/copy_all.sql`
- Набор данных расширяется скриптом:
  - `benchmark/sql/generate_large_dataset.sql` (`+100000` строк в `score_story`)

## Запуск

Из корня репозитория:

```bash
BENCH_RUNS=100 bash ./scripts/run-benchmark-indexes.sh all
```

Запуск всех сценариев явно:

```bash
BENCH_WORKLOADS=scores-flow,scores-degradation,scores-max-load,scores-recovery \
BENCH_RUNS=100 \
bash ./scripts/run-benchmark-indexes.sh all
```

Быстрый smoke-тест:

```bash
BENCH_RUNS=2 bash ./scripts/run-benchmark-indexes.sh all
```

Режим host network (Linux, полезно при проблемах bridge/DNS):

```bash
BENCH_HOST_NETWORK=1 BENCH_RUNS=2 bash ./scripts/run-benchmark-indexes.sh all
```

Фиксация лимитов ресурсов для воспроизводимости:

```bash
BENCH_APP_CPUS=1.5 BENCH_APP_MEM_LIMIT=1536m \
BENCH_DB_CPUS=1.0 BENCH_DB_MEM_LIMIT=1024m \
BENCH_LOADGEN_CPUS=1.0 BENCH_LOADGEN_MEM_LIMIT=1024m \
BENCH_RUNS=20 bash ./scripts/run-benchmark-indexes.sh all
```

Опциональные host-порты в host-network режиме:

```bash
BENCH_HOST_NETWORK=1 BENCH_HOST_APP_PORT=58082 BENCH_HOST_DB_PORT=55434 BENCH_RUNS=2 bash ./scripts/run-benchmark-indexes.sh all
```

Запуск только одного профиля:

```bash
BENCH_RUNS=20 bash ./scripts/run-benchmark-indexes.sh with-index
BENCH_RUNS=20 bash ./scripts/run-benchmark-indexes.sh without-index
```

## Результаты

- Сырые артефакты прогонов:
  - `benchmark/results/with-index/<workload>/run-*/`
  - `benchmark/results/without-index/<workload>/run-*/`
- Агрегированные отчеты:
  - `benchmark/results/summary/summary.json`
  - `benchmark/results/summary/summary.csv`
  - `benchmark/results/summary/report.md`
  - `benchmark/results/summary/plots/*.png` (если установлен `matplotlib`; установка: `python3 -m pip install matplotlib`)

## Примечания

- В режиме `BENCH_HOST_NETWORK=1` избегайте параллельных запусков с одинаковыми host-портами.
- По умолчанию запускается только `scores-flow`; для нескольких сценариев используйте `BENCH_WORKLOADS`.
