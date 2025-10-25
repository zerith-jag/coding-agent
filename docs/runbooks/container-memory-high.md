# Runbook: Container Memory High

## Alert Details

- **Alert Name**: `ContainerMemoryHigh`
- **Severity**: Critical
- **Threshold**: Memory usage > 85% for 10 minutes
- **Component**: Infrastructure
- **Category**: Capacity

## Description

This alert fires when a container is using more than 85% of its allocated memory for over 10 minutes. High memory usage can lead to out-of-memory (OOM) kills, causing service disruption.

## Impact

- Risk of container being killed by OOM killer
- Service unavailability during restart
- Potential data loss for in-flight operations
- Cascading failures to dependent services
- Degraded performance due to GC pressure

## Diagnosis

### 1. Identify Affected Container

```bash
# Check real-time memory usage
docker stats --no-stream

# Focus on specific container
docker stats coding-agent-<service-name> --no-stream

# Check memory usage history in Grafana
# Navigate to: http://localhost:3000/d/system-health/system-health
```

### 2. Check for Memory Leaks

```bash
# Check container memory over time
docker stats coding-agent-<service-name>

# If memory constantly increases → memory leak
# If memory stable at high level → undersized container
```

### 3. Check Application Logs

```bash
# Look for OutOfMemoryException
docker logs coding-agent-<service-name> 2>&1 | grep -i "outofmemory\|oom"

# Check for GC logs
docker logs coding-agent-<service-name> 2>&1 | grep -i "gc\|garbage"
```

### 4. Analyze Memory Dump

For .NET applications:

```bash
# Create memory dump
docker exec coding-agent-<service-name> dotnet-dump collect -p 1

# Copy dump file out of container
docker cp coding-agent-<service-name>:/tmp/dump_*.dmp ./

# Analyze with dotnet-dump locally
dotnet-dump analyze dump_*.dmp

# Check heap size
> dumpheap -stat

# Find large objects
> dumpheap -min 1000000

# Check for retained objects
> gcroot <address>
```

### 5. Check Cache Size

```bash
# For services using Redis
docker exec coding-agent-redis redis-cli INFO memory

# Check cache size in application
# Review memory cache configuration
```

## Resolution Steps

### Immediate Actions

1. **Restart Container** (temporary fix):
   ```bash
   docker-compose restart <service-name>
   
   # Monitor memory after restart
   watch -n 5 'docker stats --no-stream coding-agent-<service-name>'
   ```

2. **Increase Memory Limit** (if appropriate):
   ```yaml
   # Edit docker-compose.yml
   services:
     service-name:
       deploy:
         resources:
           limits:
             memory: 2G  # Increase from current limit
           reservations:
             memory: 512M
   ```

3. **Scale Horizontally** (distribute load):
   ```bash
   docker-compose up -d --scale <service-name>=3
   ```

### Investigate Memory Leak

1. **Common Causes in .NET**:
   - Event handlers not unregistered
   - Static collections growing unbounded
   - Cached data not being evicted
   - HttpClient not being reused
   - EF Core contexts not being disposed

2. **Check for Known Patterns**:
   ```csharp
   // ❌ Memory leak - event not unregistered
   public void Subscribe()
   {
       _eventBus.MessageReceived += OnMessage;
   }
   
   // ✅ Proper cleanup
   public void Subscribe()
   {
       _eventBus.MessageReceived += OnMessage;
   }
   public void Dispose()
   {
       _eventBus.MessageReceived -= OnMessage;
   }
   
   // ❌ Memory leak - static collection
   private static List<Data> _cache = new();
   
   // ✅ Use bounded cache with eviction
   private readonly IMemoryCache _cache;
   
   // ❌ Memory leak - DbContext not disposed
   var context = new AppDbContext();
   
   // ✅ Use using statement
   using var context = new AppDbContext();
   ```

3. **Review Recent Changes**:
   - Check recent commits for memory-intensive operations
   - Review new features using caching
   - Check for collections being populated but not cleared

### Fix Memory Leaks

1. **Dispose Resources Properly**:
   ```csharp
   // Use IAsyncDisposable for async cleanup
   public class Service : IAsyncDisposable
   {
       private readonly HttpClient _client;
       
       public async ValueTask DisposeAsync()
       {
           await _client.DisposeAsync();
       }
   }
   ```

2. **Limit Cache Size**:
   ```csharp
   services.AddMemoryCache(options =>
   {
       options.SizeLimit = 1024; // Limit to 1024 entries
       options.CompactionPercentage = 0.25; // Evict 25% when limit reached
   });
   
   // Set size when caching
   _cache.Set(key, value, new MemoryCacheEntryOptions
   {
       Size = 1,
       AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
   });
   ```

3. **Use Object Pooling**:
   ```csharp
   // Pool expensive objects
   services.AddSingleton<ObjectPool<StringBuilder>>(
       new DefaultObjectPoolProvider().Create(
           new StringBuilderPooledObjectPolicy()));
   ```

4. **Optimize Collections**:
   ```csharp
   // Use capacity when known
   var list = new List<T>(capacity: expectedCount);
   
   // Use struct instead of class for small objects
   public struct Point { public int X; public int Y; }
   
   // Use Span<T> for stack allocations
   Span<byte> buffer = stackalloc byte[256];
   ```

### Database Connection Management

1. **Check Connection Leaks**:
   ```bash
   # Check PostgreSQL connections
   docker exec coding-agent-postgres psql -U codingagent -d codingagent -c \
     "SELECT count(*), state 
      FROM pg_stat_activity 
      WHERE datname = 'codingagent' 
      GROUP BY state;"
   ```

2. **Configure Connection Pool**:
   ```csharp
   services.AddDbContext<AppDbContext>(options =>
       options.UseNpgsql(connectionString, npgsqlOptions =>
       {
           npgsqlOptions.MaxBatchSize(100);
           npgsqlOptions.CommandTimeout(30);
       }),
       ServiceLifetime.Scoped);
   ```

### GC Optimization

1. **Reduce GC Pressure**:
   ```csharp
   // Enable server GC (in .csproj)
   <PropertyGroup>
       <ServerGarbageCollection>true</ServerGarbageCollection>
   </PropertyGroup>
   
   // Use ArrayPool for buffers
   var buffer = ArrayPool<byte>.Shared.Rent(4096);
   try {
       // Use buffer
   } finally {
       ArrayPool<byte>.Shared.Return(buffer);
   }
   ```

2. **Monitor GC Metrics**:
   ```bash
   # Enable GC logging
   docker run -e DOTNET_gcServer=1 \
              -e DOTNET_GCHeapCount=2 \
              coding-agent-<service-name>
   ```

## Validation

1. Monitor memory usage drops below 70% in Grafana
2. Verify memory is stable over 1 hour
3. Check alert resolves in Alertmanager
4. Ensure no OOM kills in logs

## Prevention

1. **Memory Profiling**:
   - Profile memory usage regularly
   - Use memory profilers (dotMemory, VS Profiler)
   - Test with realistic data volumes
   - Set memory budgets

2. **Code Reviews**:
   - Review resource disposal patterns
   - Check for static collections
   - Verify cache eviction policies
   - Ensure proper async/await usage

3. **Testing**:
   - Add memory leak tests
   - Load test with sustained traffic
   - Monitor memory growth over time
   - Test cleanup on service shutdown

4. **Monitoring**:
   - Track GC metrics
   - Monitor cache sizes
   - Alert on memory growth trends
   - Track object allocations

5. **Resource Planning**:
   - Right-size containers based on workload
   - Leave headroom for spikes
   - Implement auto-scaling
   - Plan for peak loads

## Escalation

If unable to resolve within 30 minutes:
1. Page senior engineer
2. Consider rolling back recent changes
3. Implement memory limits at application level
4. Prepare for service restart

## Related Alerts

- `ContainerCPUHigh` - High GC activity causes CPU spikes
- `ServiceDown` - May be killed by OOM killer
- `ContainerRestartRateHigh` - Repeated OOM kills
- `APILatencyHigh` - GC pressure causes latency

## References

- [System Health Dashboard](http://localhost:3000/d/system-health/system-health)
- [.NET Memory Management](https://docs.microsoft.com/en-us/dotnet/standard/garbage-collection/)
- [dotnet-dump Documentation](https://docs.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-dump)
- [Memory Optimization Guide](../STYLEGUIDE.md)
