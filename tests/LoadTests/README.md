# Load Testing Guide

This directory contains k6 load test scripts for performance validation of the POC implementation.

## Prerequisites

- [k6](https://k6.io/docs/get-started/installation/) installed
- Docker Compose environment running
- Gateway and Chat services deployed

## Installation

### macOS
```bash
brew install k6
```

### Windows
```powershell
choco install k6
```

### Linux
```bash
sudo gpg --no-default-keyring --keyring /usr/share/keyrings/k6-archive-keyring.gpg --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys C5AD17C747E3415A3642D57D77C6C491D6AC1D69
echo "deb [signed-by=/usr/share/keyrings/k6-archive-keyring.gpg] https://dl.k6.io/deb stable main" | sudo tee /etc/apt/sources.list.d/k6.list
sudo apt-get update
sudo apt-get install k6
```

## Test Scripts

### chat-service-load.js

Simulates realistic user behavior:
1. Create conversation via REST API
2. Connect via WebSocket (SignalR)
3. Send messages in real-time
4. Fetch conversation data (test caching)
5. List messages (test pagination)

**Load Profile**:
- Warm-up: 0 → 10 users over 30s
- Ramp-up: 10 → 50 users over 1m
- Sustain: 50 users for 2m
- Cool-down: 50 → 0 users over 30s

**Total Duration**: 4 minutes

## Running Tests

### Basic Run

```bash
cd tests/LoadTests
k6 run chat-service-load.js
```

### Custom Parameters

```bash
# Override base URL
k6 run --env BASE_URL=http://staging.example.com:5000 chat-service-load.js

# Increase virtual users
k6 run --vus 100 --duration 10m chat-service-load.js

# Output results to JSON
k6 run --out json=results.json chat-service-load.js

# Send metrics to Prometheus
k6 run --out experimental-prometheus-rw chat-service-load.js
```

### Cloud Run (k6 Cloud)

```bash
# Login to k6 cloud
k6 login cloud

# Run test in cloud
k6 cloud chat-service-load.js
```

## Performance Targets

The test validates these SLA targets:

| Metric | Target | Description |
|--------|--------|-------------|
| **HTTP p95 Latency** | <100ms | 95% of HTTP requests complete in <100ms |
| **Message Latency** | <100ms | End-to-end message delivery time |
| **HTTP Error Rate** | <5% | Less than 5% of requests fail |
| **Overall Error Rate** | <5% | Including WebSocket errors |
| **WebSocket Success** | >95% | 95%+ of WebSocket connections succeed |

## Understanding Results

### Summary Statistics

```
scenarios: (100.00%) 1 scenario, 50 max VUs, 4m30s max duration
✓ conversation created
✓ response has conversation id
✓ websocket connected
✓ get conversation successful
✓ messages array returned

checks.........................: 95.00% ✓ 19000  ✗ 1000
conversations_created..........: 4000   16.666667/s
data_received..................: 12 MB  50 kB/s
data_sent......................: 6 MB   25 kB/s
http_req_duration..............: avg=45ms  min=10ms med=40ms max=120ms p(90)=75ms p(95)=85ms
  { expected_response:true }...: avg=42ms  min=10ms med=38ms max=100ms p(90)=70ms p(95)=80ms
http_req_failed................: 2.50%  ✓ 100    ✗ 3900
message_latency................: avg=35ms  min=8ms  med=30ms max=95ms  p(90)=60ms p(95)=75ms
messages_sent..................: 3800   15.833333/s
vus............................: 50     min=0    max=50
```

### Key Metrics to Analyze

1. **http_req_duration p(95)**: Should be <100ms
2. **http_req_failed**: Should be <5%
3. **message_latency p(95)**: Should be <100ms
4. **checks pass rate**: Should be >95%

### Interpreting Results

✅ **PASS Example**:
- http_req_duration p(95): 85ms ✅
- http_req_failed: 2.5% ✅
- message_latency p(95): 75ms ✅
- checks: 95% ✅

❌ **FAIL Example**:
- http_req_duration p(95): 150ms ❌ (exceeds 100ms)
- http_req_failed: 8% ❌ (exceeds 5%)
- message_latency p(95): 120ms ❌ (exceeds 100ms)

## Troubleshooting

### Connection Refused

```bash
# Check if services are running
docker compose -f ../../deployment/docker-compose/docker-compose.dev.yml ps

# Check Gateway logs
docker compose logs gateway
```

### High Error Rate

1. Check service logs for exceptions
2. Verify database connection pool size
3. Check Redis connection limits
4. Monitor CPU/memory usage of services

### High Latency

1. Check database query performance
2. Verify Redis caching is working
3. Check network latency between services
4. Monitor service CPU/memory usage

## Grafana Integration

View real-time metrics during load test:

1. Open Grafana: http://localhost:3000
2. Navigate to "API Performance" dashboard
3. Run load test
4. Watch metrics update in real-time

## Next Steps After Testing

1. Document results in `docs/POC-VALIDATION-REPORT.md`
2. Identify performance bottlenecks
3. Optimize slow endpoints
4. Re-run tests to validate improvements
5. Make Go/No-Go decision for Phase 1

## Advanced Usage

### Smoke Test (Quick Validation)

```bash
# Run with just 1 VU for 30 seconds
k6 run --vus 1 --duration 30s chat-service-load.js
```

### Stress Test (Find Breaking Point)

```bash
# Gradually increase load until system breaks
k6 run --vus 100 --duration 10m chat-service-load.js
```

### Soak Test (Long-Running Stability)

```bash
# Run at moderate load for extended period
k6 run --vus 50 --duration 2h chat-service-load.js
```

## References

- [k6 Documentation](https://k6.io/docs/)
- [k6 Metrics](https://k6.io/docs/using-k6/metrics/)
- [k6 Thresholds](https://k6.io/docs/using-k6/thresholds/)
- [SignalR with k6](https://k6.io/blog/load-testing-websockets-with-k6/)
