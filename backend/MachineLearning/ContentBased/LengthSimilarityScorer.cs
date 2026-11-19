
using System;

namespace Palette.Api.MachineLearning.ContentBased;

public static class LengthSimilarityScorer
{
    public static float Score(
        float? preferredLength,
        float? candidateLength,
        float tolerance)
    {
        if (!float.IsFinite(tolerance) || tolerance <= 0f)
        {
            throw new ArgumentException(
                "Tolerance must be finite and positive.",
                nameof(tolerance));
        }

        if (preferredLength.HasValue &&
            (!float.IsFinite(preferredLength.Value)
             || preferredLength.Value <= 0f))
        {
            throw new ArgumentException(
                "Preferred length must be finite and positive.",
                nameof(preferredLength));
        }

        if (candidateLength.HasValue &&
            (!float.IsFinite(candidateLength.Value)
             || candidateLength.Value <= 0f))
        {
            throw new ArgumentException(
                "Candidate length must be finite and positive.",
                nameof(candidateLength));
        }

        if (!preferredLength.HasValue ||
            !candidateLength.HasValue)
        {
            return 0f;
        }

        double difference =
            (double)candidateLength.Value - preferredLength.Value;

        double normalizedDifference =
            difference / tolerance;

        double similarity = Math.Exp(
            -0.5 * normalizedDifference * normalizedDifference);

        return (float)similarity;
    }
}
