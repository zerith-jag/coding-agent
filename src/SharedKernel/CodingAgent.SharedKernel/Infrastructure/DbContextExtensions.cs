using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CodingAgent.SharedKernel.Infrastructure;

/// <summary>
/// Extension methods for DbContext operations across microservices.
/// </summary>
public static class DbContextExtensions
{
    /// <summary>
    /// Applies EF Core migrations if the database provider is relational.
    /// Logs errors and optionally throws in production environment.
    /// </summary>
    /// <param name="dbContext">The database context to migrate.</param>
    /// <param name="logger">Logger for recording migration attempts and errors.</param>
    /// <param name="isProduction">Whether the app is running in production; if true, rethrows exceptions.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the migration operation.</returns>
    public static async Task MigrateDatabaseIfRelationalAsync(
        this DbContext dbContext,
        ILogger logger,
        bool isProduction = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (dbContext.Database.IsRelational())
            {
                await dbContext.Database.MigrateAsync(cancellationToken);
                logger.LogInformation("Successfully applied EF Core migrations for {DbContextType}",
                    dbContext.GetType().Name);
            }
            else
            {
                logger.LogInformation("Skipping migrations for non-relational provider ({DbContextType})",
                    dbContext.GetType().Name);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to apply {DbContextType} migrations on startup",
                dbContext.GetType().Name);

            if (isProduction)
            {
                logger.LogCritical("Migration failure in production environment; terminating startup");
                throw;
            }
            else
            {
                logger.LogWarning("Continuing startup despite migration failure in non-production environment");
            }
        }
    }
}
