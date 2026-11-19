using System;

namespace Backend.Features.Recommendations.Data;

public sealed record MediaReviewStatistics(
    Guid MediaId,
    int ReviewCount,
    float? AverageRating
);
