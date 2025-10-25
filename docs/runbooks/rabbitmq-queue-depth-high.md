# Runbook: RabbitMQ Queue Depth High

## Alert Details

- **Alert Name**: `RabbitMQQueueDepthHigh`
- **Severity**: Warning
- **Threshold**: Queue depth > 1000 messages for > 5 minutes
- **Component**: Message Bus
- **Category**: Performance

## Description

This alert fires when a RabbitMQ queue has more than 1000 messages waiting to be processed for over 5 minutes. This indicates that message consumers are unable to keep up with the publishing rate.

## Impact

- Event processing is lagging behind
- Real-time operations may be delayed
- Memory pressure on RabbitMQ
- Potential message loss if queue fills disk
- User-facing features depending on events may be stale

## Diagnosis

### 1. Identify Affected Queue

Access RabbitMQ Management UI: http://localhost:15672
- Username: codingagent
- Password: devPassword123!

Navigate to "Queues" tab and identify:
- Queue name with high message count
- Publishing rate vs. consumption rate
- Number of consumers
- Consumer utilization

### 2. Check Consumer Status

```bash
# View consumers for a specific queue
docker exec coding-agent-rabbitmq rabbitmqctl list_consumers

# Check if consumers are connected
docker exec coding-agent-rabbitmq rabbitmqctl list_channels
```

### 3. Check Consumer Service Health

```bash
# Check if consumer service is running
docker-compose ps | grep <consumer-service>

# Check consumer service logs
docker logs --tail=100 coding-agent-<consumer-service>

# Check for errors in consumer processing
docker logs coding-agent-<consumer-service> 2>&1 | grep -i "error\|exception\|fail"
```

### 4. Check Message Publishing Rate

```bash
# Check who is publishing messages
docker exec coding-agent-rabbitmq rabbitmqctl list_connections

# View publishing rate in Grafana
# Navigate to Backend Services dashboard
# Check message publish rate graph
```

### 5. Check RabbitMQ Resources

```bash
# Check RabbitMQ memory usage
docker exec coding-agent-rabbitmq rabbitmqctl status | grep -A 5 "memory"

# Check disk space
docker exec coding-agent-rabbitmq df -h
```

## Resolution Steps

### Immediate Actions

1. **Scale Consumer Service**:
   ```bash
   # Increase number of consumer instances
   docker-compose up -d --scale <consumer-service>=3
   
   # Verify new consumers are processing
   docker exec coding-agent-rabbitmq rabbitmqctl list_consumers
   ```

2. **Increase Consumer Prefetch Count**:
   ```csharp
   // In consumer configuration
   cfg.PrefetchCount = 50;  // Increase from default
   ```

3. **Verify Consumer is Running**:
   ```bash
   # If consumer is down, restart it
   docker-compose restart <consumer-service>
   ```

### Investigate Slow Processing

1. **Check Consumer Performance**:
   - Review consumer code for bottlenecks
   - Check database queries in consumer handlers
   - Verify external API calls aren't timing out
   - Look for synchronous I/O operations

2. **Enable Consumer Tracing**:
   ```bash
   # Check Jaeger for consumer traces
   # Navigate to: http://localhost:16686
   # Search for operations in consumer service
   # Identify slow spans
   ```

3. **Profile Consumer Code**:
   - Add timing logs to consumer handlers
   - Identify slow operations
   - Optimize or make async

### Reduce Publishing Rate

If consumers cannot keep up:

1. **Add Rate Limiting to Publishers**:
   ```csharp
   // Add delay between publishes if appropriate
   await Task.Delay(TimeSpan.FromMilliseconds(100));
   ```

2. **Batch Processing**:
   ```csharp
   // Modify consumer to process messages in batches
   cfg.PrefetchCount = 100;
   // Process batch together
   ```

3. **Temporary Throttling**:
   - Reduce frequency of scheduled jobs
   - Defer non-critical operations
   - Implement priority queues

### Optimize Message Handlers

1. **Use Parallel Processing** (if safe):
   ```csharp
   cfg.UseConcurrentMessageLimit(10);
   ```

2. **Optimize Database Operations**:
   - Use bulk inserts instead of individual saves
   - Batch database calls
   - Use AsNoTracking for read operations

3. **Implement Circuit Breaker**:
   - Prevent cascading failures
   - Fail fast on downstream issues

### Purge Queue (Last Resort)

⚠️ **WARNING**: Only do this if messages can be safely discarded

```bash
# Purge specific queue
docker exec coding-agent-rabbitmq rabbitmqctl purge_queue <queue-name>

# Or via management UI
# Navigate to queue → "Purge Messages" button
```

## Validation

1. Monitor queue depth decreasing in Grafana
2. Verify consumer utilization increases
3. Check alert resolves in Alertmanager
4. Monitor for 30 minutes for stability

## Prevention

1. **Right-Size Consumer Capacity**:
   - Load test with realistic message volumes
   - Determine optimal consumer count
   - Set up auto-scaling policies

2. **Implement Dead Letter Exchange**:
   ```csharp
   cfg.ReceiveEndpoint("my-queue", e =>
   {
       e.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));
       e.ConfigureDeadLetterQueueDeadLetterExchange();
       e.ConfigureDeadLetterQueueMessageTtl(TimeSpan.FromDays(7));
   });
   ```

3. **Add Consumer Metrics**:
   - Track message processing time
   - Monitor consumer lag
   - Alert on slow consumers

4. **Optimize Consumer Code**:
   - Profile and optimize hot paths
   - Use async/await properly
   - Avoid blocking operations
   - Implement batching where appropriate

5. **Capacity Planning**:
   - Monitor message volume trends
   - Plan for peak loads
   - Scale proactively

## Escalation

If unable to resolve within 30 minutes:
1. Check for incidents affecting upstream publishers
2. Consult with team owning the publisher service
3. Consider temporary disable of non-critical publishers

## Related Alerts

- `RabbitMQQueueDepthCritical` - Escalation of this alert
- `RabbitMQNoConsumers` - No consumers processing queue
- `RabbitMQMemoryHigh` - Queue depth causing memory pressure
- `ServiceDown` - Consumer service may be down
- `ContainerCPUHigh` - Consumer may be CPU-bound

## References

- [RabbitMQ Management UI](http://localhost:15672)
- [Backend Services Dashboard](http://localhost:3000/d/backend-services/backend-services)
- [MassTransit Documentation](https://masstransit.io)
- [Message Bus Architecture](../04-ML-AND-ORCHESTRATION-ADR.md)
