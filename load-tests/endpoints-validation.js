import http from 'k6/http';
import { check, group } from 'k6';

export let options = {
  vus: 10,
  duration: '30s',
};

const BASE_URL = 'http://localhost:9999';

export default function () {
  group('Endpoint Validation', function () {
    
    group('Health Check', function () {
      const response = http.get(`${BASE_URL}/`);
      check(response, {
        'health status is 200': (r) => r.status === 200,
        'health contains "Rinha"': (r) => r.body.includes('Rinha'),
      });
    });

    group('Payments Summary', function () {
      const response = http.get(`${BASE_URL}/payments-summary`);
      check(response, {
        'summary status is 200': (r) => r.status === 200,
        'summary has correct format': (r) => {
          try {
            const body = JSON.parse(r.body);
            return body.default && body.fallback;
          } catch {
            return false;
          }
        },
      });
    });

    group('Post Payment', function () {
      const payload = JSON.stringify({
        correlationId: '12345678-1234-1234-1234-123456789012',
        amount: 100.50
      });

      const response = http.post(`${BASE_URL}/payments`, payload, {
        headers: { 'Content-Type': 'application/json' },
      });

      check(response, {
        'payment status is 2xx': (r) => r.status >= 200 && r.status < 300,
      });
    });

    group('Queue Metrics', function () {
      const response = http.get(`${BASE_URL}/metrics/queue`);
      check(response, {
        'queue metrics accessible': (r) => r.status === 200,
      });
    });

  });
}
