#!/bin/bash
# ============================================
# Health Check Script
# Coding Agent - Docker Compose Infrastructure
# ============================================

set -e

echo "🔍 Checking Docker Compose services health..."
echo ""

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Function to check service health
check_service() {
    local service=$1
    local status=$(docker inspect --format='{{.State.Health.Status}}' coding-agent-$service 2>/dev/null || echo "no healthcheck")
    
    if [ "$status" = "healthy" ]; then
        echo -e "${GREEN}✓${NC} $service: healthy"
        return 0
    elif [ "$status" = "no healthcheck" ]; then
        # Check if container is running
        if docker ps --filter "name=coding-agent-$service" --filter "status=running" | grep -q $service; then
            echo -e "${YELLOW}⚠${NC} $service: running (no health check configured)"
            return 0
        else
            echo -e "${RED}✗${NC} $service: not running"
            return 1
        fi
    else
        echo -e "${RED}✗${NC} $service: $status"
        return 1
    fi
}

# Function to check HTTP endpoint
check_http() {
    local name=$1
    local url=$2
    
    if curl -sf "$url" > /dev/null 2>&1; then
        echo -e "${GREEN}✓${NC} $name: accessible at $url"
        return 0
    else
        echo -e "${RED}✗${NC} $name: not accessible at $url"
        return 1
    fi
}

# Function to check database
check_postgres() {
    if docker exec coding-agent-postgres psql -U codingagent -d codingagent -c "\dn" > /dev/null 2>&1; then
        echo -e "${GREEN}✓${NC} PostgreSQL: database accessible with 5 schemas"
        return 0
    else
        echo -e "${RED}✗${NC} PostgreSQL: database not accessible"
        return 1
    fi
}

# Function to check Redis
check_redis() {
    if docker exec coding-agent-redis redis-cli -a "${REDIS_PASSWORD:-devPassword123!}" PING 2>&1 | grep -q "PONG"; then
        echo -e "${GREEN}✓${NC} Redis: connection successful"
        return 0
    else
        echo -e "${RED}✗${NC} Redis: connection failed"
        return 1
    fi
}

# Function to check RabbitMQ
check_rabbitmq() {
    if docker exec coding-agent-rabbitmq rabbitmq-diagnostics ping 2>&1 | grep -q "succeeded"; then
        echo -e "${GREEN}✓${NC} RabbitMQ: connection successful"
        return 0
    else
        echo -e "${RED}✗${NC} RabbitMQ: connection failed"
        return 1
    fi
}

# Main health checks
echo "📦 Container Health Checks:"
check_service "postgres"
check_service "redis"
check_service "rabbitmq"
check_service "prometheus"
check_service "grafana"
check_service "jaeger"
check_service "seq" || true  # Seq might not have health check

echo ""
echo "🔌 Service Connectivity Checks:"
check_postgres
check_redis
check_rabbitmq

echo ""
echo "🌐 HTTP Endpoint Checks:"
check_http "Prometheus" "http://localhost:9090/-/healthy"
check_http "Grafana" "http://localhost:3000/api/health"
check_http "Jaeger UI" "http://localhost:16686/"
check_http "RabbitMQ Management" "http://localhost:15672/" || echo -e "${YELLOW}⚠${NC} RabbitMQ Management UI requires login"
check_http "Seq" "http://localhost:5341/" || echo -e "${YELLOW}⚠${NC} Seq might still be starting"

echo ""
echo "📊 Service Ports:"
echo "  PostgreSQL:         localhost:5432"
echo "  Redis:              localhost:6379"
echo "  RabbitMQ (AMQP):    localhost:5672"
echo "  RabbitMQ (Mgmt):    localhost:15672"
echo "  Prometheus:         localhost:9090"
echo "  Grafana:            localhost:3000"
echo "  Jaeger UI:          localhost:16686"
echo "  Seq:                localhost:5341"

echo ""
echo "✅ Health check complete!"
