import { commonSetup, runScoresScenario, think } from "./common.js";

export const options = {
  discardResponseBodies: true,
  summaryTrendStats: ["min", "avg", "med", "max", "p(50)", "p(75)", "p(90)", "p(95)", "p(99)"],
  setupTimeout: __ENV.BENCH_SETUP_TIMEOUT || "180s",
  stages: [
    { duration: __ENV.BENCH_MAXLOAD_WARMUP_DURATION || "20s", target: Number(__ENV.BENCH_MAXLOAD_WARMUP_VUS || 30) },
    { duration: __ENV.BENCH_MAXLOAD_MAIN_DURATION || "90s", target: Number(__ENV.BENCH_MAXLOAD_MAIN_VUS || 80) },
    { duration: __ENV.BENCH_MAXLOAD_COOLDOWN_DURATION || "20s", target: Number(__ENV.BENCH_MAXLOAD_COOLDOWN_VUS || 30) }
  ]
};

export const setup = commonSetup;

export default function (data) {
  runScoresScenario(data);
  think(0.05);
}
