// k6 load test — Component 1 (Field & Cultivation). PERF-01.
// Run with the backend on the http profile (localhost:5164):
//   k6 run -e FARMER_EMAIL=... -e FARMER_PASSWORD=... -e OFFICER_EMAIL=... -e OFFICER_PASSWORD=... field-cultivation-load-test.js
import http from 'k6/http';
import { check, sleep } from 'k6';

const BASE = __ENV.BASE_URL || 'http://localhost:5164/api';

export const options = {
  stages: [
    { duration: '30s', target: 25 },
    { duration: '2m', target: 25 },
    { duration: '15s', target: 0 },
  ],
  thresholds: {
    http_req_duration: ['p(95)<800'],
    http_req_failed: ['rate<0.01'],
  },
};

function login(email, password) {
  const res = http.post(`${BASE}/auth/login`, JSON.stringify({ email, password }), {
    headers: { 'Content-Type': 'application/json' },
  });
  check(res, { 'login 200': r => r.status === 200 });
  if (res.status === 0) {
    throw new Error(`Cannot reach ${BASE}. Start the backend first: cd paddywise-backend && dotnet run --launch-profile http`);
  }
  if (res.status !== 200) {
    throw new Error(`Login failed for ${email} (HTTP ${res.status}): ${res.body}. Check the email/password and that an officer account is approved.`);
  }
  return res.json().accessToken;
}

export function setup() {
  for (const k of ['FARMER_EMAIL', 'FARMER_PASSWORD', 'OFFICER_EMAIL', 'OFFICER_PASSWORD']) {
    if (!__ENV[k] || __ENV[k] === '...') throw new Error(`Missing -e ${k}=<value> on the k6 command line`);
  }
  return {
    farmer: login(__ENV.FARMER_EMAIL, __ENV.FARMER_PASSWORD),
    officer: login(__ENV.OFFICER_EMAIL, __ENV.OFFICER_PASSWORD),
  };
}

export default function (t) {
  const farmer = { headers: { Authorization: `Bearer ${t.farmer}` } };
  const officer = { headers: { Authorization: `Bearer ${t.officer}` } };

  const responses = http.batch([
    ['GET', `${BASE}/fields`, null, farmer],
    ['GET', `${BASE}/cycles`, null, farmer],
    ['GET', `${BASE}/divisions`, null, farmer],
    ['GET', `${BASE}/varieties`, null, farmer],
    ['GET', `${BASE}/plans/pending`, null, officer],
  ]);
  check(responses[0], { 'fields 200': r => r.status === 200 });
  check(responses[1], { 'cycles 200': r => r.status === 200 });
  check(responses[2], { 'divisions 200': r => r.status === 200 });
  check(responses[3], { 'varieties 200': r => r.status === 200 });
  check(responses[4], { 'pending plans 200': r => r.status === 200 });
  sleep(1);
}
