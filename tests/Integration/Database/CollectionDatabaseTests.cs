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
public class CollectionDatabaseTests
{
    private readonly PostgresFixture _fixture;

    private static readonly DateTime TestTime =
        new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    public CollectionDatabaseTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CanRetrieveCollectionItemWithCollectionUserAndMedia()
    {
        await using var db = _fixture.CreateContext();
        await using var transaction =
            await db.Database.BeginTransactionAsync();

        var seed = await SeedAsync(db);
        var item = CreateItem(seed.Collection, seed.Media);

        db.CollectionItems.Add(item);
        await db.SaveChangesAsync();

        db.ChangeTracker.Clear();

        var saved = await db.CollectionItems
            .AsNoTracking()
            .Include(value => value.Collection)
                .ThenInclude(collection => collection.User)
            .Include(value => value.Media)
            .SingleAsync(value =>
                value.CollectionId == seed.Collection.CollectionId
                && value.MediaId == seed.Media.MediaId);

        Assert.Equal(
            seed.Collection.CollectionId,
            saved.CollectionId);
        Assert.Equal(seed.Media.MediaId, saved.MediaId);
        Assert.Equal(TestTime, saved.AddedAt);

        Assert.Equal(
            seed.Collection.Name,
            saved.Collection.Name);
        Assert.Equal(
            TestTime,
            saved.Collection.CreatedAt);
        Assert.Equal(
            seed.User.UserId,
            saved.Collection.UserId);
        Assert.Equal(
            seed.User.Auth0Id,
            saved.Collection.User.Auth0Id);

        Assert.Equal(seed.Media.Title, saved.Media.Title);
        Assert.Equal(MediaType.Movie, saved.Media.MediaType);
    }

    [Fact]
    public async Task CannotSaveCollectionWithoutExistingUser()
    {
        await using var db = _fixture.CreateContext();
        await using var transaction =
            await db.Database.BeginTransactionAsync();

        db.Collections.Add(new Collection
        {
            CollectionId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Name = "Invalid owner",
            CreatedAt = TestTime,
        });

        var exception = await Assert.ThrowsAsync<DbUpdateException>(
            () => db.SaveChangesAsync());

        AssertConstraint(
            exception,
            PostgresErrorCodes.ForeignKeyViolation,
            "FK_COLLECTIONS_USERS_UserId");
    }

    [Fact]
    public async Task CannotSaveCollectionWithNullName()
    {
        await using var db = _fixture.CreateContext();
        await using var transaction =
            await db.Database.BeginTransactionAsync();

        var seed = await SeedAsync(db);

        db.Collections.Add(new Collection
        {
            CollectionId = Guid.NewGuid(),
            UserId = seed.User.UserId,
            Name = null!,
            CreatedAt = TestTime,
        });

        var exception = await Assert.ThrowsAsync<DbUpdateException>(
            () => db.SaveChangesAsync());

        var postgres = Assert.IsType<PostgresException>(
            exception.InnerException);

        Assert.Equal(
            PostgresErrorCodes.NotNullViolation,
            postgres.SqlState);
        Assert.Equal("COLLECTIONS", postgres.TableName);
        Assert.Equal("Name", postgres.ColumnName);
    }

    [Theory]
    [InlineData(true, "FK_COLLECTION_ITEMS_COLLECTIONS_CollectionId")]
    [InlineData(false, "FK_COLLECTION_ITEMS_MEDIA_MediaId")]
    public async Task CannotSaveItemWithMissingParent(
        bool missingCollection,
        string expectedConstraint)
    {
        await using var db = _fixture.CreateContext();
        await using var transaction =
            await db.Database.BeginTransactionAsync();

        var seed = await SeedAsync(db);

        db.CollectionItems.Add(new CollectionItem
        {
            CollectionId = missingCollection
                ? Guid.NewGuid()
                : seed.Collection.CollectionId,

            MediaId = missingCollection
                ? seed.Media.MediaId
                : Guid.NewGuid(),

            AddedAt = TestTime,
        });

        var exception = await Assert.ThrowsAsync<DbUpdateException>(
            () => db.SaveChangesAsync());

        AssertConstraint(
            exception,
            PostgresErrorCodes.ForeignKeyViolation,
            expectedConstraint);
    }

    [Fact]
    public async Task CannotAddSameMediaTwiceToOneCollection()
    {
        await using var db = _fixture.CreateContext();
        await using var transaction =
            await db.Database.BeginTransactionAsync();

        var seed = await SeedAsync(db);

        db.CollectionItems.Add(
            CreateItem(seed.Collection, seed.Media));

        await db.SaveChangesAsync();

        // Clear tracking so EF does not reject the duplicate key itself.
        // The assertion must exercise PostgreSQL's actual constraint.
        db.ChangeTracker.Clear();

        db.CollectionItems.Add(new CollectionItem
        {
            CollectionId = seed.Collection.CollectionId,
            MediaId = seed.Media.MediaId,
            AddedAt = TestTime.AddDays(1),
        });

        var exception = await Assert.ThrowsAsync<DbUpdateException>(
            () => db.SaveChangesAsync());

        AssertConstraint(
            exception,
            PostgresErrorCodes.UniqueViolation,
            "PK_COLLECTION_ITEMS");
    }

    [Fact]
    public async Task SameMediaCanBelongToDifferentCollections()
    {
        await using var db = _fixture.CreateContext();
        await using var transaction =
            await db.Database.BeginTransactionAsync();

        var seed = await SeedAsync(db);

        var secondCollection = new Collection
        {
            CollectionId = Guid.NewGuid(),
            UserId = seed.User.UserId,
            Name = "Second collection",
            CreatedAt = TestTime,
        };

        db.Collections.Add(secondCollection);

        db.CollectionItems.Add(
            CreateItem(seed.Collection, seed.Media));

        db.CollectionItems.Add(
            CreateItem(secondCollection, seed.Media));

        await db.SaveChangesAsync();

        db.ChangeTracker.Clear();

        Assert.True(await db.CollectionItems.AnyAsync(item =>
            item.CollectionId == seed.Collection.CollectionId
            && item.MediaId == seed.Media.MediaId));

        Assert.True(await db.CollectionItems.AnyAsync(item =>
            item.CollectionId == secondCollection.CollectionId
            && item.MediaId == seed.Media.MediaId));
    }

    [Fact]
    public async Task DeletingCollectionRemovesItsItemsButPreservesOtherData()
    {
        await using var db = _fixture.CreateContext();
        await using var transaction =
            await db.Database.BeginTransactionAsync();

        var seed = await SeedAsync(db);

        var otherCollection = new Collection
        {
            CollectionId = Guid.NewGuid(),
            UserId = seed.User.UserId,
            Name = "Keep this collection",
            CreatedAt = TestTime,
        };

        db.Collections.Add(otherCollection);

        db.CollectionItems.Add(
            CreateItem(seed.Collection, seed.Media));

        db.CollectionItems.Add(
            CreateItem(otherCollection, seed.Media));

        await db.SaveChangesAsync();

        // Load only the collection before deleting it.
        // Its items remain untracked, so PostgreSQL performs the cascade.
        db.ChangeTracker.Clear();

        var collection = await db.Collections.SingleAsync(value =>
            value.CollectionId == seed.Collection.CollectionId);

        db.Collections.Remove(collection);
        await db.SaveChangesAsync();

        db.ChangeTracker.Clear();

        Assert.False(await db.Collections.AnyAsync(value =>
            value.CollectionId == seed.Collection.CollectionId));

        Assert.False(await db.CollectionItems.AnyAsync(value =>
            value.CollectionId == seed.Collection.CollectionId));

        Assert.True(await db.Users.AnyAsync(value =>
            value.UserId == seed.User.UserId));

        Assert.True(await db.Media.AnyAsync(value =>
            value.MediaId == seed.Media.MediaId));

        Assert.True(await db.Collections.AnyAsync(value =>
            value.CollectionId == otherCollection.CollectionId));

        Assert.True(await db.CollectionItems.AnyAsync(value =>
            value.CollectionId == otherCollection.CollectionId
            && value.MediaId == seed.Media.MediaId));
    }

    [Fact]
    public async Task DeletingMediaRemovesItsItemsButPreservesCollection()
    {
        await using var db = _fixture.CreateContext();
        await using var transaction =
            await db.Database.BeginTransactionAsync();

        var seed = await SeedAsync(db);

        var otherMedia = CreateMedia();

        db.Media.Add(otherMedia);

        db.CollectionItems.Add(
            CreateItem(seed.Collection, seed.Media));

        db.CollectionItems.Add(
            CreateItem(seed.Collection, otherMedia));

        await db.SaveChangesAsync();

        db.ChangeTracker.Clear();

        var media = await db.Media.SingleAsync(value =>
            value.MediaId == seed.Media.MediaId);

        db.Media.Remove(media);
        await db.SaveChangesAsync();

        db.ChangeTracker.Clear();

        Assert.False(await db.Media.AnyAsync(value =>
            value.MediaId == seed.Media.MediaId));

        Assert.False(await db.CollectionItems.AnyAsync(value =>
            value.MediaId == seed.Media.MediaId));

        Assert.True(await db.Collections.AnyAsync(value =>
            value.CollectionId == seed.Collection.CollectionId));

        Assert.True(await db.Users.AnyAsync(value =>
            value.UserId == seed.User.UserId));

        Assert.True(await db.Media.AnyAsync(value =>
            value.MediaId == otherMedia.MediaId));

        Assert.True(await db.CollectionItems.AnyAsync(value =>
            value.CollectionId == seed.Collection.CollectionId
            && value.MediaId == otherMedia.MediaId));
    }

    [Fact]
    public async Task DeletingUserCascadesThroughCollectionsToItems()
    {
        await using var db = _fixture.CreateContext();
        await using var transaction =
            await db.Database.BeginTransactionAsync();

        var removed = await SeedAsync(db);
        var preserved = await SeedAsync(db);

        db.CollectionItems.Add(
            CreateItem(removed.Collection, removed.Media));

        db.CollectionItems.Add(
            CreateItem(preserved.Collection, preserved.Media));

        await db.SaveChangesAsync();

        db.ChangeTracker.Clear();

        var user = await db.Users.SingleAsync(value =>
            value.UserId == removed.User.UserId);

        db.Users.Remove(user);
        await db.SaveChangesAsync();

        db.ChangeTracker.Clear();

        Assert.False(await db.Users.AnyAsync(value =>
            value.UserId == removed.User.UserId));

        Assert.False(await db.Collections.AnyAsync(value =>
            value.CollectionId == removed.Collection.CollectionId));

        Assert.False(await db.CollectionItems.AnyAsync(value =>
            value.CollectionId == removed.Collection.CollectionId));

        // Deleting a user's collection must not delete shared media.
        Assert.True(await db.Media.AnyAsync(value =>
            value.MediaId == removed.Media.MediaId));

        // Another user's records must remain.
        Assert.True(await db.Users.AnyAsync(value =>
            value.UserId == preserved.User.UserId));

        Assert.True(await db.Collections.AnyAsync(value =>
            value.CollectionId == preserved.Collection.CollectionId));

        Assert.True(await db.CollectionItems.AnyAsync(value =>
            value.CollectionId == preserved.Collection.CollectionId
            && value.MediaId == preserved.Media.MediaId));

        Assert.True(await db.Media.AnyAsync(value =>
            value.MediaId == preserved.Media.MediaId));
    }

    [Fact]
    public async Task DeletingItemPreservesCollectionUserAndMedia()
    {
        await using var db = _fixture.CreateContext();
        await using var transaction =
            await db.Database.BeginTransactionAsync();

        var seed = await SeedAsync(db);

        db.CollectionItems.Add(
            CreateItem(seed.Collection, seed.Media));

        await db.SaveChangesAsync();

        db.ChangeTracker.Clear();

        var item = await db.CollectionItems.SingleAsync(value =>
            value.CollectionId == seed.Collection.CollectionId
            && value.MediaId == seed.Media.MediaId);

        db.CollectionItems.Remove(item);
        await db.SaveChangesAsync();

        db.ChangeTracker.Clear();

        Assert.False(await db.CollectionItems.AnyAsync(value =>
            value.CollectionId == seed.Collection.CollectionId
            && value.MediaId == seed.Media.MediaId));

        Assert.True(await db.Collections.AnyAsync(value =>
            value.CollectionId == seed.Collection.CollectionId));

        Assert.True(await db.Users.AnyAsync(value =>
            value.UserId == seed.User.UserId));

        Assert.True(await db.Media.AnyAsync(value =>
            value.MediaId == seed.Media.MediaId));
    }

    private static async Task<(
        User User,
        Collection Collection,
        Media Media)> SeedAsync(PaletteDbContext db)
    {
        var userId = Guid.NewGuid();

        var user = new User
        {
            UserId = userId,
            Auth0Id = $"integration|{userId:N}",
            Username = $"user_{userId:N}",
            Email = $"user-{userId:N}@example.com",
            DateJoined = TestTime,
        };

        var collection = new Collection
        {
            CollectionId = Guid.NewGuid(),
            UserId = userId,
            User = user,
            Name = "Integration test collection",
            CreatedAt = TestTime,
        };

        var media = CreateMedia();

        // Adding the collection also adds its new User navigation.
        db.Collections.Add(collection);
        db.Media.Add(media);

        await db.SaveChangesAsync();

        return (user, collection, media);
    }

    private static Media CreateMedia()
    {
        var id = Guid.NewGuid();

        return new Media
        {
            MediaId = id,
            Title = $"Collection test media {id:N}",
            MediaType = MediaType.Movie,
            ReleaseDate = new DateOnly(2024, 3, 15),
            Description = "Integration test description.",
            PosterUrl = "https://images.example/poster.jpg",
        };
    }

    private static CollectionItem CreateItem(
        Collection collection,
        Media media)
    {
        return new CollectionItem
        {
            CollectionId = collection.CollectionId,
            MediaId = media.MediaId,
            Collection = collection,
            Media = media,
            AddedAt = TestTime,
        };
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
