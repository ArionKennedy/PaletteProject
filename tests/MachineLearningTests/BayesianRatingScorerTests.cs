
using System;
using Palette.Api.MachineLearning.Popularity;
using Xunit;

public class BayesianRatingScorerTests
{
    // Mathematical accuracy

    // Verifies that ratings follow the Bayesian weighted average formula.
    [Theory]
    [InlineData(2, 5f)]
    [InlineData(5000, 4.8f)]
    [InlineData(1000, 4.7f)]
    [InlineData(500, 4.5f)]
    [InlineData(10000, 4.2f)]
    public void CalculateRatingReturnsExpectedBayesianAverage(int reviewCount, float averageRating)
    {
        const float globalAverage = 3.5f;
        const float priorWeight = 50f;

        float expected = (float)(((double)reviewCount * averageRating
            + priorWeight * globalAverage) / (reviewCount + priorWeight));

        Assert.Equal(expected, BayesianRatingScorer.CalculateRating(
            reviewCount, averageRating, globalAverage, priorWeight), 6);
    }

    // Verifies that equal weights produce the arithmetic mean.
    [Fact]
    public void CalculateRatingEqualWeightsReturnsArithmeticMean()
    {
        Assert.Equal(4f, BayesianRatingScorer.CalculateRating(50, 5f, 3f, 50f), 6);
    }

    // Verifies that the default prior weight is 50.
    [Fact]
    public void CalculateRatingUsesDefaultPriorWeight()
    {
        Assert.Equal(
            BayesianRatingScorer.CalculateRating(100, 4.5f, 3.5f, 50f),
            BayesianRatingScorer.CalculateRating(100, 4.5f, 3.5f));
    }

    // Verifies that fractional prior weights are supported.
    [Fact]
    public void CalculateRatingSupportsFractionalPriorWeight()
    {
        Assert.Equal(4.2f, BayesianRatingScorer.CalculateRating(10, 5f, 1f, 2.5f), 6);
    }

    // Normalization

    // Verifies that ratings between 1 and 5 are normalized to [0, 1].
    [Theory]
    [InlineData(1f, 0f)]
    [InlineData(2f, 0.25f)]
    [InlineData(3f, 0.50f)]
    [InlineData(4f, 0.75f)]
    [InlineData(5f, 1f)]
    public void ScoreReturnsExpectedNormalizedValue(float rating, float expected)
    {
        Assert.Equal(expected, BayesianRatingScorer.Score(100, rating, rating), 6);
    }

    // Boundary conditions

    // Verifies that unreviewed media receive the global average.
    [Fact]
    public void CalculateRatingZeroReviewsReturnsGlobalAverage()
    {
        Assert.Equal(3.5f, BayesianRatingScorer.CalculateRating(0, null, 3.5f));
    }

    // Verifies that zero reviews override a supplied media average.
    [Fact]
    public void CalculateRatingZeroReviewsIgnoresProvidedAverage()
    {
        Assert.Equal(3.5f, BayesianRatingScorer.CalculateRating(0, 5f, 3.5f));
    }

    // Verifies that unreviewed media receive the normalized global average.
    [Fact]
    public void ScoreZeroReviewsReturnsNormalizedGlobalAverage()
    {
        Assert.Equal(0.625f, BayesianRatingScorer.Score(0, null, 3.5f));
    }

    // Verifies that the maximum integer review count does not cause overflow.
    [Fact]
    public void CalculateRatingMaximumIntegerCountDoesNotOverflow()
    {
        float rating = BayesianRatingScorer.CalculateRating(int.MaxValue, 5f, 3.5f);

        Assert.True(float.IsFinite(rating));
        Assert.InRange(rating, 1f, 5f);
    }

    // Verifies that the maximum prior weight does not cause overflow.
    [Fact]
    public void CalculateRatingMaximumPriorWeightDoesNotOverflow()
    {
        float rating = BayesianRatingScorer.CalculateRating(100, 5f, 3.5f, float.MaxValue);

        Assert.True(float.IsFinite(rating));
        Assert.Equal(3.5f, rating, 6);
    }

    // Verifies that a negligible prior weight preserves the media average.
    [Fact]
    public void CalculateRatingSmallestPositivePriorApproachesAverage()
    {
        Assert.Equal(4.5f, BayesianRatingScorer.CalculateRating(
            100, 4.5f, 3.5f, float.Epsilon), 6);
    }

    // Input validation

    // Verifies that negative review counts are rejected.
    [Theory]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void CalculateRatingNegativeReviewCountThrowsException(int reviewCount)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            BayesianRatingScorer.CalculateRating(reviewCount, 4f, 3.5f));
    }

    // Verifies that invalid media average ratings are rejected.
    [Theory]
    [InlineData(0f)]
    [InlineData(5.1f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    public void CalculateRatingInvalidAverageThrowsException(float averageRating)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            BayesianRatingScorer.CalculateRating(100, averageRating, 3.5f));
    }

    // Verifies that invalid global average ratings are rejected.
    [Theory]
    [InlineData(0f)]
    [InlineData(5.1f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    public void CalculateRatingInvalidGlobalAverageThrowsException(float globalAverage)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            BayesianRatingScorer.CalculateRating(100, 4f, globalAverage));
    }

    // Verifies that nonpositive and nonfinite prior weights are rejected.
    [Theory]
    [InlineData(0f)]
    [InlineData(-1f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    public void CalculateRatingInvalidPriorWeightThrowsException(float priorWeight)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            BayesianRatingScorer.CalculateRating(100, 4f, 3.5f, priorWeight));
    }

    // Verifies that reviewed media require an average rating.
    [Fact]
    public void CalculateRatingMissingAverageWithReviewsThrowsException()
    {
        Assert.Throws<ArgumentException>(() =>
            BayesianRatingScorer.CalculateRating(100, null, 3.5f));
    }

    // Verifies that unreviewed media still require a valid global average.
    [Fact]
    public void CalculateRatingZeroReviewsStillValidatesGlobalAverage()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            BayesianRatingScorer.CalculateRating(0, null, float.NaN));
    }

    // Verifies that unreviewed media still require a valid prior weight.
    [Fact]
    public void CalculateRatingZeroReviewsStillValidatesPriorWeight()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            BayesianRatingScorer.CalculateRating(0, null, 3.5f, 0f));
    }

    // Verifies that a supplied media average must be valid even without reviews.
    [Fact]
    public void CalculateRatingZeroReviewsStillValidatesProvidedAverage()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            BayesianRatingScorer.CalculateRating(0, 6f, 3.5f));
    }

    // Algorithm properties

    // Verifies that additional reviews strengthen above-average ratings.
    [Fact]
    public void CalculateRatingHigherReviewCountIncreasesStrongRating()
    {
        float first = BayesianRatingScorer.CalculateRating(10, 5f, 3.5f);
        float second = BayesianRatingScorer.CalculateRating(100, 5f, 3.5f);
        float third = BayesianRatingScorer.CalculateRating(1000, 5f, 3.5f);

        Assert.True(second > first);
        Assert.True(third > second);
    }

    // Verifies that additional reviews lower below-average ratings.
    [Fact]
    public void CalculateRatingHigherReviewCountDecreasesWeakRating()
    {
        float first = BayesianRatingScorer.CalculateRating(10, 1f, 3.5f);
        float second = BayesianRatingScorer.CalculateRating(100, 1f, 3.5f);
        float third = BayesianRatingScorer.CalculateRating(1000, 1f, 3.5f);

        Assert.True(second < first);
        Assert.True(third < second);
    }

    // Verifies that stronger prior weights pull ratings toward the global average.
    [Fact]
    public void CalculateRatingHigherPriorWeightPullsRatingTowardGlobal()
    {
        float weakPrior = BayesianRatingScorer.CalculateRating(100, 5f, 3.5f, 10f);
        float strongPrior = BayesianRatingScorer.CalculateRating(100, 5f, 3.5f, 200f);

        Assert.True(Math.Abs(strongPrior - 3.5f) < Math.Abs(weakPrior - 3.5f));
    }

    // Verifies that equal media and global averages remain unchanged.
    [Fact]
    public void CalculateRatingEqualMediaAndGlobalAverageRemainUnchanged()
    {
        for (int count = 0; count <= 10000; count += 100)
            Assert.Equal(4f, BayesianRatingScorer.CalculateRating(count, 4f, 4f), 6);
    }

    // Verifies that large review counts cause convergence toward the media average.
    [Fact]
    public void CalculateRatingLargeReviewCountApproachesMediaAverage()
    {
        float rating = BayesianRatingScorer.CalculateRating(1000000, 5f, 3.5f);

        Assert.InRange(rating, 4.999f, 5f);
    }

    // Verifies that well-reviewed media can outrank sparsely reviewed perfect ratings.
    [Fact]
    public void CalculateRatingWellReviewedMediaOutranksSparsePerfectRating()
    {
        float sparsePerfect = BayesianRatingScorer.CalculateRating(2, 5f, 3.5f);
        float wellReviewed = BayesianRatingScorer.CalculateRating(5000, 4.8f, 3.5f);

        Assert.True(wellReviewed > sparsePerfect);
    }

    // Verifies that 10,000 randomized ratings remain between their media and global averages.
    [Fact]
    public void CalculateRatingRemainsBetweenMediaAndGlobalAverage()
    {
        var random = new Random(42);

        for (int i = 0; i < 10000; i++)
        {
            int count = random.Next(0, 1000000);
            float average = 1f + (float)random.NextDouble() * 4f;
            float global = 1f + (float)random.NextDouble() * 4f;
            float prior = random.Next(1, 1000);

            float rating = BayesianRatingScorer.CalculateRating(count, average, global, prior);

            Assert.InRange(rating, Math.Min(average, global), Math.Max(average, global));
        }
    }

    // Verifies that 10,000 randomized inputs produce finite scores within [0, 1].
    [Fact]
    public void ScoreRandomValidInputsRemainWithinBounds()
    {
        var random = new Random(42);

        for (int i = 0; i < 10000; i++)
        {
            int count = random.Next(0, 1000000);
            float average = 1f + (float)random.NextDouble() * 4f;
            float global = 1f + (float)random.NextDouble() * 4f;
            float prior = random.Next(1, 1000);

            float score = BayesianRatingScorer.Score(count, average, global, prior);

            Assert.True(float.IsFinite(score));
            Assert.InRange(score, 0f, 1f);
        }
    }
}
