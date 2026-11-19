
using System;

namespace Palette.Api.MachineLearning.Popularity;

public static class PopularityScorer
{
    // Returns a logarithmically normalized score from review counts.
    public static float Score(
        int reviewCount,
        int maxReviewCount)
    {
        if (reviewCount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(reviewCount),
                "Review count must be non-negative.");
        }

        if (maxReviewCount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxReviewCount),
                "Maximum review count must be non-negative.");
        }

        if (reviewCount == 0)
        {
            return 0f;
        }

        if (maxReviewCount == 0)
        {
            throw new ArgumentException(
                "Maximum review count must be positive " +
                "when reviews exist.",
                nameof(maxReviewCount));
        }

        double numerator =
            Math.Log(1d + reviewCount);

        double denominator =
            Math.Log(1d + maxReviewCount);

        double popularity =
            numerator / denominator;

        return (float)Math.Clamp(popularity, 0d, 1d);
    }
}