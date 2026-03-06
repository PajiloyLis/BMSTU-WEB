import http from "k6/http";
import { check, sleep } from "k6";

const baseUrl = __ENV.BENCH_BASE_URL || "http://app-under-test:8080/api/v1";
const userEmail = __ENV.BENCH_USER_EMAIL || "fedorova@example.com";
const userPassword = __ENV.BENCH_USER_PASSWORD || "fedorova";

export const options = {
  discardResponseBodies: true,
  summaryTrendStats: ["min", "avg", "med", "max", "p(50)", "p(75)", "p(90)", "p(95)", "p(99)"],
  stages: [
    { duration: __ENV.BENCH_STAGE1_DURATION || "30s", target: Number(__ENV.BENCH_STAGE1_VUS || 10) },
    { duration: __ENV.BENCH_STAGE2_DURATION || "60s", target: Number(__ENV.BENCH_STAGE2_VUS || 30) },
    { duration: __ENV.BENCH_STAGE3_DURATION || "30s", target: Number(__ENV.BENCH_STAGE3_VUS || 10) }
  ]
};

function api(path) {
  return `${baseUrl}${path}`;
}

export function setup() {
  const loginRes = http.post(
    api("/auth/login"),
    JSON.stringify({ email: userEmail, password: userPassword }),
    { headers: { "Content-Type": "application/json" } }
  );

  check(loginRes, {
    "login status 200": (r) => r.status === 200
  });

  const body = loginRes.json();
  return { token: body?.token || "", userId: body?.id || "" };
}

export default function (data) {
  const authHeaders = {
    headers: {
      Authorization: `Bearer ${data.token}`,
      "Content-Type": "application/json"
    }
  };

  const companiesRes = http.get(api("/companies"), authHeaders);
  check(companiesRes, { "companies status 200": (r) => r.status === 200 });
  const companies = companiesRes.json() || [];
  if (!companies.length) {
    sleep(0.5);
    return;
  }

  const companyId = companies[0].companyId;
  const headRes = http.get(api(`/companies/${companyId}/headPosition`), authHeaders);
  check(headRes, { "head position status 200": (r) => r.status === 200 });
  const head = headRes.json();
  if (!head || !head.id) {
    sleep(0.5);
    return;
  }

  const subordinatesRes = http.get(api(`/positions/${head.id}/subordinates`), authHeaders);
  check(subordinatesRes, { "subordinates status 200": (r) => r.status === 200 });

  const scoresRes = http.get(api(`/employees/${data.userId}/subordinates/lasrScores`), authHeaders);
  check(scoresRes, { "last scores status 200": (r) => r.status === 200 });

  sleep(0.2);
}
