using Microsoft.EntityFrameworkCore;
using Palette.Api.Data;

namespace Palette.Api.Logging;

public sealed record ShowLogItem(
    Guid MediaId,
    string Title,
    string MediaType,
    string? PosterUrl,
    DateOnly? ReleaseDate,
    int Rating,
    string? Body,
    DateTime LastReviewedAt);

public sealed record ShowLogPage(IReadOnlyList<ShowLogItem> Items, int Page, bool HasMore);

public sealed class ShowLogService(PaletteDbContext db)
{
    private const int PageSize = 50;

    public async Task<ShowLogPage> GetPage(Guid userId, int page, CancellationToken cancellationToken)
    {
        // Reviews are the source of truth. One title per user, even if they review it again.
        // This read does not create media or duplicate review activity in USER_MEDIA_LOGS.
        var reviews = db.Reviews.AsNoTracking()
            .Where(review => review.UserId == userId)
            .Where(review => review.ReviewId == db.Reviews
                .Where(other => other.UserId == userId && other.MediaId == review.MediaId)
                .OrderByDescending(other => other.CreatedAt)
                .ThenByDescending(other => other.ReviewId)
                .Select(other => other.ReviewId)
                .FirstOrDefault())
            .Select(review => new
            {
                review.MediaId,
                review.Media.Title,
                review.Media.MediaType,
                review.Media.PosterUrl,
                review.Media.ReleaseDate,
                review.Rating,
                review.Body,
                LastReviewedAt = review.CreatedAt
            });
        var items = await reviews
            .OrderByDescending(item => item.LastReviewedAt).ThenBy(item => item.MediaId)
            .Skip((page - 1) * PageSize).Take(PageSize + 1)
            .ToListAsync(cancellationToken);
        return new ShowLogPage(items.Take(PageSize)
            .Select(item => new ShowLogItem(item.MediaId, item.Title, item.MediaType.ToString(),
                item.PosterUrl, item.ReleaseDate, item.Rating, item.Body, item.LastReviewedAt))
            .ToList(), page, items.Count > PageSize);
    }
}
