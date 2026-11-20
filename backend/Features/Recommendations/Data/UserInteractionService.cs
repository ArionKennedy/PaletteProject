
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

    public UserInteractionService(PaletteDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<UserInteraction>> GetInteractionsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var interactions = new List<UserInteraction>();

        // Movies added to My Library.
        interactions.AddRange(
            await GetLibraryInteractionsAsync(
                userId,
                cancellationToken));

        // Movies saved to collections.
        interactions.AddRange(
            await GetCollectionInteractionsAsync(
                userId,
                cancellationToken));

        // Movies marked as completed.
        interactions.AddRange(
            await GetCompletedInteractionsAsync(
                userId,
                cancellationToken));

        // Movies rated positively.
        interactions.AddRange(
            await GetPositiveReviewInteractionsAsync(
                userId,
                cancellationToken));

        Console.WriteLine(
            $"[UserInteractionService] Total interactions: {interactions.Count}");

        return interactions.AsReadOnly();
    }

    // My Library is backed by USER_MEDIA_LOGS.
    // Any existing library log represents a saved item,
    // regardless of its completion status.
    private async Task<IEnumerable<UserInteraction>>
        GetLibraryInteractionsAsync(
            Guid userId,
            CancellationToken cancellationToken)
    {
        var items = await _dbContext.UserMediaLogs
            .AsNoTracking()
            .Where(log => log.UserId == userId)
            .GroupBy(log => log.MediaId)
            .Select(group => new
            {
                MediaId = group.Key,
                LoggedDate = group.Max(log => log.LoggedDate)
            })
            .ToListAsync(cancellationToken);

        Console.WriteLine(
            $"[UserInteractionService] Library items: {items.Count}");

        return items.Select(item => new UserInteraction(
            userId,
            item.MediaId,
            InteractionType.Stored,
            ToUtcTimestamp(
                item.LoggedDate.ToDateTime(
                    TimeOnly.MinValue))));
    }

    // Collection membership is another saved-item signal.
    private async Task<IEnumerable<UserInteraction>>
        GetCollectionInteractionsAsync(
            Guid userId,
            CancellationToken cancellationToken)
    {
        // Avoid counting the same movie twice as Stored
        // when it exists in both My Library and a collection.
        var libraryMediaIds = _dbContext.UserMediaLogs
            .AsNoTracking()
            .Where(log => log.UserId == userId)
            .Select(log => log.MediaId);

        var items = await _dbContext.CollectionItems
            .AsNoTracking()
            .Where(item => item.Collection.UserId == userId)
            .Where(item =>
                !libraryMediaIds.Contains(item.MediaId))
            .Select(item => new
            {
                item.MediaId,
                item.AddedAt
            })
            .ToListAsync(cancellationToken);

        Console.WriteLine(
            $"[UserInteractionService] Additional collection items: {items.Count}");

        return items.Select(item => new UserInteraction(
            userId,
            item.MediaId,
            InteractionType.Stored,
            ToUtcTimestamp(item.AddedAt)));
    }

    // Completion is an additional interaction,
    // not a requirement for appearing in My Library.
    private async Task<IEnumerable<UserInteraction>>
        GetCompletedInteractionsAsync(
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

        Console.WriteLine(
            $"[UserInteractionService] Completed items: {logs.Count}");

        return logs.Select(log => new UserInteraction(
            userId,
            log.MediaId,
            InteractionType.Completed,
            ToUtcTimestamp(
                log.LoggedDate.ToDateTime(
                    TimeOnly.MinValue))));
    }

    // Positive ratings strengthen preferences.
    private async Task<IEnumerable<UserInteraction>>
        GetPositiveReviewInteractionsAsync(
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

        Console.WriteLine(
            $"[UserInteractionService] Positive reviews: {reviews.Count}");

        return reviews.Select(review => new UserInteraction(
            userId,
            review.MediaId,
            InteractionType.PositiveReview,
            ToUtcTimestamp(review.CreatedAt)));
    }

    private static DateTimeOffset ToUtcTimestamp(
        DateTime timestamp)
    {
        return timestamp.Kind == DateTimeKind.Unspecified
            ? new DateTimeOffset(timestamp, TimeSpan.Zero)
            : new DateTimeOffset(timestamp).ToUniversalTime();
    }
}
