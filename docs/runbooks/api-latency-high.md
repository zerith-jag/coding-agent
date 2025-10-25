# Runbook: API Latency High

## Alert Details

- **Alert Name**: `APILatencyHigh`
- **Severity**: Warning
- **Threshold**: P95 latency > 500ms over 5 minutes
- **Component**: API Services
- **Category**: Performance

## Description

This alert fires when the 95th percentile (P95) response time exceeds 500ms for a service over a 5-minute window. This means that 5% of requests are taking longer than 500ms, which may impact user experience.

## Impact

- Users experiencing slow response times
- Degraded user experience
- Potential timeout errors
- May lead to increased error rates if latency continues to rise

## Diagnosis

### 1. Identify Affected Service

```bash
# Check Prometheus for latency metrics
# Navigate to: http://localhost:9090
# Query: histogram_quantile(0.95, sum by (service, le) (rate(http_server_request_duration_seconds_bucket[5m])))
```

### 2. Check Recent Changes

- Were any deployments made recently?
- Did traffic patterns change?
- Are there scheduled jobs running?

### 3. Check Database Performance

```bash
# View slow queries
docker exec coding-agent-postgres psql -U codingagent -d codingagent -c \
  "SELECT query, calls, mean_exec_time, max_exec_time 
   FROM pg_stat_statements 
   WHERE mean_exec_time > 100 
   ORDER BY mean_exec_time DESC 
   LIMIT 20;"

# Check for locks
docker exec coding-agent-postgres psql -U codingagent -d codingagent -c \
  "SELECT blocked_locks.pid AS blocked_pid,
          blocked_activity.usename AS blocked_user,
          blocking_locks.pid AS blocking_pid,
          blocking_activity.usename AS blocking_user,
          blocked_activity.query AS blocked_statement,
          blocking_activity.query AS blocking_statement
   FROM pg_catalog.pg_locks blocked_locks
   JOIN pg_catalog.pg_stat_activity blocked_activity ON blocked_activity.pid = blocked_locks.pid
   JOIN pg_catalog.pg_locks blocking_locks ON blocking_locks.locktype = blocked_locks.locktype
   JOIN pg_catalog.pg_stat_activity blocking_activity ON blocking_activity.pid = blocking_locks.pid
   WHERE NOT blocked_locks.granted;"
```

### 4. Check Resource Usage

```bash
# Check container CPU usage
docker stats --no-stream

# Check specific service
docker stats coding-agent-<service-name> --no-stream
```

### 5. Check External Dependencies

- RabbitMQ queue depth (backpressure?)
- Redis latency (cache misses?)
- External API calls (3rd party slowdown?)

### 6. Review Traces

Navigate to Jaeger UI: http://localhost:16686
- Find slow traces for the affected service
- Identify bottlenecks in the request flow
- Look for N+1 query patterns

## Resolution Steps

### Quick Wins

1. **Check Cache Hit Rate**:
   ```bash
   # Connect to Redis
   docker exec -it coding-agent-redis redis-cli
   # Run: INFO stats
   # Look at keyspace_hits vs keyspace_misses
   ```

2. **Clear Cache** (if appropriate):
   ```bash
   docker exec coding-agent-redis redis-cli FLUSHDB
   ```

3. **Restart Slow Service**:
   ```bash
   docker-compose restart <service-name>
   ```

### Database Optimization

1. **Add Missing Indexes**:
   ```sql
   -- Identify missing indexes
   SELECT schemaname, tablename, attname, n_distinct, correlation
   FROM pg_stats
   WHERE schemaname NOT IN ('pg_catalog', 'information_schema')
   AND n_distinct > 100
   ORDER BY n_distinct DESC;
   
   -- Create index (example)
   CREATE INDEX CONCURRENTLY idx_conversations_user_id 
   ON chat.conversations(user_id);
   ```

2. **Update Statistics**:
   ```bash
   docker exec coding-agent-postgres psql -U codingagent -d codingagent -c "ANALYZE;"
   ```

3. **Check Connection Pool**:
   - Review EF Core connection pool settings
   - Increase MaxPoolSize if needed
   - Check for connection leaks

### Application Optimization

1. **Enable Response Caching**:
   ```csharp
   // In Program.cs
   builder.Services.AddResponseCaching();
   app.UseResponseCaching();
   ```

2. **Optimize Queries**:
   - Use `.AsNoTracking()` for read-only queries
   - Use projection (`.Select()`) instead of loading full entities
   - Implement pagination
   - Add eager loading with `.Include()` to avoid N+1

3. **Implement Background Processing**:
   - Move long-running operations to background jobs
   - Use MassTransit for async message processing
   - Return 202 Accepted for operations that can be async

### Scale Horizontally

```bash
# Scale service to 3 instances
docker-compose up -d --scale <service-name>=3

# Verify all instances are healthy
docker-compose ps
```

## Validation

1. Check P95 latency drops below 300ms in Grafana
2. Verify alert resolves in Alertmanager
3. Monitor for 30 minutes to ensure stability
4. Review user feedback

## Prevention

1. **Performance Testing**:
   - Add load tests to CI/CD pipeline
   - Test with realistic data volumes
   - Identify bottlenecks early

2. **Query Optimization**:
   - Enable EF Core query logging in development
   - Review query plans for slow queries
   - Add appropriate indexes

3. **Caching Strategy**:
   - Cache frequently accessed data
   - Implement cache warming
   - Set appropriate TTLs

4. **Code Reviews**:
   - Review database queries
   - Check for N+1 patterns
   - Verify proper use of async/await

5. **Monitoring**:
   - Add custom metrics for business operations
   - Set up SLOs (Service Level Objectives)
   - Alert on SLO violations

## Escalation

If unable to resolve within 1 hour:
1. Escalate to senior engineer
2. Consider temporary traffic reduction
3. Notify stakeholders of degraded performance

## Related Alerts

- `APIErrorRateHigh` - High latency may lead to timeouts and errors
- `ContainerCPUHigh` - CPU saturation causes latency
- `PostgreSQLDown` - Database issues cause latency
- `RabbitMQQueueDepthHigh` - Message processing backlog

## References

- [API Gateway Dashboard](http://localhost:3000/d/api-gateway/api-gateway)
- [Database Dashboard](http://localhost:3000/d/database-postgresql/database)
- [Jaeger UI](http://localhost:16686)
- [Performance Best Practices](../STYLEGUIDE.md)
