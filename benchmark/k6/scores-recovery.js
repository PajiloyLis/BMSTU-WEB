import { commonSetup, runScoresScenario, think } from "./common.js";

export const options = {
  discardResponseBodies: true,
  summaryTrendStats: ["min", "avg", "med", "max", "p(50)", "p(75)", "p(90)", "p(95)", "p(99)"],
  setupTimeout: __ENV.BENCH_SETUP_TIMEOUT || "180s",
  stages: [
    { duration: __ENV.BENCH_RECOVERY_BASELINE_DURATION || "20s", target: Number(__ENV.BENCH_RECOVERY_BASELINE_VUS || 20) },
    { duration: __ENV.BENCH_RECOVERY_OVERLOAD_DURATION || "30s", target: Number(__ENV.BENCH_RECOVERY_OVERLOAD_VUS || 120) },
    { duration: __ENV.BENCH_RECOVERY_RECOVER_DURATION || "60s", target: Number(__ENV.BENCH_RECOVERY_RECOVER_VUS || 20) }
  ]
};

export const setup = commonSetup;

export default function (data) {
  runScoresScenario(data);
  think(0.1);
}
