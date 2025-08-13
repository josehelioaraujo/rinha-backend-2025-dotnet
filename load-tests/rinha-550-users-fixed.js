import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
    stages: [
        { duration: '30s', target: 550 },   
        { duration: '120s', target: 550 },  
        { duration: '30s', target: 0 },     
    ],
    
    thresholds: {
        'http_req_duration': ['p(99)<3000'],  
        'http_req_failed': ['rate<0.05'],     
        'checks': ['rate>0.95'],              // ← CORRIGIDO: usa métrica válida
    },
};

const BASE_URL = 'http://localhost:9999';

export default function () {
    const isPayment = Math.random() < 0.7;
    
    if (isPayment) {
        const payload = JSON.stringify({
            correlationId: generateUUID(),
            amount: Math.round(Math.random() * 10000) / 100,
        });
        
        const params = {
            headers: { 'Content-Type': 'application/json' },
            timeout: '3s',
        };
        
        const response = http.post(`${BASE_URL}/payments`, payload, params);
        
        check(response, {
            'POST status 2xx': (r) => r.status >= 200 && r.status < 300,
            'POST < 1000ms': (r) => r.timings.duration < 1000,
        });
        
    } else {
        const response = http.get(`${BASE_URL}/payments-summary`, { timeout: '2s' });
        
        check(response, {
            'GET status 200': (r) => r.status === 200,
            'GET has default': (r) => {
                try {
                    return JSON.parse(r.body).default !== undefined;
                } catch { return false; }
            },
            'GET < 500ms': (r) => r.timings.duration < 500,
        });
    }
    
    sleep(0.05);
}

function generateUUID() {
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function(c) {
        const r = Math.random() * 16 | 0;
        const v = c === 'x' ? r : (r & 0x3 | 0x8);
        return v.toString(16);
    });
}

export function handleSummary(data) {
    const p99 = data.metrics.http_req_duration.values['p(99)'];
    const p95 = data.metrics.http_req_duration.values['p(95)'];
    const median = data.metrics.http_req_duration.values['p(50)'];
    const errorRate = data.metrics.http_req_failed.values.rate * 100;
    const rps = data.metrics.http_reqs.values.rate;
    
    console.log('\n🎯 RINHA BACKEND 2025 - RESULTADOS FINAIS:');
    console.log('=========================================');
    console.log(`📊 p99: ${p99?.toFixed(2) || 'N/A'}ms`);
    console.log(`📊 p95: ${p95?.toFixed(2) || 'N/A'}ms`);
    console.log(`📊 Median: ${median?.toFixed(2) || 'N/A'}ms`);
    console.log(`📊 Error Rate: ${errorRate?.toFixed(2) || 'N/A'}%`);
    console.log(`📊 Throughput: ${rps?.toFixed(0) || 'N/A'} RPS`);
    console.log('');
    
    console.log('🏆 COMPARAÇÃO COM TOP 5:');
    console.log('========================');
    console.log('🥇 #1 Java:    1.25ms');
    console.log('🥈 #2 Bun:     1.3ms');
    console.log('🥉 #3 Go:      1.45ms');
    console.log('#4 Rust:    1.61ms');
    console.log('#5 Rust:    1.62ms');
    console.log(`🎯 Nosso:     ${p99?.toFixed(2) || 'N/A'}ms`);
    console.log('');
    
    if (p99 && p99 <= 1.3) {
        console.log('🎉 QUALIFICADO PARA TOP 3! 🏆');
    } else if (p99 && p99 <= 1.6) {
        console.log('🎖️  QUALIFICADO PARA TOP 5! 🎖️');
    } else if (p99 && p99 <= 3.0) {
        console.log('📈 Posição competitiva TOP 10');
    } else {
        console.log('🔥 Necessário otimizações ultra-agressivas');
    }
    
    return { 'summary.json': JSON.stringify(data, null, 2) };
}
