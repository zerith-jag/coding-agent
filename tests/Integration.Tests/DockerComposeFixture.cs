using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using Xunit;

namespace CodingAgent.Integration.Tests;

/// <summary>
/// Test fixture that manages Docker Compose environment for integration tests
/// This fixture ensures all services are running before tests execute
/// </summary>
public class DockerComposeFixture : IAsyncLifetime
{
    private const string DockerComposeFile = "../../../../deployment/docker-compose/docker-compose.dev.yml";
    private const string ProjectName = "coding-agent-test";

    public DockerComposeFixture()
    {
        // Configuration will be initialized in InitializeAsync
    }

    /// <summary>
    /// Starts the Docker Compose environment before running tests
    /// </summary>
    public async Task InitializeAsync()
    {
        // TODO: Implement Docker Compose startup using Testcontainers
        // For now, this assumes services are started manually with:
        // docker compose -f deployment/docker-compose/docker-compose.dev.yml up -d
        
        Console.WriteLine("DockerComposeFixture: Initializing test environment");
        Console.WriteLine("NOTE: Please ensure services are running manually:");
        Console.WriteLine("  cd deployment/docker-compose");
        Console.WriteLine("  docker compose -f docker-compose.dev.yml up -d");
        
        // Wait for services to be healthy
        await Task.Delay(TimeSpan.FromSeconds(10));
        
        // TODO: Add health check validations:
        // - Check PostgreSQL is accepting connections
        // - Check Redis is responding to PING
        // - Check RabbitMQ management API is accessible
        // - Check Gateway is responding (when implemented)
        // - Check Chat Service is responding (when implemented)
        
        Console.WriteLine("DockerComposeFixture: Initialization complete");
    }

    /// <summary>
    /// Cleanup after all tests are complete
    /// </summary>
    public async Task DisposeAsync()
    {
        Console.WriteLine("DockerComposeFixture: Cleaning up test environment");
        
        // TODO: Implement proper cleanup
        // For now, services remain running for inspection
        // Developers can manually stop with:
        // docker compose -f deployment/docker-compose/docker-compose.dev.yml down -v
        
        await Task.CompletedTask;
    }

    /// <summary>
    /// Validates that a service is healthy
    /// </summary>
    private async Task<bool> IsServiceHealthy(string serviceUrl)
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            var response = await client.GetAsync($"{serviceUrl}/health");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
