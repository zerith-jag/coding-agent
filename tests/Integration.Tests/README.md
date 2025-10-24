# POC Testing Guide

This directory contains integration tests for validating the POC implementation of the Coding Agent microservices architecture.

## Prerequisites

- .NET 9 SDK
- Docker Desktop
- Visual Studio Code or Visual Studio 2022

## Project Structure

```
Integration.Tests/
├── Integration.Tests.csproj     # Test project file
├── GatewayChatFlowTests.cs      # E2E test scenarios
└── DockerComposeFixture.cs      # Test environment setup
```

## Running Tests

### 1. Start Infrastructure Services

Before running tests, start the required infrastructure services:

```bash
cd ../../deployment/docker-compose
docker compose -f docker-compose.dev.yml up -d postgres redis rabbitmq seq
```

### 2. Start Application Services (when implemented)

```bash
# Start Gateway and Chat Service
docker compose -f docker-compose.dev.yml --profile full up -d
```

### 3. Run Integration Tests

```bash
cd tests/Integration.Tests
dotnet test
```

### Run Specific Test Categories

```bash
# Run only E2E tests
dotnet test --filter Category=E2E

# Run with verbose output
dotnet test --logger "console;verbosity=detailed"

# Generate coverage report
dotnet test --collect:"XPlat Code Coverage"
```

## Test Scenarios

### GatewayChatFlowTests

1. **FullFlow_CreateConversationAndSendMessage_ShouldSucceed**
   - Creates a conversation via REST API
   - Connects via SignalR WebSocket
   - Sends a message
   - Validates message is received in real-time
   - Verifies message persistence in database

2. **CachedConversations_ShouldReduceDatabaseLoad**
   - Creates a conversation
   - Fetches conversation twice
   - Validates second fetch is faster (from cache)
   - Verifies data consistency

3. **InfrastructureServices_ShouldBeHealthy**
   - Validates infrastructure services are running
   - Can run without application services

## Current Status

⚠️ **Note**: Tests are currently marked as `Skip` because Gateway and Chat services are not yet implemented. Once services are built:

1. Remove the `Skip` attribute from tests
2. Run tests to validate POC functionality
3. Update POC validation report with results

## Configuration

Tests expect services at these URLs:
- Gateway: http://localhost:5000
- Chat Service: http://localhost:5001 (internal)
- PostgreSQL: localhost:5432
- Redis: localhost:6379
- RabbitMQ: localhost:5672

## Troubleshooting

### Tests Fail to Connect

```bash
# Check if services are running
docker compose -f ../../deployment/docker-compose/docker-compose.dev.yml ps

# View service logs
docker compose -f ../../deployment/docker-compose/docker-compose.dev.yml logs gateway chat-service
```

### Database Connection Issues

```bash
# Connect to PostgreSQL
docker exec -it coding-agent-postgres psql -U dev -d coding_agent

# Check if tables exist
\dt
```

### Redis Connection Issues

```bash
# Test Redis connection
docker exec -it coding-agent-redis redis-cli PING
```

## Next Steps

1. Implement Gateway service with YARP
2. Implement Chat service with SignalR + EF Core
3. Remove `Skip` attributes from tests
4. Run full test suite
5. Update POC-VALIDATION-REPORT.md with results

## References

- [xUnit Documentation](https://xunit.net/)
- [FluentAssertions Documentation](https://fluentassertions.com/)
- [Testcontainers Documentation](https://dotnet.testcontainers.org/)
- [SignalR Client Documentation](https://learn.microsoft.com/en-us/aspnet/core/signalr/dotnet-client)
