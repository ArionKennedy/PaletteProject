using System;

namespace Palette.Api.MachineLearning.Hybrid;

public sealed record RecommendationResult(
    Guid MediaId,
    float? ContentScore,
    float PopularityScore,
    float RatingQualityScore,
    float FinalScore
);
