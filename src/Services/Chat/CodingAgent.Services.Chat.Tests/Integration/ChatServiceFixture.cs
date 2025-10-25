using Testcontainers.PostgreSql;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using System.Collections.Concurrent;
using Xunit;

namespace CodingAgent.Services.Chat.Tests.Integration;

public sealed class ChatServiceFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _postgres;
    public HttpClient Client { get; private set; } = default!;
    public WebApplicationFactory<Program> Factory { get; private set; } = default!;

    public ChatServiceFixture() { }

    public async Task InitializeAsync()
    {
        string? connectionString = null;

        // Try to start PostgreSQL Testcontainer; fall back to in-memory if Docker is unavailable
        try
        {
            _postgres = new PostgreSqlBuilder()
                .WithDatabase("chat_tests")
                .WithUsername("postgres")
                .WithPassword("postgres")
                .WithImage("postgres:16-alpine")
                .Build();

            await _postgres.StartAsync();
            connectionString = _postgres.GetConnectionString();
        }
        catch (ArgumentException)
        {
            // Docker endpoint not detected; continue with in-memory database
            connectionString = null;
        }

        Factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                // Ensure non-production environment for tests so the app uses in-memory DB
                builder.UseEnvironment("Development");

                builder.ConfigureAppConfiguration((ctx, config) =>
                {
                    if (!string.IsNullOrEmpty(connectionString))
                    {
                        var dict = new Dictionary<string, string?>
                        {
                            ["ConnectionStrings:ChatDb"] = connectionString
                        };
                        config.AddInMemoryCollection(dict!);
                    }
                });
            });

        Client = Factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        Client.Dispose();
        Factory.Dispose();
        if (_postgres is not null)
        {
            try
            { await _postgres.StopAsync(); }
            catch { /* ignore */ }
            try
            { await _postgres.DisposeAsync(); }
            catch { /* ignore */ }
        }
    }
}

[CollectionDefinition("ChatServiceCollection")]
public sealed class ChatServiceCollection : ICollectionFixture<ChatServiceFixture>
{
}
