
using System;
using System.Linq;
using Palette.Api.MachineLearning;
using Xunit;

public class SyntheticDataGeneratorTests
{
    // Verifies that the default configuration generates 3,000 ratings.
    [Fact]
    public void GenerateDefaultConfigurationReturns3000Ratings()
    {
        Assert.Equal(3000, DataGenerator.Generate().Count);
    }

    // Verifies that valid configurations produce the expected datasets.
    [Theory]
    [InlineData(100, 200, 30, 3000)]
    [InlineData(10, 20, 5, 50)]
    [InlineData(1, 1, 1, 1)]
    [InlineData(5, 10, 10, 50)]
    [InlineData(10, 1, 1, 10)]
    public void GenerateValidConfigurationReturnsExpectedDataset(
        int userCount, int movieCount, int ratingsPerUser, int expectedCount)
    {
        var ratings = DataGenerator.Generate(userCount, movieCount, ratingsPerUser);

        Assert.Equal(expectedCount, ratings.Count);
        Assert.Equal(userCount, ratings.Select(r => r.UserId).Distinct().Count());
        Assert.InRange(ratings.Select(r => r.MediaId).Distinct().Count(), 1, movieCount);

        Assert.All(ratings.GroupBy(r => r.UserId), group =>
            Assert.Equal(ratingsPerUser, group.Count()));
    }

    // Verifies that all generated ratings are finite and within [1, 5].
    [Fact]
    public void GenerateValidConfigurationReturnsValidRatings()
    {
        Assert.All(DataGenerator.Generate(), rating =>
        {
            Assert.True(float.IsFinite(rating.Rating));
            Assert.InRange(rating.Rating, 1f, 5f);
        });
    }

    // Verifies that generated user and media identifiers are nonempty.
    [Fact]
    public void GenerateValidConfigurationReturnsValidIdentifiers()
    {
        Assert.All(DataGenerator.Generate(), rating =>
        {
            Assert.NotEqual(Guid.Empty, rating.UserId);
            Assert.NotEqual(Guid.Empty, rating.MediaId);
        });
    }

    // Verifies that generated user-media pairs are unique.
    [Fact]
    public void GenerateValidConfigurationReturnsUniqueUserMediaPairs()
    {
        var ratings = DataGenerator.Generate();

        Assert.Equal(ratings.Count,
            ratings.Select(r => (r.UserId, r.MediaId)).Distinct().Count());
    }

    // Verifies that every user rates every movie at maximum ratings per user.
    [Fact]
    public void GenerateMaximumRatingsPerUserRatesEveryMovie()
    {
        const int movieCount = 10;
        var ratings = DataGenerator.Generate(5, movieCount, movieCount);

        Assert.All(ratings.GroupBy(r => r.UserId), group =>
            Assert.Equal(movieCount, group.Select(r => r.MediaId).Distinct().Count()));

        Assert.Equal(movieCount, ratings.Select(r => r.MediaId).Distinct().Count());
    }

    // Verifies that identical seeds produce identical rating values.
    [Fact]
    public void GenerateSameSeedReturnsIdenticalRatingValues()
    {
        var first = DataGenerator.Generate(seed: 42);
        var second = DataGenerator.Generate(seed: 42);

        Assert.Equal(first.Select(r => r.Rating).ToArray(),
            second.Select(r => r.Rating).ToArray());
    }

    // Verifies that different seeds produce different rating values.
    [Fact]
    public void GenerateDifferentSeedsReturnsDifferentRatingValues()
    {
        var first = DataGenerator.Generate(seed: 42);
        var second = DataGenerator.Generate(seed: 100);

        Assert.False(first.Select(r => r.Rating)
            .SequenceEqual(second.Select(r => r.Rating)));
    }

    // Verifies that invalid generator configurations are rejected.
    [Theory]
    [InlineData(0, 10, 5)]
    [InlineData(-1, 10, 5)]
    [InlineData(10, 0, 5)]
    [InlineData(10, -1, 5)]
    [InlineData(10, 10, 0)]
    [InlineData(10, 10, -1)]
    [InlineData(10, 5, 6)]
    public void GenerateInvalidConfigurationThrowsArgumentException(
        int userCount, int movieCount, int ratingsPerUser)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            DataGenerator.Generate(userCount, movieCount, ratingsPerUser));
    }
}