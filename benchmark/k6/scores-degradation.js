import { commonSetup, runScoresScenario, think } from "./common.js";

export const options = {
  discardResponseBodies: true,
  summaryTrendStats: ["min", "avg", "med", "max", "p(50)", "p(75)", "p(90)", "p(95)", "p(99)"],
  setupTimeout: __ENV.BENCH_SETUP_TIMEOUT || "180s",
  stages: [
    { duration: __ENV.BENCH_DEGRADATION_STAGE1_DURATION || "2s", target: Number(__ENV.BENCH_DEGRADATION_STAGE1_VUS || 10) },
    { duration: __ENV.BENCH_DEGRADATION_STAGE2_DURATION || "2s", target: Number(__ENV.BENCH_DEGRADATION_STAGE2_VUS || 25) },
    { duration: __ENV.BENCH_DEGRADATION_STAGE3_DURATION || "2s", target: Number(__ENV.BENCH_DEGRADATION_STAGE3_VUS || 50) },
    { duration: __ENV.BENCH_DEGRADATION_STAGE4_DURATION || "2s", target: Number(__ENV.BENCH_DEGRADATION_STAGE4_VUS || 75) },
    { duration: __ENV.BENCH_DEGRADATION_STAGE5_DURATION || "2s", target: Number(__ENV.BENCH_DEGRADATION_STAGE5_VUS || 100) }
  ]
};

export const setup = commonSetup;

export default function (data) {
  runScoresScenario(data);
  think(0.1);
}
