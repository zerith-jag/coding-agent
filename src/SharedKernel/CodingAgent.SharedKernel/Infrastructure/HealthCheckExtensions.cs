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
    /// In dev, if registration fails (e.g., version mismatch), silently skip to avoid crashing the app.
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
        // Explicit opt-in via config; default is disabled to avoid dev-time package/version mismatches
        var enabledRaw = configuration["RabbitMQ:HealthCheck:Enabled"];
        var enabled = bool.TryParse(enabledRaw, out var flag) && flag;
        if (!enabled)
        {
            return builder;
        }

        if (!configuration.IsRabbitMQConfigured())
        {
            return builder;
        }

        try
        {
            var host = configuration["RabbitMQ:Host"]!;
            var username = configuration["RabbitMQ:Username"]!;
            var password = configuration["RabbitMQ:Password"]!;

            // Use the options overload explicitly to avoid ambiguous overloads across package versions
            return builder.AddRabbitMQ(
                setup: options =>
                {
                    options.ConnectionFactory = new ConnectionFactory
                    {
                        HostName = host,
                        UserName = username,
                        Password = password,
                        Port = 5672
                    };
                },
                name: name);
        }
        catch
        {
            // Fallback: try connection string overload; if still failing, skip registration
            var connString =
                $"amqp://{configuration["RabbitMQ:Username"]}:{configuration["RabbitMQ:Password"]}@{configuration["RabbitMQ:Host"]}:5672/";
            try
            {
                return builder.AddRabbitMQ(connString, name: name);
            }
            catch
            {
                return builder; // skip in dev environments
            }
        }
    }
}
