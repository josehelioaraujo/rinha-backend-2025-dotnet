# Rinha Backend 2025 - .NET Ultra Performance

## Objetivo
Backend de alta performance para competicao Rinha de Backend 2025, focado em processamento de pagamentos com latencia sub-millisecond.

## Performance Targets
- **p99 Latency**: < 1.3ms (TOP 5 target)
- **Throughput**: > 10k RPS por instancia
- **Memory**: < 167MB por instancia
- **CPU**: < 0.7 por instancia

## Stack Tecnologica
- **.NET 8.0** com Minimal APIs
- **SQLite** com WAL mode + cache in-memory
- **Circuit Breaker** nativo customizado
- **Channel-based** pipeline assincrono
- **Nginx** load balancer
- **Docker** multi-stage build

## Quick Start

### Pre-requisitos
- WSL Ubuntu com Docker Engine
- .NET 8.0 SDK
- Git

### Build Local
```bash
# Clone do projeto
git clone <your-repo>
cd rinha-backend-2025-dotnet

# Build da imagem
docker build -t rinha-local:latest .

# Teste local (sem payment processors)
docker run -p 8080:8080 rinha-local:latest
curl http://localhost:8080
