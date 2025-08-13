import http from 'k6/http';
import { check, sleep } from 'k6';

// Configuração para 550 usuários simultâneos (carga da Rinha oficial)
export const options = {
    // Rampa de carga: 0→550 usuários em 30s, mantém 550 por 120s, desce para 0 em 30s
    stages: [
        { duration: '30s', target: 550 },   // Ramp-up para 550 usuários
        { duration: '120s', target: 550 },  // Mantém 550 usuários por 2 minutos
        { duration: '30s', target: 0 },     // Ramp-down
    ],
    
    // Thresholds para competição
    thresholds: {
        'http_req_duration': ['p(99)<3000'],  // p99 < 3s (limite superior)
        'http_req_failed': ['rate<0.05'],     // < 5% erro
        'errors': ['rate<0.05'],              // < 5% erro custom
    },
};

const BASE_URL = 'http://localhost:9999';

export default function () {
    // 70% POST /payments, 30% GET /payments-summary (proporção realista)
    const isPayment = Math.random() < 0.7;
    
    if (isPayment) {
        // POST /payments
        const payload = JSON.stringify({
            correlationId: generateUUID(),
            amount: Math.round(Math.random() * 10000) / 100, // R$ 0.01 - R$ 100.00
        });
        
        const params = {
            headers: { 'Content-Type': 'application/json' },
            timeout: '3s',
        };
        
        const response = http.post(`${BASE_URL}/payments`, payload, params);
        
        check(response, {
            'POST /payments status is 2xx': (r) => r.status >= 200 && r.status < 300,
            'POST /payments response time < 1000ms': (r) => r.timings.duration < 1000,
            'POST /payments response time < 100ms': (r) => r.timings.duration < 100,
        });
        
    } else {
        // GET /payments-summary
        const params = { timeout: '2s' };
        const response = http.get(`${BASE_URL}/payments-summary`, params);
        
        check(response, {
            'GET /payments-summary status is 200': (r) => r.status === 200,
            'GET /payments-summary has default field': (r) => {
                try {
                    const json = JSON.parse(r.body);
                    return json.default !== undefined;
                } catch {
                    return false;
                }
            },
            'GET /payments-summary has fallback field': (r) => {
                try {
                    const json = JSON.parse(r.body);
                    return json.fallback !== undefined;
                } catch {
                    return false;
                }
            },
            'GET /payments-summary response time < 500ms': (r) => r.timings.duration < 500,
            'GET /payments-summary response time < 50ms': (r) => r.timings.duration < 50,
        });
    }
    
    // Think time mínimo para simular comportamento real
    sleep(0.05); // 50ms entre requests
}

// Generate UUID v4
function generateUUID() {
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function(c) {
        const r = Math.random() * 16 | 0;
        const v = c === 'x' ? r : (r & 0x3 | 0x8);
        return v.toString(16);
    });
}

// Summary customizado para análise de performance
export function handleSummary(data) {
    const p99 = data.metrics.http_req_duration.values['p(99)'];
    const p95 = data.metrics.http_req_duration.values['p(95)'];
    const p90 = data.metrics.http_req_duration.values['p(90)'];
    const median = data.metrics.http_req_duration.values['p(50)'];
    const errorRate = data.metrics.http_req_failed.values.rate * 100;
    const rps = data.metrics.http_reqs.values.rate;
    
    console.log('\n🎯 ANÁLISE COMPETITIVA DA RINHA:');
    console.log('================================');
    console.log(`📊 p99 Latency: ${p99.toFixed(2)}ms`);
    console.log(`📊 p95 Latency: ${p95.toFixed(2)}ms`);
    console.log(`📊 p90 Latency: ${p90.toFixed(2)}ms`);
    console.log(`📊 Median: ${median.toFixed(2)}ms`);
    console.log(`📊 Error Rate: ${errorRate.toFixed(2)}%`);
    console.log(`📊 Throughput: ${rps.toFixed(0)} RPS`);
    console.log('');
    
    // Comparação com TOP 5
    console.log('🏆 COMPARAÇÃO COM TOP 5:');
    console.log('========================');
    console.log('🥇 #1 Java:    1.25ms');
    console.log('🥈 #2 Bun:     1.3ms');
    console.log('🥉 #3 Go:      1.45ms');
    console.log('#4 Rust:    1.61ms');
    console.log('#5 Rust:    1.62ms');
    console.log(`🎯 Nosso:     ${p99.toFixed(2)}ms`);
    console.log('');
    
    if (p99 <= 1.3) {
        console.log('🎉 QUALIFICADO PARA TOP 3! 🏆');
    } else if (p99 <= 1.6) {
        console.log('🎖️  QUALIFICADO PARA TOP 5! 🎖️');
    } else if (p99 <= 3.0) {
        console.log('📈 Posição competitiva TOP 10');
    } else {
        console.log('🔥 Necessário otimizações ultra-agressivas');
    }
    
    return {
        'summary.json': JSON.stringify(data, null, 2),
    };
}
