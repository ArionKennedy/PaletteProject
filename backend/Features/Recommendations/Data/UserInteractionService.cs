
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Palette.Api.Data;
using Palette.Api.MachineLearning.ImplicitFeedback;

namespace Backend.Features.Recommendations.Data;

public sealed class UserInteractionService
{
    private readonly PaletteDbContext _dbContext;

    // Initializes the user interaction service.
    public UserInteractionService(PaletteDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    // Retrieves all supported recommendation interactions for a user.
    public async Task<IReadOnlyList<UserInteraction>> GetInteractionsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var interactions = new List<UserInteraction>();

        interactions.AddRange(
            await GetStoredInteractionsAsync(userId, cancellationToken));

        interactions.AddRange(
            await GetCompletedInteractionsAsync(userId, cancellationToken));

        interactions.AddRange(
            await GetPositiveReviewInteractionsAsync(userId, cancellationToken));

        return interactions.AsReadOnly();
    }

    // Retrieves media stored in the user's collections.
    private async Task<IEnumerable<UserInteraction>> GetStoredInteractionsAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var items = await _dbContext.CollectionItems
            .AsNoTracking()
            .Where(item => item.Collection.UserId == userId)
            .Select(item => new
            {
                item.MediaId,
                item.AddedAt
            })
            .ToListAsync(cancellationToken);

        return items.Select(item => new UserInteraction(
            userId,
            item.MediaId,
            InteractionType.Stored,
            ToUtcTimestamp(item.AddedAt)));
    }

    // Retrieves media that the user has completed.
    private async Task<IEnumerable<UserInteraction>> GetCompletedInteractionsAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var logs = await _dbContext.UserMediaLogs
            .AsNoTracking()
            .Where(log => log.UserId == userId
                && log.Status != null
                && log.Status.Trim().ToLower() == "completed")
            .Select(log => new
            {
                log.MediaId,
                log.LoggedDate
            })
            .ToListAsync(cancellationToken);

        return logs.Select(log => new UserInteraction(
            userId,
            log.MediaId,
            InteractionType.Completed,
            ToUtcTimestamp(
                log.LoggedDate.ToDateTime(TimeOnly.MinValue))));
    }

    // Retrieves media that the user has positively reviewed.
    private async Task<IEnumerable<UserInteraction>> GetPositiveReviewInteractionsAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var reviews = await _dbContext.Reviews
            .AsNoTracking()
            .Where(review => review.UserId == userId
                && review.Rating >= 4
                && review.Rating <= 5)
            .Select(review => new
            {
                review.MediaId,
                review.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return reviews.Select(review => new UserInteraction(
            userId,
            review.MediaId,
            InteractionType.PositiveReview,
            ToUtcTimestamp(review.CreatedAt)));
    }

    // Converts database timestamps into UTC interaction timestamps.
    private static DateTimeOffset ToUtcTimestamp(DateTime timestamp)
    {
        return timestamp.Kind == DateTimeKind.Unspecified
            ? new DateTimeOffset(timestamp, TimeSpan.Zero)
            : new DateTimeOffset(timestamp).ToUniversalTime();
    }
}