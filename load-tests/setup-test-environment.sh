#!/bin/bash
# Setup completo para testes de carga

set -e

echo "SETUP: Ambiente de Teste de Carga"
echo "================================="

PROJECT_ROOT="/root/rinha-backend-2025-dotnet"

echo "1. Parando containers existentes..."
docker stop $(docker ps -q) 2>/dev/null || echo "Nenhum container rodando"

echo "2. Buildando imagem atualizada..."
cd "$PROJECT_ROOT"
docker build -t rinha-backend-2025:latest .

echo "3. Subindo ambiente completo..."
# Backend em cluster (2 instâncias + nginx)
docker run -d --name api1 -p 8081:8080 rinha-backend-2025:latest
docker run -d --name api2 -p 8082:8080 rinha-backend-2025:latest

# Nginx básico para load balancing
docker run -d --name nginx-lb -p 9999:80 \
  -v "$PROJECT_ROOT/docker/nginx.conf:/etc/nginx/nginx.conf:ro" \
  nginx:alpine

echo "4. Aguardando inicialização..."
sleep 10

echo "5. Verificando health checks..."
curl -f http://localhost:9999/ && echo "✅ Nginx OK"
curl -f http://localhost:8081/ && echo "✅ API1 OK"  
curl -f http://localhost:8082/ && echo "✅ API2 OK"

echo "✅ Ambiente pronto para testes!"
