using System;
using System.Linq;
using Palette.Api.MachineLearning;
using Xunit;

public class SyntheticDataGeneratorTests
{
    [Fact]
    public void GenerateDefaultConfigurationReturns3000Ratings()
    {
        // Act
        var ratings = DataGenerator.Generate();

        // Assert
        Assert.Equal(3000, ratings.Count);
    }

    [Theory]
    [InlineData(100, 200, 30, 3000)]
    [InlineData(10, 20, 5, 50)]
    [InlineData(1, 1, 1, 1)]
    [InlineData(5, 10, 10, 50)]
    [InlineData(10, 1, 1, 10)]
    public void GenerateValidConfigurationReturnsExpectedDataset(
        int userCount,
        int movieCount,
        int ratingsPerUser,
        int expectedCount)
    {
        // Act
        var ratings = DataGenerator.Generate(
            userCount, movieCount, ratingsPerUser);

        // Assert
        Assert.Equal(expectedCount, ratings.Count);

        Assert.Equal(userCount,
            ratings.Select(r => r.UserId).Distinct().Count());

        Assert.InRange(
            ratings.Select(r => r.MediaId).Distinct().Count(),
            1,
            movieCount);

        Assert.All(ratings.GroupBy(r => r.UserId), group =>
            Assert.Equal(ratingsPerUser, group.Count()));
    }

    [Fact]
    public void GenerateValidConfigurationReturnsValidRatings()
    {
        // Act
        var ratings = DataGenerator.Generate();

        // Assert
        Assert.All(ratings, rating =>
        {
            Assert.True(float.IsFinite(rating.Rating));
            Assert.InRange(rating.Rating, 1f, 5f);
        });
    }

    [Fact]
    public void GenerateValidConfigurationReturnsValidIdentifiers()
    {
        // Act
        var ratings = DataGenerator.Generate();

        // Assert
        Assert.All(ratings, rating =>
        {
            Assert.NotEqual(Guid.Empty, rating.UserId);
            Assert.NotEqual(Guid.Empty, rating.MediaId);
        });
    }

    [Fact]
    public void GenerateValidConfigurationReturnsUniqueUserMediaPairs()
    {
        // Act
        var ratings = DataGenerator.Generate();

        var uniquePairs = ratings
            .Select(r => (r.UserId, r.MediaId))
            .Distinct()
            .Count();

        // Assert
        Assert.Equal(ratings.Count, uniquePairs);
    }

    [Fact]
    public void GenerateMaximumRatingsPerUserRatesEveryMovie()
    {
        // Arrange
        const int movieCount = 10;

        // Act
        var ratings = DataGenerator.Generate(
            userCount: 5,
            movieCount: movieCount,
            ratingsPerUser: movieCount);

        // Assert
        Assert.All(ratings.GroupBy(r => r.UserId), group =>
        {
            Assert.Equal(movieCount,
                group.Select(r => r.MediaId).Distinct().Count());
        });

        Assert.Equal(movieCount,
            ratings.Select(r => r.MediaId).Distinct().Count());
    }

    [Fact]
    public void GenerateSameSeedReturnsIdenticalRatingValues()
    {
        // Act
        var first = DataGenerator.Generate(seed: 42);
        var second = DataGenerator.Generate(seed: 42);

        // Assert
        Assert.Equal(
            first.Select(r => r.Rating).ToArray(),
            second.Select(r => r.Rating).ToArray());
    }

    [Fact]
    public void GenerateDifferentSeedsReturnsDifferentRatingValues()
    {
        // Act
        var first = DataGenerator.Generate(seed: 42);
        var second = DataGenerator.Generate(seed: 100);

        // Assert
        Assert.False(
            first.Select(r => r.Rating)
                .SequenceEqual(second.Select(r => r.Rating)));
    }

    [Theory]
    [InlineData(0, 10, 5)]
    [InlineData(-1, 10, 5)]
    [InlineData(10, 0, 5)]
    [InlineData(10, -1, 5)]
    [InlineData(10, 10, 0)]
    [InlineData(10, 10, -1)]
    [InlineData(10, 5, 6)]
    public void GenerateInvalidConfigurationThrowsArgumentException(
        int userCount,
        int movieCount,
        int ratingsPerUser)
    {
        // Act & Assert
        Assert.ThrowsAny<ArgumentException>(() =>
            DataGenerator.Generate(
                userCount, movieCount, ratingsPerUser));
    }
}