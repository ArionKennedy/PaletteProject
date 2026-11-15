using Microsoft.EntityFrameworkCore;
using Npgsql;
using Palette.Api.Models;
using Palette.Api.Enums;
using Palette.Tests.Infrastructure;
using Xunit;

namespace Palette.Tests.Integration.Database;

[Collection("Postgres Integration Tests")]
public class UserMediaLogDatabaseTests
{
    private readonly PostgresFixture _fixture;

    public UserMediaLogDatabaseTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CanSaveUserMediaLog()
    {
        // arrange
        await using var db = _fixture.CreateContext();
        await using var transaction = await db.Database.BeginTransactionAsync();

        var userId = Guid.NewGuid();
        var mediaId = Guid.NewGuid();
        var logId = Guid.NewGuid();

        var user = new User
        {
            UserId = userId,
            Auth0Id = $"test-auth-{userId}",
            Username = "testuser",
            Email = $"test-{userId}@example.com",
            DateJoined = DateTime.UtcNow
        };

        var media = new Media
        {
            MediaId = mediaId,
            Title = "Integration Test Movie",
            MediaType = MediaType.Movie
        };

        var log = new UserMediaLog
        {
            LogId = logId,
            UserId = userId,
            MediaId = mediaId,
            LoggedDate = DateOnly.FromDateTime(DateTime.UtcNow)
        };

        // act
        db.Set<User>().Add(user);
        db.Set<Media>().Add(media);
        await db.SaveChangesAsync();

        db.Set<UserMediaLog>().Add(log);
        await db.SaveChangesAsync();

        db.ChangeTracker.Clear();

        var saved = await db.Set<UserMediaLog>().SingleOrDefaultAsync(l => l.LogId == logId);

        // assert
        Assert.NotNull(saved);
        Assert.Equal(userId, saved.UserId);
        Assert.Equal(mediaId, saved.MediaId);
        Assert.Equal(logId, saved.LogId);
    }

    [Fact]
    public async Task CannotSaveLogWithoutUser()
    {
        // arrange
        await using var db = _fixture.CreateContext();
        await using var transaction =
            await db.Database.BeginTransactionAsync();

        var mediaId = Guid.NewGuid();

        var media = new Media
        {
            MediaId = mediaId,
            Title = "Integration Test Movie",
            MediaType = MediaType.Movie
        };

        db.Set<Media>().Add(media);
        await db.SaveChangesAsync();

        var log = new UserMediaLog
        {
            LogId = Guid.NewGuid(),
            UserId = Guid.NewGuid(), // Nonexistent user
            MediaId = mediaId,
            LoggedDate = DateOnly.FromDateTime(DateTime.UtcNow)
        };

        db.Set<UserMediaLog>().Add(log);

        // act
        var exception = await Assert.ThrowsAsync<DbUpdateException>(
            () => db.SaveChangesAsync());

        // assert
        var postgresException = Assert.IsType<PostgresException>(exception.InnerException);

        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, postgresException.SqlState);
        Assert.Equal("FK_USER_MEDIA_LOGS_USERS_UserId", postgresException.ConstraintName);
    }

    [Fact]
    public async Task DeletingUserCascadesToLogs()
    {
        // arrange
        await using var db = _fixture.CreateContext();
        await using var transaction = await db.Database.BeginTransactionAsync();

        var userId = Guid.NewGuid();
        var mediaId = Guid.NewGuid();
        var logId = Guid.NewGuid();

        var user = new User
        {
            UserId = userId,
            Auth0Id = $"test-auth-{userId}",
            Username = "testuser",
            Email = $"test-{userId}@example.com",
            DateJoined = DateTime.UtcNow
        };

        var media = new Media
        {
            MediaId = mediaId,
            Title = "Integration Test Movie",
            MediaType = MediaType.Movie
        };

        var log = new UserMediaLog
        {
            LogId = logId,
            UserId = userId,
            MediaId = mediaId,
            LoggedDate = DateOnly.FromDateTime(DateTime.UtcNow)
        };

        db.Set<User>().Add(user);
        db.Set<Media>().Add(media);
        await db.SaveChangesAsync();

        db.Set<UserMediaLog>().Add(log);
        await db.SaveChangesAsync();

        // clear tracked entities to test database-side cascade.
        db.ChangeTracker.Clear();

        // act - delete User
        var savedUser = await db.Set<User>().SingleAsync(u => u.UserId == userId);

        db.Set<User>().Remove(savedUser);
        await db.SaveChangesAsync();

        db.ChangeTracker.Clear();

        var savedLog = await db.Set<UserMediaLog>().SingleOrDefaultAsync(l => l.LogId == logId);
        var savedMedia = await db.Set<Media>().SingleOrDefaultAsync(m => m.MediaId == mediaId);

        // assert
        Assert.Null(savedLog);
        Assert.NotNull(savedMedia);
        Assert.Equal(mediaId, savedMedia.MediaId);
    }
}
