#!/usr/bin/env python3

from __future__ import annotations

import csv
import json
import re
import statistics
import sys
from dataclasses import dataclass
from pathlib import Path


UNIT_TO_MIB = {
    "b": 1.0 / (1024 * 1024),
    "kb": 1.0 / 1024,
    "kib": 1.0 / 1024,
    "mb": 1.0,
    "mib": 1.0,
    "gb": 1024.0,
    "gib": 1024.0,
}
PLOTS_DISABLED_WARNED = False


@dataclass
class RunMetrics:
    med: float
    p95: float
    p99: float
    req_failed_rate: float
    req_rate: float


def safe_mean(values: list[float]) -> float:
    return statistics.mean(values) if values else 0.0


def safe_median(values: list[float]) -> float:
    return statistics.median(values) if values else 0.0


def parse_percent(value: str) -> float:
    cleaned = value.replace("%", "").strip()
    return float(cleaned) if cleaned else 0.0


def parse_number_unit(raw: str) -> tuple[float, str]:
    match = re.search(r"([0-9]+(?:\.[0-9]+)?)\s*([A-Za-z]+)", raw)
    if not match:
        return 0.0, "mib"
    number = float(match.group(1))
    unit = match.group(2).lower()
    return number, unit


def to_mib(number: float, unit: str) -> float:
    return number * UNIT_TO_MIB.get(unit, 1.0)


def parse_first_part_to_mib(value: str) -> float:
    part = value.split("/")[0].strip()
    number, unit = parse_number_unit(part)
    return to_mib(number, unit)


def parse_total_pair_to_mib(value: str) -> float:
    parts = [p.strip() for p in value.split("/")]
    total = 0.0
    for part in parts[:2]:
        number, unit = parse_number_unit(part)
        total += to_mib(number, unit)
    return total


def metric_summary(values: list[float]) -> dict:
    if not values:
        return {"min": 0.0, "max": 0.0, "median": 0.0, "mean": 0.0}
    return {
        "min": min(values),
        "max": max(values),
        "median": safe_median(values),
        "mean": safe_mean(values),
    }


def parse_run_summary(summary_file: Path) -> RunMetrics:
    data = json.loads(summary_file.read_text(encoding="utf-8"))
    metrics = data.get("metrics", {})
    req_dur_raw = metrics.get("http_req_duration", {})
    req_fail_raw = metrics.get("http_req_failed", {})
    reqs_raw = metrics.get("http_reqs", {})

    req_dur = req_dur_raw.get("values", req_dur_raw)
    req_fail = req_fail_raw.get("values", req_fail_raw)
    reqs = reqs_raw.get("values", reqs_raw)
    failed_rate = req_fail.get("rate", req_fail.get("value", 0))
    return RunMetrics(
        med=float(req_dur.get("med", 0)),
        p95=float(req_dur.get("p(95)", 0)),
        p99=float(req_dur.get("p(99)", 0)),
        req_failed_rate=float(failed_rate),
        req_rate=float(reqs.get("rate", 0)),
    )


def collect_run_dirs(scenario_dir: Path) -> list[Path]:
    return sorted([p for p in scenario_dir.iterdir() if p.is_dir() and p.name.startswith("run-")], key=lambda p: p.name)


def summarize_scenario(scenario_dir: Path) -> tuple[dict, list[RunMetrics]]:
    run_dirs = collect_run_dirs(scenario_dir)

    runs_metrics: list[RunMetrics] = []
    app_cpu: list[float] = []
    app_mem: list[float] = []
    app_net: list[float] = []
    app_block: list[float] = []
    db_cpu: list[float] = []
    db_mem: list[float] = []
    db_net: list[float] = []
    db_block: list[float] = []

    for run in run_dirs:
        summary_file = run / "k6-summary.json"
        if summary_file.exists():
            runs_metrics.append(parse_run_summary(summary_file))

        stats_file = run / "docker-stats.csv"
        if stats_file.exists():
            with stats_file.open("r", encoding="utf-8") as fh:
                reader = csv.DictReader(fh)
                for row in reader:
                    container = row["container"]
                    cpu = parse_percent(row["cpu_perc"])
                    mem = parse_first_part_to_mib(row["mem_usage"])
                    net = parse_total_pair_to_mib(row["net_io"])
                    block = parse_total_pair_to_mib(row["block_io"])

                    if "app-under-test" in container:
                        app_cpu.append(cpu)
                        app_mem.append(mem)
                        app_net.append(net)
                        app_block.append(block)
                    elif "test-db" in container:
                        db_cpu.append(cpu)
                        db_mem.append(mem)
                        db_net.append(net)
                        db_block.append(block)

    med_values = [m.med for m in runs_metrics]
    p95_values = [m.p95 for m in runs_metrics]
    p99_values = [m.p99 for m in runs_metrics]
    failed_values = [m.req_failed_rate for m in runs_metrics]
    rate_values = [m.req_rate for m in runs_metrics]

    summary = {
        "runs": len(run_dirs),
        "http_req_duration_ms": {
            "med_mean": safe_mean(med_values),
            "med_median": safe_median(med_values),
            "p95_mean": safe_mean(p95_values),
            "p95_median": safe_median(p95_values),
            "p99_mean": safe_mean(p99_values),
            "p99_median": safe_median(p99_values),
        },
        "http_req_failed_rate": {
            "mean": safe_mean(failed_values),
            "median": safe_median(failed_values),
        },
        "http_reqs_rate": {
            "mean": safe_mean(rate_values),
            "median": safe_median(rate_values),
        },
        "resources": {
            "app-under-test": {
                "cpu_perc": metric_summary(app_cpu),
                "mem_mib": metric_summary(app_mem),
                "net_io_mib": metric_summary(app_net),
                "block_io_mib": metric_summary(app_block),
            },
            "test-db": {
                "cpu_perc": metric_summary(db_cpu),
                "mem_mib": metric_summary(db_mem),
                "net_io_mib": metric_summary(db_net),
                "block_io_mib": metric_summary(db_block),
            },
        },
    }
    return summary, runs_metrics


def generate_plots(summary_dir: Path, profile: str, scenario: str, runs_metrics: list[RunMetrics]) -> None:
    global PLOTS_DISABLED_WARNED
    try:
        import matplotlib.pyplot as plt  # type: ignore
    except Exception:
        if not PLOTS_DISABLED_WARNED:
            print("[WARN] matplotlib не установлен. Генерация графиков пропущена.")
            PLOTS_DISABLED_WARNED = True
        return

    if not runs_metrics:
        return

    plot_dir = summary_dir / "plots"
    plot_dir.mkdir(parents=True, exist_ok=True)
    prefix = f"{profile}-{scenario}"

    run_index = list(range(1, len(runs_metrics) + 1))
    p95_values = [m.p95 for m in runs_metrics]

    plt.figure(figsize=(10, 4))
    plt.plot(run_index, p95_values, marker="o", linewidth=1)
    plt.title(f"P95 по прогонам: {profile}/{scenario}")
    plt.xlabel("Прогон")
    plt.ylabel("P95, мс")
    plt.grid(True, alpha=0.3)
    plt.tight_layout()
    plt.savefig(plot_dir / f"{prefix}-p95-timeseries.png", dpi=120)
    plt.close()

    plt.figure(figsize=(8, 4))
    plt.hist(p95_values, bins=min(20, max(5, len(p95_values) // 2)))
    plt.title(f"Гистограмма P95: {profile}/{scenario}")
    plt.xlabel("P95, мс")
    plt.ylabel("Частота")
    plt.tight_layout()
    plt.savefig(plot_dir / f"{prefix}-p95-histogram.png", dpi=120)
    plt.close()

    percentile_labels = ["p50", "p75", "p90", "p95", "p99"]
    percentile_values = [
        safe_median([m.med for m in runs_metrics]),
        statistics.quantiles([m.p95 for m in runs_metrics], n=4)[1] if len(runs_metrics) >= 4 else safe_median([m.p95 for m in runs_metrics]),
        statistics.quantiles([m.p95 for m in runs_metrics], n=10)[8] if len(runs_metrics) >= 10 else max([m.p95 for m in runs_metrics]),
        safe_median([m.p95 for m in runs_metrics]),
        safe_median([m.p99 for m in runs_metrics]),
    ]
    plt.figure(figsize=(8, 4))
    plt.bar(percentile_labels, percentile_values)
    plt.title(f"Перцентили задержки: {profile}/{scenario}")
    plt.ylabel("Задержка, мс")
    plt.tight_layout()
    plt.savefig(plot_dir / f"{prefix}-percentiles.png", dpi=120)
    plt.close()


def write_summary_csv(summary_dir: Path, aggregate: dict) -> None:
    csv_file = summary_dir / "summary.csv"
    with csv_file.open("w", encoding="utf-8", newline="") as fh:
        writer = csv.writer(fh)
        writer.writerow(
            [
                "profile",
                "scenario",
                "runs",
                "p95_mean_ms",
                "p99_mean_ms",
                "failed_rate_mean",
                "req_rate_mean",
                "app_cpu_median",
                "app_mem_mib_median",
                "app_net_io_mib_median",
                "app_block_io_mib_median",
                "db_cpu_median",
                "db_mem_mib_median",
                "db_net_io_mib_median",
                "db_block_io_mib_median",
            ]
        )

        for profile_name, scenarios in aggregate.items():
            for scenario_name, data in scenarios.items():
                writer.writerow(
                    [
                        profile_name,
                        scenario_name,
                        data["runs"],
                        round(data["http_req_duration_ms"]["p95_mean"], 3),
                        round(data["http_req_duration_ms"]["p99_mean"], 3),
                        round(data["http_req_failed_rate"]["mean"], 6),
                        round(data["http_reqs_rate"]["mean"], 3),
                        round(data["resources"]["app-under-test"]["cpu_perc"]["median"], 3),
                        round(data["resources"]["app-under-test"]["mem_mib"]["median"], 3),
                        round(data["resources"]["app-under-test"]["net_io_mib"]["median"], 3),
                        round(data["resources"]["app-under-test"]["block_io_mib"]["median"], 3),
                        round(data["resources"]["test-db"]["cpu_perc"]["median"], 3),
                        round(data["resources"]["test-db"]["mem_mib"]["median"], 3),
                        round(data["resources"]["test-db"]["net_io_mib"]["median"], 3),
                        round(data["resources"]["test-db"]["block_io_mib"]["median"], 3),
                    ]
                )


def write_report_md(summary_dir: Path, aggregate: dict) -> None:
    report_file = summary_dir / "report.md"
    lines = ["# Benchmark Summary", ""]
    for profile_name, scenarios in aggregate.items():
        lines.append(f"## Profile: {profile_name}")
        lines.append("")
        for scenario_name, data in scenarios.items():
            lines.append(f"### Scenario: {scenario_name}")
            lines.append(f"- Runs: {data['runs']}")
            lines.append(f"- p95 mean: {data['http_req_duration_ms']['p95_mean']:.3f} ms")
            lines.append(f"- p99 mean: {data['http_req_duration_ms']['p99_mean']:.3f} ms")
            lines.append(f"- Failed rate mean: {data['http_req_failed_rate']['mean']:.6f}")
            lines.append(f"- Request rate mean: {data['http_reqs_rate']['mean']:.3f} req/s")
            lines.append("")
    report_file.write_text("\n".join(lines), encoding="utf-8")


def main() -> int:
    if len(sys.argv) != 2:
        print("Usage: aggregate-benchmark-results.py <results_dir>")
        return 1

    results_dir = Path(sys.argv[1]).resolve()
    summary_dir = results_dir / "summary"
    summary_dir.mkdir(parents=True, exist_ok=True)

    profiles = [p for p in results_dir.iterdir() if p.is_dir() and p.name != "summary"]
    aggregate: dict[str, dict[str, dict]] = {}

    for profile in sorted(profiles, key=lambda p: p.name):
        scenario_dirs = [d for d in profile.iterdir() if d.is_dir()]
        aggregate[profile.name] = {}
        for scenario_dir in sorted(scenario_dirs, key=lambda d: d.name):
            # Backward compatibility for old structure profile/run-XXX.
            if scenario_dir.name.startswith("run-"):
                summary, run_metrics = summarize_scenario(profile)
                aggregate[profile.name]["scores-flow"] = summary
                generate_plots(summary_dir, profile.name, "scores-flow", run_metrics)
                break

            summary, run_metrics = summarize_scenario(scenario_dir)
            aggregate[profile.name][scenario_dir.name] = summary
            generate_plots(summary_dir, profile.name, scenario_dir.name, run_metrics)

    (summary_dir / "summary.json").write_text(
        json.dumps(aggregate, ensure_ascii=False, indent=2), encoding="utf-8"
    )
    write_summary_csv(summary_dir, aggregate)
    write_report_md(summary_dir, aggregate)

    print(f"[INFO] Summary written to: {summary_dir}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
