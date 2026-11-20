using Palette.Api.MachineLearning.ContentBased;

namespace Palette.Api.MachineLearning.Hybrid;

public sealed record RecommendationCandidate(
    MediaFeatures Media,
    int ReviewCount,
    float? AverageRating = null
);
