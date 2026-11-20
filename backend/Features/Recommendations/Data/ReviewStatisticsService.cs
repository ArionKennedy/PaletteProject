
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using Microsoft.EntityFrameworkCore;
using Palette.Api.Data;

namespace Backend.Features.Recommendations.Data;

public sealed class ReviewStatisticsService
{
    private readonly PaletteDbContext _dbContext;

    public ReviewStatisticsService(PaletteDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        _dbContext = dbContext;
    }

    public async Task<CommunityReviewStatistics> GetStatisticsAsync(
        CancellationToken cancellationToken = default)
    {
        var aggregatedReviews = await _dbContext.Reviews
            .AsNoTracking()
            .Where(review =>
                review.Rating >= 1 &&
                review.Rating <= 5)
            .GroupBy(review => review.MediaId)
            .Select(group => new
            {
                MediaId = group.Key,
                ReviewCount = group.Count(),
                RatingSum = group.Sum(
                    review => (long)review.Rating)
            })
            .ToListAsync(cancellationToken);

        var mediaStatistics = aggregatedReviews
            .ToDictionary(
                group => group.MediaId,
                group => new MediaReviewStatistics(
                    group.MediaId,
                    group.ReviewCount,
                    (float)((double)group.RatingSum /
                        group.ReviewCount)));

        long totalReviews = 0;
        long totalRatingSum = 0;

        int maxReviewCount = 0;

        foreach (var group in aggregatedReviews)
        {
            totalReviews += group.ReviewCount;
            totalRatingSum += group.RatingSum;

            maxReviewCount = Math.Max(
                maxReviewCount,
                group.ReviewCount);
        }

        float globalAverageRating = totalReviews == 0
            ? 3f
            : (float)((double)totalRatingSum / totalReviews);

        return new CommunityReviewStatistics(
            new ReadOnlyDictionary<Guid, MediaReviewStatistics>(
                mediaStatistics),
            maxReviewCount,
            globalAverageRating);
    }
}