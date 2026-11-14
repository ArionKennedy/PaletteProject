using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Palette.Api.Data;
using Testcontainers.PostgreSql;
using Xunit;

namespace Palette.Tests.Infrastructure;

public class PostgresFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _container;
    private DbContextOptions<PaletteDbContext>? _options;

    public PaletteDbContext CreateContext()
    {
        if (_options is null)
        {
            throw new InvalidOperationException(
                "The test database has not been initialized.");
        }

        return new PaletteDbContext(_options);
    }

    public async Task InitializeAsync()
    {
        try
        {
            var connectionString =
                Environment.GetEnvironmentVariable(
                    "PALETTE_TEST_DB_CONNECTION");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                // No local configuration required.
                // Testcontainers chooses an available host port.
                _container = new PostgreSqlBuilder("postgres:17")
                    .WithDatabase("palette_integration_test")
                    .WithUsername("palette_test")
                    .WithPassword("temporary-test-password")
                    .Build();

                await _container.StartAsync();

                connectionString =
                    _container.GetConnectionString();
            }
            else
            {
                // Preserve the existing CI/local override, while
                // preventing accidental use of a development database.
                ValidateExternalConnection(connectionString);
            }

            _options =
                new DbContextOptionsBuilder<PaletteDbContext>()
                    .UseNpgsql(connectionString)
                    .Options;

            await using var db = CreateContext();

            await db.Database.MigrateAsync();
        }
        catch
        {
            // Clean up a temporary container if initialization fails.
            await DisposeAsync();
            throw;
        }
    }

    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            var container = _container;
            _container = null;

            await container.DisposeAsync();
        }
    }

    private static void ValidateExternalConnection(
        string connectionString)
    {
        var connection =
            new NpgsqlConnectionStringBuilder(connectionString);

        if (connection.Database != "palette_integration_test"
            || (connection.Host != "localhost"
                && connection.Host != "127.0.0.1"))
        {
            throw new InvalidOperationException(
                "Integration tests must use the local " +
                "palette_integration_test database.");
        }
    }
}

[CollectionDefinition("Postgres Integration Tests")]
public class PostgresTestCollection
    : ICollectionFixture<PostgresFixture>
{
}
