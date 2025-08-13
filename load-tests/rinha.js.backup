import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate, Trend } from 'k6/metrics';

// Métricas customizadas
export let errorRate = new Rate('errors');
export let paymentDuration = new Trend('payment_duration');
export let summaryDuration = new Trend('summary_duration');

// Configuração do teste (similar à Rinha oficial)
export let options = {
  stages: [
    { duration: '10s', target: 50 },   // Warm-up
    { duration: '30s', target: __ENV.MAX_REQUESTS || 550 }, // Ramp-up
    { duration: '60s', target: __ENV.MAX_REQUESTS || 550 }, // Sustained load
    { duration: '10s', target: 0 },    // Cool-down
  ],
  thresholds: {
    http_req_duration: ['p(95)<1500'], // 95% das requests < 1.5s
    http_req_duration: ['p(99)<3000'], // 99% das requests < 3s (target: <1.3ms ideal)
    http_req_failed: ['rate<0.05'],    // Error rate < 5%
    errors: ['rate<0.05'],
  },
};

const BASE_URL = 'http://localhost:9999';

// Gerar correlation ID único
function generateCorrelationId() {
  return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function(c) {
    var r = Math.random() * 16 | 0, v = c == 'x' ? r : (r & 0x3 | 0x8);
    return v.toString(16);
  });
}

// Gerar valor de pagamento aleatório
function generatePaymentAmount() {
  return Math.floor(Math.random() * 10000) / 100; // 0.01 a 99.99
}

export default function () {
  // 70% POST /payments + 30% GET /payments-summary (peso realista)
  const testType = Math.random();
  
  if (testType < 0.7) {
    // Teste POST /payments
    const payload = JSON.stringify({
      correlationId: generateCorrelationId(),
      amount: generatePaymentAmount()
    });

    const params = {
      headers: {
        'Content-Type': 'application/json',
      },
    };

    const response = http.post(`${BASE_URL}/payments`, payload, params);
    
    // Verificações específicas para payments
    const paymentsSuccess = check(response, {
      'POST /payments status is 2xx': (r) => r.status >= 200 && r.status < 300,
      'POST /payments response time < 1000ms': (r) => r.timings.duration < 1000,
      'POST /payments response time < 100ms': (r) => r.timings.duration < 100, // Target ideal
    });

    paymentDuration.add(response.timings.duration);
    errorRate.add(!paymentsSuccess);

  } else {
    // Teste GET /payments-summary
    const summaryParams = Math.random() < 0.3 ? 
      '?from=2025-01-01T00:00:00.000Z&to=2025-12-31T23:59:59.000Z' : '';
    
    const response = http.get(`${BASE_URL}/payments-summary${summaryParams}`);
    
    // Verificações específicas para summary
    const summarySuccess = check(response, {
      'GET /payments-summary status is 200': (r) => r.status === 200,
      'GET /payments-summary has default field': (r) => {
        try {
          const body = JSON.parse(r.body);
          return body.default && typeof body.default.totalRequests === 'number';
        } catch {
          return false;
        }
      },
      'GET /payments-summary has fallback field': (r) => {
        try {
          const body = JSON.parse(r.body);
          return body.fallback && typeof body.fallback.totalRequests === 'number';
        } catch {
          return false;
        }
      },
      'GET /payments-summary response time < 500ms': (r) => r.timings.duration < 500,
      'GET /payments-summary response time < 50ms': (r) => r.timings.duration < 50, // Target com cache
    });

    summaryDuration.add(response.timings.duration);
    errorRate.add(!summarySuccess);
  }

  // Pequena pausa entre requests (mais realista)
  sleep(Math.random() * 0.1); // 0-100ms
}

export function handleSummary(data) {
  return {
    'load-test-results.json': JSON.stringify(data, null, 2),
    stdout: `
=== RINHA BACKEND 2025 - LOAD TEST RESULTS ===

📊 PERFORMANCE SUMMARY:
- Total Requests: ${data.metrics.http_reqs.values.count}
- Request Rate: ${data.metrics.http_reqs.values.rate.toFixed(2)} req/s
- Failed Requests: ${(data.metrics.http_req_failed.values.rate * 100).toFixed(2)}%

⚡ LATENCY ANALYSIS:
- Average Response Time: ${data.metrics.http_req_duration.values.avg.toFixed(2)}ms
- P95 Response Time: ${data.metrics.http_req_duration.values['p(95)'].toFixed(2)}ms
- P99 Response Time: ${data.metrics.http_req_duration.values['p(99)'].toFixed(2)}ms

🎯 RINHA TARGETS:
- P99 < 1.3ms Target: ${data.metrics.http_req_duration.values['p(99)'] < 1.3 ? '✅ PASSED' : '❌ FAILED'}
- Error Rate < 5%: ${data.metrics.http_req_failed.values.rate < 0.05 ? '✅ PASSED' : '❌ FAILED'}
- Throughput > 10k RPS: ${data.metrics.http_reqs.values.rate > 10000 ? '✅ PASSED' : '❌ TARGET'}

===============================================
`,
  };
}
