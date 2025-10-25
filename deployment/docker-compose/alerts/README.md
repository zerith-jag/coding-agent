# Alerting Configuration Guide

This document describes the alerting infrastructure for the Coding Agent microservices platform.

## Overview

The alerting system consists of:
- **Prometheus** - Metrics collection and alert rule evaluation
- **Alertmanager** - Alert routing, grouping, and notification delivery
- **Grafana** - Alert visualization and dashboard integration

## Architecture

```
┌─────────────┐    scrapes    ┌────────────┐
│  Services   │ ───────────▶  │ Prometheus │
│  /metrics   │               │            │
└─────────────┘               └─────┬──────┘
                                    │ evaluates rules
                                    ▼
                              ┌─────────────┐
                              │ Alert Rules │
                              │  (*.yml)    │
                              └─────┬───────┘
                                    │ fires
                                    ▼
                              ┌──────────────┐    routes    ┌────────────┐
                              │ Alertmanager │ ──────────▶  │  Webhooks  │
                              │              │              │  Email     │
                              └──────┬───────┘              │  Slack     │
                                     │                      └────────────┘
                                     │ queries
                                     ▼
                              ┌─────────────┐
                              │   Grafana   │
                              │  Dashboards │
                              └─────────────┘
```

## Alert Categories

### 1. API Alerts (`alerts/api-alerts.yml`)

Monitor API health and performance:

| Alert | Threshold | Severity | Description |
|-------|-----------|----------|-------------|
| `APIErrorRateHigh` | >5% over 5m | Critical | High error rate affecting users |
| `APILatencyHigh` | P95 >500ms over 5m | Warning | Slow response times |
| `APIRequestRateHigh` | >1000 req/s over 5m | Warning | Unusual traffic spike |
| `CircuitBreakerOpen` | State=Open for 2m | Critical | Service communication failed |
| `RateLimiterActivelyThrottling` | >10 rejections/s | Warning | High request throttling |

### 2. Infrastructure Alerts (`alerts/infrastructure-alerts.yml`)

Monitor system resources and service health:

| Alert | Threshold | Severity | Description |
|-------|-----------|----------|-------------|
| `ContainerCPUHigh` | >80% for 10m | Warning | High CPU usage |
| `ContainerMemoryHigh` | >85% for 10m | Critical | Risk of OOM kill |
| `ContainerRestartRateHigh` | >0.1 restarts/min | Critical | Frequent container restarts |
| `DiskSpaceLow` | <20% available | Warning | Low disk space |
| `ServiceDown` | up=0 for 2m | Critical | Service not responding |
| `PostgreSQLDown` | up=0 for 1m | Critical | Database unavailable |
| `RedisDown` | up=0 for 1m | Critical | Cache unavailable |
| `RabbitMQDown` | up=0 for 1m | Critical | Message bus unavailable |

### 3. Message Bus Alerts (`alerts/messagebus-alerts.yml`)

Monitor RabbitMQ health and message processing:

| Alert | Threshold | Severity | Description |
|-------|-----------|----------|-------------|
| `RabbitMQQueueDepthHigh` | >1000 messages for 5m | Warning | Message backlog |
| `RabbitMQQueueDepthCritical` | >10000 messages for 10m | Critical | Severe message backlog |
| `RabbitMQNoConsumers` | consumers=0 with messages | Critical | No active consumers |
| `RabbitMQHighPublishRate` | >1000 msg/s | Warning | Unusual message volume |
| `RabbitMQConsumerUtilizationHigh` | >90% for 10m | Warning | Consumers at capacity |
| `RabbitMQConnectionFailures` | >1 closure/s | Critical | Connection issues |
| `RabbitMQMemoryHigh` | >80% for 5m | Warning | High memory usage |
| `RabbitMQDiskSpaceLow` | <1GB available | Critical | Low disk space |

## Configuration Files

### Alert Rules

Alert rules are defined in YAML files under `deployment/docker-compose/alerts/`:

```yaml
groups:
  - name: api_alerts
    interval: 30s
    rules:
      - alert: APIErrorRateHigh
        expr: |
          (sum by (service) (rate(http_server_requests_total{status=~"5.."}[5m]))
           / sum by (service) (rate(http_server_requests_total[5m]))) > 0.05
        for: 5m
        labels:
          severity: critical
          component: api
        annotations:
          summary: "High error rate for {{ $labels.service }}"
          description: "Error rate is {{ $value | humanizePercentage }}"
          runbook_url: "https://github.com/.../api-error-rate-high.md"
```

### Alertmanager Configuration

Alertmanager routing is configured in `deployment/docker-compose/alertmanager.yml`:

```yaml
route:
  receiver: 'default-receiver'
  group_by: ['alertname', 'severity', 'service']
  group_wait: 30s
  group_interval: 5m
  repeat_interval: 4h
  
  routes:
    - match:
        severity: critical
      receiver: 'critical-alerts'
      repeat_interval: 1h
```

## Access Points

| Service | URL | Credentials |
|---------|-----|-------------|
| Prometheus | http://localhost:9090 | None |
| Alertmanager | http://localhost:9093 | None |
| Grafana | http://localhost:3000 | admin/admin |
| Alerts Dashboard | http://localhost:3000/d/alerts-slos | admin/admin |

## Using the System

### Viewing Alerts

**In Prometheus:**
1. Navigate to http://localhost:9090/alerts
2. View active alerts and their states (Inactive, Pending, Firing)
3. Click alert name to see query and evaluation details

**In Alertmanager:**
1. Navigate to http://localhost:9093
2. View firing alerts grouped by severity and service
3. Silence alerts temporarily if needed
4. View alert history and routing

**In Grafana:**
1. Navigate to http://localhost:3000/d/alerts-slos
2. View active alerts panel at the top
3. See metrics that alerts are based on
4. Click through to runbook documentation

### Silencing Alerts

To temporarily silence an alert:

```bash
# Via Alertmanager UI
# Navigate to http://localhost:9093/#/silences
# Click "New Silence"
# Set matchers (e.g., alertname="APIErrorRateHigh")
# Set duration and comment

# Via API
curl -X POST http://localhost:9093/api/v1/silences \
  -H "Content-Type: application/json" \
  -d '{
    "matchers": [
      {"name": "alertname", "value": "APIErrorRateHigh", "isRegex": false}
    ],
    "startsAt": "2025-10-25T10:00:00Z",
    "endsAt": "2025-10-25T12:00:00Z",
    "createdBy": "engineer@example.com",
    "comment": "Maintenance window"
  }'
```

### Testing Alerts

#### Test API Error Rate Alert

```bash
# Generate errors in a service
for i in {1..100}; do
  curl -X POST http://localhost:5000/api/test/error
  sleep 0.1
done

# Check alert in Prometheus after 5 minutes
# http://localhost:9090/alerts
```

#### Test Latency Alert

```bash
# Generate slow requests
for i in {1..100}; do
  curl -X GET "http://localhost:5000/api/test/slow?delay=1000"
  sleep 0.1
done
```

#### Test Queue Depth Alert

```bash
# Publish many messages
docker exec coding-agent-chat-service curl -X POST http://localhost:5001/api/test/publish-events?count=2000
```

#### Test CPU Alert

```bash
# Stress test container
docker exec coding-agent-chat-service curl -X POST http://localhost:5001/api/test/cpu-stress?duration=600
```

#### Test Memory Alert

```bash
# Allocate memory
docker exec coding-agent-chat-service curl -X POST http://localhost:5001/api/test/memory-stress?mb=500
```

## Notification Channels

### Phase 1 (Current)
- ✅ Webhook endpoints (for development)
- ✅ Grafana UI visualization
- ✅ Alertmanager UI

### Phase 2 (Planned)
- Email notifications (configure SMTP)
- Slack integration (configure webhook)
- PagerDuty integration (configure service key)

### Configuring Email Notifications

Edit `alertmanager.yml`:

```yaml
global:
  smtp_from: 'alerts@coding-agent.local'
  smtp_smarthost: 'smtp.gmail.com:587'
  smtp_auth_username: 'your-email@gmail.com'
  smtp_auth_password: 'your-app-password'
  smtp_require_tls: true

receivers:
  - name: 'critical-alerts'
    email_configs:
      - to: 'oncall@example.com'
        headers:
          Subject: '[CRITICAL] {{ .GroupLabels.alertname }}'
```

### Configuring Slack Notifications

Edit `alertmanager.yml`:

```yaml
receivers:
  - name: 'critical-alerts'
    slack_configs:
      - api_url: 'https://hooks.slack.com/services/YOUR/WEBHOOK/URL'
        channel: '#alerts-critical'
        title: '[{{ .Status | toUpper }}] {{ .GroupLabels.alertname }}'
        text: '{{ range .Alerts }}{{ .Annotations.description }}{{ end }}'
```

## Runbook Documentation

Each alert has a corresponding runbook in `docs/runbooks/`:

- Diagnosis steps
- Resolution procedures
- Prevention measures
- Escalation process

**Accessing Runbooks:**
- From alert annotations: Click "runbook_url" link
- From GitHub: https://github.com/JustAGameZA/coding-agent/tree/main/docs/runbooks
- From Grafana: Links in dashboard panels

## Best Practices

### Alert Design

1. **Actionable**: Every alert should require a human action
2. **Clear**: Alert messages should indicate the problem
3. **Scoped**: Alerts should be specific to a component or service
4. **Tiered**: Use severity levels appropriately (Critical vs Warning)

### Response Procedures

1. **Acknowledge** alert in Alertmanager
2. **Open** corresponding runbook
3. **Diagnose** using provided steps
4. **Fix** following resolution procedures
5. **Validate** fix worked and alert resolved
6. **Document** incident for post-mortem

### SLO Targets

Current Service Level Objectives (SLOs):

- **Availability**: 99.9% uptime (43 minutes downtime/month)
- **Error Rate**: <1% of requests
- **Latency**: P95 <500ms, P99 <1000ms
- **Queue Lag**: <5 minutes for event processing

## Troubleshooting

### Alerts Not Firing

```bash
# Check Prometheus is scraping targets
curl http://localhost:9090/api/v1/targets

# Check rule evaluation
curl http://localhost:9090/api/v1/rules

# Verify alert rule syntax
docker exec coding-agent-prometheus promtool check rules /etc/prometheus/alerts/*.yml
```

### Alertmanager Not Receiving Alerts

```bash
# Check Prometheus → Alertmanager connectivity
curl http://localhost:9090/api/v1/alertmanagers

# Check Alertmanager logs
docker logs coding-agent-alertmanager

# Verify Alertmanager config
docker exec coding-agent-alertmanager amtool check-config /etc/alertmanager/alertmanager.yml
```

### Grafana Not Showing Alerts

```bash
# Check Alertmanager datasource
curl -u admin:admin http://localhost:3000/api/datasources

# Check Grafana logs
docker logs coding-agent-grafana

# Verify datasource connectivity
curl -u admin:admin http://localhost:3000/api/datasources/proxy/uid/alertmanager/api/v1/alerts
```

## Maintenance

### Updating Alert Rules

1. Edit rule files in `deployment/docker-compose/alerts/`
2. Validate syntax:
   ```bash
   docker exec coding-agent-prometheus promtool check rules /etc/prometheus/alerts/*.yml
   ```
3. Reload Prometheus:
   ```bash
   curl -X POST http://localhost:9090/-/reload
   ```

### Updating Alertmanager Config

1. Edit `deployment/docker-compose/alertmanager.yml`
2. Validate syntax:
   ```bash
   docker exec coding-agent-alertmanager amtool check-config /etc/alertmanager/alertmanager.yml
   ```
3. Reload Alertmanager:
   ```bash
   curl -X POST http://localhost:9093/-/reload
   ```

## References

- [Prometheus Alerting Documentation](https://prometheus.io/docs/alerting/latest/overview/)
- [Alertmanager Documentation](https://prometheus.io/docs/alerting/latest/alertmanager/)
- [Grafana Alerting Documentation](https://grafana.com/docs/grafana/latest/alerting/)
- [Implementation Roadmap](../../docs/02-IMPLEMENTATION-ROADMAP.md)
- [Runbooks](../../docs/runbooks/README.md)
