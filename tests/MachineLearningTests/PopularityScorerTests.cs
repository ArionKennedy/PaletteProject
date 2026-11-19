
using System;
using Palette.Api.MachineLearning.Popularity;
using Xunit;

public class PopularityScorerTests
{
    // ==================================================
    // Mathematical Accuracy Tests (8)
    // ==================================================

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(500)]
    [InlineData(1000)]
    [InlineData(5000)]
    // Verifies that scoring returns the expected logarithmic value.
    public void ScoreReturnsExpectedLogarithmicValue(
        int reviewCount)
    {
        const int maxReviewCount = 10000;

        float expected = (float)(
            Math.Log(1d + reviewCount) /
            Math.Log(1d + maxReviewCount));

        Assert.Equal(
            expected,
            PopularityScorer.Score(reviewCount, maxReviewCount),
            6);
    }

    [Fact]
    // Verifies that scoring with known logarithmic ratio returns half.
    public void ScoreKnownLogarithmicRatioReturnsHalf()
    {
        // ln(10) / ln(100) = 0.5

        Assert.Equal(
            0.5f,
            PopularityScorer.Score(9, 99),
            6);
    }

    [Fact]
    // Verifies that scoring with known logarithmic ratio returns quarter.
    public void ScoreKnownLogarithmicRatioReturnsQuarter()
    {
        // ln(10) / ln(10000) = 0.25

        Assert.Equal(
            0.25f,
            PopularityScorer.Score(9, 9999),
            6);
    }

    // ==================================================
    // Boundary Condition Tests (6)
    // ==================================================

    [Fact]
    // Verifies that scoring with zero reviews returns zero.
    public void ScoreZeroReviewsReturnsZero()
    {
        Assert.Equal(
            0f,
            PopularityScorer.Score(0, 10000));
    }

    [Fact]
    // Verifies that scoring with zero reviews and zero maximum returns zero.
    public void ScoreZeroReviewsAndZeroMaximumReturnsZero()
    {
        Assert.Equal(
            0f,
            PopularityScorer.Score(0, 0));
    }

    [Fact]
    // Verifies that scoring with equal to maximum returns one.
    public void ScoreEqualToMaximumReturnsOne()
    {
        Assert.Equal(
            1f,
            PopularityScorer.Score(10000, 10000));
    }

    [Fact]
    // Verifies that scoring with above maximum is clamped to one.
    public void ScoreAboveMaximumIsClampedToOne()
    {
        Assert.Equal(
            1f,
            PopularityScorer.Score(20000, 10000));
    }

    [Fact]
    // Verifies that scoring with smallest positive maximum returns one.
    public void ScoreSmallestPositiveMaximumReturnsOne()
    {
        Assert.Equal(
            1f,
            PopularityScorer.Score(1, 1));
    }

    [Fact]
    // Verifies that scoring with maximum integer values does not overflow.
    public void ScoreMaximumIntegerValuesDoesNotOverflow()
    {
        Assert.Equal(
            1f,
            PopularityScorer.Score(
                int.MaxValue,
                int.MaxValue));
    }

    // ==================================================
    // Invalid Input Tests (5)
    // ==================================================

    [Theory]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    // Verifies that scoring with negative review count throws an exception.
    public void ScoreNegativeReviewCountThrowsException(
        int reviewCount)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            PopularityScorer.Score(reviewCount, 10000));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    // Verifies that scoring with negative maximum throws an exception.
    public void ScoreNegativeMaximumThrowsException(
        int maxReviewCount)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            PopularityScorer.Score(100, maxReviewCount));
    }

    [Fact]
    // Verifies that scoring with positive reviews with zero maximum throws an exception.
    public void ScorePositiveReviewsWithZeroMaximumThrowsException()
    {
        Assert.Throws<ArgumentException>(() =>
            PopularityScorer.Score(100, 0));
    }

    // ==================================================
    // Algorithm Property Tests (7)
    // ==================================================

    [Fact]
    // Verifies that scoring increases as review count increases.
    public void ScoreIncreasesAsReviewCountIncreases()
    {
        const int maxReviewCount = 10000;

        float previous = PopularityScorer.Score(
            0, maxReviewCount);

        for (int count = 1; count <= maxReviewCount; count++)
        {
            float current = PopularityScorer.Score(
                count, maxReviewCount);

            Assert.True(current > previous);

            previous = current;
        }
    }

    [Fact]
    // Verifies that scoring decreases as reference maximum increases.
    public void ScoreDecreasesAsReferenceMaximumIncreases()
    {
        const int reviewCount = 100;

        float first = PopularityScorer.Score(
            reviewCount, 1000);

        float second = PopularityScorer.Score(
            reviewCount, 10000);

        float third = PopularityScorer.Score(
            reviewCount, 100000);

        Assert.True(first > second);
        Assert.True(second > third);
    }

    [Fact]
    // Verifies that scoring with exhibits diminishing returns .
    public void ScoreExhibitsDiminishingReturns()
    {
        const int maxReviewCount = 10000;

        float earlyIncrease =
            PopularityScorer.Score(200, maxReviewCount) -
            PopularityScorer.Score(100, maxReviewCount);

        float lateIncrease =
            PopularityScorer.Score(9200, maxReviewCount) -
            PopularityScorer.Score(9100, maxReviewCount);

        Assert.True(earlyIncrease > lateIncrease);
    }

    [Fact]
    // Verifies that logarithmic scoring exceeds linear normalization for intermediate counts.
    public void ScoreExceedsLinearNormalizationBetweenBoundaries()
    {
        const int maxReviewCount = 10000;

        for (int count = 1; count < maxReviewCount; count++)
        {
            float linearScore =
                (float)count / maxReviewCount;

            float logarithmicScore =
                PopularityScorer.Score(count, maxReviewCount);

            Assert.True(logarithmicScore > linearScore);
        }
    }

    [Fact]
    // Verifies that repeated scoring calls return the same value.
    public void ScoreRepeatedCallsReturnSameValue()
    {
        float expected = PopularityScorer.Score(500, 10000);

        for (int i = 0; i < 100; i++)
        {
            Assert.Equal(
                expected,
                PopularityScorer.Score(500, 10000));
        }
    }

    [Fact]
    // Verifies that valid review counts always produce scores within [0, 1].
    public void ScoreRandomValidInputsRemainWithinBounds()
    {
        var random = new Random(42);

        for (int i = 0; i < 10000; i++)
        {
            int reviewCount = random.Next(0, 1000000);
            int maxReviewCount = random.Next(1, 1000000);

            float score = PopularityScorer.Score(
                reviewCount, maxReviewCount);

            Assert.True(float.IsFinite(score));
            Assert.InRange(score, 0f, 1f);
        }
    }

    [Fact]
    // Verifies that scoring remains one above reference maximum.
    public void ScoreRemainsOneAboveReferenceMaximum()
    {
        const int maxReviewCount = 1000;

        for (int count = 1000; count <= 10000; count += 100)
        {
            Assert.Equal(
                1f,
                PopularityScorer.Score(count, maxReviewCount));
        }
    }
}
