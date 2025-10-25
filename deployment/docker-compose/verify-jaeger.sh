#!/bin/bash
# =============================================================================
# Jaeger Distributed Tracing - Verification Script
# =============================================================================
# 
# This script verifies that Jaeger is running and can receive traces from
# microservices.
#
# Usage:
#   ./verify-jaeger.sh
#
# Prerequisites:
#   - Docker Compose services running (docker compose up -d)
#   - curl and jq installed
#
# =============================================================================

set -e

echo "========================================="
echo "Jaeger Tracing Verification"
echo "========================================="
echo ""

# Color codes
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Check if Jaeger container is running
echo "1. Checking Jaeger container status..."
if docker ps --filter "name=coding-agent-jaeger" --format "{{.Status}}" | grep -q "healthy"; then
    echo -e "${GREEN}✓${NC} Jaeger container is running and healthy"
else
    echo -e "${RED}✗${NC} Jaeger container is not healthy"
    echo "   Run: docker compose up -d jaeger"
    exit 1
fi
echo ""

# Check Jaeger health endpoint
echo "2. Checking Jaeger health endpoint..."
HEALTH_STATUS=$(curl -s http://localhost:14269/ | grep -o '"status":"[^"]*"' | cut -d'"' -f4)
if [ "$HEALTH_STATUS" = "Server available" ]; then
    echo -e "${GREEN}✓${NC} Jaeger health endpoint responding"
else
    echo -e "${RED}✗${NC} Jaeger health endpoint not responding"
    exit 1
fi
echo ""

# Check Jaeger UI
echo "3. Checking Jaeger UI..."
UI_TITLE=$(curl -s http://localhost:16686/ | grep -o "<title>.*</title>")
if [[ "$UI_TITLE" == *"Jaeger UI"* ]]; then
    echo -e "${GREEN}✓${NC} Jaeger UI accessible at http://localhost:16686"
else
    echo -e "${RED}✗${NC} Jaeger UI not accessible"
    exit 1
fi
echo ""

# Check OTLP gRPC port
echo "4. Checking OTLP gRPC endpoint (port 4317)..."
if nc -z localhost 4317 2>/dev/null; then
    echo -e "${GREEN}✓${NC} OTLP gRPC endpoint listening on port 4317"
else
    echo -e "${YELLOW}⚠${NC} OTLP gRPC endpoint not reachable (this may be expected)"
fi
echo ""

# Check OTLP HTTP port
echo "5. Checking OTLP HTTP endpoint (port 4318)..."
if nc -z localhost 4318 2>/dev/null; then
    echo -e "${GREEN}✓${NC} OTLP HTTP endpoint listening on port 4318"
else
    echo -e "${YELLOW}⚠${NC} OTLP HTTP endpoint not reachable (this may be expected)"
fi
echo ""

# List services that have sent traces
echo "6. Checking for traces in Jaeger..."
SERVICES=$(curl -s "http://localhost:16686/api/services" | grep -o '"data":\[.*\]' | grep -o '\["[^"]*"' | tr -d '["')
if [ -n "$SERVICES" ]; then
    echo -e "${GREEN}✓${NC} Found traces from services:"
    echo "$SERVICES" | while read -r service; do
        [ -n "$service" ] && echo "   - $service"
    done
else
    echo -e "${YELLOW}⚠${NC} No traces found yet"
    echo "   Traces will appear after services start and handle requests"
fi
echo ""

# Summary
echo "========================================="
echo "Summary"
echo "========================================="
echo ""
echo "Jaeger Configuration:"
echo "  • UI:         http://localhost:16686"
echo "  • OTLP gRPC:  http://localhost:4317 (from host)"
echo "                http://jaeger:4317 (from containers)"
echo "  • OTLP HTTP:  http://localhost:4318 (from host)"
echo "                http://jaeger:4318 (from containers)"
echo ""
echo "To generate test traces:"
echo "  1. Start services: docker compose up -d"
echo "  2. Make requests to services (e.g., curl http://localhost:5000/health)"
echo "  3. View traces in Jaeger UI: http://localhost:16686"
echo ""
echo -e "${GREEN}✓${NC} Jaeger is ready to receive traces!"
echo ""
