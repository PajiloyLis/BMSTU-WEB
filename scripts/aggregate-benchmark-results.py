#!/usr/bin/env python3

from __future__ import annotations

import csv
import json
import statistics
import sys
from pathlib import Path


def parse_percent(value: str) -> float:
    return float(value.replace("%", "").strip())


def parse_memory_to_mib(value: str) -> float:
    # Input example: "123.4MiB / 7.6GiB"
    left = value.split("/")[0].strip()
    number = float("".join(ch for ch in left if (ch.isdigit() or ch == ".")))
    unit = left.replace(str(number), "").strip()
    unit = unit.lower()
    if unit == "gib":
        return number * 1024
    if unit == "kib":
        return number / 1024
    if unit == "mib":
        return number
    if unit == "b":
        return number / (1024 * 1024)
    return number


def safe_mean(values: list[float]) -> float:
    return statistics.mean(values) if values else 0.0


def safe_median(values: list[float]) -> float:
    return statistics.median(values) if values else 0.0


def summarize_profile(profile_dir: Path) -> dict:
    run_dirs = sorted([p for p in profile_dir.iterdir() if p.is_dir() and p.name.startswith("run-")])

    duration_p95 = []
    duration_p99 = []
    duration_med = []
    req_failed = []
    req_rate = []

    app_cpu = []
    app_mem = []
    db_cpu = []
    db_mem = []

    for run in run_dirs:
        summary_file = run / "k6-summary.json"
        if summary_file.exists():
            data = json.loads(summary_file.read_text(encoding="utf-8"))
            metrics = data.get("metrics", {})
            req_dur = metrics.get("http_req_duration", {}).get("values", {})
            req_fail = metrics.get("http_req_failed", {}).get("values", {})
            reqs = metrics.get("http_reqs", {}).get("values", {})

            duration_med.append(float(req_dur.get("med", 0)))
            duration_p95.append(float(req_dur.get("p(95)", 0)))
            duration_p99.append(float(req_dur.get("p(99)", 0)))
            req_failed.append(float(req_fail.get("rate", 0)))
            req_rate.append(float(reqs.get("rate", 0)))

        stats_file = run / "docker-stats.csv"
        if stats_file.exists():
            with stats_file.open("r", encoding="utf-8") as fh:
                reader = csv.DictReader(fh)
                for row in reader:
                    container = row["container"]
                    cpu = parse_percent(row["cpu_perc"])
                    mem = parse_memory_to_mib(row["mem_usage"])
                    if "app-under-test" in container:
                        app_cpu.append(cpu)
                        app_mem.append(mem)
                    elif "test-db" in container:
                        db_cpu.append(cpu)
                        db_mem.append(mem)

    return {
        "runs": len(run_dirs),
        "http_req_duration_ms": {
            "med_mean": safe_mean(duration_med),
            "med_median": safe_median(duration_med),
            "p95_mean": safe_mean(duration_p95),
            "p95_median": safe_median(duration_p95),
            "p99_mean": safe_mean(duration_p99),
            "p99_median": safe_median(duration_p99),
        },
        "http_req_failed_rate": {
            "mean": safe_mean(req_failed),
            "median": safe_median(req_failed),
        },
        "http_reqs_rate": {
            "mean": safe_mean(req_rate),
            "median": safe_median(req_rate),
        },
        "resources": {
            "app-under-test": {
                "cpu_perc": {
                    "min": min(app_cpu) if app_cpu else 0.0,
                    "max": max(app_cpu) if app_cpu else 0.0,
                    "median": safe_median(app_cpu),
                },
                "mem_mib": {
                    "min": min(app_mem) if app_mem else 0.0,
                    "max": max(app_mem) if app_mem else 0.0,
                    "median": safe_median(app_mem),
                },
            },
            "test-db": {
                "cpu_perc": {
                    "min": min(db_cpu) if db_cpu else 0.0,
                    "max": max(db_cpu) if db_cpu else 0.0,
                    "median": safe_median(db_cpu),
                },
                "mem_mib": {
                    "min": min(db_mem) if db_mem else 0.0,
                    "max": max(db_mem) if db_mem else 0.0,
                    "median": safe_median(db_mem),
                },
            },
        },
    }


def main() -> int:
    if len(sys.argv) != 2:
        print("Usage: aggregate-benchmark-results.py <results_dir>")
        return 1

    results_dir = Path(sys.argv[1]).resolve()
    summary_dir = results_dir / "summary"
    summary_dir.mkdir(parents=True, exist_ok=True)

    profiles = [p for p in results_dir.iterdir() if p.is_dir() and p.name != "summary"]
    aggregate = {}
    for profile in sorted(profiles, key=lambda p: p.name):
        aggregate[profile.name] = summarize_profile(profile)

    (summary_dir / "summary.json").write_text(
        json.dumps(aggregate, ensure_ascii=False, indent=2), encoding="utf-8"
    )

    csv_file = summary_dir / "summary.csv"
    with csv_file.open("w", encoding="utf-8", newline="") as fh:
        writer = csv.writer(fh)
        writer.writerow(
            [
                "profile",
                "runs",
                "p95_mean_ms",
                "p99_mean_ms",
                "failed_rate_mean",
                "req_rate_mean",
                "app_cpu_median",
                "app_mem_mib_median",
                "db_cpu_median",
                "db_mem_mib_median",
            ]
        )
        for profile_name, data in aggregate.items():
            writer.writerow(
                [
                    profile_name,
                    data["runs"],
                    round(data["http_req_duration_ms"]["p95_mean"], 3),
                    round(data["http_req_duration_ms"]["p99_mean"], 3),
                    round(data["http_req_failed_rate"]["mean"], 6),
                    round(data["http_reqs_rate"]["mean"], 3),
                    round(data["resources"]["app-under-test"]["cpu_perc"]["median"], 3),
                    round(data["resources"]["app-under-test"]["mem_mib"]["median"], 3),
                    round(data["resources"]["test-db"]["cpu_perc"]["median"], 3),
                    round(data["resources"]["test-db"]["mem_mib"]["median"], 3),
                ]
            )

    print(f"[INFO] Summary written to: {summary_dir}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
