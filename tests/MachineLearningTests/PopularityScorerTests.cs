
using System;
using Palette.Api.MachineLearning.Popularity;
using Xunit;

public class PopularityScorerTests
{
    // Mathematical accuracy

    // Verifies that popularity scores follow logarithmic normalization.
    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(500)]
    [InlineData(1000)]
    [InlineData(5000)]
    public void ScoreReturnsExpectedLogarithmicValue(int reviewCount)
    {
        const int maxReviewCount = 10000;
        float expected = (float)(Math.Log(1d + reviewCount) / Math.Log(1d + maxReviewCount));

        Assert.Equal(expected, PopularityScorer.Score(reviewCount, maxReviewCount), 6);
    }

    // Verifies that ln(10) / ln(100) returns 0.5.
    [Fact]
    public void ScoreKnownLogarithmicRatioReturnsHalf()
    {
        Assert.Equal(0.5f, PopularityScorer.Score(9, 99), 6);
    }

    // Verifies that ln(10) / ln(10000) returns 0.25.
    [Fact]
    public void ScoreKnownLogarithmicRatioReturnsQuarter()
    {
        Assert.Equal(0.25f, PopularityScorer.Score(9, 9999), 6);
    }

    // Boundary conditions

    // Verifies that media without reviews receive a score of zero.
    [Fact]
    public void ScoreZeroReviewsReturnsZero()
    {
        Assert.Equal(0f, PopularityScorer.Score(0, 10000));
    }

    // Verifies that an empty review database produces a score of zero.
    [Fact]
    public void ScoreZeroReviewsAndZeroMaximumReturnsZero()
    {
        Assert.Equal(0f, PopularityScorer.Score(0, 0));
    }

    // Verifies that the maximum review count produces a score of one.
    [Fact]
    public void ScoreEqualToMaximumReturnsOne()
    {
        Assert.Equal(1f, PopularityScorer.Score(10000, 10000));
    }

    // Verifies that counts exceeding the maximum are clamped to one.
    [Fact]
    public void ScoreAboveMaximumIsClampedToOne()
    {
        Assert.Equal(1f, PopularityScorer.Score(20000, 10000));
    }

    // Verifies that the smallest positive maximum produces a score of one.
    [Fact]
    public void ScoreSmallestPositiveMaximumReturnsOne()
    {
        Assert.Equal(1f, PopularityScorer.Score(1, 1));
    }

    // Verifies that maximum integer values do not cause overflow.
    [Fact]
    public void ScoreMaximumIntegerValuesDoesNotOverflow()
    {
        Assert.Equal(1f, PopularityScorer.Score(int.MaxValue, int.MaxValue));
    }

    // Input validation

    // Verifies that negative review counts are rejected.
    [Theory]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void ScoreNegativeReviewCountThrowsException(int reviewCount)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            PopularityScorer.Score(reviewCount, 10000));
    }

    // Verifies that negative maximum review counts are rejected.
    [Theory]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void ScoreNegativeMaximumThrowsException(int maxReviewCount)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            PopularityScorer.Score(100, maxReviewCount));
    }

    // Verifies that positive review counts require a positive maximum.
    [Fact]
    public void ScorePositiveReviewsWithZeroMaximumThrowsException()
    {
        Assert.Throws<ArgumentException>(() => PopularityScorer.Score(100, 0));
    }

    // Algorithm properties

    // Verifies that popularity strictly increases with the review count.
    [Fact]
    public void ScoreIncreasesAsReviewCountIncreases()
    {
        const int maxReviewCount = 10000;
        float previous = PopularityScorer.Score(0, maxReviewCount);

        for (int count = 1; count <= maxReviewCount; count++)
        {
            float current = PopularityScorer.Score(count, maxReviewCount);

            Assert.True(current > previous);
            previous = current;
        }
    }

    // Verifies that increasing the reference maximum reduces popularity.
    [Fact]
    public void ScoreDecreasesAsReferenceMaximumIncreases()
    {
        const int reviewCount = 100;

        float first = PopularityScorer.Score(reviewCount, 1000);
        float second = PopularityScorer.Score(reviewCount, 10000);
        float third = PopularityScorer.Score(reviewCount, 100000);

        Assert.True(first > second);
        Assert.True(second > third);
    }

    // Verifies that additional reviews provide diminishing popularity gains.
    [Fact]
    public void ScoreExhibitsDiminishingReturns()
    {
        const int maxReviewCount = 10000;

        float earlyIncrease = PopularityScorer.Score(200, maxReviewCount)
            - PopularityScorer.Score(100, maxReviewCount);

        float lateIncrease = PopularityScorer.Score(9200, maxReviewCount)
            - PopularityScorer.Score(9100, maxReviewCount);

        Assert.True(earlyIncrease > lateIncrease);
    }

    // Verifies that logarithmic normalization exceeds linear normalization between boundaries.
    [Fact]
    public void ScoreExceedsLinearNormalizationBetweenBoundaries()
    {
        const int maxReviewCount = 10000;

        for (int count = 1; count < maxReviewCount; count++)
        {
            float linear = (float)count / maxReviewCount;
            float logarithmic = PopularityScorer.Score(count, maxReviewCount);

            Assert.True(logarithmic > linear);
        }
    }

    // Verifies that repeated calls produce identical popularity scores.
    [Fact]
    public void ScoreRepeatedCallsReturnSameValue()
    {
        float expected = PopularityScorer.Score(500, 10000);

        for (int i = 0; i < 100; i++)
            Assert.Equal(expected, PopularityScorer.Score(500, 10000));
    }

    // Verifies that 10,000 randomized inputs produce finite scores within [0, 1].
    [Fact]
    public void ScoreRandomValidInputsRemainWithinBounds()
    {
        var random = new Random(42);

        for (int i = 0; i < 10000; i++)
        {
            int reviewCount = random.Next(0, 1000000);
            int maxReviewCount = random.Next(1, 1000000);
            float score = PopularityScorer.Score(reviewCount, maxReviewCount);

            Assert.True(float.IsFinite(score));
            Assert.InRange(score, 0f, 1f);
        }
    }

    // Verifies that scores remain capped at one above the reference maximum.
    [Fact]
    public void ScoreRemainsOneAboveReferenceMaximum()
    {
        const int maxReviewCount = 1000;

        for (int count = 1000; count <= 10000; count += 100)
            Assert.Equal(1f, PopularityScorer.Score(count, maxReviewCount));
    }
}
