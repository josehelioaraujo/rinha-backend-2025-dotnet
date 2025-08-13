import http from 'k6/http';
import { check } from 'k6';

export const options = {
  stages: [
    { duration: '30s', target: 550 }, // Ramp up
    { duration: '60s', target: 550 }, // Stay at 550
    { duration: '30s', target: 0 },   // Ramp down
  ],
  thresholds: {
    http_req_duration: ['p(99)<3000'], // 3s threshold
    http_req_failed: ['rate<0.05'],    // <5% error rate
  },
};

export default function () {
  // POST /payments (80% of requests)
  if (Math.random() < 0.8) {
    const payload = {
      correlationId: 'test-' + Math.random().toString(36).substr(2, 9),
      amount: Math.floor(Math.random() * 10000) / 100
    };
    
    const response = http.post('http://localhost:9999/payments', JSON.stringify(payload), {
      headers: { 'Content-Type': 'application/json' },
    });
    
    check(response, {
      'POST status is 2xx': (r) => r.status >= 200 && r.status < 300,
    });
  } 
  // GET /payments-summary (20% of requests)
  else {
    const response = http.get('http://localhost:9999/payments-summary');
    
    check(response, {
      'GET status is 200': (r) => r.status === 200,
    });
  }
}
