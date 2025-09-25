import http from 'k6/http';
import { check, sleep, group } from 'k6';

export const options = {
  scenarios: {
    ramping_sessions: {
      executor: 'ramping-vus',
      startVUs: 0,
      stages: [
        { duration: '10s', target: 5 },
        { duration: '20s', target: 15 },
        { duration: '10s', target: 0 }
      ]
    }
  },
  thresholds: {
    http_req_duration: ['p(95)<250']
  }
};

const baseUrl = __ENV.BASE_URL || 'http://localhost:5243'; // adjust port if different

function createSession() {
  const res = http.post(`${baseUrl}/api/sessions`, '{}', { headers: { 'Content-Type': 'application/json' } });
  check(res, { 'created 201': r => r.status === 201 });
  if (res.status !== 201) return null;
  try {
    const json = res.json();
    return json.code;
  } catch { return null; }
}

function join(sessionCode, name) {
  const res = http.post(`${baseUrl}/api/sessions/${sessionCode}/participants`, JSON.stringify({ displayName: name }), { headers: { 'Content-Type': 'application/json' } });
  check(res, { 'join 200': r => r.status === 200 });
}

function addWorkItem(sessionCode, title) {
  const res = http.post(`${baseUrl}/api/sessions/${sessionCode}/work-items`, JSON.stringify({ title }), { headers: { 'Content-Type': 'application/json' } });
  check(res, { 'add work item 200': r => r.status === 200 });
  if (res.status !== 200) return null;
  try { return res.json().workItems[0].id; } catch { return null; }
}

function submitEstimate(sessionCode, workItemId, participantId, value) {
  http.post(`${baseUrl}/api/sessions/${sessionCode}/work-items/${workItemId}/estimates`, JSON.stringify({ participantId, value }), { headers: { 'Content-Type': 'application/json' } });
}

export default function () {
  group('session flow', () => {
    const code = createSession();
    if (!code) return;
    join(code, `User_${__VU}_${__ITER}`);
    const workItemId = addWorkItem(code, 'API endpoint');
    sleep(0.2);
    // submitting estimate with dummy participant/workItem IDs not strictly validated here; load focuses on create/join/add
  });
  sleep(1);
}
