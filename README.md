# Rinha Backend 2025 - .NET Ultra Performance

## 🚀 Stack Tecnológica

### Core
- **Linguagem**: C# (.NET 8.0)
- **Framework**: ASP.NET Core com Minimal APIs
- **Runtime**: Kestrel Server (ultra-otimizado)

### Persistência
- **Database**: SQLite com WAL mode
- **Cache**: In-Memory caching híbrido
- **Data Access**: ADO.NET direto (zero overhead)

### Arquitetura
- **Load Balancer**: Nginx (2 instâncias API)
- **Messaging**: Bounded Channels (pipeline assíncrono)
- **Resiliência**: Circuit Breakers nativos
- **Concorrência**: Background workers paralelos

### Containerização
- **Container**: Docker multi-stage build
- **Orquestração**: Docker Compose
- **Registry**: GitHub Container Registry

## 🎯 Arquitetura de Alta Performance

### Pipeline Assíncrono
```
POST /payments → Queue (Bounded Channel) → Background Workers → Database
                    ↓ (Response imediata sub-ms)
                  HTTP 200 OK
```

### Estratégia de Negócio
1. **Default Processor** (menor taxa) → Tentativa prioritária
2. **Circuit Breaker** → Proteção contra falhas
3. **Fallback Processor** → Backup automático
4. **Cache híbrido** → GET /payments-summary otimizado

### Otimizações Implementadas
- **Kestrel**: 50k conexões simultâneas
- **HTTP Clients**: Connection pooling agressivo
- **Database**: WAL mode + índices otimizados
- **Memory**: Object pooling + GC tuning
- **JSON**: Serialização ultra-rápida

## 📊 Performance Targets

- **Throughput**: >10k RPS sustentado
- **Latência**: p99 < 1.3ms (target competição)
- **Recursos**: 1.5 CPU + 350MB RAM (conforme regras)
- **Disponibilidade**: >99.9% com circuit breakers

## 🐳 Como Executar

### Pré-requisitos
```bash
# 1. Subir Payment Processors (obrigatório primeiro)
git clone https://github.com/zanfranceschi/rinha-de-backend-2025
cd rinha-de-backend-2025/payment-processor
docker compose up -d
```

### Executar Backend
```bash
# 2. Clonar este repositório
git clone https://github.com/josehelioaraujo/rinha-backend-2025-dotnet
cd rinha-backend-2025-dotnet

# 3. Subir backend (aguarda rede payment-processor)
docker compose up -d

# 4. Verificar funcionamento
curl http://localhost:9999/
curl http://localhost:9999/payments-summary
```

### Teste de Carga
```bash
# Instalar k6
sudo apt install k6

# Executar teste Rinha
k6 run -e MAX_REQUESTS=550 rinha.js
```

## 🏗️ Estrutura do Projeto

```
├── src/RinhaBackend2025/          # Código fonte .NET
│   ├── Models/                    # DTOs e entidades
│   ├── Services/                  # Lógica de negócio
│   ├── Extensions/                # Configurações DI
│   └── Program.cs                 # Entry point
├── docker/
│   └── nginx.conf                 # Load balancer config
├── docker-compose.yml             # Orquestração
└── README.md                      # Este arquivo
```

## 🔗 Links

- **Código Fonte**: https://github.com/josehelioaraujo/rinha-backend-2025-dotnet
- **Container Image**: ghcr.io/josehelioaraujo/rinha-backend-2025-dotnet
- **Rinha de Backend**: https://github.com/zanfranceschi/rinha-de-backend-2025

## 🏆 Diferenciais Técnicos

1. **Pipeline Assíncrono**: Response imediata + processamento em background
2. **Circuit Breakers Nativos**: Zero dependências externas
3. **Bounded Channels**: Backpressure automático
4. **Cache Híbrido**: Invalidação inteligente
5. **SQLite WAL**: Performance + consistência ACID
6. **Kestrel Tuning**: Configurações extremas de performance

Desenvolvido com foco na **máxima performance** e **resiliência** para a Rinha de Backend 2025! 🚀

**Autor**: Hélio Andrade  
**GitHub**: https://github.com/josehelioaraujo  
**LinkedIn**: https://www.linkedin.com/in/helio-andrade-b8b5517
