
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

    // Splits the default dataset using configurable ratios and seed.
    private DatasetSplit Split(
        double trainingRatio = 0.8,
        double validationRatio = 0.1,
        int seed = 42)
    {
        return DatasetSplitter.Split(_ratings, trainingRatio, validationRatio, seed);
    }

    // Extracts unique user-media pairs from rating observations.
    private static HashSet<(Guid UserId, Guid MediaId)> GetUserMediaPairs(
        IEnumerable<RatingObservation> ratings)
    {
        return ratings.Select(r => (r.UserId, r.MediaId)).ToHashSet();
    }

    // Creates a dense dataset where every user rates every movie.
    private static List<RatingObservation> CreateDenseRatings(int userCount, int movieCount)
    {
        var users = Enumerable.Range(0, userCount)
            .Select(_ => Guid.NewGuid()).ToArray();

        var movies = Enumerable.Range(0, movieCount)
            .Select(_ => Guid.NewGuid()).ToArray();

        return users.SelectMany(user => movies.Select(movie =>
            new RatingObservation(user, movie, 4f))).ToList();
    }

    // Verifies that every original observation is preserved exactly once.
    private static void AssertPreservedObservations(
        IReadOnlyList<RatingObservation> original, DatasetSplit split)
    {
        var combined = split.Training
            .Concat(split.Validation).Concat(split.Testing).ToList();

        Assert.Equal(original.Count, combined.Count);
        Assert.True(original.ToHashSet().SetEquals(combined));
        Assert.Equal(original.Count, combined.Distinct().Count());
    }

    // Verifies that a rare observation appears exclusively in training.
    private void AssertTrainingOnly(RatingObservation observation)
    {
        var ratings = _ratings.ToList();
        ratings.Add(observation);

        var split = DatasetSplitter.Split(ratings);

        Assert.Contains(observation, split.Training);
        Assert.DoesNotContain(observation, split.Validation);
        Assert.DoesNotContain(observation, split.Testing);
    }

    // Functional tests

    // Verifies that the default ratios produce the correct dataset sizes.
    [Fact]
    public void SplitDefaultRatiosReturnsCorrectDatasetSizes()
    {
        var split = DatasetSplitter.Split(_ratings);

        Assert.Equal(2400, split.Training.Count);
        Assert.Equal(300, split.Validation.Count);
        Assert.Equal(300, split.Testing.Count);
    }

    // Verifies that custom ratios produce the expected dataset sizes.
    [Theory]
    [InlineData(0.6, 0.2, 1800, 600, 600)]
    [InlineData(0.5, 0.25, 1500, 750, 750)]
    [InlineData(0.9, 0.05, 2700, 150, 150)]
    public void SplitCustomRatiosReturnsCorrectDatasetSizes(
        double trainingRatio, double validationRatio,
        int expectedTraining, int expectedValidation, int expectedTesting)
    {
        var split = Split(trainingRatio, validationRatio);

        Assert.Equal(expectedTraining, split.Training.Count);
        Assert.Equal(expectedValidation, split.Validation.Count);
        Assert.Equal(expectedTesting, split.Testing.Count);
    }

    // Verifies that observations do not overlap between datasets.
    [Fact]
    public void SplitValidRatingsReturnsDisjointDatasets()
    {
        var split = Split();

        Assert.Empty(split.Training.Intersect(split.Validation));
        Assert.Empty(split.Training.Intersect(split.Testing));
        Assert.Empty(split.Validation.Intersect(split.Testing));
    }

    // Verifies that user-media pairs do not overlap between datasets.
    [Fact]
    public void SplitValidRatingsReturnsDisjointUserMediaPairs()
    {
        var split = Split();

        var training = GetUserMediaPairs(split.Training);
        var validation = GetUserMediaPairs(split.Validation);
        var testing = GetUserMediaPairs(split.Testing);

        Assert.Empty(training.Intersect(validation));
        Assert.Empty(training.Intersect(testing));
        Assert.Empty(validation.Intersect(testing));
    }

    // Verifies that splitting preserves every original observation.
    [Fact]
    public void SplitValidRatingsPreservesAllObservations()
    {
        AssertPreservedObservations(_ratings, Split());
    }

    // Verifies that splitting does not modify the input dataset.
    [Fact]
    public void SplitValidRatingsDoesNotModifyInput()
    {
        var original = _ratings.ToArray();

        Split();

        Assert.Equal(original, _ratings.ToArray());
    }

    // Reproducibility tests

    // Verifies that identical seeds produce identical datasets.
    [Fact]
    public void SplitSameSeedReturnsIdenticalDatasets()
    {
        var first = Split(seed: 42);
        var second = Split(seed: 42);

        Assert.Equal(first.Training.ToArray(), second.Training.ToArray());
        Assert.Equal(first.Validation.ToArray(), second.Validation.ToArray());
        Assert.Equal(first.Testing.ToArray(), second.Testing.ToArray());
    }

    // Verifies that different seeds produce different training datasets.
    [Fact]
    public void SplitDifferentSeedsReturnsDifferentDatasets()
    {
        var first = Split(seed: 42);
        var second = Split(seed: 100);

        Assert.False(first.Training.ToHashSet().SetEquals(second.Training));
    }

    // Training coverage tests

    // Verifies that every user appears in the training dataset.
    [Fact]
    public void SplitValidRatingsGuaranteesUserCoverage()
    {
        var knownUsers = Split().Training.Select(r => r.UserId).ToHashSet();

        Assert.All(_ratings, rating => Assert.Contains(rating.UserId, knownUsers));
    }

    // Verifies that every media item appears in the training dataset.
    [Fact]
    public void SplitValidRatingsGuaranteesMediaCoverage()
    {
        var knownMedia = Split().Training.Select(r => r.MediaId).ToHashSet();

        Assert.All(_ratings, rating => Assert.Contains(rating.MediaId, knownMedia));
    }

    // Verifies that the only observation for a rare user is included in training.
    [Fact]
    public void SplitRareUserIncludesObservationInTraining()
    {
        AssertTrainingOnly(new RatingObservation(
            Guid.NewGuid(), _ratings[0].MediaId, 5f));
    }

    // Verifies that the only observation for rare media is included in training.
    [Fact]
    public void SplitRareMediaIncludesObservationInTraining()
    {
        AssertTrainingOnly(new RatingObservation(
            _ratings[0].UserId, Guid.NewGuid(), 5f));
    }

    // Verifies that an observation with a rare user and media is included in training.
    [Fact]
    public void SplitRareUserAndMediaIncludesObservationInTraining()
    {
        AssertTrainingOnly(new RatingObservation(
            Guid.NewGuid(), Guid.NewGuid(), 5f));
    }

    // Boundary tests

    // Verifies that uneven dataset sizes preserve every observation.
    [Fact]
    public void SplitUnevenDatasetSizePreservesAllObservations()
    {
        var ratings = _ratings.Take(2999).ToList();
        var split = DatasetSplitter.Split(ratings);

        Assert.Equal(2399, split.Training.Count);
        Assert.Equal(299, split.Validation.Count);
        Assert.Equal(301, split.Testing.Count);

        AssertPreservedObservations(ratings, split);
    }

    // Verifies that the minimum default dataset produces nonempty partitions.
    [Fact]
    public void SplitMinimumDefaultDatasetReturnsNonEmptyDatasets()
    {
        var ratings = CreateDenseRatings(5, 2);
        var split = DatasetSplitter.Split(ratings);

        Assert.Equal(8, split.Training.Count);
        Assert.Single(split.Validation);
        Assert.Single(split.Testing);

        AssertPreservedObservations(ratings, split);
    }

    // Verifies that exact training capacity still produces a valid split.
    [Fact]
    public void SplitExactTrainingCoverageReturnsValidSplit()
    {
        Guid movieA = Guid.NewGuid();
        Guid movieB = Guid.NewGuid();

        var users = Enumerable.Range(0, 8)
            .Select(_ => Guid.NewGuid()).ToArray();

        var ratings = users.Select((user, index) =>
            new RatingObservation(user, index < 4 ? movieA : movieB, 4f)).ToList();

        ratings.Add(new RatingObservation(users[0], movieB, 3f));
        ratings.Add(new RatingObservation(users[4], movieA, 5f));

        var split = DatasetSplitter.Split(ratings);

        Assert.Equal(8, split.Training.Count);
        Assert.Single(split.Validation);
        Assert.Single(split.Testing);
        Assert.Equal(8, split.Training.Select(r => r.UserId).Distinct().Count());

        AssertPreservedObservations(ratings, split);
    }

    // Input validation

    // Verifies that null datasets are rejected.
    [Fact]
    public void SplitNullRatingsThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => DatasetSplitter.Split(null!));
    }

    // Verifies that empty datasets are rejected.
    [Fact]
    public void SplitEmptyRatingsThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => DatasetSplitter.Split([]));
    }

    // Verifies that insufficient observations are rejected.
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(9)]
    public void SplitInsufficientRatingsThrowsArgumentException(int count)
    {
        Assert.Throws<ArgumentException>(() =>
            DatasetSplitter.Split(_ratings.Take(count).ToList()));
    }

    // Verifies that insufficient training capacity is rejected.
    [Fact]
    public void SplitInsufficientTrainingCapacityThrowsInvalidOperationException()
    {
        var ratings = Enumerable.Range(0, 10).Select(_ =>
            new RatingObservation(Guid.NewGuid(), Guid.NewGuid(), 4f)).ToList();

        Assert.Throws<InvalidOperationException>(() => DatasetSplitter.Split(ratings));
    }

    // Verifies that invalid, nonfinite and incompatible split ratios are rejected.
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
    public void SplitInvalidRatiosThrowsArgumentException(
        double trainingRatio, double validationRatio)
    {
        Assert.Throws<ArgumentException>(() => Split(trainingRatio, validationRatio));
    }

    // Verifies that duplicate user-media pairs are rejected regardless of rating.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SplitDuplicateUserMediaPairsThrowsArgumentException(bool identicalRating)
    {
        Guid userId = Guid.NewGuid();
        Guid mediaId = Guid.NewGuid();

        var ratings = new List<RatingObservation>
        {
            new(userId, mediaId, 4f),
            new(userId, mediaId, identicalRating ? 4f : 5f)
        };

        Assert.Equal(ratings[0].UserId, ratings[1].UserId);
        Assert.Equal(ratings[0].MediaId, ratings[1].MediaId);
        Assert.Single(GetUserMediaPairs(ratings));

        var exception = Assert.Throws<ArgumentException>(() =>
            DatasetSplitter.Split(ratings));

        Assert.Contains("Duplicate", exception.Message);
    }
}