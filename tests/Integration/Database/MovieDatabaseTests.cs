using Microsoft.EntityFrameworkCore;
using Npgsql;
using Palette.Api.Models;
using Palette.Api.Enums;
using Palette.Tests.Infrastructure;
using Xunit;

namespace Palette.Tests.Integration.Database;

[Collection("Postgres Integration Tests")]
public class MovieDatabaseTests
{
    private readonly PostgresFixture _fixture;

    public MovieDatabaseTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CanSaveMovieWithMedia()
    {
        // arrange
        await using var db = _fixture.CreateContext();
        await using var transaction = await db.Database.BeginTransactionAsync();

        var mediaId = Guid.NewGuid();

        var movie = new Movie
        {
            MediaId = mediaId,
            TmdbMovieId = 123456,
            RuntimeMinutes = 120,
            Media = new Media
            {
                MediaId = mediaId,
                Title = "Integration Test Movie",
                MediaType = MediaType.Movie
            }
        };

        // act
        db.Set<Movie>().Add(movie);
        await db.SaveChangesAsync();

        db.ChangeTracker.Clear();

        var saved = await db.Set<Movie>()
            .Include(m => m.Media)
            .SingleOrDefaultAsync(m => m.MediaId == mediaId);

        // assert
        Assert.NotNull(saved);
        Assert.Equal(123456, saved.TmdbMovieId);
        Assert.Equal(120, saved.RuntimeMinutes);
        Assert.Equal("Integration Test Movie", saved.Media.Title);
        Assert.Equal(MediaType.Movie, saved.Media.MediaType);
    }

    [Fact]
    public async Task CannotSaveMovieWithoutMedia()
    {
        // arrange
        await using var db = _fixture.CreateContext();
        await using var transaction = await db.Database.BeginTransactionAsync();

        var movie = new Movie
        {
            MediaId = Guid.NewGuid(),
            TmdbMovieId = 123456,
            RuntimeMinutes = 120
        };

        db.Set<Movie>().Add(movie);

        // act
        var exception = await Assert.ThrowsAsync<DbUpdateException>(
            () => db.SaveChangesAsync());

        // assert
        var postgresException = Assert.IsType<PostgresException>(exception.InnerException);

        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, postgresException.SqlState);
    }

    [Fact]
    public async Task CannotSaveDuplicateTmdbMovieId()
    {
        // arrange
        await using var db = _fixture.CreateContext();
        await using var transaction = await db.Database.BeginTransactionAsync();

        var firstMovie = new Movie
        {
            MediaId = Guid.NewGuid(),
            TmdbMovieId = 123456,
            RuntimeMinutes = 120,
            Media = new Media
            {
                Title = "First Movie",
                MediaType = MediaType.Movie
            }
        };

        // act - save the first movie successfully
        db.Set<Movie>().Add(firstMovie);
        await db.SaveChangesAsync();

        var secondMovie = new Movie
        {
            MediaId = Guid.NewGuid(),
            TmdbMovieId = 123456,
            RuntimeMinutes = 100,
            Media = new Media
            {
                Title = "Second Movie",
                MediaType = MediaType.Movie
            }
        };

        db.Set<Movie>().Add(secondMovie);

        // act & assert - the second movie should fail
        var exception = await Assert.ThrowsAsync<DbUpdateException>(
            () => db.SaveChangesAsync());

        var postgresException = Assert.IsType<PostgresException>(exception.InnerException);
        Assert.Equal(PostgresErrorCodes.UniqueViolation, postgresException.SqlState);
        Assert.Equal("IX_MOVIES_TmdbMovieId", postgresException.ConstraintName);
    }

    [Fact]
    public async Task DeletingMediaCascadesToMovie()
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

        var movie = new Movie
        {
            MediaId = mediaId,
            TmdbMovieId = 123456,
            RuntimeMinutes = 120,
            Media = media
        };

        db.Set<Movie>().Add(movie);
        await db.SaveChangesAsync();

        // act - delete Media
        db.ChangeTracker.Clear();
        var savedMedia = await db.Set<Media>().SingleAsync(m => m.MediaId == mediaId);

        db.Set<Media>().Remove(savedMedia);
        await db.SaveChangesAsync();

        // assert
        db.ChangeTracker.Clear();
        var savedMovie = await db.Set<Movie>().SingleOrDefaultAsync(m => m.MediaId == mediaId);
        Assert.Null(savedMovie);
    }
}
