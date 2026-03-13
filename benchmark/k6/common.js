import http from "k6/http";
import { check, sleep } from "k6";

const baseUrl = __ENV.BENCH_BASE_URL || "http://app-under-test:8080/api/v1";
const userEmail = __ENV.BENCH_USER_EMAIL || "fedorova@example.com";
const userPassword = __ENV.BENCH_USER_PASSWORD || "fedorova";

function api(path) {
  return `${baseUrl}${path}`;
}

export function commonSetup() {
  let loginRes = null;
  let body = null;
  for (let i = 0; i < 60; i++) {
    loginRes = http.post(
      api("/auth/login"),
      JSON.stringify({ email: userEmail, password: userPassword }),
      { headers: { "Content-Type": "application/json" } }
    );
    if (loginRes.status === 200) {
      body = loginRes.json();
      return { token: body?.token || "", userId: body?.id || "" };
    }
    sleep(1);
  }

  check(loginRes, {
    "login status 200": (r) => r.status === 200
  });
  return { token: "", userId: "" };
}

export function runScoresScenario(data) {
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
}

export function think(seconds = 0.2) {
  sleep(seconds);
}
