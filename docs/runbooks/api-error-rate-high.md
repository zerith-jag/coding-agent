# Runbook: API Error Rate High

## Alert Details

- **Alert Name**: `APIErrorRateHigh`
- **Severity**: Critical
- **Threshold**: Error rate > 5% over 5 minutes
- **Component**: API Services
- **Category**: Availability

## Description

This alert fires when a service is experiencing an error rate exceeding 5% over a 5-minute window. High error rates indicate that users are experiencing failures when interacting with the system.

## Impact

- Users experiencing service degradation or complete failures
- Potential data loss if errors occur during write operations
- Negative impact on user experience and trust
- May trigger cascading failures in dependent services

## Diagnosis

### 1. Check Alert Details

Review the alert in Grafana or Prometheus to identify:
- Which service is affected (`service` label)
- Current error rate percentage
- Duration of the issue

### 2. Check Service Logs

```bash
# View recent logs for the affected service
docker logs --tail=100 -f coding-agent-<service-name>

# Filter for errors
docker logs coding-agent-<service-name> 2>&1 | grep -i "error\|exception\|fail"
```

### 3. Check Service Health

```bash
# Check service health endpoint
curl http://localhost:5000/<service-name>/health

# Check Grafana API Gateway dashboard
# http://localhost:3000/d/api-gateway/api-gateway
```

### 4. Check Dependencies

```bash
# Verify PostgreSQL is healthy
docker exec coding-agent-postgres pg_isready

# Verify Redis is responding
docker exec coding-agent-redis redis-cli ping

# Verify RabbitMQ is healthy
docker exec coding-agent-rabbitmq rabbitmq-diagnostics ping
```

### 5. Review Metrics

In Grafana, check:
- Request rate trends (sudden spike or drop?)
- Latency metrics (correlated with errors?)
- Circuit breaker state (is it open?)
- Database connection pool usage
- CPU and memory usage

## Resolution Steps

### Immediate Actions (Stop the Bleeding)

1. **Scale Up** (if resource constrained):
   ```bash
   docker-compose up -d --scale <service-name>=3
   ```

2. **Circuit Breaker** (if dependency issue):
   - Errors may be coming from downstream service
   - Circuit breaker should automatically open after 5 failures
   - Check circuit breaker metrics in Grafana

3. **Rate Limiting** (if attack suspected):
   ```bash
   # Temporarily reduce rate limits in Gateway appsettings.json
   # Restart gateway
   docker-compose restart gateway
   ```

### Root Cause Fixes

1. **Application Error** (bugs in code):
   - Review stack traces in logs
   - Fix code and deploy hotfix
   - Add monitoring to prevent recurrence

2. **Database Issues**:
   ```bash
   # Check for slow queries
   docker exec coding-agent-postgres psql -U codingagent -d codingagent \
     -c "SELECT query, calls, total_exec_time, mean_exec_time FROM pg_stat_statements ORDER BY mean_exec_time DESC LIMIT 10;"
   
   # Check connection count
   docker exec coding-agent-postgres psql -U codingagent -d codingagent \
     -c "SELECT count(*) FROM pg_stat_activity;"
   ```

3. **Resource Exhaustion**:
   - Check CPU/Memory usage
   - Identify memory leaks or CPU-intensive operations
   - Scale horizontally or optimize code

4. **External Dependency Failure**:
   - Check health of downstream services
   - Verify circuit breaker is working
   - Implement fallback/degraded mode if possible

## Validation

After implementing fixes:

1. Verify error rate drops below 1% in Grafana
2. Check alert resolves in Alertmanager
3. Monitor for 30 minutes to ensure stability
4. Review user feedback/support tickets

## Prevention

1. **Add Integration Tests**:
   - Test error scenarios
   - Verify error handling paths
   - Test circuit breaker behavior

2. **Improve Observability**:
   - Add structured logging for error cases
   - Add custom metrics for business operations
   - Set up distributed tracing for complex flows

3. **Load Testing**:
   - Regularly test service under load
   - Identify breaking points
   - Plan capacity accordingly

4. **Code Reviews**:
   - Review error handling patterns
   - Ensure proper validation
   - Check for null reference exceptions

## Escalation

If unable to resolve within 30 minutes:

1. Page on-call engineer via PagerDuty
2. Create incident in incident management system
3. Start incident response procedure
4. Notify stakeholders

## Related Alerts

- `APILatencyHigh` - Often occurs together with error rate issues
- `CircuitBreakerOpen` - May indicate upstream service issues
- `ServiceDown` - Complete service failure
- `ContainerMemoryHigh` - May cause OOM errors

## References

- [API Gateway Dashboard](http://localhost:3000/d/api-gateway/api-gateway)
- [Backend Services Dashboard](http://localhost:3000/d/backend-services/backend-services)
- [Implementation Roadmap](../02-IMPLEMENTATION-ROADMAP.md)
- [Service Catalog](../01-SERVICE-CATALOG.md)
