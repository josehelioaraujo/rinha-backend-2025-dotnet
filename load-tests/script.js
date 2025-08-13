import http from 'k6/http';
import { sleep, check } from 'k6';
import { Counter } from 'k6/metrics';

// Configuração do teste
export const options = {
  stages: [
    { duration: '30s', target: 100 }, // Aumento gradual para 100 usuários em 30 segundos
    { duration: '60s', target: 100 }, // Manter 100 usuários por 1 minuto
    { duration: '30s', target: 0 },   // Redução gradual para 0 usuários
  ],
  thresholds: {
    http_req_failed: ['rate<0.01'], // Taxa de erro deve ser menor que 1%
    http_req_duration: ['p(95)<500'], // 95% das requisições devem responder em menos de 500ms
  },
};

// Contador para IDs de correlação
let counter = new Counter('correlationIds');

// Função para gerar um ID de correlação único
function generateCorrelationId() {
  counter.add(1);
  return `test-${Date.now()}-${Math.floor(Math.random() * 1000000)}`;
}

// Função principal de teste
export default function() {
  // Configuração base da API
  const baseUrl = 'http://localhost:9999';
  const headers = { 'Content-Type': 'application/json' };
  
  // Teste 1: Verificar o endpoint de saúde
  const healthRes = http.get(`${baseUrl}/health`);
  check(healthRes, {
    'health status is 200': (r) => r.status === 200,
    'health response has correct format': (r) => {
      const body = JSON.parse(r.body);
      return body.status === 'Healthy';
    },
  });
  
  // Pequena pausa para não sobrecarregar
  sleep(0.5);
  
  // Teste 2: Enviar um pagamento
  const correlationId = generateCorrelationId();
  const amount = Math.floor(Math.random() * 1000) + 1; // Valor entre 1 e 1000
  
  const paymentRes = http.post(
    `${baseUrl}/payments`,
    JSON.stringify({
      correlationId: correlationId,
      amount: amount
    }),
    { headers: headers }
  );
  
  check(paymentRes, {
    'payment status is 200': (r) => r.status === 200,
    'payment response has correct data': (r) => {
      const body = JSON.parse(r.body);
      return body.correlationId === correlationId && 
             body.amount === amount &&
             body.status === 'approved';
    },
  });
  
  // Pausa entre iterações
  sleep(1);
}
