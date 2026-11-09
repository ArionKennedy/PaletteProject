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
}
