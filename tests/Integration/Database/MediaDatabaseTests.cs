using Microsoft.EntityFrameworkCore;
using Palette.Api.Models;
using Palette.Api.Enums;
using Palette.Tests.Infrastructure;
using Xunit;

namespace Palette.Tests.Integration.Database;

[Collection("Postgres Integration Tests")]
public class MediaDatabaseTests
{
    private readonly PostgresFixture _fixture;

    public MediaDatabaseTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CanSaveAndRetreveMedia()
    {
        // arrange
        await using var db = _fixture.CreateContext();
        await using var transaction = await db.Database.BeginTransactionAsync();

        var mediaId = Guid.NewGuid();

        var media = new Media
        {
            MediaId = mediaId,
            Title = "Integration Test Movie",
            MediaType = MediaType.Movie
        };

        // act
        db.Media.Add(media);
        await db.SaveChangesAsync();

        // clear tracked entities so the next query reads from PostgreSQL
        db.ChangeTracker.Clear();

        var saved = await db.Media.SingleOrDefaultAsync(m => m.MediaId == mediaId);

        // assert
        Assert.NotNull(saved);
        Assert.Equal("Integration Test Movie", saved.Title);
        Assert.Equal(MediaType.Movie, saved.MediaType);
    }
}
