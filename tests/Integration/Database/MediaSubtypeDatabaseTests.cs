using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Palette.Api.Data;
using Palette.Api.Enums;
using Palette.Api.Models;
using Palette.Tests.Infrastructure;
using Xunit;

namespace Palette.Tests.Integration.Database;

[Collection("Postgres Integration Tests")]
public class MediaSubtypeDatabaseTests
{
    private readonly PostgresFixture _fixture;

    public MediaSubtypeDatabaseTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Theory]
    [InlineData(MediaType.Movie)]
    [InlineData(MediaType.TVShow)]
    [InlineData(MediaType.Book)]
    [InlineData(MediaType.Game)]
    public async Task CanPersistSubtypeAndLoadItThroughMedia(
        MediaType mediaType)
    {
        await using var db = _fixture.CreateContext();
        await using var transaction =
            await db.Database.BeginTransactionAsync();

        var mediaId = Guid.NewGuid();
        var externalId = CreateExternalId();

        AddSubtype(
            db,
            mediaType,
            mediaId,
            externalId,
            includeMedia: true);

        await db.SaveChangesAsync();

        db.ChangeTracker.Clear();

        var saved = await db.Media
            .AsNoTracking()
            .Include(media => media.Movie)
            .Include(media => media.TvShow)
            .Include(media => media.Book)
            .Include(media => media.Game)
            .SingleAsync(media => media.MediaId == mediaId);

        Assert.Equal(mediaType, saved.MediaType);
        Assert.Equal("Subtype test media", saved.Title);
        Assert.Equal(
            new DateOnly(2024, 3, 15),
            saved.ReleaseDate);
        Assert.Equal(
            "Subtype test description.",
            saved.Description);
        Assert.Equal(
            "https://images.example/poster.jpg",
            saved.PosterUrl);

        switch (mediaType)
        {
            case MediaType.Movie:
                Assert.NotNull(saved.Movie);
                Assert.Equal(mediaId, saved.Movie.MediaId);
                Assert.Equal(
                    externalId,
                    saved.Movie.TmdbMovieId);
                Assert.Equal(180, saved.Movie.RuntimeMinutes);

                Assert.Null(saved.TvShow);
                Assert.Null(saved.Book);
                Assert.Null(saved.Game);
                break;

            case MediaType.TVShow:
                Assert.NotNull(saved.TvShow);
                Assert.Equal(mediaId, saved.TvShow.MediaId);
                Assert.Equal(
                    externalId,
                    saved.TvShow.TmdbTvId);
                Assert.Equal(3, saved.TvShow.NumberOfSeasons);

                Assert.Null(saved.Movie);
                Assert.Null(saved.Book);
                Assert.Null(saved.Game);
                break;

            case MediaType.Book:
                Assert.NotNull(saved.Book);
                Assert.Equal(mediaId, saved.Book.MediaId);
                Assert.Equal(
                    $"OL{externalId}W",
                    saved.Book.OpenLibraryId);
                Assert.Equal(
                    "9781234567897",
                    saved.Book.ISBN);

                Assert.Null(saved.Movie);
                Assert.Null(saved.TvShow);
                Assert.Null(saved.Game);
                break;

            case MediaType.Game:
                Assert.NotNull(saved.Game);
                Assert.Equal(mediaId, saved.Game.MediaId);
                Assert.Equal(
                    externalId,
                    saved.Game.RawgGameId);

                Assert.Null(saved.Movie);
                Assert.Null(saved.TvShow);
                Assert.Null(saved.Book);
                break;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(mediaType));
        }
    }

    [Theory]
    [InlineData(
        MediaType.Movie,
        "FK_MOVIES_MEDIA_MediaId")]
    [InlineData(
        MediaType.TVShow,
        "FK_TV_SHOWS_MEDIA_MediaId")]
    [InlineData(
        MediaType.Book,
        "FK_BOOKS_MEDIA_MediaId")]
    [InlineData(
        MediaType.Game,
        "FK_GAMES_MEDIA_MediaId")]
    public async Task CannotSaveSubtypeWithoutExistingMedia(
        MediaType mediaType,
        string expectedConstraint)
    {
        await using var db = _fixture.CreateContext();
        await using var transaction =
            await db.Database.BeginTransactionAsync();

        AddSubtype(
            db,
            mediaType,
            Guid.NewGuid(),
            CreateExternalId(),
            includeMedia: false);

        var exception = await Assert.ThrowsAsync<DbUpdateException>(
            () => db.SaveChangesAsync());

        AssertConstraint(
            exception,
            PostgresErrorCodes.ForeignKeyViolation,
            expectedConstraint);
    }

    [Theory]
    [InlineData(
        MediaType.Movie,
        "IX_MOVIES_TmdbMovieId")]
    [InlineData(
        MediaType.TVShow,
        "IX_TV_SHOWS_TmdbTvId")]
    [InlineData(
        MediaType.Book,
        "IX_BOOKS_OpenLibraryId")]
    [InlineData(
        MediaType.Game,
        "IX_GAMES_RawgGameId")]
    public async Task CannotSaveDuplicateExternalId(
        MediaType mediaType,
        string expectedConstraint)
    {
        await using var db = _fixture.CreateContext();
        await using var transaction =
            await db.Database.BeginTransactionAsync();

        var externalId = CreateExternalId();

        AddSubtype(
            db,
            mediaType,
            Guid.NewGuid(),
            externalId,
            includeMedia: true);

        await db.SaveChangesAsync();

        db.ChangeTracker.Clear();

        // Use a different Media primary key so the failure can only
        // be caused by the duplicated external identifier.
        AddSubtype(
            db,
            mediaType,
            Guid.NewGuid(),
            externalId,
            includeMedia: true);

        var exception = await Assert.ThrowsAsync<DbUpdateException>(
            () => db.SaveChangesAsync());

        AssertConstraint(
            exception,
            PostgresErrorCodes.UniqueViolation,
            expectedConstraint);
    }

    [Theory]
    [InlineData(MediaType.Movie)]
    [InlineData(MediaType.TVShow)]
    [InlineData(MediaType.Book)]
    [InlineData(MediaType.Game)]
    public async Task DeletingMediaCascadesToSubtypeButPreservesOtherRecords(
        MediaType mediaType)
    {
        await using var db = _fixture.CreateContext();
        await using var transaction =
            await db.Database.BeginTransactionAsync();

        var removedId = Guid.NewGuid();
        var preservedId = Guid.NewGuid();
        var externalId = CreateExternalId();

        AddSubtype(
            db,
            mediaType,
            removedId,
            externalId,
            includeMedia: true);

        AddSubtype(
            db,
            mediaType,
            preservedId,
            externalId + 1,
            includeMedia: true);

        await db.SaveChangesAsync();

        // Keep dependent entities untracked so PostgreSQL, rather
        // than EF's tracked graph, performs the cascade.
        db.ChangeTracker.Clear();

        var media = await db.Media.SingleAsync(
            value => value.MediaId == removedId);

        db.Media.Remove(media);
        await db.SaveChangesAsync();

        db.ChangeTracker.Clear();

        Assert.False(await db.Media.AnyAsync(
            value => value.MediaId == removedId));

        Assert.False(await SubtypeExistsAsync(
            db,
            mediaType,
            removedId));

        Assert.True(await db.Media.AnyAsync(
            value => value.MediaId == preservedId));

        Assert.True(await SubtypeExistsAsync(
            db,
            mediaType,
            preservedId));
    }

    [Theory]
    [InlineData(MediaType.Movie)]
    [InlineData(MediaType.TVShow)]
    [InlineData(MediaType.Book)]
    [InlineData(MediaType.Game)]
    public async Task DeletingSubtypeDoesNotDeleteParentMedia(
        MediaType mediaType)
    {
        await using var db = _fixture.CreateContext();
        await using var transaction =
            await db.Database.BeginTransactionAsync();

        var mediaId = Guid.NewGuid();

        AddSubtype(
            db,
            mediaType,
            mediaId,
            CreateExternalId(),
            includeMedia: true);

        await db.SaveChangesAsync();

        db.ChangeTracker.Clear();

        object subtype = mediaType switch
        {
            MediaType.Movie => await db.Set<Movie>()
                .SingleAsync(value => value.MediaId == mediaId),

            MediaType.TVShow => await db.Set<TvShow>()
                .SingleAsync(value => value.MediaId == mediaId),

            MediaType.Book => await db.Set<Book>()
                .SingleAsync(value => value.MediaId == mediaId),

            MediaType.Game => await db.Set<Game>()
                .SingleAsync(value => value.MediaId == mediaId),

            _ => throw new ArgumentOutOfRangeException(
                nameof(mediaType)),
        };

        db.Remove(subtype);
        await db.SaveChangesAsync();

        db.ChangeTracker.Clear();

        Assert.False(await SubtypeExistsAsync(
            db,
            mediaType,
            mediaId));

        var media = await db.Media
            .AsNoTracking()
            .SingleAsync(value => value.MediaId == mediaId);

        Assert.Equal("Subtype test media", media.Title);
        Assert.Equal(mediaType, media.MediaType);
    }

    [Fact]
    public async Task MovieCanHaveUnknownRuntime()
    {
        await using var db = _fixture.CreateContext();
        await using var transaction =
            await db.Database.BeginTransactionAsync();

        var mediaId = Guid.NewGuid();

        db.Set<Movie>().Add(new Movie
        {
            MediaId = mediaId,
            TmdbMovieId = CreateExternalId(),
            RuntimeMinutes = null,
            Media = CreateMedia(mediaId, MediaType.Movie),
        });

        await db.SaveChangesAsync();

        db.ChangeTracker.Clear();

        var saved = await db.Set<Movie>()
            .AsNoTracking()
            .SingleAsync(value => value.MediaId == mediaId);

        Assert.Null(saved.RuntimeMinutes);
    }

    [Fact]
    public async Task TvShowCanHaveUnknownSeasonCount()
    {
        await using var db = _fixture.CreateContext();
        await using var transaction =
            await db.Database.BeginTransactionAsync();

        var mediaId = Guid.NewGuid();

        db.Set<TvShow>().Add(new TvShow
        {
            MediaId = mediaId,
            TmdbTvId = CreateExternalId(),
            NumberOfSeasons = null,
            Media = CreateMedia(mediaId, MediaType.TVShow),
        });

        await db.SaveChangesAsync();

        db.ChangeTracker.Clear();

        var saved = await db.Set<TvShow>()
            .AsNoTracking()
            .SingleAsync(value => value.MediaId == mediaId);

        Assert.Null(saved.NumberOfSeasons);
    }

    [Fact]
    public async Task BookCanHaveNoIsbn()
    {
        await using var db = _fixture.CreateContext();
        await using var transaction =
            await db.Database.BeginTransactionAsync();

        var mediaId = Guid.NewGuid();
        var openLibraryId = $"OL{Guid.NewGuid():N}W";

        db.Set<Book>().Add(new Book
        {
            MediaId = mediaId,
            OpenLibraryId = openLibraryId,
            ISBN = null,
            Media = CreateMedia(mediaId, MediaType.Book),
        });

        await db.SaveChangesAsync();

        db.ChangeTracker.Clear();

        var saved = await db.Set<Book>()
            .AsNoTracking()
            .SingleAsync(value => value.MediaId == mediaId);

        Assert.Equal(openLibraryId, saved.OpenLibraryId);
        Assert.Null(saved.ISBN);
    }

    [Fact]
    public async Task BookCannotHaveNullOpenLibraryId()
    {
        await using var db = _fixture.CreateContext();
        await using var transaction =
            await db.Database.BeginTransactionAsync();

        var mediaId = Guid.NewGuid();

        db.Set<Book>().Add(new Book
        {
            MediaId = mediaId,
            OpenLibraryId = null!,
            ISBN = null,
            Media = CreateMedia(mediaId, MediaType.Book),
        });

        var exception = await Assert.ThrowsAsync<DbUpdateException>(
            () => db.SaveChangesAsync());

        var postgres = Assert.IsType<PostgresException>(
            exception.InnerException);

        Assert.Equal(
            PostgresErrorCodes.NotNullViolation,
            postgres.SqlState);
        Assert.Equal("BOOKS", postgres.TableName);
        Assert.Equal("OpenLibraryId", postgres.ColumnName);
    }

    private static void AddSubtype(
        PaletteDbContext db,
        MediaType mediaType,
        Guid mediaId,
        int externalId,
        bool includeMedia)
    {
        var media = includeMedia
            ? CreateMedia(mediaId, mediaType)
            : null;

        switch (mediaType)
        {
            case MediaType.Movie:
                db.Set<Movie>().Add(new Movie
                {
                    MediaId = mediaId,
                    TmdbMovieId = externalId,
                    RuntimeMinutes = 180,
                    Media = media!,
                });
                break;

            case MediaType.TVShow:
                db.Set<TvShow>().Add(new TvShow
                {
                    MediaId = mediaId,
                    TmdbTvId = externalId,
                    NumberOfSeasons = 3,
                    Media = media!,
                });
                break;

            case MediaType.Book:
                db.Set<Book>().Add(new Book
                {
                    MediaId = mediaId,
                    OpenLibraryId = $"OL{externalId}W",
                    ISBN = "9781234567897",
                    Media = media!,
                });
                break;

            case MediaType.Game:
                db.Set<Game>().Add(new Game
                {
                    MediaId = mediaId,
                    RawgGameId = externalId,
                    Media = media!,
                });
                break;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(mediaType));
        }
    }

    private static Task<bool> SubtypeExistsAsync(
        PaletteDbContext db,
        MediaType mediaType,
        Guid mediaId)
    {
        return mediaType switch
        {
            MediaType.Movie => db.Set<Movie>()
                .AnyAsync(value => value.MediaId == mediaId),

            MediaType.TVShow => db.Set<TvShow>()
                .AnyAsync(value => value.MediaId == mediaId),

            MediaType.Book => db.Set<Book>()
                .AnyAsync(value => value.MediaId == mediaId),

            MediaType.Game => db.Set<Game>()
                .AnyAsync(value => value.MediaId == mediaId),

            _ => throw new ArgumentOutOfRangeException(
                nameof(mediaType)),
        };
    }

    private static Media CreateMedia(
        Guid mediaId,
        MediaType mediaType)
    {
        return new Media
        {
            MediaId = mediaId,
            Title = "Subtype test media",
            MediaType = mediaType,
            ReleaseDate = new DateOnly(2024, 3, 15),
            Description = "Subtype test description.",
            PosterUrl = "https://images.example/poster.jpg",
        };
    }

    private static int CreateExternalId()
    {
        // Leave room for a second, distinct identifier in cascade tests.
        return Random.Shared.Next(1, int.MaxValue - 1);
    }

    private static void AssertConstraint(
        DbUpdateException exception,
        string expectedSqlState,
        string expectedConstraint)
    {
        var postgres = Assert.IsType<PostgresException>(
            exception.InnerException);

        Assert.Equal(expectedSqlState, postgres.SqlState);
        Assert.Equal(expectedConstraint, postgres.ConstraintName);
    }
}
