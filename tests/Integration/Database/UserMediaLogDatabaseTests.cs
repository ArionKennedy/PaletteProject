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

    [Fact]
    public async Task CannotSaveLogWithoutMedia()
    {
        await using var db = _fixture.CreateContext();
        await using var transaction =
            await db.Database.BeginTransactionAsync();

        var userId = Guid.NewGuid();

        var user = new User
        {
            UserId = userId,
            Auth0Id = $"integration|{userId:N}",
            Username = "Log foreign key test user",
            Email = $"{userId:N}@example.com",
            DateJoined = new DateTime(
                2026, 1, 1, 12, 0, 0, DateTimeKind.Utc),
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        db.ChangeTracker.Clear();

        db.UserMediaLogs.Add(new UserMediaLog
        {
            LogId = Guid.NewGuid(),
            UserId = userId,
            MediaId = Guid.NewGuid(),
            Status = "watched",
            LoggedDate = new DateOnly(2026, 1, 2),
        });

        var exception = await Assert.ThrowsAsync<DbUpdateException>(
            () => db.SaveChangesAsync());

        var postgres = Assert.IsType<PostgresException>(
            exception.InnerException);

        Assert.Equal(
            PostgresErrorCodes.ForeignKeyViolation,
            postgres.SqlState);

        Assert.Equal(
            "FK_USER_MEDIA_LOGS_MEDIA_MediaId",
            postgres.ConstraintName);
    }

    [Fact]
    public async Task DeletingMediaCascadesToLogsButPreservesUsersAndOtherLogs()
    {
        await using var db = _fixture.CreateContext();
        await using var transaction =
            await db.Database.BeginTransactionAsync();

        var testTime = new DateTime(
            2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        var loggedDate = new DateOnly(2026, 1, 2);

        var firstUserId = Guid.NewGuid();
        var secondUserId = Guid.NewGuid();

        db.Users.AddRange(
            new User
            {
                UserId = firstUserId,
                Auth0Id = $"integration|{firstUserId:N}",
                Username = "First viewer",
                Email = $"{firstUserId:N}@example.com",
                DateJoined = testTime,
            },
            new User
            {
                UserId = secondUserId,
                Auth0Id = $"integration|{secondUserId:N}",
                Username = "Second viewer",
                Email = $"{secondUserId:N}@example.com",
                DateJoined = testTime,
            });

        var removedMediaId = Guid.NewGuid();
        var preservedMediaId = Guid.NewGuid();

        db.Media.AddRange(
            new Media
            {
                MediaId = removedMediaId,
                Title = "Remove this media",
                MediaType = MediaType.Movie,
            },
            new Media
            {
                MediaId = preservedMediaId,
                Title = "Keep this media",
                MediaType = MediaType.Movie,
            });

        await db.SaveChangesAsync();

        var preservedLogId = Guid.NewGuid();

        db.UserMediaLogs.AddRange(
            new UserMediaLog
            {
                LogId = Guid.NewGuid(),
                UserId = firstUserId,
                MediaId = removedMediaId,
                Status = "watched",
                LoggedDate = loggedDate,
            },
            new UserMediaLog
            {
                LogId = Guid.NewGuid(),
                UserId = secondUserId,
                MediaId = removedMediaId,
                Status = "watching",
                LoggedDate = loggedDate,
            },
            new UserMediaLog
            {
                LogId = preservedLogId,
                UserId = firstUserId,
                MediaId = preservedMediaId,
                Status = "watched",
                LoggedDate = loggedDate,
            });

        await db.SaveChangesAsync();

        // Leave logs untracked so PostgreSQL performs the cascade.
        db.ChangeTracker.Clear();

        var mediaToDelete = await db.Media.SingleAsync(
            media => media.MediaId == removedMediaId);

        db.Media.Remove(mediaToDelete);
        await db.SaveChangesAsync();

        db.ChangeTracker.Clear();

        Assert.False(await db.Media.AnyAsync(
            media => media.MediaId == removedMediaId));

        Assert.False(await db.UserMediaLogs.AnyAsync(
            log => log.MediaId == removedMediaId));

        Assert.True(await db.Users.AnyAsync(
            user => user.UserId == firstUserId));

        Assert.True(await db.Users.AnyAsync(
            user => user.UserId == secondUserId));

        Assert.True(await db.Media.AnyAsync(
            media => media.MediaId == preservedMediaId));

        var preservedLog = await db.UserMediaLogs
            .AsNoTracking()
            .SingleAsync(log => log.LogId == preservedLogId);

        Assert.Equal(firstUserId, preservedLog.UserId);
        Assert.Equal(preservedMediaId, preservedLog.MediaId);
        Assert.Equal("watched", preservedLog.Status);
        Assert.Equal(loggedDate, preservedLog.LoggedDate);
    }
}
