# 🚀 Docker Compose Deployment Guide

Production-ready Docker Compose configuration for the Coding Agent microservices platform.

## 📋 Table of Contents

- [Overview](#overview)
- [Prerequisites](#prerequisites)
- [Quick Start](#quick-start)
- [Services](#services)
- [Configuration](#configuration)
- [Health Checks](#health-checks)
- [Monitoring & Observability](#monitoring--observability)
- [Troubleshooting](#troubleshooting)
- [Production Considerations](#production-considerations)

## 🎯 Overview

This Docker Compose setup provides all infrastructure services required for the Coding Agent platform:

- **Database**: PostgreSQL 16 with pre-configured schemas
- **Cache**: Redis 7 with persistence
- **Messaging**: RabbitMQ 3.12 with management UI
- **Observability**: Prometheus, Grafana, Jaeger, Seq

## 📦 Prerequisites

### Required Software

- **Docker Desktop** 4.25+ or **Docker Engine** 24+
- **Docker Compose** 2.23+ (included with Docker Desktop)
- **Git** for cloning the repository
- **4GB+ RAM** available for containers
- **10GB+ Disk Space** for volumes

### Operating System

- ✅ Windows 10/11 with WSL2
- ✅ macOS 12+ (Intel or Apple Silicon)
- ✅ Linux (Ubuntu 20.04+, Debian 11+, etc.)

### Verification

```bash
# Check Docker version
docker --version
# Should be: Docker version 24.0.0 or higher

# Check Docker Compose version
docker compose version
# Should be: Docker Compose version 2.23.0 or higher

# Verify Docker is running
docker ps
# Should show: CONTAINER ID, IMAGE, COMMAND, etc. (may be empty)
```

## 🚀 Quick Start

### 1. Clone Repository

```bash
git clone https://github.com/JustAGameZA/coding-agent.git
cd coding-agent/deployment/docker-compose
```

### 2. Configure Environment

```bash
# Copy example environment file
cp .env.example .env

# Edit .env with your configuration
# IMPORTANT: Change default passwords!
nano .env  # or use your preferred editor
```

### 3. Start Services

```bash
# Start all infrastructure services
docker compose up -d

# View logs
docker compose logs -f

# Check service status
docker compose ps
```

### 4. Verify Services

All services should show status as "healthy" after ~30 seconds:

```bash
docker compose ps

# Expected output:
# NAME                       STATUS              PORTS
# coding-agent-postgres      Up (healthy)        0.0.0.0:5432->5432/tcp
# coding-agent-redis         Up (healthy)        0.0.0.0:6379->6379/tcp
# coding-agent-rabbitmq      Up (healthy)        0.0.0.0:5672->5672/tcp, 0.0.0.0:15672->15672/tcp
# coding-agent-prometheus    Up (healthy)        0.0.0.0:9090->9090/tcp
# coding-agent-grafana       Up (healthy)        0.0.0.0:3000->3000/tcp
# coding-agent-jaeger        Up (healthy)        multiple ports
# coding-agent-seq           Up (healthy)        0.0.0.0:5341->80/tcp
```

### 5. Access Services

| Service | URL | Credentials |
|---------|-----|-------------|
| **PostgreSQL** | `localhost:5432` | user: `codingagent`<br>password: `devPassword123!` |
| **Redis** | `localhost:6379` | password: `devPassword123!` |
| **RabbitMQ Management** | http://localhost:15672 | user: `codingagent`<br>password: `devPassword123!` |
| **Grafana** | http://localhost:3000 | user: `admin`<br>password: `admin` |
| **Prometheus** | http://localhost:9090 | No auth |
| **Jaeger UI** | http://localhost:16686 | No auth |
| **Seq** | http://localhost:5341 | No auth (first run) |

## 🏗️ Services

### PostgreSQL Database

**Purpose**: Primary data store for all microservices

**Schemas**:
- `chat` - Conversations, messages, attachments
- `orchestration` - Tasks, executions, results
- `github` - Repositories, pull requests, issues
- `cicd` - Workflow runs, build jobs, deployments
- `auth` - Users, roles, permissions

**Connection String**:
```
Host=localhost;Port=5432;Database=codingagent;Username=codingagent;Password=devPassword123!
```

**Management Tools**:
```bash
# Connect via psql
docker exec -it coding-agent-postgres psql -U codingagent -d codingagent

# List schemas
\dn

# List tables in a schema
\dt chat.*

# View table structure
\d chat.conversations
```

### Redis Cache

**Purpose**: High-performance caching and session storage

**Features**:
- Persistence enabled (AOF)
- Password-protected
- Connection pooling ready

**Connection String**:
```
localhost:6379,password=devPassword123!
```

**Management**:
```bash
# Connect to Redis CLI
docker exec -it coding-agent-redis redis-cli -a devPassword123!

# Test connection
PING
# Should return: PONG

# View cache keys
KEYS *

# Monitor real-time commands
MONITOR
```

### RabbitMQ Message Queue

**Purpose**: Asynchronous communication between microservices

**Features**:
- Management UI enabled
- Default vhost configured
- Ready for MassTransit integration

**Ports**:
- `5672` - AMQP protocol
- `15672` - Management UI

**Management UI**: http://localhost:15672
- Create exchanges, queues, bindings
- Monitor message rates
- View connection statistics

### Prometheus Metrics

**Purpose**: Metrics collection and storage

**Features**:
- 30-day retention
- Pre-configured service discovery
- Ready for Grafana integration

**URL**: http://localhost:9090

**Example Queries**:
```promql
# HTTP request rate
rate(http_requests_total[5m])

# 95th percentile response time
histogram_quantile(0.95, rate(http_request_duration_seconds_bucket[5m]))

# Memory usage by service
container_memory_usage_bytes{service=~".*-service"}
```

### Grafana Dashboards

**Purpose**: Visualization and alerting

**Features**:
- Pre-configured Prometheus datasource
- Jaeger integration for traces
- Import/export dashboards

**URL**: http://localhost:3000

**First-Time Setup**:
1. Login with `admin/admin`
2. Change password (prompted)
3. Navigate to Dashboards → Import
4. Import dashboard by ID or JSON

**Recommended Dashboards**:
- Node Exporter Full (ID: 1860)
- Docker Monitoring (ID: 893)
- RabbitMQ Overview (ID: 10991)
- PostgreSQL Database (ID: 9628)

### Jaeger Tracing

**Purpose**: Distributed tracing across microservices

**Features**:
- OpenTelemetry compatible
- OTLP gRPC and HTTP endpoints
- In-memory storage (all-in-one deployment)
- Distributed trace correlation with correlation IDs

**URL**: http://localhost:16686

**OTLP Endpoints**:
- gRPC: `http://localhost:4317` (from host) or `http://jaeger:4317` (from containers)
- HTTP: `http://localhost:4318` (from host) or `http://jaeger:4318` (from containers)

**Usage in .NET Services**:

All services are pre-configured to send traces to Jaeger via OTLP gRPC. The endpoint is configurable in `appsettings.json`:

```json
{
  "OpenTelemetry": {
    "Endpoint": "http://jaeger:4317"
  }
}
```

Override at runtime with environment variables:
```bash
-e OpenTelemetry__Endpoint=http://jaeger:4317
```

**Verifying Traces**:

1. **Quick verification using the provided script**:
   ```bash
   cd deployment/docker-compose
   ./verify-jaeger.sh
   ```

2. **Manual verification steps**:

   a. **Start all infrastructure services**:
   ```bash
   docker compose up -d
   ```

   b. **Wait for services to be healthy** (~30 seconds):
   ```bash
   docker compose ps
   # All services should show "Up (healthy)"
   ```

   c. **Access Jaeger UI**: http://localhost:16686

   d. **Generate test traces** by making requests to services:
   ```bash
   # Via Gateway (recommended - shows full trace)
   curl http://localhost:5000/api/chat/ping
   curl http://localhost:5000/api/orchestration/ping
   
   # Direct to services
   curl http://localhost:5001/health  # Chat service
   curl http://localhost:5002/health  # Orchestration service
   ```

   e. **View traces in Jaeger UI**:
   - Select service from dropdown (e.g., "CodingAgent.Gateway")
   - Click "Find Traces"
   - Click on a trace to see the full span timeline
   - Verify correlation IDs propagate across services (look for `X-Correlation-Id` tag)

**Troubleshooting**:

- **No traces appearing**: 
  - Check service logs: `docker compose logs gateway chat orchestration`
  - Verify Jaeger is healthy: `curl http://localhost:14269/`
  - Ensure OpenTelemetry endpoint is configured correctly in service appsettings
  
- **Traces missing correlation**: 
  - Verify Gateway is propagating correlation ID headers
  - Check that downstream services are instrumented with ASP.NET Core instrumentation

### Seq Structured Logging

**Purpose**: Centralized log aggregation and search

**Features**:
- Structured logging with full-text search
- Real-time log streaming
- Query language for filtering

**URL**: http://localhost:5341

**Ingestion**: `http://localhost:5342`

**Usage in .NET**:
```csharp
builder.Host.UseSerilog((context, configuration) =>
{
    configuration
        .WriteTo.Seq("http://seq:5341")
        .Enrich.WithProperty("Application", "ChatService");
});
```

## ⚙️ Configuration

### Environment Variables

Edit `.env` to customize configuration:

```bash
# Database
POSTGRES_USER=codingagent
POSTGRES_PASSWORD=your-secure-password

# Cache
REDIS_PASSWORD=your-redis-password

# Messaging
RABBITMQ_USER=codingagent
RABBITMQ_PASSWORD=your-rabbitmq-password

# Grafana (change default!)
GRAFANA_USER=admin
GRAFANA_PASSWORD=your-grafana-password
```

### Volume Management

**List volumes**:
```bash
docker volume ls | grep coding-agent
```

**Inspect volume**:
```bash
docker volume inspect coding-agent_postgres_data
```

**Backup volume**:
```bash
# Backup PostgreSQL data
docker run --rm \
  -v coding-agent_postgres_data:/data \
  -v $(pwd):/backup \
  alpine tar czf /backup/postgres-backup-$(date +%Y%m%d).tar.gz /data

# Backup Redis data
docker run --rm \
  -v coding-agent_redis_data:/data \
  -v $(pwd):/backup \
  alpine tar czf /backup/redis-backup-$(date +%Y%m%d).tar.gz /data
```

**Restore volume**:
```bash
# Stop services first
docker compose down

# Restore PostgreSQL
docker run --rm \
  -v coding-agent_postgres_data:/data \
  -v $(pwd):/backup \
  alpine sh -c "cd /data && tar xzf /backup/postgres-backup-YYYYMMDD.tar.gz --strip 1"

# Restart services
docker compose up -d
```

## 🏥 Health Checks

### Check All Services

```bash
# View health status
docker compose ps

# Check specific service health
docker inspect --format='{{.State.Health.Status}}' coding-agent-postgres
```

### Manual Health Checks

**PostgreSQL**:
```bash
docker exec coding-agent-postgres pg_isready -U codingagent
```

**Redis**:
```bash
docker exec coding-agent-redis redis-cli -a devPassword123! PING
```

**RabbitMQ**:
```bash
docker exec coding-agent-rabbitmq rabbitmq-diagnostics ping
```

**Prometheus**:
```bash
curl http://localhost:9090/-/healthy
```

**Grafana**:
```bash
curl http://localhost:3000/api/health
```

**Jaeger**:
```bash
curl http://localhost:14269/
```

## 📊 Monitoring & Observability

### Viewing Logs

```bash
# All services
docker compose logs -f

# Specific service
docker compose logs -f postgres
docker compose logs -f redis
docker compose logs -f rabbitmq

# Last 100 lines
docker compose logs --tail=100 postgres

# Since timestamp
docker compose logs --since 2024-10-24T10:00:00
```

### Resource Usage

```bash
# View container stats
docker stats

# Specific containers
docker stats coding-agent-postgres coding-agent-redis coding-agent-rabbitmq
```

### Metrics Endpoints

Once microservices are running, they will expose metrics:

- Gateway: http://localhost:5000/metrics
- Chat Service: http://localhost:5001/metrics
- Orchestration: http://localhost:5002/metrics
- ML Classifier: http://localhost:5003/metrics

## 🔧 Troubleshooting

### Services Won't Start

**Check logs**:
```bash
docker compose logs [service-name]
```

**Common issues**:
1. **Port already in use**: Change port in `.env` or `docker-compose.yml`
2. **Insufficient memory**: Increase Docker memory limit in Docker Desktop
3. **Volume permissions**: On Linux, ensure proper permissions

### PostgreSQL Connection Failed

```bash
# Check if container is running
docker compose ps postgres

# Check logs
docker compose logs postgres

# Test connection
docker exec -it coding-agent-postgres psql -U codingagent -d codingagent -c "SELECT version();"
```

### Redis Connection Failed

```bash
# Check if running
docker compose ps redis

# Test connection without password
docker exec -it coding-agent-redis redis-cli PING

# Test with password
docker exec -it coding-agent-redis redis-cli -a devPassword123! PING
```

### RabbitMQ Not Accessible

```bash
# Check status
docker compose ps rabbitmq

# View logs
docker compose logs rabbitmq

# Check ports
docker ps | grep rabbitmq

# Restart service
docker compose restart rabbitmq
```

### Health Checks Failing

```bash
# View detailed health check logs
docker inspect coding-agent-postgres | jq '.[0].State.Health'

# Increase health check interval
# Edit docker-compose.yml health check settings
```

### Reset Everything

```bash
# Stop and remove containers
docker compose down

# Remove volumes (WARNING: deletes all data!)
docker compose down -v

# Remove images
docker compose down --rmi all

# Clean start
docker compose up -d
```

## 🏭 Production Considerations

### Security

1. **Change default passwords** in `.env`
2. **Enable SSL/TLS** for external connections
3. **Use secrets management** (Docker Swarm secrets, Kubernetes secrets)
4. **Network isolation** with custom networks
5. **Regular security updates** of base images

### Backup Strategy

```bash
# Automated daily backup script
#!/bin/bash
DATE=$(date +%Y%m%d)
docker exec coding-agent-postgres pg_dump -U codingagent codingagent > backup-$DATE.sql
gzip backup-$DATE.sql
aws s3 cp backup-$DATE.sql.gz s3://your-bucket/backups/
```

### Resource Limits

Add resource limits in `docker-compose.yml`:

```yaml
services:
  postgres:
    deploy:
      resources:
        limits:
          cpus: '2'
          memory: 2G
        reservations:
          cpus: '1'
          memory: 1G
```

### Scaling

For production workloads, consider:
- **Kubernetes**: For orchestration and auto-scaling
- **Separate database servers**: Dedicated PostgreSQL clusters
- **Redis Cluster**: For high availability
- **RabbitMQ Cluster**: For message queue HA
- **Load balancing**: Multiple service instances

### Monitoring & Alerts

Configure Grafana alerts:
1. Navigate to Alerting → Alert rules
2. Create alerts for:
   - High memory usage (>80%)
   - High CPU usage (>80%)
   - Disk space low (<10%)
   - Service downtime
   - High error rates

### Maintenance

```bash
# Regular maintenance tasks

# Update images
docker compose pull

# Recreate containers with new images
docker compose up -d

# Prune unused resources
docker system prune -a --volumes

# Vacuum PostgreSQL
docker exec coding-agent-postgres psql -U codingagent -d codingagent -c "VACUUM ANALYZE;"

# Check Redis memory
docker exec coding-agent-redis redis-cli -a "$REDIS_PASSWORD" INFO memory
## 📚 Additional Resources

- [Docker Compose Documentation](https://docs.docker.com/compose/)
- [PostgreSQL Documentation](https://www.postgresql.org/docs/)
- [Redis Documentation](https://redis.io/documentation)
- [RabbitMQ Documentation](https://www.rabbitmq.com/documentation.html)
- [Prometheus Documentation](https://prometheus.io/docs/)
- [Grafana Documentation](https://grafana.com/docs/)
- [Jaeger Documentation](https://www.jaegertracing.io/docs/)

## 🆘 Getting Help

- **Documentation**: [docs/](../../docs)
- **Issues**: [GitHub Issues](https://github.com/JustAGameZA/coding-agent/issues)
- **Discussions**: [GitHub Discussions](https://github.com/JustAGameZA/coding-agent/discussions)

---

**Last Updated**: October 24, 2025  
**Version**: 1.0.0  
**Maintainer**: Coding Agent Team
