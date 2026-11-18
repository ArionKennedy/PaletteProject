using Palette.Tests.Infrastructure;
using Xunit;

namespace Palette.Tests.Integration.Database;

[Collection("Postgres Integration Tests")]
public class DatabaseConnectionTests
{
    private readonly PostgresFixture _fixture;

    public DatabaseConnectionTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CanConnectToTestDatabase()
    {
        // arrange
        await using var db = _fixture.CreateContext();

        // act
        var connected = await db.Database.CanConnectAsync();

        // assert
        Assert.True(connected);
    }
}
