// PERF-01: API load test for Component 3 (Pest & Disease).
// Run (backend must be running on http://localhost:5164):
//   k6 run -e EMAIL=farmer@example.com -e PASSWORD=YourPassword load-test.js
import http from 'k6/http';
import { check, sleep } from 'k6';

const BASE = __ENV.BASE_URL || 'http://localhost:5164/api';

export const options = {
  stages: [
    { duration: '30s', target: 25 }, // ramp up to 25 virtual users
    { duration: '2m', target: 25 },  // stay at 25 users for 2 minutes
    { duration: '15s', target: 0 },  // ramp down
  ],
  thresholds: {
    http_req_duration: ['p(95)<800'], // 95% of requests faster than 800 ms
    http_req_failed: ['rate<0.01'],   // less than 1% errors
  },
};

// Log in once before the test; every virtual user reuses the token.
export function setup() {
  const res = http.post(`${BASE}/auth/login`,
    JSON.stringify({ email: __ENV.EMAIL, password: __ENV.PASSWORD }),
    { headers: { 'Content-Type': 'application/json' } });
  if (res.status !== 200) {
    throw new Error(`Login failed with status ${res.status}. Check EMAIL and PASSWORD.`);
  }
  return { token: res.json('accessToken') };
}

export default function (data) {
  const params = { headers: { Authorization: `Bearer ${data.token}` } };

  const obs = http.get(`${BASE}/observations`, Object.assign({ tags: { name: 'GET /observations' } }, params));
  check(obs, { 'observations: status 200': (r) => r.status === 200 });

  const kb = http.get(`${BASE}/pest-disease-knowledge`, Object.assign({ tags: { name: 'GET /pest-disease-knowledge' } }, params));
  check(kb, { 'knowledge base: status 200': (r) => r.status === 200 });

  sleep(1); // think time between iterations, like a real user
}
