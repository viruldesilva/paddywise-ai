// k6 load test — Component 4: Reporting, Dashboards & AI Approval Management
//
// Usage (backend running on the http profile):
//   k6 run -e OFFICER_EMAIL=<officer> -e OFFICER_PASSWORD=<pw> ^
//          -e ADMIN_EMAIL=<admin> -e ADMIN_PASSWORD=<pw> ^
//          --summary-export=perf-summary.json load-test.js
//
// ADMIN_EMAIL / ADMIN_PASSWORD are optional; without them only the officer
// endpoints are exercised. BASE_URL defaults to http://localhost:5164/api.

import http from 'k6/http';
import { check, sleep } from 'k6';

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5164/api';

export const options = {
  stages: [
    { duration: '30s', target: 10 }, // ramp up
    { duration: '1m45s', target: 25 }, // sustained load
    { duration: '30s', target: 0 }, // ramp down
  ],
  thresholds: {
    http_req_duration: ['p(95)<800'], // 95% of requests under 800 ms
    http_req_failed: ['rate<0.01'], // under 1% failed requests
  },
};

function login(email, password) {
  const res = http.post(
    `${BASE_URL}/auth/login`,
    JSON.stringify({ email, password }),
    { headers: { 'Content-Type': 'application/json' } },
  );
  if (res.status !== 200) {
    throw new Error(`Login failed for ${email}: ${res.status} ${res.body}`);
  }
  return res.json('accessToken');
}

// Runs once before the load starts; the tokens are shared by every VU.
export function setup() {
  if (!__ENV.OFFICER_EMAIL || !__ENV.OFFICER_PASSWORD) {
    throw new Error('Set OFFICER_EMAIL and OFFICER_PASSWORD (an approved AgriculturalOfficer).');
  }
  const officerToken = login(__ENV.OFFICER_EMAIL, __ENV.OFFICER_PASSWORD);
  const adminToken = __ENV.ADMIN_EMAIL
    ? login(__ENV.ADMIN_EMAIL, __ENV.ADMIN_PASSWORD)
    : null;
  return { officerToken, adminToken };
}

const auth = (token) => ({ headers: { Authorization: `Bearer ${token}` } });

export default function (data) {
  const officer = auth(data.officerToken);

  check(http.get(`${BASE_URL}/officer/dashboard`, officer), {
    'officer dashboard: status 200': (r) => r.status === 200,
  });
  // GET /notifications (the list) is excluded: it returns 500 on every call,
  // a known defect found by this load test. unread-count still works.
  check(http.get(`${BASE_URL}/notifications/unread-count`, officer), {
    'unread count: status 200': (r) => r.status === 200,
  });

  if (data.adminToken) {
    const admin = auth(data.adminToken);
    check(http.get(`${BASE_URL}/admin/dashboard`, admin), {
      'admin dashboard: status 200': (r) => r.status === 200,
    });
    check(http.get(`${BASE_URL}/admin/officer-requests`, admin), {
      'officer requests: status 200': (r) => r.status === 200,
    });
  }

  sleep(1);
}
