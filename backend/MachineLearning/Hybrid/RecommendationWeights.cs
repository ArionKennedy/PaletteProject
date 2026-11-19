namespace Palette.Api.MachineLearning.Hybrid;

public sealed record RecommendationWeights(
    float Content = 0.70f,
    float Popularity = 0.10f,
    float RatingQuality = 0.20f
);
