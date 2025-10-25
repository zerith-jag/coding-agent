using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace CodingAgent.SharedKernel.Infrastructure;

/// <summary>
/// Extension methods for configuring RabbitMQ across microservices.
/// </summary>
public static class RabbitMQConfigurationExtensions
{
    /// <summary>
    /// Configures RabbitMQ host for MassTransit with consistent validation logic.
    /// In production, requires explicit configuration. In non-production, defaults to localhost/guest.
    /// </summary>
    /// <param name="cfg">The RabbitMQ configurator.</param>
    /// <param name="configuration">Configuration provider.</param>
    /// <param name="hostEnvironment">Host environment to determine production mode.</param>
    /// <exception cref="InvalidOperationException">Thrown when production configuration is missing.</exception>
    public static void ConfigureRabbitMQHost(
        this IRabbitMqBusFactoryConfigurator cfg,
        IConfiguration configuration,
        IHostEnvironment hostEnvironment)
    {
        var cfgHostRaw = configuration["RabbitMQ:Host"];
        var cfgUserRaw = configuration["RabbitMQ:Username"];
        var cfgPassRaw = configuration["RabbitMQ:Password"];
        var isProd = hostEnvironment.IsProduction();

        // Default only in non-production
        var host = isProd ? cfgHostRaw : (cfgHostRaw ?? "localhost");
        var username = isProd ? cfgUserRaw : (cfgUserRaw ?? "guest");
        var password = isProd ? cfgPassRaw : (cfgPassRaw ?? "guest");

        if (isProd && (string.IsNullOrWhiteSpace(host) ||
                       string.IsNullOrWhiteSpace(username) ||
                       string.IsNullOrWhiteSpace(password)))
        {
            throw new InvalidOperationException(
                "RabbitMQ configuration (Host/Username/Password) is required in Production");
        }

        if (!string.IsNullOrWhiteSpace(host))
        {
            cfg.Host(host!, h =>
            {
                if (!string.IsNullOrWhiteSpace(username))
                {
                    h.Username(username!);
                }
                if (!string.IsNullOrWhiteSpace(password))
                {
                    h.Password(password!);
                }
            });
        }
    }

    /// <summary>
    /// Validates whether RabbitMQ is fully configured.
    /// </summary>
    /// <param name="configuration">Configuration provider.</param>
    /// <returns>True if host, username, and password are all configured.</returns>
    public static bool IsRabbitMQConfigured(this IConfiguration configuration)
    {
        var host = configuration["RabbitMQ:Host"];
        var username = configuration["RabbitMQ:Username"];
        var password = configuration["RabbitMQ:Password"];

        return !string.IsNullOrWhiteSpace(host) &&
               !string.IsNullOrWhiteSpace(username) &&
               !string.IsNullOrWhiteSpace(password);
    }
}
