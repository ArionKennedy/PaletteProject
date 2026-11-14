using Microsoft.EntityFrameworkCore;
using Npgsql;
using Palette.Api.Models;
using Palette.Api.Enums;
using Palette.Tests.Infrastructure;
using Xunit;

namespace Palette.Tests.Integration.Database;

[Collection("Postgres Integration Tests")]
public class ReviewDatabaseTests
{
    private readonly PostgresFixture _fixture;

    public ReviewDatabaseTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CanSaveReviewWithUserAndMedia()
    {
        // arrange
        await using var db = _fixture.CreateContext();
        await using var transaction = await db.Database.BeginTransactionAsync();

        var userId = Guid.NewGuid();
        var mediaId = Guid.NewGuid();
        var reviewId = Guid.NewGuid();

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

        var review = new Review
        {
            ReviewId = reviewId,
            UserId = userId,
            MediaId = mediaId,
            Rating = 5,
            Body = "Great movie!",
            CreatedAt = DateTime.UtcNow
        };

        // act
        db.Set<User>().Add(user);
        db.Set<Media>().Add(media);
        await db.SaveChangesAsync();

        db.Set<Review>().Add(review);
        await db.SaveChangesAsync();

        db.ChangeTracker.Clear();

        var saved = await db.Set<Review>().SingleOrDefaultAsync(r => r.ReviewId == reviewId);

        // assert
        Assert.NotNull(saved);
        Assert.Equal(userId, saved.UserId);
        Assert.Equal(mediaId, saved.MediaId);
        Assert.Equal(5, saved.Rating);
        Assert.Equal("Great movie!", saved.Body);
    }

    [Fact]
    public async Task CannotSaveReviewWithoutMedia()
    {
        // arrange
        await using var db = _fixture.CreateContext();
        await using var transaction =
            await db.Database.BeginTransactionAsync();

        var userId = Guid.NewGuid();

        var user = new User
        {
            UserId = userId,
            Auth0Id = $"test-auth-{userId}",
            Username = "testuser",
            Email = $"test-{userId}@example.com",
            DateJoined = DateTime.UtcNow
        };

        db.Set<User>().Add(user);
        await db.SaveChangesAsync();

        var review = new Review
        {
            ReviewId = Guid.NewGuid(),
            UserId = userId,
            MediaId = Guid.NewGuid(), // Nonexistent Media
            Rating = 5,
            Body = "Test review",
            CreatedAt = DateTime.UtcNow
        };

        db.Set<Review>().Add(review);

        // act
        var exception = await Assert.ThrowsAsync<DbUpdateException>(
            () => db.SaveChangesAsync());

        // assert
        var postgresException = Assert.IsType<PostgresException>(exception.InnerException);

        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, postgresException.SqlState);
        Assert.Equal("FK_REVIEWS_MEDIA_MediaId", postgresException.ConstraintName);
    }

    [Fact]
    public async Task DeletingUserCascadesToReviews()
    {
        // arrange
        await using var db = _fixture.CreateContext();
        await using var transaction = await db.Database.BeginTransactionAsync();

        var userId = Guid.NewGuid();
        var mediaId = Guid.NewGuid();
        var reviewId = Guid.NewGuid();

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

        var review = new Review
        {
            ReviewId = reviewId,
            UserId = userId,
            MediaId = mediaId,
            Rating = 4,
            Body = "Good movie!",
            CreatedAt = DateTime.UtcNow
        };

        db.Set<User>().Add(user);
        db.Set<Media>().Add(media);
        await db.SaveChangesAsync();

        db.Set<Review>().Add(review);
        await db.SaveChangesAsync();

        // clear tracked entities to test database-side cascade.
        db.ChangeTracker.Clear();

        // act - delete User
        var savedUser = await db.Set<User>().SingleAsync(u => u.UserId == userId);

        db.Set<User>().Remove(savedUser);
        await db.SaveChangesAsync();

        db.ChangeTracker.Clear();

        var savedReview = await db.Set<Review>().SingleOrDefaultAsync(r => r.ReviewId == reviewId);
        var savedMedia = await db.Set<Media>().SingleOrDefaultAsync(m => m.MediaId == mediaId);

        // assert
        Assert.Null(savedReview);
        Assert.NotNull(savedMedia);
        Assert.Equal(mediaId, savedMedia.MediaId);
    }

    [Fact]
    public async Task CannotSaveReviewWithoutUser()
    {
        await using var db = _fixture.CreateContext();
        await using var transaction =
            await db.Database.BeginTransactionAsync();

        var mediaId = Guid.NewGuid();

        var media = new Media
        {
            MediaId = mediaId,
            Title = "Review foreign key test",
            MediaType = MediaType.Movie,
        };

        db.Media.Add(media);
        await db.SaveChangesAsync();

        db.ChangeTracker.Clear();

        db.Reviews.Add(new Review
        {
            ReviewId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            MediaId = mediaId,
            Rating = 4,
            Body = "This review has no existing user.",
            CreatedAt = new DateTime(
                2026, 1, 1, 12, 0, 0, DateTimeKind.Utc),
        });

        var exception = await Assert.ThrowsAsync<DbUpdateException>(
            () => db.SaveChangesAsync());

        var postgres = Assert.IsType<PostgresException>(
            exception.InnerException);

        Assert.Equal(
            PostgresErrorCodes.ForeignKeyViolation,
            postgres.SqlState);

        Assert.Equal(
            "FK_REVIEWS_USERS_UserId",
            postgres.ConstraintName);
    }

    [Fact]
    public async Task DeletingMediaCascadesToReviewsButPreservesUsersAndOtherReviews()
    {
        await using var db = _fixture.CreateContext();
        await using var transaction =
            await db.Database.BeginTransactionAsync();

        var testTime = new DateTime(
            2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        var firstUserId = Guid.NewGuid();
        var secondUserId = Guid.NewGuid();

        var firstUser = new User
        {
            UserId = firstUserId,
            Auth0Id = $"integration|{firstUserId:N}",
            Username = "First reviewer",
            Email = $"{firstUserId:N}@example.com",
            DateJoined = testTime,
        };

        var secondUser = new User
        {
            UserId = secondUserId,
            Auth0Id = $"integration|{secondUserId:N}",
            Username = "Second reviewer",
            Email = $"{secondUserId:N}@example.com",
            DateJoined = testTime,
        };

        var removedMediaId = Guid.NewGuid();
        var preservedMediaId = Guid.NewGuid();

        db.Users.AddRange(firstUser, secondUser);

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

        var firstRemovedReviewId = Guid.NewGuid();
        var secondRemovedReviewId = Guid.NewGuid();
        var preservedReviewId = Guid.NewGuid();

        db.Reviews.AddRange(
            new Review
            {
                ReviewId = firstRemovedReviewId,
                UserId = firstUserId,
                MediaId = removedMediaId,
                Rating = 4,
                Body = "First review of removed media.",
                CreatedAt = testTime,
            },
            new Review
            {
                ReviewId = secondRemovedReviewId,
                UserId = secondUserId,
                MediaId = removedMediaId,
                Rating = 5,
                Body = "Second review of removed media.",
                CreatedAt = testTime,
            },
            new Review
            {
                ReviewId = preservedReviewId,
                UserId = firstUserId,
                MediaId = preservedMediaId,
                Rating = 3,
                Body = "Keep this review.",
                CreatedAt = testTime,
            });

        await db.SaveChangesAsync();

        // Load only Media so PostgreSQL performs the dependent deletion.
        db.ChangeTracker.Clear();

        var mediaToDelete = await db.Media.SingleAsync(
            media => media.MediaId == removedMediaId);

        db.Media.Remove(mediaToDelete);
        await db.SaveChangesAsync();

        db.ChangeTracker.Clear();

        Assert.False(await db.Media.AnyAsync(
            media => media.MediaId == removedMediaId));

        Assert.False(await db.Reviews.AnyAsync(
            review => review.MediaId == removedMediaId));

        Assert.True(await db.Users.AnyAsync(
            user => user.UserId == firstUserId));

        Assert.True(await db.Users.AnyAsync(
            user => user.UserId == secondUserId));

        Assert.True(await db.Media.AnyAsync(
            media => media.MediaId == preservedMediaId));

        var preservedReview = await db.Reviews
            .AsNoTracking()
            .SingleAsync(review =>
                review.ReviewId == preservedReviewId);

        Assert.Equal(firstUserId, preservedReview.UserId);
        Assert.Equal(preservedMediaId, preservedReview.MediaId);
        Assert.Equal(3, preservedReview.Rating);
        Assert.Equal("Keep this review.", preservedReview.Body);
    }
}
