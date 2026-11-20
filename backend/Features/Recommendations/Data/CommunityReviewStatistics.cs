
using System;
using System.Collections.Generic;

namespace Backend.Features.Recommendations.Data;

public sealed record CommunityReviewStatistics(
    IReadOnlyDictionary<Guid, MediaReviewStatistics> MediaStatistics,
    int MaxReviewCount,
    float GlobalAverageRating)
{
    public MediaReviewStatistics GetMediaStatistics(Guid mediaId)
    {
        if (MediaStatistics.TryGetValue(
            mediaId, out var statistics))
        {
            return statistics;
        }

        return new MediaReviewStatistics(
            mediaId,
            ReviewCount: 0,
            AverageRating: null);
    }
}
