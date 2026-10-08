// k6 load test — Component 2 (Crop Activity & Resource Management).
// 50 virtual users for 5 minutes on activity retrieval and the officer recommendation queue.
// Run with the backend on the http profile (localhost:5164):
//   k6 run -e FARMER_EMAIL=... -e FARMER_PASSWORD=... -e OFFICER_EMAIL=... -e OFFICER_PASSWORD=... \
//     --summary-export=perf-summary.json load-test-cropresource.js
import http from 'k6/http';
import { check, sleep } from 'k6';

const BASE = __ENV.BASE_URL || 'http://localhost:5164/api';

export const options = {
  stages: [
    { duration: '30s', target: 50 },
    { duration: '4m', target: 50 },
    { duration: '30s', target: 0 },
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
  const farmer = login(__ENV.FARMER_EMAIL, __ENV.FARMER_PASSWORD);
  const officer = login(__ENV.OFFICER_EMAIL, __ENV.OFFICER_PASSWORD);
  // Use the farmer's newest cycle for the per-cycle activity list, if they have one.
  const cycles = http.get(`${BASE}/cycles`, { headers: { Authorization: `Bearer ${farmer}` } });
  const list = cycles.status === 200 ? cycles.json() : [];
  return { farmer, officer, cycleId: list.length ? list[0].id : null };
}

export default function (t) {
  const farmer = { headers: { Authorization: `Bearer ${t.farmer}` } };
  const officer = { headers: { Authorization: `Bearer ${t.officer}` } };

  const requests = [
    ['GET', `${BASE}/activities`, null, farmer],
    ['GET', `${BASE}/activities?activityType=Irrigation`, null, farmer],
    ['GET', `${BASE}/recommendations/pending`, null, officer],
    ['GET', `${BASE}/recommendations/pending?status=PENDING_OFFICER_REVIEW`, null, officer],
  ];
  if (t.cycleId) requests.push(['GET', `${BASE}/cycles/${t.cycleId}/activities`, null, farmer]);

  const res = http.batch(requests);
  check(res[0], { 'activity history 200': r => r.status === 200 });
  check(res[1], { 'filtered history 200': r => r.status === 200 });
  check(res[2], { 'officer review queue 200': r => r.status === 200 });
  check(res[3], { 'pending-only queue 200': r => r.status === 200 });
  if (t.cycleId) check(res[4], { 'cycle activities 200': r => r.status === 200 });
  sleep(1);
}
