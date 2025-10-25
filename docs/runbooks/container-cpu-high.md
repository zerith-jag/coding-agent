# Runbook: Container CPU High

## Alert Details

- **Alert Name**: `ContainerCPUHigh`
- **Severity**: Warning
- **Threshold**: CPU usage > 80% for 10 minutes
- **Component**: Infrastructure
- **Category**: Capacity

## Description

This alert fires when a container is using more than 80% of its allocated CPU for over 10 minutes. Sustained high CPU usage can lead to performance degradation and service instability.

## Impact

- Increased response times
- Potential request timeouts
- Service instability
- May cascade to other services
- Risk of container throttling or OOM kill

## Diagnosis

### 1. Identify Affected Container

```bash
# Check real-time CPU usage
docker stats --no-stream

# Focus on specific container
docker stats coding-agent-<service-name> --no-stream

# Check CPU usage history in Grafana
# Navigate to: http://localhost:3000/d/system-health/system-health
```

### 2. Check Container Logs

```bash
# View recent logs
docker logs --tail=200 coding-agent-<service-name>

# Check for CPU-intensive operations
docker logs coding-agent-<service-name> 2>&1 | grep -i "processing\|computing\|calculation"
```

### 3. Identify CPU-Intensive Operations

```bash
# Get shell access to container
docker exec -it coding-agent-<service-name> /bin/bash

# For .NET services, check thread pool
# Look for thread pool exhaustion
# Check GC collections
```

### 4. Check Request Load

```bash
# Check request rate in Grafana
# High request rate may cause CPU spike

# Check for stuck requests
# Look at request duration metrics
```

### 5. Review Recent Changes

- Was there a recent deployment?
- Did traffic patterns change?
- Are there new features being used?
- Is a batch job running?

## Resolution Steps

### Immediate Actions

1. **Scale Horizontally**:
   ```bash
   # Add more instances to distribute load
   docker-compose up -d --scale <service-name>=3
   ```

2. **Check for CPU Hogs**:
   ```bash
   # For .NET containers, collect diagnostics
   docker exec coding-agent-<service-name> dotnet-counters collect -p 1
   
   # Look for:
   # - High GC time
   # - Thread pool starvation
   # - Lock contention
   ```

3. **Temporary Rate Limiting**:
   ```bash
   # Reduce rate limits in Gateway
   # Edit: src/Gateway/CodingAgent.Gateway/appsettings.json
   # Restart gateway
   docker-compose restart gateway
   ```

### Investigate Root Cause

1. **Profile Application**:
   ```bash
   # For .NET apps, use dotnet-trace
   docker exec coding-agent-<service-name> dotnet-trace collect -p 1 --duration 00:00:30
   
   # Analyze trace file locally
   # Look for hot paths
   ```

2. **Check for Infinite Loops**:
   - Review recent code changes
   - Check for retry loops without backoff
   - Verify background tasks are sleeping

3. **Check Database Queries**:
   ```bash
   # Inefficient queries can cause CPU spikes
   docker exec coding-agent-postgres psql -U codingagent -d codingagent -c \
     "SELECT query, calls, mean_exec_time, stddev_exec_time 
      FROM pg_stat_statements 
      WHERE calls > 100 
      ORDER BY mean_exec_time DESC 
      LIMIT 20;"
   ```

4. **Check Garbage Collection**:
   - High GC pressure indicates memory issues
   - May need to optimize memory usage
   - Consider increasing memory limits

### Application Optimization

1. **Optimize Hot Paths**:
   - Use profiling data to identify bottlenecks
   - Cache expensive computations
   - Use async/await properly
   - Avoid blocking calls

2. **Reduce Allocations**:
   ```csharp
   // Use ArrayPool for temporary buffers
   var buffer = ArrayPool<byte>.Shared.Rent(1024);
   try {
       // Use buffer
   } finally {
       ArrayPool<byte>.Shared.Return(buffer);
   }
   
   // Use ValueTask for hot paths
   ValueTask<Result> GetResultAsync();
   ```

3. **Optimize Regex**:
   ```csharp
   // Use compiled regex for hot paths
   private static readonly Regex Pattern = 
       new Regex(@"pattern", RegexOptions.Compiled);
   ```

4. **Implement Caching**:
   ```csharp
   // Cache expensive computations
   var result = await _cache.GetOrCreateAsync(
       cacheKey,
       async entry => {
           entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
           return await ExpensiveOperation();
       });
   ```

### Container Configuration

1. **Increase CPU Limits** (if appropriate):
   ```yaml
   # In docker-compose.yml
   services:
     service-name:
       deploy:
         resources:
           limits:
             cpus: '2.0'  # Increase from current limit
           reservations:
             cpus: '0.5'
   ```

2. **Adjust Thread Pool**:
   ```csharp
   // In Program.cs
   ThreadPool.SetMinThreads(100, 100);
   ```

## Validation

1. Monitor CPU usage drops below 60% in Grafana
2. Check alert resolves in Alertmanager
3. Verify response times are normal
4. Monitor for 30 minutes for stability

## Prevention

1. **Load Testing**:
   - Test service under realistic load
   - Identify CPU bottlenecks
   - Plan capacity accordingly

2. **Performance Profiling**:
   - Profile critical paths regularly
   - Benchmark performance-sensitive code
   - Set performance budgets

3. **Code Reviews**:
   - Review for CPU-intensive operations
   - Check for proper async/await usage
   - Verify caching strategies

4. **Monitoring**:
   - Set up CPU usage tracking per endpoint
   - Monitor GC metrics
   - Track thread pool usage
   - Alert on abnormal patterns

5. **Resource Planning**:
   - Right-size containers based on workload
   - Plan for peak traffic
   - Implement auto-scaling

## Escalation

If unable to resolve within 1 hour:
1. Escalate to senior engineer or architect
2. Consider rolling back recent changes
3. Implement traffic shedding if necessary
4. Notify stakeholders

## Related Alerts

- `ContainerMemoryHigh` - Often occurs together with CPU issues
- `APILatencyHigh` - CPU saturation causes latency
- `ServiceDown` - May be killed due to resource exhaustion
- `ContainerRestartRateHigh` - May restart due to health checks failing

## References

- [System Health Dashboard](http://localhost:3000/d/system-health/system-health)
- [.NET Performance Best Practices](https://docs.microsoft.com/en-us/dotnet/core/diagnostics/)
- [Docker Resource Constraints](https://docs.docker.com/config/containers/resource_constraints/)
- [Performance Guidelines](../STYLEGUIDE.md)
