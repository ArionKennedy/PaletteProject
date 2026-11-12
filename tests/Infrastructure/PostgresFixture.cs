using Microsoft.EntityFrameworkCore;
using Npgsql;
using Palette.Api.Data;
using Xunit;

namespace Palette.Tests.Infrastructure;

public class PostgresFixture : IAsyncLifetime
{
    private readonly DbContextOptions<PaletteDbContext> _options;

    public PostgresFixture()
    {
        var connectionString = Environment.GetEnvironmentVariable(
            "PALETTE_TEST_DB_CONNECTION")
            ?? throw new InvalidOperationException(
                "PALETTE_TEST_DB_CONNECTION is not configured.");

        var connection = new NpgsqlConnectionStringBuilder(connectionString);

        // prevent accidentally testing against Supabase or development DB.
        if (connection.Database != "palette_integration_test" ||
            (connection.Host != "localhost" &&
            connection.Host != "127.0.0.1"))
        {
            throw new InvalidOperationException(
                "Integration tests must use the local test database.");
        }

        _options = new DbContextOptionsBuilder<PaletteDbContext>()
            .UseNpgsql(connectionString)
            .Options;
    }

    public PaletteDbContext CreateContext()
    {
        return new PaletteDbContext(_options);
    }

    public async Task InitializeAsync()
    {
        await using var db = CreateContext();
        await db.Database.MigrateAsync();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }
}

// share the fixture across database integration test classes.
[CollectionDefinition("Postgres Integration Tests")]
public class PostgresTestCollection : ICollectionFixture<PostgresFixture>
{
}
