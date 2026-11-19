using System;
using System.Collections.Generic;
using System.Linq;
using Palette.Api.MachineLearning;
using Palette.Api.MachineLearning.Data;
using Xunit;

public class DatasetSplitterTests
{
    private readonly List<RatingObservation> _ratings =
        DataGenerator.Generate().ToList();

    // Functional tests

    [Fact]
    // Verifies that splitting ratings with default ratios returns the correct dataset sizes.
    public void SplitDefaultRatiosReturnsCorrectDatasetSizes()
    {
        // Act
        var split = DatasetSplitter.Split(_ratings);

        // Assert
        Assert.Equal(2400, split.Training.Count);
        Assert.Equal(300, split.Validation.Count);
        Assert.Equal(300, split.Testing.Count);
    }

    [Theory]
    [InlineData(0.6, 0.2, 1800, 600, 600)]
    [InlineData(0.5, 0.25, 1500, 750, 750)]
    [InlineData(0.9, 0.05, 2700, 150, 150)]
    // Verifies that splitting ratings with custom ratios returns the correct dataset sizes.
    public void SplitCustomRatiosReturnsCorrectDatasetSizes(
        double trainingRatio,
        double validationRatio,
        int expectedTraining,
        int expectedValidation,
        int expectedTesting)
    {
        // Act
        var split = DatasetSplitter.Split(
            _ratings,
            trainingRatio,
            validationRatio);

        // Assert
        Assert.Equal(expectedTraining, split.Training.Count);
        Assert.Equal(expectedValidation, split.Validation.Count);
        Assert.Equal(expectedTesting, split.Testing.Count);
    }

    [Fact]
    // Verifies that splitting ratings with valid ratings returns disjoint datasets.
    public void SplitValidRatingsReturnsDisjointDatasets()
    {
        // Act
        var split = DatasetSplitter.Split(_ratings);

        // Assert
        Assert.Empty(split.Training.Intersect(split.Validation));
        Assert.Empty(split.Training.Intersect(split.Testing));
        Assert.Empty(split.Validation.Intersect(split.Testing));
    }

    [Fact]
    // Verifies that splitting ratings with valid ratings returns disjoint user media pairs.
    public void SplitValidRatingsReturnsDisjointUserMediaPairs()
    {
        // Act
        var split = DatasetSplitter.Split(_ratings);

        var training = GetUserMediaPairs(split.Training);
        var validation = GetUserMediaPairs(split.Validation);
        var testing = GetUserMediaPairs(split.Testing);

        // Assert
        Assert.Empty(training.Intersect(validation));
        Assert.Empty(training.Intersect(testing));
        Assert.Empty(validation.Intersect(testing));
    }

    [Fact]
    // Verifies that splitting ratings with valid ratings preserves all observations.
    public void SplitValidRatingsPreservesAllObservations()
    {
        // Act
        var split = DatasetSplitter.Split(_ratings);

        // Assert
        AssertPreservedObservations(_ratings, split);
    }

    [Fact]
    // Verifies that splitting ratings with valid ratings does not modify input.
    public void SplitValidRatingsDoesNotModifyInput()
    {
        // Arrange
        var original = _ratings.ToArray();

        // Act
        DatasetSplitter.Split(_ratings);

        // Assert
        Assert.Equal(original, _ratings.ToArray());
    }

    // Reproducibility tests

    [Fact]
    // Verifies that splitting ratings with same seed returns the identical datasets.
    public void SplitSameSeedReturnsIdenticalDatasets()
    {
        // Act
        var first = DatasetSplitter.Split(_ratings, seed: 42);
        var second = DatasetSplitter.Split(_ratings, seed: 42);

        // Assert
        Assert.Equal(
            first.Training.ToArray(),
            second.Training.ToArray());

        Assert.Equal(
            first.Validation.ToArray(),
            second.Validation.ToArray());

        Assert.Equal(
            first.Testing.ToArray(),
            second.Testing.ToArray());
    }

    [Fact]
    // Verifies that splitting ratings with different seeds returns different datasets.
    public void SplitDifferentSeedsReturnsDifferentDatasets()
    {
        // Act
        var first = DatasetSplitter.Split(_ratings, seed: 42);
        var second = DatasetSplitter.Split(_ratings, seed: 100);

        // Assert
        Assert.False(first.Training
            .ToHashSet()
            .SetEquals(second.Training));
    }

    // Training coverage tests

    [Fact]
    // Verifies that splitting ratings with valid ratings guarantees user coverage.
    public void SplitValidRatingsGuaranteesUserCoverage()
    {
        // Act
        var split = DatasetSplitter.Split(_ratings);

        var knownUsers = split.Training
            .Select(r => r.UserId)
            .ToHashSet();

        // Assert
        Assert.All(_ratings, rating =>
            Assert.Contains(rating.UserId, knownUsers));
    }

    [Fact]
    // Verifies that splitting ratings with valid ratings guarantees media coverage.
    public void SplitValidRatingsGuaranteesMediaCoverage()
    {
        // Act
        var split = DatasetSplitter.Split(_ratings);

        var knownMedia = split.Training
            .Select(r => r.MediaId)
            .ToHashSet();

        // Assert
        Assert.All(_ratings, rating =>
            Assert.Contains(rating.MediaId, knownMedia));
    }

    [Fact]
    // Verifies that splitting ratings with rare user includes observation in training.
    public void SplitRareUserIncludesObservationInTraining()
    {
        // Arrange
        var ratings = _ratings.ToList();

        var rareRating = new RatingObservation(
            Guid.NewGuid(),
            ratings[0].MediaId,
            5f);

        ratings.Add(rareRating);

        // Act
        var split = DatasetSplitter.Split(ratings);

        // Assert
        Assert.Contains(rareRating, split.Training);
        Assert.DoesNotContain(rareRating, split.Validation);
        Assert.DoesNotContain(rareRating, split.Testing);
    }

    [Fact]
    // Verifies that splitting ratings with rare media includes observation in training.
    public void SplitRareMediaIncludesObservationInTraining()
    {
        // Arrange
        var ratings = _ratings.ToList();

        var rareRating = new RatingObservation(
            ratings[0].UserId,
            Guid.NewGuid(),
            5f);

        ratings.Add(rareRating);

        // Act
        var split = DatasetSplitter.Split(ratings);

        // Assert
        Assert.Contains(rareRating, split.Training);
        Assert.DoesNotContain(rareRating, split.Validation);
        Assert.DoesNotContain(rareRating, split.Testing);
    }

    [Fact]
    // Verifies that splitting ratings with rare user and media includes observation in training.
    public void SplitRareUserAndMediaIncludesObservationInTraining()
    {
        // Arrange
        var ratings = _ratings.ToList();

        var rareRating = new RatingObservation(
            Guid.NewGuid(),
            Guid.NewGuid(),
            5f);

        ratings.Add(rareRating);

        // Act
        var split = DatasetSplitter.Split(ratings);

        // Assert
        Assert.Contains(rareRating, split.Training);
        Assert.DoesNotContain(rareRating, split.Validation);
        Assert.DoesNotContain(rareRating, split.Testing);
    }

    // Boundary tests

    [Fact]
    // Verifies that splitting ratings with uneven dataset size preserves all observations.
    public void SplitUnevenDatasetSizePreservesAllObservations()
    {
        // Arrange
        var ratings = _ratings.Take(2999).ToList();

        // Act
        var split = DatasetSplitter.Split(ratings);

        // Assert
        Assert.Equal(2399, split.Training.Count);
        Assert.Equal(299, split.Validation.Count);
        Assert.Equal(301, split.Testing.Count);

        AssertPreservedObservations(ratings, split);
    }

    [Fact]
    // Verifies that splitting ratings with minimum default dataset returns non empty datasets.
    public void SplitMinimumDefaultDatasetReturnsNonEmptyDatasets()
    {
        // Arrange
        var ratings = CreateDenseRatings(
            userCount: 5,
            movieCount: 2);

        // Act
        var split = DatasetSplitter.Split(ratings);

        // Assert
        Assert.Equal(8, split.Training.Count);
        Assert.Single(split.Validation);
        Assert.Single(split.Testing);

        AssertPreservedObservations(ratings, split);
    }

    [Fact]
    // Verifies that splitting ratings with exact training coverage returns the valid split.
    public void SplitExactTrainingCoverageReturnsValidSplit()
    {
        // Arrange
        var movieA = Guid.NewGuid();
        var movieB = Guid.NewGuid();

        var users = Enumerable.Range(0, 8)
            .Select(_ => Guid.NewGuid())
            .ToArray();

        var ratings = users
            .Select((user, index) => new RatingObservation(
                user,
                index < 4 ? movieA : movieB,
                4f))
            .ToList();

        ratings.Add(new RatingObservation(
            users[0], movieB, 3f));

        ratings.Add(new RatingObservation(
            users[4], movieA, 5f));

        // Act
        var split = DatasetSplitter.Split(ratings);

        // Assert
        Assert.Equal(8, split.Training.Count);
        Assert.Single(split.Validation);
        Assert.Single(split.Testing);

        Assert.Equal(8, split.Training
            .Select(r => r.UserId)
            .Distinct()
            .Count());

        AssertPreservedObservations(ratings, split);
    }

    // Exception tests

    [Fact]
    // Verifies that splitting ratings with null ratings throws ArgumentNullException.
    public void SplitNullRatingsThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            DatasetSplitter.Split(null!));
    }

    [Fact]
    // Verifies that splitting ratings with empty ratings throws ArgumentException.
    public void SplitEmptyRatingsThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            DatasetSplitter.Split(
                Array.Empty<RatingObservation>()));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(9)]
    // Verifies that splitting ratings with insufficient ratings throws ArgumentException.
    public void SplitInsufficientRatingsThrowsArgumentException(
        int count)
    {
        // Arrange
        var ratings = _ratings.Take(count).ToList();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            DatasetSplitter.Split(ratings));
    }

    [Fact]
    // Verifies that splitting ratings with insufficient training capacity throws InvalidOperationException.
    public void SplitInsufficientTrainingCapacityThrowsInvalidOperationException()
    {
        // Arrange
        var ratings = Enumerable.Range(0, 10)
            .Select(_ => new RatingObservation(
                Guid.NewGuid(),
                Guid.NewGuid(),
                4f))
            .ToList();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            DatasetSplitter.Split(ratings));
    }

    [Theory]
    [InlineData(0.0, 0.1)]
    [InlineData(-0.5, 0.1)]
    [InlineData(0.8, 0.0)]
    [InlineData(0.8, -0.1)]
    [InlineData(0.8, 0.3)]
    [InlineData(0.5, 0.5)]
    [InlineData(1.0, 0.1)]
    [InlineData(0.1, 1.0)]
    [InlineData(double.NaN, 0.1)]
    [InlineData(0.8, double.NaN)]
    [InlineData(double.PositiveInfinity, 0.1)]
    [InlineData(0.8, double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity, 0.1)]
    [InlineData(0.8, double.NegativeInfinity)]
    // Verifies that splitting ratings with invalid ratios throws ArgumentException.
    public void SplitInvalidRatiosThrowsArgumentException(
        double trainingRatio,
        double validationRatio)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            DatasetSplitter.Split(
                _ratings,
                trainingRatio,
                validationRatio));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    // Verifies that splitting ratings with duplicate user media pairs throws ArgumentException.
    public void SplitDuplicateUserMediaPairsThrowsArgumentException(
        bool identicalRating)
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        Guid mediaId = Guid.NewGuid();

        var ratings = new List<RatingObservation>
        {
            new(userId, mediaId, 4f),
            new(userId, mediaId, identicalRating ? 4f : 5f)
        };

        // Verify identifiers
        Assert.Equal(ratings[0].UserId, ratings[1].UserId);
        Assert.Equal(ratings[0].MediaId, ratings[1].MediaId);

        // Verify duplicate relationship
        Assert.Single(ratings
            .Select(r => (r.UserId, r.MediaId))
            .Distinct());

        // Act
        var exception = Assert.Throws<ArgumentException>(() =>
            DatasetSplitter.Split(ratings));

        // Assert
        Assert.Contains("Duplicate", exception.Message);
    }

    // Helper methods

    private static HashSet<(Guid UserId, Guid MediaId)>
        GetUserMediaPairs(IEnumerable<RatingObservation> ratings)
    {
        return ratings
            .Select(r => (r.UserId, r.MediaId))
            .ToHashSet();
    }

    // Creates dense user-media ratings for split tests.
    private static List<RatingObservation> CreateDenseRatings(
        int userCount,
        int movieCount)
    {
        var users = Enumerable.Range(0, userCount)
            .Select(_ => Guid.NewGuid())
            .ToArray();

        var movies = Enumerable.Range(0, movieCount)
            .Select(_ => Guid.NewGuid())
            .ToArray();

        return users.SelectMany(user =>
            movies.Select(movie =>
                new RatingObservation(user, movie, 4f)))
            .ToList();
    }

    // Asserts that the split preserves every original observation.
    private static void AssertPreservedObservations(
        IReadOnlyList<RatingObservation> original,
        DatasetSplit split)
    {
        var combined = split.Training
            .Concat(split.Validation)
            .Concat(split.Testing)
            .ToList();

        Assert.Equal(original.Count, combined.Count);

        Assert.True(original
            .ToHashSet()
            .SetEquals(combined));

        Assert.Equal(
            original.Count,
            combined.Distinct().Count());
    }

    
}