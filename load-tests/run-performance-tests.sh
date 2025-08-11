#!/bin/bash
# Execução completa dos testes de performance

set -e

echo "RINHA BACKEND 2025 - PERFORMANCE TESTS"
echo "======================================"

LOAD_TEST_DIR="/root/rinha-backend-2025-dotnet/load-tests"
cd "$LOAD_TEST_DIR"

# Configurar dashboard k6
export K6_WEB_DASHBOARD=true
export K6_WEB_DASHBOARD_PORT=5665
export K6_WEB_DASHBOARD_PERIOD=2s
export K6_WEB_DASHBOARD_OPEN=false
export K6_WEB_DASHBOARD_EXPORT='performance-report.html'

echo "1. Validação dos endpoints..."
k6 run endpoints-validation.js

echo "2. Teste progressivo (1k a 10k usuários)..."
k6 run progressive-test.js

echo "3. Teste de carga padrão Rinha (550 usuários)..."
k6 run -e MAX_REQUESTS=550 rinha.js

echo "4. Teste de alta carga (1000 usuários)..."
k6 run -e MAX_REQUESTS=1000 rinha.js

echo "5. Teste target competição (p99 < 1.3ms)..."
k6 run -e MAX_REQUESTS=2000 rinha.js

echo ""
echo "🎉 TESTES COMPLETOS!"
echo "==================="
echo "📊 Resultados em: load-test-results.json"
echo "📈 Dashboard HTML: performance-report.html"
echo "🌐 Dashboard ao vivo: http://localhost:5665"
