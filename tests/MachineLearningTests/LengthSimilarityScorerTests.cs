
using System;
using Palette.Api.MachineLearning.ContentBased;
using Xunit;

public class LengthSimilarityScorerTests
{
    // ==================================================
    // Mathematical Accuracy Tests
    // ==================================================

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
        float preferredLength,
        float candidateLength,
        float tolerance,
        float expected)
    {
        // Act
        float result = LengthSimilarityScorer.Score(
            preferredLength, candidateLength, tolerance);

        // Assert
        Assert.Equal(expected, result, 6);
    }

    // ==================================================
    // Missing Length Information Tests
    // ==================================================

    [Fact]
    public void ScoreMissingPreferredLengthReturnsZero()
    {
        // Act
        float result = LengthSimilarityScorer.Score(
            null, 120f, 45f);

        // Assert
        Assert.Equal(0f, result);
    }

    [Fact]
    public void ScoreMissingCandidateLengthReturnsZero()
    {
        // Act
        float result = LengthSimilarityScorer.Score(
            120f, null, 45f);

        // Assert
        Assert.Equal(0f, result);
    }

    [Fact]
    public void ScoreBothLengthsMissingReturnsZero()
    {
        // Act
        float result = LengthSimilarityScorer.Score(
            null, null, 45f);

        // Assert
        Assert.Equal(0f, result);
    }

    // ==================================================
    // Invalid Preferred Length Tests
    // ==================================================

    [Theory]
    [InlineData(0f)]
    [InlineData(-1f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    public void ScoreInvalidPreferredLengthThrowsArgumentException(
        float preferredLength)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            LengthSimilarityScorer.Score(
                preferredLength, 120f, 45f));

        Assert.Equal("preferredLength", exception.ParamName);
    }

    // ==================================================
    // Invalid Candidate Length Tests
    // ==================================================

    [Theory]
    [InlineData(0f)]
    [InlineData(-1f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    public void ScoreInvalidCandidateLengthThrowsArgumentException(
        float candidateLength)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            LengthSimilarityScorer.Score(
                120f, candidateLength, 45f));

        Assert.Equal("candidateLength", exception.ParamName);
    }

    // ==================================================
    // Invalid Tolerance Tests
    // ==================================================

    [Theory]
    [InlineData(0f)]
    [InlineData(-1f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    public void ScoreInvalidToleranceThrowsArgumentException(
        float tolerance)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            LengthSimilarityScorer.Score(
                120f, 150f, tolerance));

        Assert.Equal("tolerance", exception.ParamName);
    }

    // ==================================================
    // Mathematical Property Tests
    // ==================================================

    [Fact]
    public void ScoreCloserLengthReturnsHigherSimilarity()
    {
        // Act
        float closer = LengthSimilarityScorer.Score(
            120f, 130f, 45f);

        float farther = LengthSimilarityScorer.Score(
            120f, 180f, 45f);

        // Assert
        Assert.True(closer > farther);
    }

    [Fact]
    public void ScoreEqualLengthDifferencesReturnsSameSimilarity()
    {
        // Act
        float shorter = LengthSimilarityScorer.Score(
            120f, 90f, 45f);

        float longer = LengthSimilarityScorer.Score(
            120f, 150f, 45f);

        // Assert
        Assert.Equal(shorter, longer, 6);
    }

    [Fact]
    public void ScoreHigherToleranceReturnsHigherSimilarity()
    {
        // Act
        float lowerTolerance = LengthSimilarityScorer.Score(
            120f, 180f, 15f);

        float higherTolerance = LengthSimilarityScorer.Score(
            120f, 180f, 60f);

        // Assert
        Assert.True(higherTolerance > lowerTolerance);
    }

    [Fact]
    public void ScoreScaledInputsReturnsSameSimilarity()
    {
        // Act
        float original = LengthSimilarityScorer.Score(
            120f, 150f, 45f);

        float scaled = LengthSimilarityScorer.Score(
            1200f, 1500f, 450f);

        // Assert
        Assert.Equal(original, scaled, 6);
    }

    [Fact]
    public void ScoreExtremelyDifferentLengthsApproachesZero()
    {
        // Act
        float result = LengthSimilarityScorer.Score(
            120f, 10000f, 45f);

        // Assert
        Assert.InRange(result, 0f, 0.000001f);
    }

    [Fact]
    public void ScoreExactMatchIsIndependentOfTolerance()
    {
        // Act
        float first = LengthSimilarityScorer.Score(
            120f, 120f, 1f);

        float second = LengthSimilarityScorer.Score(
            120f, 120f, 1000f);

        // Assert
        Assert.Equal(1f, first);
        Assert.Equal(1f, second);
    }

    [Fact]
    public void ScoreRandomValidInputsRemainsInRange()
    {
        // Arrange
        Random random = new Random(42);

        for (int iteration = 0; iteration < 1000; iteration++)
        {
            float preferredLength =
                (float)(random.NextDouble() * 1000 + 1);

            float candidateLength =
                (float)(random.NextDouble() * 1000 + 1);

            float tolerance =
                (float)(random.NextDouble() * 100 + 1);

            // Act
            float result = LengthSimilarityScorer.Score(
                preferredLength, candidateLength, tolerance);

            // Assert
            Assert.True(float.IsFinite(result));
            Assert.InRange(result, 0f, 1f);
        }
    }
}
