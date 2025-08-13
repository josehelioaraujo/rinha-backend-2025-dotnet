#!/bin/bash
set -e

echo "SETUP: Ambiente de Produção (Nginx + 2 APIs)"
echo "============================================="

PROJECT_ROOT="/root/rinha-backend-2025-dotnet"

echo "1. Parando containers existentes..."
docker stop $(docker ps -q) 2>/dev/null || echo "Nenhum container rodando"

echo "2. Buildando imagem otimizada..."
cd "$PROJECT_ROOT"
docker build -t rinha-production:latest .

echo "3. Criando rede backend..."
docker network create rinha-backend 2>/dev/null || echo "Rede já existe"

echo "4. Subindo 2 instâncias da API..."
docker run -d --name api1 --network rinha-backend \
  -e INSTANCE_ID=API-1 \
  rinha-production:latest

docker run -d --name api2 --network rinha-backend \
  -e INSTANCE_ID=API-2 \
  rinha-production:latest

echo "5. Configurando nginx.conf otimizado..."
cat > /tmp/nginx-prod.conf << 'NGINX_EOF'
events {
    worker_connections 2048;
    use epoll;
    multi_accept on;
    worker_rlimit_nofile 4096;
}

http {
    # Performance extrema
    sendfile on;
    tcp_nopush on;
    tcp_nodelay on;
    keepalive_timeout 15;
    keepalive_requests 1000;
    
    # Upstream otimizado
    upstream backend {
        least_conn;
        server api1:8080 max_fails=2 fail_timeout=5s;
        server api2:8080 max_fails=2 fail_timeout=5s;
        keepalive 64;
    }
    
    server {
        listen 80;
        server_tokens off;
        
        # Otimizações de performance
        location / {
            proxy_pass http://backend;
            proxy_http_version 1.1;
            proxy_set_header Connection "";
            proxy_set_header Host $host;
            proxy_set_header X-Real-IP $remote_addr;
            
            # Timeouts agressivos
            proxy_connect_timeout 1s;
            proxy_send_timeout 2s;
            proxy_read_timeout 2s;
            
            # Buffer otimizações
            proxy_buffering off;
            proxy_request_buffering off;
        }
    }
}
NGINX_EOF

echo "6. Subindo nginx load balancer..."
docker run -d --name nginx-lb --network rinha-backend \
  -p 9999:80 \
  -v /tmp/nginx-prod.conf:/etc/nginx/nginx.conf:ro \
  nginx:alpine

echo "7. Aguardando inicialização..."
sleep 10

echo "8. Verificando health checks..."
for i in {1..30}; do
  if curl -f http://localhost:9999/ > /dev/null 2>&1; then
    echo "✅ Ambiente pronto!"
    break
  fi
  echo "Aguardando... ($i/30)"
  sleep 2
done

echo "✅ AMBIENTE DE PRODUÇÃO ATIVO!"
echo "================================"
echo "- 2 instâncias API"
echo "- Nginx load balancer"
echo "- Porta 9999 ativa"
