#!/bin/bash
# ============================================
# Alert Configuration Validation Script
# Coding Agent - Microservices Platform
# ============================================
# This script validates the alerting configuration files

set -e

echo "🔍 Validating Alert Configuration..."
echo ""

# Colors
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Check if files exist
echo "📁 Checking configuration files..."
FILES=(
    "alerts/api-alerts.yml"
    "alerts/infrastructure-alerts.yml"
    "alerts/messagebus-alerts.yml"
    "alertmanager.yml"
    "prometheus.yml"
    "docker-compose.yml"
    "grafana/provisioning/datasources/datasources.yml"
    "grafana/provisioning/dashboards/alerts-slos.json"
)

for file in "${FILES[@]}"; do
    if [ -f "$file" ]; then
        echo -e "  ${GREEN}✓${NC} $file exists"
    else
        echo -e "  ${RED}✗${NC} $file not found"
        exit 1
    fi
done

echo ""
echo "🔬 Validating YAML syntax..."

# Validate YAML files
YAML_FILES=(
    "alerts/api-alerts.yml"
    "alerts/infrastructure-alerts.yml"
    "alerts/messagebus-alerts.yml"
    "alertmanager.yml"
    "prometheus.yml"
)

for file in "${YAML_FILES[@]}"; do
    if python3 -c "import yaml; yaml.safe_load(open('$file'))" 2>/dev/null; then
        echo -e "  ${GREEN}✓${NC} $file - Valid YAML"
    else
        echo -e "  ${RED}✗${NC} $file - Invalid YAML"
        exit 1
    fi
done

echo ""
echo "🔬 Validating JSON syntax..."

# Validate JSON files
if python3 -c "import json; json.load(open('grafana/provisioning/dashboards/alerts-slos.json'))" 2>/dev/null; then
    echo -e "  ${GREEN}✓${NC} alerts-slos.json - Valid JSON"
else
    echo -e "  ${RED}✗${NC} alerts-slos.json - Invalid JSON"
    exit 1
fi

echo ""
echo "🔬 Validating Docker Compose..."

# Validate docker-compose
if docker compose config --quiet 2>/dev/null; then
    echo -e "  ${GREEN}✓${NC} docker-compose.yml - Valid configuration"
else
    echo -e "  ${RED}✗${NC} docker-compose.yml - Invalid configuration"
    exit 1
fi

echo ""
echo "📊 Alert Rule Summary..."

# Count alerts per file
for file in alerts/*.yml; do
    count=$(grep -c "^      - alert:" "$file" || true)
    filename=$(basename "$file")
    echo -e "  ${YELLOW}▸${NC} $filename: $count alerts"
done

echo ""
echo "📚 Runbook Summary..."

# Count runbooks
runbook_count=$(ls -1 ../../docs/runbooks/*.md 2>/dev/null | wc -l)
echo -e "  ${YELLOW}▸${NC} Total runbooks: $runbook_count"

echo ""
echo -e "${GREEN}✅ All validations passed!${NC}"
echo ""
echo "Next steps:"
echo "  1. Start the stack: docker compose up -d"
echo "  2. Access Prometheus: http://localhost:9090/alerts"
echo "  3. Access Alertmanager: http://localhost:9093"
echo "  4. Access Grafana: http://localhost:3000/d/alerts-slos"
echo ""
