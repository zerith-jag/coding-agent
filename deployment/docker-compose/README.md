# POC Testing & Validation - Quick Start

This guide helps you set up and run the POC integration testing and validation environment.

## 📋 Overview

The POC testing framework includes:
- **Docker Compose** development environment with all infrastructure services
- **Integration tests** for end-to-end validation
- **Load tests** (k6) for performance benchmarking
- **Validation report** framework for Go/No-Go decision

## 🚀 Quick Start

### 1. Prerequisites

Ensure you have the following installed:
- [Docker Desktop](https://www.docker.com/products/docker-desktop) (latest)
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [k6](https://k6.io/docs/get-started/installation/)
- [Git](https://git-scm.com/)

Verify installations:
```bash
docker --version
dotnet --version
k6 version
```

### 2. Clone Repository

```bash
git clone https://github.com/JustAGameZA/coding-agent.git
cd coding-agent
```

### 3. Start Infrastructure Services

Start PostgreSQL, Redis, RabbitMQ, and observability stack:

```bash
cd deployment/docker-compose
docker compose -f docker-compose.dev.yml up -d
```

Verify services are healthy:
```bash
docker compose -f docker-compose.dev.yml ps
```

Expected output (all services should show "healthy" or "running"):
```
NAME                      STATUS
coding-agent-postgres     Up (healthy)
coding-agent-redis        Up (healthy)
coding-agent-rabbitmq     Up (healthy)
coding-agent-seq          Up
coding-agent-prometheus   Up
coding-agent-grafana      Up
coding-agent-jaeger       Up
```

### 4. Access Services

| Service | URL | Credentials |
|---------|-----|-------------|
| RabbitMQ Management | http://localhost:15672 | dev / dev123 |
| Seq Logs | http://localhost:5341 | - |
| Prometheus | http://localhost:9090 | - |
| Grafana | http://localhost:3000 | admin / admin123 |
| Jaeger UI | http://localhost:16686 | - |

## 📦 Current Status

⚠️ **Important**: This is the infrastructure and testing framework for the POC. The actual application services (Gateway and Chat) are not yet implemented.

### What's Ready
- ✅ Docker Compose configuration
- ✅ Infrastructure services (PostgreSQL, Redis, RabbitMQ)
- ✅ Observability stack (Seq, Prometheus, Grafana, Jaeger)
- ✅ Integration test framework (xUnit + Testcontainers)
- ✅ Load test scripts (k6)
- ✅ POC validation report template

### What's Pending
- ⏳ Gateway service implementation (Phase 0, Week 2)
- ⏳ Chat service implementation (Phase 0, Week 2)
- ⏳ SharedKernel library creation
- ⏳ Actual test execution and validation

## 🧪 Testing (When Services Are Implemented)

### Integration Tests

```bash
cd tests/Integration.Tests
dotnet test
```

Run specific test categories:
```bash
# E2E tests only
dotnet test --filter Category=E2E

# With detailed output
dotnet test --logger "console;verbosity=detailed"
```

### Load Tests

```bash
cd tests/LoadTests
k6 run chat-service-load.js
```

Custom parameters:
```bash
# More users
k6 run --vus 100 --duration 10m chat-service-load.js

# Save results
k6 run --out json=results.json chat-service-load.js
```

## 📊 Validation Report

The POC validation report is located at:
```
docs/POC-VALIDATION-REPORT.md
```

Update this report after running tests with:
- Functional test results
- Performance metrics
- Go/No-Go recommendation

## 🛠️ Development Workflow

### For Service Developers

Once you implement the Gateway and Chat services:

1. **Update Dockerfiles**
   - Replace placeholder entrypoints with actual service code
   - Add proper build steps for .NET projects

2. **Start Application Services**
   ```bash
   docker compose -f docker-compose.dev.yml --profile full up -d
   ```

3. **Remove Test Skips**
   - Edit `tests/Integration.Tests/GatewayChatFlowTests.cs`
   - Remove `Skip` attributes from test methods

4. **Run Tests**
   ```bash
   cd tests/Integration.Tests
   dotnet test
   ```

5. **Run Load Tests**
   ```bash
   cd tests/LoadTests
   k6 run chat-service-load.js
   ```

6. **Update Validation Report**
   - Fill in actual metrics in `docs/POC-VALIDATION-REPORT.md`
   - Make Go/No-Go recommendation

## 🐛 Troubleshooting

### Services Won't Start

```bash
# Check Docker daemon
docker info

# View service logs
docker compose -f docker-compose.dev.yml logs

# Restart services
docker compose -f docker-compose.dev.yml restart
```

### Port Conflicts

If ports are already in use, edit `docker-compose.dev.yml` to use different ports:
```yaml
postgres:
  ports:
    - "5433:5432"  # Changed from 5432
```

### Reset Environment

```bash
# Stop and remove all containers, networks, and volumes
docker compose -f docker-compose.dev.yml down -v

# Start fresh
docker compose -f docker-compose.dev.yml up -d
```

## 📚 Documentation

- [Docker Compose Configuration](deployment/docker-compose/docker-compose.dev.yml)
- [Integration Tests README](tests/Integration.Tests/README.md)
- [Load Tests README](tests/LoadTests/README.md)
- [POC Validation Report](docs/POC-VALIDATION-REPORT.md)
- [Implementation Roadmap](docs/02-IMPLEMENTATION-ROADMAP.md)

## 🎯 Next Steps

Following the Phase 0, Week 2 plan:

1. **Days 1-2**: Implement Gateway Service POC
   - YARP reverse proxy
   - Health endpoints
   - OpenTelemetry integration

2. **Days 3-4**: Implement Chat Service POC
   - REST API endpoints
   - SignalR hub
   - PostgreSQL integration
   - Redis caching
   - RabbitMQ events

3. **Day 5**: Run validation tests
   - Execute integration tests
   - Run load tests
   - Measure performance
   - Update validation report
   - Make Go/No-Go decision

## 🤝 Contributing

When contributing to the POC:

1. Follow the [Copilot Chat Directives](../.github/copilot-instructions.md)
2. Use Conventional Commits (feat:, fix:, docs:, etc.)
3. Run tests before committing
4. Update documentation as needed

## 📞 Support

- **Issues**: [GitHub Issues](https://github.com/JustAGameZA/coding-agent/issues)
- **Discussions**: [GitHub Discussions](https://github.com/JustAGameZA/coding-agent/discussions)
- **Documentation**: [docs/](../docs/)

---

**Last Updated**: October 24, 2025  
**Phase**: Phase 0 - Architecture & POC  
**Status**: Infrastructure Ready, Awaiting Service Implementation
