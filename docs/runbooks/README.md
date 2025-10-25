# Alerting Runbooks

This directory contains runbooks for responding to alerts in the Coding Agent microservices platform.

## Purpose

Runbooks provide step-by-step guidance for:
- Diagnosing the root cause of alerts
- Resolving issues quickly and safely
- Preventing future occurrences
- Escalating when necessary

## Available Runbooks

### API Alerts
- [API Error Rate High](api-error-rate-high.md) - High error rates (>5%) affecting users
- [API Latency High](api-latency-high.md) - Slow response times (P95 >500ms)
- [Circuit Breaker Open](circuit-breaker-open.md) - Service communication failures
- [Rate Limiter Throttling](rate-limiter-throttling.md) - High request throttling

### Infrastructure Alerts
- [Container CPU High](container-cpu-high.md) - CPU usage >80% for 10 minutes
- [Container Memory High](container-memory-high.md) - Memory usage >85% for 10 minutes
- [Container Restart Rate High](container-restart-rate-high.md) - Frequent container restarts
- [Disk Space Low](disk-space-low.md) - Low disk space warning
- [Service Down](service-down.md) - Service not responding to health checks
- [PostgreSQL Down](postgresql-down.md) - Database unavailable
- [Redis Down](redis-down.md) - Cache unavailable
- [RabbitMQ Down](rabbitmq-down.md) - Message bus unavailable

### Message Bus Alerts
- [RabbitMQ Queue Depth High](rabbitmq-queue-depth-high.md) - Queue backlog >1000 messages
- [RabbitMQ Queue Depth Critical](rabbitmq-queue-depth-critical.md) - Critical queue backlog >10000 messages
- [RabbitMQ No Consumers](rabbitmq-no-consumers.md) - Queue has no active consumers
- [RabbitMQ Connection Failures](rabbitmq-connection-failures.md) - Connection issues
- [RabbitMQ Memory High](rabbitmq-memory-high.md) - High memory usage in RabbitMQ
- [RabbitMQ Disk Space Low](rabbitmq-disk-space-low.md) - Low disk space in RabbitMQ

## Runbook Structure

Each runbook follows a standard structure:

### 1. Alert Details
- Alert name and severity
- Threshold values
- Component and category

### 2. Description
- What the alert means
- When it fires

### 3. Impact
- User-facing impact
- System impact
- Business impact

### 4. Diagnosis
- Steps to identify root cause
- Commands to run
- Metrics to check
- Logs to review

### 5. Resolution Steps
- Immediate actions (stop the bleeding)
- Root cause fixes
- Long-term solutions

### 6. Validation
- How to verify the fix worked
- What to monitor after resolution

### 7. Prevention
- Steps to prevent recurrence
- Improvements to make
- Tests to add

### 8. Escalation
- When to escalate
- Who to contact
- What information to provide

### 9. Related Alerts
- Other alerts that may fire together
- Dependencies and correlations

### 10. References
- Dashboards
- Documentation
- External resources

## Quick Reference

### Access Points

- **Prometheus**: http://localhost:9090
- **Alertmanager**: http://localhost:9093
- **Grafana**: http://localhost:3000 (admin/admin)
- **RabbitMQ Management**: http://localhost:15672 (codingagent/devPassword123!)
- **Jaeger**: http://localhost:16686

### Common Commands

```bash
# View all running containers
docker-compose ps

# Check container logs
docker logs --tail=100 -f coding-agent-<service-name>

# Check container stats
docker stats --no-stream

# Restart a service
docker-compose restart <service-name>

# Scale a service
docker-compose up -d --scale <service-name>=3

# Check PostgreSQL health
docker exec coding-agent-postgres pg_isready

# Check Redis health
docker exec coding-agent-redis redis-cli ping

# Check RabbitMQ health
docker exec coding-agent-rabbitmq rabbitmq-diagnostics ping
```

### Grafana Dashboards

- **System Health**: http://localhost:3000/d/system-health/system-health
- **API Gateway**: http://localhost:3000/d/api-gateway/api-gateway
- **Backend Services**: http://localhost:3000/d/backend-services/backend-services
- **PostgreSQL**: http://localhost:3000/d/database-postgresql/database
- **Redis**: http://localhost:3000/d/cache-redis/redis-cache

## Contributing

When creating new runbooks:

1. Follow the standard structure above
2. Include specific commands that can be copy-pasted
3. Provide examples from actual incidents when possible
4. Link to relevant dashboards and documentation
5. Keep language clear and action-oriented
6. Test procedures before documenting

## Alert Severity Levels

- **Critical**: Immediate action required, user-facing impact
- **Warning**: Action required soon, potential for impact
- **Info**: Informational, no immediate action required

## On-Call Response

### Critical Alerts (15 min response time)
1. Acknowledge alert in Alertmanager
2. Open runbook for alert
3. Follow diagnosis steps
4. Implement immediate fix
5. Monitor for resolution
6. Document incident

### Warning Alerts (1 hour response time)
1. Review alert in Alertmanager
2. Check if already resolved
3. Open runbook if still active
4. Investigate during business hours
5. Implement preventive measures

## Incident Management

For major incidents:

1. **Declare Incident**: Use incident management system
2. **Assign Roles**: Incident Commander, Communications Lead, Technical Lead
3. **Follow Runbook**: Use relevant runbook as guide
4. **Communicate**: Update status page and stakeholders
5. **Resolve**: Implement fix and validate
6. **Post-Mortem**: Document lessons learned

## Support

For questions or improvements to runbooks:
- Create an issue in GitHub
- Contact the platform team
- Update runbook based on new learnings

## References

- [Implementation Roadmap](../02-IMPLEMENTATION-ROADMAP.md)
- [Service Catalog](../01-SERVICE-CATALOG.md)
- [System Overview](../00-OVERVIEW.md)
- [Quick Start Guide](../QUICK-START.md)
