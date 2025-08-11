import http from 'k6/http';
import { check } from 'k6';

export let options = {
  stages: [
    { duration: '30s', target: 100 },   // 100 usuários
    { duration: '30s', target: 500 },   // 500 usuários
    { duration: '30s', target: 1000 },  // 1k usuários
    { duration: '30s', target: 2000 },  // 2k usuários
    { duration: '30s', target: 5000 },  // 5k usuários
    { duration: '30s', target: 10000 }, // 10k usuários (target)
    { duration: '30s', target: 0 },     // Cool-down
  ],
};

const BASE_URL = 'http://localhost:9999';

export default function () {
  // Teste simples de carga crescente
  const response = http.get(`${BASE_URL}/`);
  
  check(response, {
    'status is 200': (r) => r.status === 200,
    'response time < 2000ms': (r) => r.timings.duration < 2000,
  });
}
