
using System;
using Palette.Api.MachineLearning.ContentBased;
using Xunit;

public class LengthSimilarityScorerTests
{
    // Mathematical accuracy

    // Verifies that valid lengths produce the expected Gaussian similarity.
    [Theory]
    [InlineData(120f, 120f, 45f, 1f)]
    [InlineData(120f, 150f, 30f, 0.606531f)]
    [InlineData(120f, 90f, 30f, 0.606531f)]
    [InlineData(120f, 180f, 30f, 0.135335f)]
    [InlineData(120f, 210f, 30f, 0.011109f)]
    [InlineData(120f, 135f, 30f, 0.882497f)]
    [InlineData(300f, 400f, 100f, 0.606531f)]
    [InlineData(3f, 5f, 2f, 0.606531f)]
    public void ScoreValidLengthsReturnsExpectedResult(
        float preferredLength, float candidateLength, float tolerance, float expected)
    {
        Assert.Equal(expected, LengthSimilarityScorer.Score(
            preferredLength, candidateLength, tolerance), 6);
    }

    // Missing length information

    // Verifies that a missing preferred length returns zero.
    [Fact]
    public void ScoreMissingPreferredLengthReturnsZero()
    {
        Assert.Equal(0f, LengthSimilarityScorer.Score(null, 120f, 45f));
    }

    // Verifies that a missing candidate length returns zero.
    [Fact]
    public void ScoreMissingCandidateLengthReturnsZero()
    {
        Assert.Equal(0f, LengthSimilarityScorer.Score(120f, null, 45f));
    }

    // Verifies that two missing lengths return zero.
    [Fact]
    public void ScoreBothLengthsMissingReturnsZero()
    {
        Assert.Equal(0f, LengthSimilarityScorer.Score(null, null, 45f));
    }

    // Input validation

    // Verifies that nonpositive and nonfinite preferred lengths are rejected.
    [Theory]
    [InlineData(0f)]
    [InlineData(-1f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    public void ScoreInvalidPreferredLengthThrowsArgumentException(float preferredLength)
    {
        Assert.Throws<ArgumentException>("preferredLength", () =>
            LengthSimilarityScorer.Score(preferredLength, 120f, 45f));
    }

    // Verifies that nonpositive and nonfinite candidate lengths are rejected.
    [Theory]
    [InlineData(0f)]
    [InlineData(-1f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    public void ScoreInvalidCandidateLengthThrowsArgumentException(float candidateLength)
    {
        Assert.Throws<ArgumentException>("candidateLength", () =>
            LengthSimilarityScorer.Score(120f, candidateLength, 45f));
    }

    // Verifies that nonpositive and nonfinite tolerances are rejected.
    [Theory]
    [InlineData(0f)]
    [InlineData(-1f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    public void ScoreInvalidToleranceThrowsArgumentException(float tolerance)
    {
        Assert.Throws<ArgumentException>("tolerance", () =>
            LengthSimilarityScorer.Score(120f, 150f, tolerance));
    }

    // Mathematical properties

    // Verifies that lengths closer to the preferred length receive higher scores.
    [Fact]
    public void ScoreCloserLengthReturnsHigherSimilarity()
    {
        float closer = LengthSimilarityScorer.Score(120f, 130f, 45f);
        float farther = LengthSimilarityScorer.Score(120f, 180f, 45f);

        Assert.True(closer > farther);
    }

    // Verifies that equal differences produce identical similarity scores.
    [Fact]
    public void ScoreEqualLengthDifferencesReturnsSameSimilarity()
    {
        float shorter = LengthSimilarityScorer.Score(120f, 90f, 45f);
        float longer = LengthSimilarityScorer.Score(120f, 150f, 45f);

        Assert.Equal(shorter, longer, 6);
    }

    // Verifies that increasing tolerance increases similarity for differing lengths.
    [Fact]
    public void ScoreHigherToleranceReturnsHigherSimilarity()
    {
        float lower = LengthSimilarityScorer.Score(120f, 180f, 15f);
        float higher = LengthSimilarityScorer.Score(120f, 180f, 60f);

        Assert.True(higher > lower);
    }

    // Verifies that proportionally scaling all inputs preserves similarity.
    [Fact]
    public void ScoreScaledInputsReturnsSameSimilarity()
    {
        float original = LengthSimilarityScorer.Score(120f, 150f, 45f);
        float scaled = LengthSimilarityScorer.Score(1200f, 1500f, 450f);

        Assert.Equal(original, scaled, 6);
    }

    // Verifies that extremely different lengths produce near-zero similarity.
    [Fact]
    public void ScoreExtremelyDifferentLengthsApproachesZero()
    {
        Assert.InRange(LengthSimilarityScorer.Score(120f, 10000f, 45f),
            0f, 0.000001f);
    }

    // Verifies that exact length matches receive a score of one regardless of tolerance.
    [Fact]
    public void ScoreExactMatchIsIndependentOfTolerance()
    {
        Assert.Equal(1f, LengthSimilarityScorer.Score(120f, 120f, 1f));
        Assert.Equal(1f, LengthSimilarityScorer.Score(120f, 120f, 1000f));
    }

    // Verifies that 1,000 randomized inputs produce finite scores within [0, 1].
    [Fact]
    public void ScoreRandomValidInputsRemainsInRange()
    {
        var random = new Random(42);

        for (int iteration = 0; iteration < 1000; iteration++)
        {
            float preferredLength = (float)(random.NextDouble() * 1000 + 1);
            float candidateLength = (float)(random.NextDouble() * 1000 + 1);
            float tolerance = (float)(random.NextDouble() * 100 + 1);

            float result = LengthSimilarityScorer.Score(
                preferredLength, candidateLength, tolerance);

            Assert.True(float.IsFinite(result));
            Assert.InRange(result, 0f, 1f);
        }
    }
}