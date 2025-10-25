using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;

namespace CodingAgent.SharedKernel.Infrastructure;

/// <summary>
/// Extension methods for health check registration across microservices.
/// </summary>
public static class HealthCheckExtensions
{
    /// <summary>
    /// Adds RabbitMQ health check if RabbitMQ is configured.
    /// Uses ConnectionFactory to avoid embedding credentials in connection strings.
    /// </summary>
    /// <param name="builder">The health checks builder.</param>
    /// <param name="configuration">Configuration provider.</param>
    /// <param name="name">Name for the health check (default: "rabbitmq").</param>
    /// <returns>The same health checks builder for chaining.</returns>
    public static IHealthChecksBuilder AddRabbitMQHealthCheckIfConfigured(
        this IHealthChecksBuilder builder,
        IConfiguration configuration,
        string name = "rabbitmq")
    {
        if (!configuration.IsRabbitMQConfigured())
        {
            return builder;
        }

        var host = configuration["RabbitMQ:Host"]!;
        var username = configuration["RabbitMQ:Username"]!;
        var password = configuration["RabbitMQ:Password"]!;

        return builder.AddRabbitMQ(
            _ => new ConnectionFactory
            {
                HostName = host,
                UserName = username,
                Password = password,
                Port = 5672
            },
            name: name);
    }
}
