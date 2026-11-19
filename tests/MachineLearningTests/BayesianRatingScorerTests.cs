
using System;
using Palette.Api.MachineLearning.Popularity;
using Xunit;

public class BayesianRatingScorerTests
{
    // ==================================================
    // Mathematical Accuracy Tests (8)
    // ==================================================

    [Theory]
    [InlineData(2, 5f)]
    [InlineData(5000, 4.8f)]
    [InlineData(1000, 4.7f)]
    [InlineData(500, 4.5f)]
    [InlineData(10000, 4.2f)]
    // Verifies that calculating a rating returns the expected bayesian average.
    public void CalculateRatingReturnsExpectedBayesianAverage(
        int reviewCount,
        float averageRating)
    {
        const float globalAverage = 3.5f;
        const float priorWeight = 50f;

        float expected = (float)(
            ((double)reviewCount * averageRating +
            priorWeight * globalAverage) /
            (reviewCount + priorWeight));

        Assert.Equal(
            expected,
            BayesianRatingScorer.CalculateRating(
                reviewCount,
                averageRating,
                globalAverage,
                priorWeight),
            6);
    }

    [Fact]
    // Verifies that calculating a rating with equal weights returns the arithmetic mean.
    public void CalculateRatingEqualWeightsReturnsArithmeticMean()
    {
        Assert.Equal(
            4f,
            BayesianRatingScorer.CalculateRating(
                50, 5f, 3f, 50f),
            6);
    }

    [Fact]
    // Verifies that calculating a rating uses default prior weight.
    public void CalculateRatingUsesDefaultPriorWeight()
    {
        Assert.Equal(
            BayesianRatingScorer.CalculateRating(
                100, 4.5f, 3.5f, 50f),
            BayesianRatingScorer.CalculateRating(
                100, 4.5f, 3.5f));
    }

    [Fact]
    // Verifies that calculating a rating supports fractional prior weight.
    public void CalculateRatingSupportsFractionalPriorWeight()
    {
        Assert.Equal(
            4.2f,
            BayesianRatingScorer.CalculateRating(
                10, 5f, 1f, 2.5f),
            6);
    }

    // ==================================================
    // Normalization Tests (5)
    // ==================================================

    [Theory]
    [InlineData(1f, 0f)]
    [InlineData(2f, 0.25f)]
    [InlineData(3f, 0.50f)]
    [InlineData(4f, 0.75f)]
    [InlineData(5f, 1f)]
    // Verifies that scoring returns the expected normalized value.
    public void ScoreReturnsExpectedNormalizedValue(
        float rating,
        float expected)
    {
        Assert.Equal(
            expected,
            BayesianRatingScorer.Score(
                100, rating, rating),
            6);
    }

    // ==================================================
    // Boundary Condition Tests (6)
    // ==================================================

    [Fact]
    // Verifies that calculating a rating with zero reviews returns global average.
    public void CalculateRatingZeroReviewsReturnsGlobalAverage()
    {
        Assert.Equal(
            3.5f,
            BayesianRatingScorer.CalculateRating(
                0, null, 3.5f));
    }

    [Fact]
    // Verifies that calculating a rating with zero reviews ignores provided average.
    public void CalculateRatingZeroReviewsIgnoresProvidedAverage()
    {
        Assert.Equal(
            3.5f,
            BayesianRatingScorer.CalculateRating(
                0, 5f, 3.5f));
    }

    [Fact]
    // Verifies that scoring with zero reviews returns the normalized global average.
    public void ScoreZeroReviewsReturnsNormalizedGlobalAverage()
    {
        Assert.Equal(
            0.625f,
            BayesianRatingScorer.Score(
                0, null, 3.5f));
    }

    [Fact]
    // Verifies that calculating a rating with maximum integer count does not overflow.
    public void CalculateRatingMaximumIntegerCountDoesNotOverflow()
    {
        float rating = BayesianRatingScorer.CalculateRating(
            int.MaxValue, 5f, 3.5f);

        Assert.True(float.IsFinite(rating));
        Assert.InRange(rating, 1f, 5f);
    }

    [Fact]
    // Verifies that calculating a rating with maximum prior weight does not overflow.
    public void CalculateRatingMaximumPriorWeightDoesNotOverflow()
    {
        float rating = BayesianRatingScorer.CalculateRating(
            100, 5f, 3.5f, float.MaxValue);

        Assert.True(float.IsFinite(rating));
        Assert.Equal(3.5f, rating, 6);
    }

    [Fact]
    // Verifies that calculating a rating with smallest positive prior approaches average.
    public void CalculateRatingSmallestPositivePriorApproachesAverage()
    {
        Assert.Equal(
            4.5f,
            BayesianRatingScorer.CalculateRating(
                100, 4.5f, 3.5f, float.Epsilon),
            6);
    }

    // ==================================================
    // Input Validation Tests (21)
    // ==================================================

    [Theory]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    // Verifies that calculating a rating with negative review count throws an exception.
    public void CalculateRatingNegativeReviewCountThrowsException(
        int reviewCount)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            BayesianRatingScorer.CalculateRating(
                reviewCount, 4f, 3.5f));
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(5.1f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    // Verifies that calculating a rating with invalid average throws an exception.
    public void CalculateRatingInvalidAverageThrowsException(
        float averageRating)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            BayesianRatingScorer.CalculateRating(
                100, averageRating, 3.5f));
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(5.1f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    // Verifies that calculating a rating with invalid global average throws an exception.
    public void CalculateRatingInvalidGlobalAverageThrowsException(
        float globalAverage)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            BayesianRatingScorer.CalculateRating(
                100, 4f, globalAverage));
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(-1f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    // Verifies that calculating a rating with invalid prior weight throws an exception.
    public void CalculateRatingInvalidPriorWeightThrowsException(
        float priorWeight)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            BayesianRatingScorer.CalculateRating(
                100, 4f, 3.5f, priorWeight));
    }

    [Fact]
    // Verifies that calculating a rating with missing average with reviews throws an exception.
    public void CalculateRatingMissingAverageWithReviewsThrowsException()
    {
        Assert.Throws<ArgumentException>(() =>
            BayesianRatingScorer.CalculateRating(
                100, null, 3.5f));
    }

    [Fact]
    // Verifies that calculating a rating with zero reviews still validates global average.
    public void CalculateRatingZeroReviewsStillValidatesGlobalAverage()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            BayesianRatingScorer.CalculateRating(
                0, null, float.NaN));
    }

    [Fact]
    // Verifies that calculating a rating with zero reviews still validates prior weight.
    public void CalculateRatingZeroReviewsStillValidatesPriorWeight()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            BayesianRatingScorer.CalculateRating(
                0, null, 3.5f, 0f));
    }

    [Fact]
    // Verifies that calculating a rating with zero reviews still validates provided average.
    public void CalculateRatingZeroReviewsStillValidatesProvidedAverage()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            BayesianRatingScorer.CalculateRating(
                0, 6f, 3.5f));
    }

    // ==================================================
    // Algorithm Property Tests (8)
    // ==================================================

    [Fact]
    // Verifies that calculating a rating with higher review count increases strong rating.
    public void CalculateRatingHigherReviewCountIncreasesStrongRating()
    {
        float first = BayesianRatingScorer.CalculateRating(
            10, 5f, 3.5f);

        float second = BayesianRatingScorer.CalculateRating(
            100, 5f, 3.5f);

        float third = BayesianRatingScorer.CalculateRating(
            1000, 5f, 3.5f);

        Assert.True(second > first);
        Assert.True(third > second);
    }

    [Fact]
    // Verifies that calculating a rating with higher review count decreases weak rating.
    public void CalculateRatingHigherReviewCountDecreasesWeakRating()
    {
        float first = BayesianRatingScorer.CalculateRating(
            10, 1f, 3.5f);

        float second = BayesianRatingScorer.CalculateRating(
            100, 1f, 3.5f);

        float third = BayesianRatingScorer.CalculateRating(
            1000, 1f, 3.5f);

        Assert.True(second < first);
        Assert.True(third < second);
    }

    [Fact]
    // Verifies calculating a rating for higher prior weight pulls rating toward global.
    public void CalculateRatingHigherPriorWeightPullsRatingTowardGlobal()
    {
        float weakPrior = BayesianRatingScorer.CalculateRating(
            100, 5f, 3.5f, 10f);

        float strongPrior = BayesianRatingScorer.CalculateRating(
            100, 5f, 3.5f, 200f);

        Assert.True(
            Math.Abs(strongPrior - 3.5f) <
            Math.Abs(weakPrior - 3.5f));
    }

    [Fact]
    // Verifies calculating a rating for equal media and global average remain unchanged.
    public void CalculateRatingEqualMediaAndGlobalAverageRemainUnchanged()
    {
        for (int count = 0; count <= 10000; count += 100)
        {
            Assert.Equal(
                4f,
                BayesianRatingScorer.CalculateRating(
                    count, 4f, 4f),
                6);
        }
    }

    [Fact]
    // Verifies that calculating a rating with large review count approaches media average.
    public void CalculateRatingLargeReviewCountApproachesMediaAverage()
    {
        float rating = BayesianRatingScorer.CalculateRating(
            1000000, 5f, 3.5f);

        Assert.InRange(rating, 4.999f, 5f);
    }

    [Fact]
    // Verifies that calculating a rating with well reviewed media outranks sparse perfect rating.
    public void CalculateRatingWellReviewedMediaOutranksSparsePerfectRating()
    {
        float sparsePerfect = BayesianRatingScorer.CalculateRating(
            2, 5f, 3.5f);

        float wellReviewed = BayesianRatingScorer.CalculateRating(
            5000, 4.8f, 3.5f);

        Assert.True(wellReviewed > sparsePerfect);
    }

    [Fact]
    // Verifies that calculating a rating remains between media and global average.
    public void CalculateRatingRemainsBetweenMediaAndGlobalAverage()
    {
        var random = new Random(42);

        for (int i = 0; i < 10000; i++)
        {
            int count = random.Next(0, 1000000);

            float average =
                1f + (float)random.NextDouble() * 4f;

            float global =
                1f + (float)random.NextDouble() * 4f;

            float prior = random.Next(1, 1000);

            float rating = BayesianRatingScorer.CalculateRating(
                count, average, global, prior);

            Assert.InRange(
                rating,
                Math.Min(average, global),
                Math.Max(average, global));
        }
    }

    [Fact]
    // Verifies that scores from random valid inputs stay within [0, 1].
    public void ScoreRandomValidInputsRemainWithinBounds()
    {
        var random = new Random(42);

        for (int i = 0; i < 10000; i++)
        {
            int count = random.Next(0, 1000000);

            float average =
                1f + (float)random.NextDouble() * 4f;

            float global =
                1f + (float)random.NextDouble() * 4f;

            float prior = random.Next(1, 1000);

            float score = BayesianRatingScorer.Score(
                count, average, global, prior);

            Assert.True(float.IsFinite(score));
            Assert.InRange(score, 0f, 1f);
        }
    }
}