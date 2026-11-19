using System;
using System.Collections.Generic;
using System.Linq;
using Palette.Api.MachineLearning;
using Palette.Api.MachineLearning.Baselines;
using Xunit;

public class MediaMeanModelTests
{
    private readonly MediaMeanModel _model = new();

    // Mathematical correctness

    [Fact]
    public void PredictSingleRatingReturnsSameRating()
    {
        Guid mediaId = Guid.NewGuid();

        _model.Train(CreateMediaRatings(mediaId, 4f));

        Assert.Equal(4f,
            _model.Predict(Guid.NewGuid(), mediaId));
    }

    [Fact]
    public void PredictMultipleRatingsReturnsCorrectMean()
    {
        Guid mediaId = Guid.NewGuid();

        _model.Train(
            CreateMediaRatings(mediaId, 5f, 4f, 2f, 3f, 4f));

        Assert.Equal(3.6f,
            _model.Predict(Guid.NewGuid(), mediaId), 5);
    }

    [Fact]
    public void PredictFractionalRatingsReturnsCorrectMean()
    {
        Guid mediaId = Guid.NewGuid();

        _model.Train(
            CreateMediaRatings(mediaId, 2.5f, 3.5f, 4.5f));

        Assert.Equal(3.5f,
            _model.Predict(Guid.NewGuid(), mediaId), 5);
    }

    [Fact]
    public void PredictMultipleMediaReturnsIndividualMeans()
    {
        Guid mediaA = Guid.NewGuid();
        Guid mediaB = Guid.NewGuid();
        Guid mediaC = Guid.NewGuid();

        var ratings = CreateMediaRatings(mediaA, 5f, 4f, 5f)
            .Concat(CreateMediaRatings(mediaB, 1f, 2f))
            .Concat(CreateMediaRatings(mediaC, 3f, 4f, 2f))
            .ToList();

        _model.Train(ratings);

        Guid userId = Guid.NewGuid();

        Assert.Equal(14f / 3f,
            _model.Predict(userId, mediaA), 5);

        Assert.Equal(1.5f,
            _model.Predict(userId, mediaB), 5);

        Assert.Equal(3f,
            _model.Predict(userId, mediaC), 5);
    }

    [Fact]
    public void PredictMediaRatedBySameUserReturnsIndividualMeans()
    {
        Guid userId = Guid.NewGuid();
        Guid mediaA = Guid.NewGuid();
        Guid mediaB = Guid.NewGuid();

        var ratings = new List<RatingObservation>
        {
            new(userId, mediaA, 5f),
            new(userId, mediaB, 1f)
        };

        _model.Train(ratings);

        Assert.Equal(5f, _model.Predict(userId, mediaA));
        Assert.Equal(1f, _model.Predict(userId, mediaB));
    }

    // Boundary values

    [Theory]
    [InlineData(1f)]
    [InlineData(3f)]
    [InlineData(5f)]
    public void PredictIdenticalRatingsReturnsSameRating(float rating)
    {
        Guid mediaId = Guid.NewGuid();

        _model.Train(
            CreateMediaRatings(mediaId, rating, rating, rating));

        Assert.Equal(rating,
            _model.Predict(Guid.NewGuid(), mediaId));
    }

    [Fact]
    public void PredictMinimumAndMaximumRatingsReturnsMidpoint()
    {
        Guid mediaId = Guid.NewGuid();

        _model.Train(CreateMediaRatings(mediaId, 1f, 5f));

        Assert.Equal(3f,
            _model.Predict(Guid.NewGuid(), mediaId));
    }

    // Prediction consistency

    [Fact]
    public void PredictSameMediaDifferentUsersReturnsSamePrediction()
    {
        Guid mediaId = Guid.NewGuid();

        _model.Train(CreateMediaRatings(mediaId, 2f, 4f));

        float first = _model.Predict(Guid.NewGuid(), mediaId);
        float second = _model.Predict(Guid.NewGuid(), mediaId);

        Assert.Equal(3f, first);
        Assert.Equal(first, second);
    }

    [Fact]
    public void PredictKnownAndUnknownUsersReturnsSamePrediction()
    {
        Guid mediaId = Guid.NewGuid();

        var ratings = CreateMediaRatings(mediaId, 2f, 4f);

        _model.Train(ratings);

        float known = _model.Predict(
            ratings[0].UserId, mediaId);

        float unknown = _model.Predict(
            Guid.NewGuid(), mediaId);

        Assert.Equal(3f, known);
        Assert.Equal(known, unknown);
    }

    [Fact]
    public void PredictRepeatedCallsReturnsSamePrediction()
    {
        Guid userId = Guid.NewGuid();
        Guid mediaId = Guid.NewGuid();

        _model.Train(CreateMediaRatings(mediaId, 1f, 3f, 5f));

        float expected = _model.Predict(userId, mediaId);

        for (int i = 0; i < 100; i++)
        {
            Assert.Equal(expected,
                _model.Predict(userId, mediaId));
        }
    }

    // Cold start behaviour

    [Fact]
    public void PredictUnknownMediaReturnsGlobalMean()
    {
        var ratings = CreateMediaRatings(Guid.NewGuid(), 5f, 4f)
            .Concat(CreateMediaRatings(Guid.NewGuid(), 1f, 2f))
            .ToList();

        _model.Train(ratings);

        Assert.Equal(3f,
            _model.Predict(Guid.NewGuid(), Guid.NewGuid()));
    }

    [Fact]
    public void PredictUnknownMediaUsesObservationWeightedGlobalMean()
    {
        var ratings = CreateMediaRatings(Guid.NewGuid(), 5f, 5f, 5f)
            .Concat(CreateMediaRatings(Guid.NewGuid(), 1f))
            .ToList();

        _model.Train(ratings);

        Assert.Equal(4f,
            _model.Predict(Guid.NewGuid(), Guid.NewGuid()));
    }

    [Fact]
    public void PredictUnknownMediaItemsReturnsSameGlobalMean()
    {
        _model.Train(
            CreateMediaRatings(Guid.NewGuid(), 2f, 4f));

        Guid userId = Guid.NewGuid();

        float first = _model.Predict(userId, Guid.NewGuid());
        float second = _model.Predict(userId, Guid.NewGuid());

        Assert.Equal(3f, first);
        Assert.Equal(first, second);
    }

    [Fact]
    public void PredictKnownUserUnknownMediaReturnsGlobalMean()
    {
        Guid mediaId = Guid.NewGuid();

        var ratings = CreateMediaRatings(mediaId, 1f, 5f);

        _model.Train(ratings);

        Assert.Equal(3f,
            _model.Predict(
                ratings[0].UserId, Guid.NewGuid()));
    }

    // Input validation

    [Fact]
    public void TrainNullRatingsThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            _model.Train(null!));
    }

    [Fact]
    public void TrainEmptyRatingsThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            _model.Train(Array.Empty<RatingObservation>()));
    }

    [Theory]
    [InlineData(-1f)]
    [InlineData(0f)]
    [InlineData(0.99f)]
    [InlineData(5.01f)]
    [InlineData(6f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    public void TrainInvalidRatingThrowsArgumentException(
        float invalidRating)
    {
        var ratings = CreateMediaRatings(
            Guid.NewGuid(), 3f, invalidRating, 4f);

        Assert.Throws<ArgumentException>(() =>
            _model.Train(ratings));
    }

    // Model state

    [Fact]
    public void PredictBeforeTrainingThrowsInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(() =>
            _model.Predict(Guid.NewGuid(), Guid.NewGuid()));
    }

    [Fact]
    public void TrainInvalidDatasetLeavesModelUntrained()
    {
        Guid mediaId = Guid.NewGuid();

        var ratings = CreateMediaRatings(mediaId, 4f, float.NaN);

        Assert.Throws<ArgumentException>(() =>
            _model.Train(ratings));

        Assert.Throws<InvalidOperationException>(() =>
            _model.Predict(Guid.NewGuid(), mediaId));
    }

    // Retraining

    [Fact]
    public void TrainNewRatingsReplacesPreviousMediaMean()
    {
        Guid mediaId = Guid.NewGuid();

        _model.Train(CreateMediaRatings(mediaId, 1f, 2f, 3f));

        Assert.Equal(2f,
            _model.Predict(Guid.NewGuid(), mediaId));

        _model.Train(CreateMediaRatings(mediaId, 4f, 5f));

        Assert.Equal(4.5f,
            _model.Predict(Guid.NewGuid(), mediaId));
    }

    [Fact]
    public void TrainNewDatasetRemovesPreviousMedia()
    {
        Guid oldMedia = Guid.NewGuid();
        Guid newMedia = Guid.NewGuid();

        _model.Train(CreateMediaRatings(oldMedia, 1f));

        Assert.Equal(1f,
            _model.Predict(Guid.NewGuid(), oldMedia));

        _model.Train(CreateMediaRatings(newMedia, 5f));

        // Old media now receives the global mean.
        Assert.Equal(5f,
            _model.Predict(Guid.NewGuid(), oldMedia));

        Assert.Equal(5f,
            _model.Predict(Guid.NewGuid(), newMedia));
    }

    [Fact]
    public void TrainNewDatasetUpdatesGlobalMeanFallback()
    {
        Guid unknownMedia = Guid.NewGuid();

        _model.Train(
            CreateMediaRatings(Guid.NewGuid(), 1f, 3f));

        Assert.Equal(2f,
            _model.Predict(Guid.NewGuid(), unknownMedia));

        _model.Train(
            CreateMediaRatings(Guid.NewGuid(), 4f, 5f));

        Assert.Equal(4.5f,
            _model.Predict(Guid.NewGuid(), unknownMedia));
    }

    [Fact]
    public void TrainSameDatasetProducesSamePredictions()
    {
        Guid mediaId = Guid.NewGuid();

        var ratings = CreateMediaRatings(mediaId, 1f, 3f, 5f);

        _model.Train(ratings);

        float first = _model.Predict(Guid.NewGuid(), mediaId);

        _model.Train(ratings);

        float second = _model.Predict(Guid.NewGuid(), mediaId);

        Assert.Equal(first, second);
    }

    // State preservation

    [Fact]
    public void TrainInvalidDatasetPreservesPreviousMediaMean()
    {
        Guid mediaId = Guid.NewGuid();

        _model.Train(CreateMediaRatings(mediaId, 2f, 4f));

        var invalidRatings =
            CreateMediaRatings(mediaId, 5f, float.NaN);

        Assert.Throws<ArgumentException>(() =>
            _model.Train(invalidRatings));

        Assert.Equal(3f,
            _model.Predict(Guid.NewGuid(), mediaId));
    }

    [Fact]
    public void TrainInvalidDatasetPreservesGlobalMeanFallback()
    {
        _model.Train(
            CreateMediaRatings(Guid.NewGuid(), 2f, 4f));

        var invalidRatings =
            CreateMediaRatings(Guid.NewGuid(), 5f, float.NaN);

        Assert.Throws<ArgumentException>(() =>
            _model.Train(invalidRatings));

        Assert.Equal(3f,
            _model.Predict(Guid.NewGuid(), Guid.NewGuid()));
    }

    // Data integrity

    [Fact]
    public void TrainValidRatingsDoesNotModifyInput()
    {
        var ratings = CreateMediaRatings(Guid.NewGuid(), 5f, 2f)
            .Concat(CreateMediaRatings(Guid.NewGuid(), 4f, 1f))
            .ToList();

        var original = ratings.ToArray();

        _model.Train(ratings);

        Assert.Equal(original, ratings.ToArray());
    }

    [Fact]
    public void TrainDifferentRatingOrderReturnsSamePredictions()
    {
        Guid mediaA = Guid.NewGuid();
        Guid mediaB = Guid.NewGuid();

        var ratings = CreateMediaRatings(mediaA, 5f, 2f)
            .Concat(CreateMediaRatings(mediaB, 4f, 1f))
            .ToList();

        _model.Train(ratings);

        float firstA = _model.Predict(Guid.NewGuid(), mediaA);
        float firstB = _model.Predict(Guid.NewGuid(), mediaB);

        _model.Train(ratings.AsEnumerable().Reverse().ToArray());

        Assert.Equal(firstA,
            _model.Predict(Guid.NewGuid(), mediaA), 5);

        Assert.Equal(firstB,
            _model.Predict(Guid.NewGuid(), mediaB), 5);
    }

    // Scalability and numerical correctness

    [Fact]
    public void TrainLargeDatasetReturnsCorrectMeans()
    {
        Guid mediaA = Guid.NewGuid();
        Guid mediaB = Guid.NewGuid();

        var ratings = CreateMediaRatings(
                mediaA, Enumerable.Repeat(5f, 5000).ToArray())
            .Concat(CreateMediaRatings(
                mediaB, Enumerable.Repeat(1f, 5000).ToArray()))
            .ToList();

        _model.Train(ratings);

        Assert.Equal(5f,
            _model.Predict(Guid.NewGuid(), mediaA));

        Assert.Equal(1f,
            _model.Predict(Guid.NewGuid(), mediaB));

        Assert.Equal(3f,
            _model.Predict(Guid.NewGuid(), Guid.NewGuid()));
    }

    [Fact]
    public void TrainManyMediaReturnsCorrectIndividualMeans()
    {
        var ratings = new List<RatingObservation>();
        var expectedMeans = new Dictionary<Guid, float>();

        for (int i = 0; i < 1000; i++)
        {
            Guid mediaId = Guid.NewGuid();
            float rating = (i % 5) + 1f;

            ratings.AddRange(
                CreateMediaRatings(mediaId, rating, rating));

            expectedMeans[mediaId] = rating;
        }

        _model.Train(ratings);

        foreach (var entry in expectedMeans)
        {
            Assert.Equal(entry.Value,
                _model.Predict(Guid.NewGuid(), entry.Key));
        }
    }

    // Recommendation ranking

    [Fact]
    public void PredictHigherRatedMediaReceivesHigherPrediction()
    {
        Guid mediaA = Guid.NewGuid();
        Guid mediaB = Guid.NewGuid();

        var ratings = CreateMediaRatings(mediaA, 5f, 4f, 5f)
            .Concat(CreateMediaRatings(mediaB, 2f, 1f, 3f))
            .ToList();

        _model.Train(ratings);

        Guid userId = Guid.NewGuid();

        Assert.True(
            _model.Predict(userId, mediaA) >
            _model.Predict(userId, mediaB));
    }

    [Fact]
    public void PredictEqualMediaMeansReturnsEqualPredictions()
    {
        Guid mediaA = Guid.NewGuid();
        Guid mediaB = Guid.NewGuid();

        var ratings = CreateMediaRatings(mediaA, 1f, 5f)
            .Concat(CreateMediaRatings(mediaB, 3f, 3f, 3f))
            .ToList();

        _model.Train(ratings);

        Guid userId = Guid.NewGuid();

        Assert.Equal(3f, _model.Predict(userId, mediaA));
        Assert.Equal(3f, _model.Predict(userId, mediaB));
    }

    [Fact]
    public void PredictHigherMeanWithFewerRatingsRanksHigher()
    {
        Guid mediaA = Guid.NewGuid();
        Guid mediaB = Guid.NewGuid();

        var ratings = CreateMediaRatings(mediaA, 5f)
            .Concat(CreateMediaRatings(
                mediaB, Enumerable.Repeat(4f, 100).ToArray()))
            .ToList();

        _model.Train(ratings);

        Guid userId = Guid.NewGuid();

        Assert.True(
            _model.Predict(userId, mediaA) >
            _model.Predict(userId, mediaB));
    }

    // Helper method

    private static IReadOnlyList<RatingObservation> CreateMediaRatings(
        Guid mediaId,
        params float[] values)
    {
        return values.Select(value =>
            new RatingObservation(
                Guid.NewGuid(),
                mediaId,
                value))
            .ToList();
    }
}