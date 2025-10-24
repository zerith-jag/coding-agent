# 🐳 Docker Compose - Local Development & Staging

Production-grade Docker Compose configuration for local development and staging environments with comprehensive infrastructure services.

## 📋 Table of Contents

- [Overview](#overview)
- [Services](#services)
- [Prerequisites](#prerequisites)
- [Quick Start](#quick-start)
- [Configuration](#configuration)
- [Usage](#usage)
- [Backup & Restore](#backup--restore)
- [Service URLs](#service-urls)
- [Health Checks](#health-checks)
- [Troubleshooting](#troubleshooting)
- [Advanced Usage](#advanced-usage)

---

## 🎯 Overview

This Docker Compose setup provides all infrastructure services needed for the Coding Agent platform:

- **Database**: PostgreSQL 16 with automatic schema creation
- **Cache**: Redis 7 with persistence
- **Message Queue**: RabbitMQ 3.12 with management UI
- **Metrics**: Prometheus for metrics collection
- **Visualization**: Grafana with pre-configured dashboards
- **Tracing**: Jaeger for distributed tracing
- **Logging**: Seq for structured logging

**Key Features**:
- ✅ Health checks for all services
- ✅ Volume persistence across restarts
- ✅ Network isolation
- ✅ Environment-based configuration
- ✅ Development overrides
- ✅ Automated backups
- ✅ Easy startup/teardown

---

## 🛠️ Services

| Service | Version | Port(s) | Purpose |
|---------|---------|---------|---------|
| **PostgreSQL** | 16-alpine | 5432 | Primary database with per-service schemas |
| **Redis** | 7-alpine | 6379 | Caching and session storage |
| **RabbitMQ** | 3.12-management | 5672, 15672 | Message broker with management UI |
| **Prometheus** | latest | 9090 | Metrics collection and storage |
| **Grafana** | latest | 3000 | Metrics visualization and dashboards |
| **Jaeger** | latest | 16686, 4317, 4318 | Distributed tracing UI and collector |
| **Seq** | latest | 5341 | Structured logging and analysis |

---

## 📦 Prerequisites

1. **Docker Desktop** (v20.10+)
   - [Download for Windows](https://docs.docker.com/desktop/install/windows-install/)
   - [Download for Mac](https://docs.docker.com/desktop/install/mac-install/)
   - [Download for Linux](https://docs.docker.com/desktop/install/linux-install/)

2. **Docker Compose** (v2.0+)
   - Included with Docker Desktop
   - Verify: `docker compose version`

3. **PowerShell** (for backup/restore scripts)
   - Windows: Built-in
   - Mac/Linux: [Install PowerShell Core](https://docs.microsoft.com/en-us/powershell/scripting/install/installing-powershell)

4. **Minimum System Resources**:
   - RAM: 8 GB (16 GB recommended)
   - Disk: 20 GB free space
   - CPU: 4 cores

---

## 🚀 Quick Start

### 1. Clone the Repository

```bash
git clone https://github.com/JustAGameZA/coding-agent.git
cd coding-agent/deployment/docker-compose
```

### 2. Configure Environment (Optional)

```bash
# Copy environment template
cp ../../.env.example ../../.env

# Edit with your preferred values
# On Windows: notepad ../../.env
# On Mac/Linux: nano ../../.env
```

### 3. Start All Services

```bash
# Production-like mode
docker compose up -d

# Or with development overrides
docker compose -f docker-compose.yml -f docker-compose.dev.yml up -d
```

### 4. Verify Services

```bash
# Check service status
docker compose ps

# View logs
docker compose logs -f

# Check health
docker compose ps --format "table {{.Service}}\t{{.Status}}\t{{.Ports}}"
```

### 5. Access Services

Open your browser and navigate to:
- **Grafana**: http://localhost:3000 (admin/admin)
- **RabbitMQ Management**: http://localhost:15672 (rabbitmq/rabbitmq)
- **Jaeger UI**: http://localhost:16686
- **Prometheus**: http://localhost:9090
- **Seq**: http://localhost:5341

---

## ⚙️ Configuration

### Environment Variables

All services can be configured via environment variables in `.env` file:

```bash
# PostgreSQL
POSTGRES_USER=postgres          # Database user
POSTGRES_PASSWORD=changeme      # Database password (CHANGE THIS!)
POSTGRES_PORT=5432              # External port

# Redis
REDIS_PASSWORD=changeme         # Redis password (CHANGE THIS!)
REDIS_PORT=6379                 # External port

# RabbitMQ
RABBITMQ_USER=rabbitmq          # RabbitMQ user
RABBITMQ_PASSWORD=changeme      # RabbitMQ password (CHANGE THIS!)
RABBITMQ_PORT=5672              # AMQP port
RABBITMQ_MGMT_PORT=15672        # Management UI port

# Monitoring
PROMETHEUS_PORT=9090            # Prometheus UI port
GRAFANA_PORT=3000               # Grafana UI port
GRAFANA_PASSWORD=admin          # Grafana admin password
SEQ_PORT=5341                   # Seq UI port
```

### PostgreSQL Schemas

The following schemas are automatically created on first startup:

- `chat` - Chat service data
- `orchestration` - Task orchestration data
- `github` - GitHub integration data
- `cicd` - CI/CD monitoring data
- `auth` - Authentication data

Initialization script: `init-scripts/01-init-schemas.sql`

---

## 📖 Usage

### Start Services

```bash
# Start all services (detached mode)
docker compose up -d

# Start with development overrides
docker compose -f docker-compose.yml -f docker-compose.dev.yml up -d

# Start specific services only
docker compose up -d postgres redis rabbitmq

# Start with logs visible
docker compose up
```

### Stop Services

```bash
# Stop all services (preserves volumes)
docker compose down

# Stop and remove volumes (DESTRUCTIVE!)
docker compose down -v

# Stop specific service
docker compose stop postgres
```

### View Logs

```bash
# All services
docker compose logs -f

# Specific service
docker compose logs -f postgres

# Last 100 lines
docker compose logs --tail=100 redis

# Since timestamp
docker compose logs --since 2024-01-01T00:00:00
```

### Restart Services

```bash
# Restart all services
docker compose restart

# Restart specific service
docker compose restart rabbitmq
```

### Service Health

```bash
# Check health status
docker compose ps

# Inspect specific service
docker inspect coding-agent-postgres

# Execute health check manually
docker compose exec postgres pg_isready -U postgres
docker compose exec redis redis-cli ping
docker compose exec rabbitmq rabbitmq-diagnostics -q ping
```

---

## 💾 Backup & Restore

### Backup

Create backups of all data volumes:

```powershell
# Run backup script
cd scripts
./backup.ps1
```

**What gets backed up**:
- PostgreSQL: Full database dump (all schemas)
- Redis: RDB snapshot
- RabbitMQ: Queue definitions and configuration

Backups are stored in `deployment/docker-compose/backups/` with timestamp:
- `postgres-YYYYMMDD-HHMMSS.sql`
- `redis-YYYYMMDD-HHMMSS.rdb`
- `rabbitmq-YYYYMMDD-HHMMSS.json`

**Automatic cleanup**: Backups older than 30 days are automatically removed.

### Restore

Restore from a specific backup:

```powershell
# Run restore script
cd scripts
./restore.ps1 -BackupTimestamp "20251024-120000"
```

**⚠️ Warning**: This will overwrite current data!

---

## 🌐 Service URLs

### Management UIs

| Service | URL | Default Credentials |
|---------|-----|-------------------|
| **Grafana** | http://localhost:3000 | admin / admin |
| **RabbitMQ Management** | http://localhost:15672 | rabbitmq / rabbitmq |
| **Jaeger UI** | http://localhost:16686 | - |
| **Prometheus** | http://localhost:9090 | - |
| **Seq** | http://localhost:5341 | - |

### Service Endpoints

| Service | Connection String | Notes |
|---------|------------------|-------|
| **PostgreSQL** | `Host=localhost;Port=5432;Database=coding_agent;Username=postgres;Password=postgres` | Use schema-qualified names |
| **Redis** | `localhost:6379,password=redis` | Requires AUTH |
| **RabbitMQ** | `amqp://rabbitmq:rabbitmq@localhost:5672/coding-agent` | Virtual host: `coding-agent` |

---

## 🏥 Health Checks

All services have health checks configured:

### PostgreSQL
```bash
docker compose exec postgres pg_isready -U postgres
# Expected: "postgres:5432 - accepting connections"
```

### Redis
```bash
docker compose exec redis redis-cli --no-auth-warning -a redis ping
# Expected: "PONG"
```

### RabbitMQ
```bash
docker compose exec rabbitmq rabbitmq-diagnostics -q ping
# Expected: "Ping succeeded"
```

### Check All Health Statuses
```bash
docker compose ps --format "table {{.Service}}\t{{.Status}}"
```

---

## 🔧 Troubleshooting

### Services Won't Start

**Issue**: Services fail to start or are unhealthy

```bash
# Check logs for errors
docker compose logs postgres
docker compose logs redis
docker compose logs rabbitmq

# Check disk space
docker system df

# Clean up unused resources
docker system prune -a --volumes
```

### Port Conflicts

**Issue**: "Port already in use" error

```bash
# Find process using the port (Windows)
netstat -ano | findstr :5432

# Find process using the port (Mac/Linux)
lsof -i :5432

# Change port in .env file
POSTGRES_PORT=5433  # Use different port
```

### PostgreSQL Connection Issues

**Issue**: Cannot connect to PostgreSQL

```bash
# Check if PostgreSQL is running
docker compose ps postgres

# Check logs
docker compose logs postgres

# Test connection from container
docker compose exec postgres psql -U postgres -d coding_agent -c "\dn"

# Test connection from host
psql -h localhost -p 5432 -U postgres -d coding_agent
```

### Redis Connection Issues

**Issue**: Redis authentication failures

```bash
# Check if Redis is running
docker compose ps redis

# Test connection without auth (if password removed)
docker compose exec redis redis-cli ping

# Test connection with auth
docker compose exec redis redis-cli -a redis ping

# Check Redis logs
docker compose logs redis
```

### RabbitMQ Issues

**Issue**: RabbitMQ management UI not accessible

```bash
# Check if RabbitMQ is running
docker compose ps rabbitmq

# Check logs
docker compose logs rabbitmq

# Verify management plugin is enabled
docker compose exec rabbitmq rabbitmq-plugins list

# Restart RabbitMQ
docker compose restart rabbitmq
```

### Volume Permission Issues

**Issue**: Permission denied errors in logs

```bash
# On Linux, fix volume permissions
sudo chown -R 999:999 postgres_data/
sudo chown -R 1000:1000 redis_data/

# Or reset volumes completely (DESTRUCTIVE!)
docker compose down -v
docker compose up -d
```

### Out of Memory

**Issue**: Services crashing due to memory

```bash
# Check memory usage
docker stats

# Increase Docker Desktop memory:
# Docker Desktop → Settings → Resources → Memory → 8GB+
```

---

## 🚀 Advanced Usage

### Run Specific Service Combination

```bash
# Only database and cache
docker compose up -d postgres redis

# Only monitoring stack
docker compose up -d prometheus grafana jaeger

# Everything except observability
docker compose up -d postgres redis rabbitmq
```

### Connect from Application Services

**Example: .NET Service Connection Strings**

```csharp
// appsettings.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=postgres;Port=5432;Database=coding_agent;Username=postgres;Password=postgres;Search Path=chat"
  },
  "Redis": {
    "Configuration": "redis:6379,password=redis"
  },
  "RabbitMQ": {
    "Host": "rabbitmq",
    "VirtualHost": "coding-agent",
    "Username": "rabbitmq",
    "Password": "rabbitmq"
  }
}
```

**Note**: When services are in the same Docker network, use service names (e.g., `postgres`, `redis`) instead of `localhost`.

### Scale Services

```bash
# Not applicable for stateful services (postgres, redis)
# But can run multiple instances of custom services

# Example: Run 3 instances of a custom service
docker compose up -d --scale custom-service=3
```

### Network Inspection

```bash
# List networks
docker network ls

# Inspect backend network
docker network inspect docker-compose_backend

# See which containers are connected
docker network inspect docker-compose_backend --format='{{range .Containers}}{{.Name}} {{end}}'
```

### Volume Management

```bash
# List volumes
docker volume ls

# Inspect volume
docker volume inspect docker-compose_postgres_data

# Backup volume (alternative method)
docker run --rm -v docker-compose_postgres_data:/data -v $(pwd)/backups:/backup alpine tar czf /backup/postgres-manual.tar.gz /data

# Restore volume (alternative method)
docker run --rm -v docker-compose_postgres_data:/data -v $(pwd)/backups:/backup alpine tar xzf /backup/postgres-manual.tar.gz -C /
```

### Custom Configuration

#### Add PostgreSQL Extension

Create `init-scripts/02-extensions.sql`:

```sql
-- Enable extensions
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pg_trgm";
CREATE EXTENSION IF NOT EXISTS "btree_gin";
```

#### Modify Prometheus Scrape Targets

Edit `prometheus/prometheus.yml` to add your services:

```yaml
scrape_configs:
  - job_name: 'my-custom-service'
    static_configs:
      - targets: ['my-service:5000']
    metrics_path: '/metrics'
```

#### Add Grafana Dashboard

1. Place JSON dashboard file in `grafana/dashboards/`
2. Restart Grafana: `docker compose restart grafana`
3. Dashboard will auto-load on startup

---

## 📊 Monitoring & Observability

### Prometheus Metrics

Access Prometheus at http://localhost:9090

**Useful Queries**:
```promql
# Check service availability
up

# Memory usage by service
container_memory_usage_bytes

# CPU usage
rate(container_cpu_usage_seconds_total[5m])
```

### Grafana Dashboards

Access Grafana at http://localhost:3000 (admin/admin)

**Pre-configured Dashboards**:
- System Overview - General health and status

**Import Additional Dashboards**:
1. Go to Dashboards → Import
2. Enter Dashboard ID from [Grafana Dashboard Library](https://grafana.com/grafana/dashboards/)
3. Recommended IDs:
   - PostgreSQL: 9628
   - Redis: 11835
   - RabbitMQ: 10991

### Jaeger Tracing

Access Jaeger at http://localhost:16686

**OTLP Endpoints**:
- gRPC: `http://localhost:4317`
- HTTP: `http://localhost:4318`

### Seq Logging

Access Seq at http://localhost:5341

**Send logs via HTTP**:
```bash
curl -X POST http://localhost:5341/api/events/raw \
  -H "Content-Type: application/vnd.serilog.clef" \
  -d '{"@t":"2024-01-01T00:00:00Z","@mt":"Hello, {Name}!","Name":"World"}'
```

---

## 🧪 Testing

### Test PostgreSQL Connection

```bash
# From host
docker compose exec postgres psql -U postgres -d coding_agent -c "\dn"

# Expected output: List of schemas (chat, orchestration, github, cicd, auth)
```

### Test Redis

```bash
# Ping test
docker compose exec redis redis-cli --no-auth-warning -a redis ping
# Expected: PONG

# Set/Get test
docker compose exec redis redis-cli --no-auth-warning -a redis SET test "Hello"
docker compose exec redis redis-cli --no-auth-warning -a redis GET test
# Expected: "Hello"
```

### Test RabbitMQ

```bash
# Check vhost
docker compose exec rabbitmq rabbitmqctl list_vhosts
# Expected: coding-agent should be listed

# Check queues (will be empty initially)
docker compose exec rabbitmq rabbitmqctl list_queues -p coding-agent
```

### Automated Test Script

```bash
# Create test script
cat > test-services.sh << 'EOF'
#!/bin/bash
echo "Testing PostgreSQL..."
docker compose exec -T postgres psql -U postgres -d coding_agent -c "SELECT 1" || exit 1

echo "Testing Redis..."
docker compose exec -T redis redis-cli --no-auth-warning -a redis ping || exit 1

echo "Testing RabbitMQ..."
docker compose exec -T rabbitmq rabbitmq-diagnostics -q ping || exit 1

echo "All tests passed!"
EOF

chmod +x test-services.sh
./test-services.sh
```

---

## 🔒 Security Notes

### Production Deployment

**⚠️ Before deploying to production**:

1. **Change all default passwords** in `.env`:
   ```bash
   POSTGRES_PASSWORD=<strong-random-password>
   REDIS_PASSWORD=<strong-random-password>
   RABBITMQ_PASSWORD=<strong-random-password>
   GRAFANA_PASSWORD=<strong-random-password>
   ```

2. **Use secrets management**:
   - Consider Docker Secrets or HashiCorp Vault
   - Don't commit `.env` to version control

3. **Restrict network access**:
   - Remove port mappings for internal services
   - Use reverse proxy for external access

4. **Enable TLS/SSL**:
   - Configure SSL for PostgreSQL
   - Use HTTPS for Grafana
   - Enable TLS for RabbitMQ

5. **Regular backups**:
   - Automate backup script with cron/Task Scheduler
   - Store backups in secure location (e.g., S3, Azure Blob)

---

## 📚 Additional Resources

- [Docker Compose Documentation](https://docs.docker.com/compose/)
- [PostgreSQL Documentation](https://www.postgresql.org/docs/)
- [Redis Documentation](https://redis.io/documentation)
- [RabbitMQ Documentation](https://www.rabbitmq.com/documentation.html)
- [Prometheus Documentation](https://prometheus.io/docs/)
- [Grafana Documentation](https://grafana.com/docs/)
- [Jaeger Documentation](https://www.jaegertracing.io/docs/)

---

## 🤝 Contributing

Found an issue or have a suggestion? Please:
1. Check existing issues
2. Open a new issue with details
3. Submit a PR with improvements

---

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](../../LICENSE) file for details.

---

**Last Updated**: October 24, 2025  
**Maintained By**: Coding Agent Team
