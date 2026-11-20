
using System;

namespace Palette.Api.MachineLearning.Popularity;

public static class BayesianRatingScorer
{
    // Calculates a Bayesian average by blending media and global ratings.
    public static float CalculateRating(
        int reviewCount,
        float? averageRating,
        float globalAverageRating,
        float priorWeight = 50f)
    {
        if (reviewCount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(reviewCount),
                "Review count must be non-negative.");
        }

        ValidateRating(
            globalAverageRating,
            nameof(globalAverageRating));

        if (!float.IsFinite(priorWeight) ||
            priorWeight <= 0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(priorWeight),
                "Prior weight must be finite and positive.");
        }

        if (averageRating.HasValue)
        {
            ValidateRating(
                averageRating.Value,
                nameof(averageRating));
        }

        if (reviewCount == 0)
        {
            return globalAverageRating;
        }

        if (!averageRating.HasValue)
        {
            throw new ArgumentException(
                "An average rating is required when reviews exist.",
                nameof(averageRating));
        }

        double numerator =
            (double)reviewCount * averageRating.Value +
            (double)priorWeight * globalAverageRating;

        double denominator =
            (double)reviewCount + priorWeight;

        double bayesianRating = numerator / denominator;

        return (float)Math.Clamp(
            bayesianRating, 1d, 5d);
    }

    // Normalizes a Bayesian rating to the range [0, 1].
    public static float Score(
        int reviewCount,
        float? averageRating,
        float globalAverageRating,
        float priorWeight = 50f)
    {
        float rating = CalculateRating(
            reviewCount,
            averageRating,
            globalAverageRating,
            priorWeight);

        return Math.Clamp(
            (rating - 1f) / 4f, 0f, 1f);
    }

    // Rejects ratings outside the finite 1-to-5 range.
    private static void ValidateRating(
        float rating,
        string parameterName)
    {
        if (!float.IsFinite(rating) ||
            rating < 1f ||
            rating > 5f)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                "Ratings must be finite and between 1 and 5.");
        }
    }
}
