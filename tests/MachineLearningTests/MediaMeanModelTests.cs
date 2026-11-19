
using System;
using System.Collections.Generic;
using System.Linq;
using Palette.Api.MachineLearning;
using Palette.Api.MachineLearning.Baselines;
using Xunit;

public class MediaMeanModelTests
{
    private readonly MediaMeanModel _model = new();
    private readonly Guid _mediaId = Guid.NewGuid();

    // Creates rating observations for the specified media.
    private static IReadOnlyList<RatingObservation> CreateMediaRatings(
        Guid mediaId, params float[] values)
    {
        return values.Select(value =>
            new RatingObservation(Guid.NewGuid(), mediaId, value)).ToList();
    }

    // Trains the model using ratings for the default media.
    private void Train(params float[] values)
    {
        _model.Train(CreateMediaRatings(_mediaId, values));
    }

    // Predicts a rating for the default or specified media.
    private float Predict(Guid? mediaId = null)
    {
        return _model.Predict(Guid.NewGuid(), mediaId ?? _mediaId);
    }

    // Mathematical correctness

    // Verifies that a single rating produces the same prediction.
    [Fact]
    public void PredictSingleRatingReturnsSameRating()
    {
        Train(4f);

        Assert.Equal(4f, Predict());
    }

    // Verifies that multiple ratings produce the correct mean.
    [Fact]
    public void PredictMultipleRatingsReturnsCorrectMean()
    {
        Train(5f, 4f, 2f, 3f, 4f);

        Assert.Equal(3.6f, Predict(), 5);
    }

    // Verifies that fractional ratings produce the correct mean.
    [Fact]
    public void PredictFractionalRatingsReturnsCorrectMean()
    {
        Train(2.5f, 3.5f, 4.5f);

        Assert.Equal(3.5f, Predict(), 5);
    }

    // Verifies that multiple media maintain individual rating means.
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

        Assert.Equal(14f / 3f, _model.Predict(userId, mediaA), 5);
        Assert.Equal(1.5f, _model.Predict(userId, mediaB), 5);
        Assert.Equal(3f, _model.Predict(userId, mediaC), 5);
    }

    // Verifies that media rated by the same user maintain individual means.
    [Fact]
    public void PredictMediaRatedBySameUserReturnsIndividualMeans()
    {
        Guid userId = Guid.NewGuid();
        Guid mediaA = Guid.NewGuid();
        Guid mediaB = Guid.NewGuid();

        _model.Train(
        [
            new RatingObservation(userId, mediaA, 5f),
            new RatingObservation(userId, mediaB, 1f)
        ]);

        Assert.Equal(5f, _model.Predict(userId, mediaA));
        Assert.Equal(1f, _model.Predict(userId, mediaB));
    }

    // Boundary values

    // Verifies that identical ratings produce the same prediction.
    [Theory]
    [InlineData(1f)]
    [InlineData(3f)]
    [InlineData(5f)]
    public void PredictIdenticalRatingsReturnsSameRating(float rating)
    {
        Train(rating, rating, rating);

        Assert.Equal(rating, Predict());
    }

    // Verifies that minimum and maximum ratings produce their midpoint.
    [Fact]
    public void PredictMinimumAndMaximumRatingsReturnsMidpoint()
    {
        Train(1f, 5f);

        Assert.Equal(3f, Predict());
    }

    // Prediction consistency

    // Verifies that different users receive identical predictions for the same media.
    [Fact]
    public void PredictSameMediaDifferentUsersReturnsSamePrediction()
    {
        Train(2f, 4f);

        float first = Predict();
        float second = Predict();

        Assert.Equal(3f, first);
        Assert.Equal(first, second);
    }

    // Verifies that known and unknown users receive identical predictions.
    [Fact]
    public void PredictKnownAndUnknownUsersReturnsSamePrediction()
    {
        var ratings = CreateMediaRatings(_mediaId, 2f, 4f);
        _model.Train(ratings);

        float known = _model.Predict(ratings[0].UserId, _mediaId);

        Assert.Equal(3f, known);
        Assert.Equal(known, Predict());
    }

    // Verifies that 100 repeated predictions produce identical results.
    [Fact]
    public void PredictRepeatedCallsReturnsSamePrediction()
    {
        Train(1f, 3f, 5f);

        Guid userId = Guid.NewGuid();
        float expected = _model.Predict(userId, _mediaId);

        for (int i = 0; i < 100; i++)
            Assert.Equal(expected, _model.Predict(userId, _mediaId));
    }

    // Cold start behaviour

    // Verifies that unknown media receive the global mean.
    [Fact]
    public void PredictUnknownMediaReturnsGlobalMean()
    {
        var ratings = CreateMediaRatings(Guid.NewGuid(), 5f, 4f)
            .Concat(CreateMediaRatings(Guid.NewGuid(), 1f, 2f)).ToList();

        _model.Train(ratings);

        Assert.Equal(3f, Predict(Guid.NewGuid()));
    }

    // Verifies that the global mean is weighted by individual observations.
    [Fact]
    public void PredictUnknownMediaUsesObservationWeightedGlobalMean()
    {
        var ratings = CreateMediaRatings(Guid.NewGuid(), 5f, 5f, 5f)
            .Concat(CreateMediaRatings(Guid.NewGuid(), 1f)).ToList();

        _model.Train(ratings);

        Assert.Equal(4f, Predict(Guid.NewGuid()));
    }

    // Verifies that different unknown media receive identical global means.
    [Fact]
    public void PredictUnknownMediaItemsReturnsSameGlobalMean()
    {
        Train(2f, 4f);

        Guid userId = Guid.NewGuid();
        float first = _model.Predict(userId, Guid.NewGuid());
        float second = _model.Predict(userId, Guid.NewGuid());

        Assert.Equal(3f, first);
        Assert.Equal(first, second);
    }

    // Verifies that known users receive the global mean for unknown media.
    [Fact]
    public void PredictKnownUserUnknownMediaReturnsGlobalMean()
    {
        var ratings = CreateMediaRatings(_mediaId, 1f, 5f);
        _model.Train(ratings);

        Assert.Equal(3f, _model.Predict(ratings[0].UserId, Guid.NewGuid()));
    }

    // Input validation

    // Verifies that null training datasets are rejected.
    [Fact]
    public void TrainNullRatingsThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _model.Train(null!));
    }

    // Verifies that empty training datasets are rejected.
    [Fact]
    public void TrainEmptyRatingsThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => _model.Train([]));
    }

    // Verifies that out-of-range and nonfinite ratings are rejected.
    [Theory]
    [InlineData(-1f)]
    [InlineData(0f)]
    [InlineData(0.99f)]
    [InlineData(5.01f)]
    [InlineData(6f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    public void TrainInvalidRatingThrowsArgumentException(float invalidRating)
    {
        Assert.Throws<ArgumentException>(() => Train(3f, invalidRating, 4f));
    }

    // Model state

    // Verifies that predictions cannot be made before training.
    [Fact]
    public void PredictBeforeTrainingThrowsInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(() => Predict());
    }

    // Verifies that unsuccessful training leaves the model untrained.
    [Fact]
    public void TrainInvalidDatasetLeavesModelUntrained()
    {
        Assert.Throws<ArgumentException>(() => Train(4f, float.NaN));
        Assert.Throws<InvalidOperationException>(() => Predict());
    }

    // Retraining

    // Verifies that retraining replaces the previous media mean.
    [Fact]
    public void TrainNewRatingsReplacesPreviousMediaMean()
    {
        Train(1f, 2f, 3f);
        Assert.Equal(2f, Predict());

        Train(4f, 5f);
        Assert.Equal(4.5f, Predict());
    }

    // Verifies that retraining removes media from previous datasets.
    [Fact]
    public void TrainNewDatasetRemovesPreviousMedia()
    {
        Guid newMedia = Guid.NewGuid();

        Train(1f);
        Assert.Equal(1f, Predict());

        _model.Train(CreateMediaRatings(newMedia, 5f));

        Assert.Equal(5f, Predict());
        Assert.Equal(5f, Predict(newMedia));
    }

    // Verifies that retraining updates the global mean fallback.
    [Fact]
    public void TrainNewDatasetUpdatesGlobalMeanFallback()
    {
        Guid unknownMedia = Guid.NewGuid();

        _model.Train(CreateMediaRatings(Guid.NewGuid(), 1f, 3f));
        Assert.Equal(2f, Predict(unknownMedia));

        _model.Train(CreateMediaRatings(Guid.NewGuid(), 4f, 5f));
        Assert.Equal(4.5f, Predict(unknownMedia));
    }

    // Verifies that training on the same dataset preserves predictions.
    [Fact]
    public void TrainSameDatasetProducesSamePredictions()
    {
        var ratings = CreateMediaRatings(_mediaId, 1f, 3f, 5f);

        _model.Train(ratings);
        float first = Predict();

        _model.Train(ratings);

        Assert.Equal(first, Predict());
    }

    // State preservation

    // Verifies that unsuccessful retraining preserves the previous media mean.
    [Fact]
    public void TrainInvalidDatasetPreservesPreviousMediaMean()
    {
        Train(2f, 4f);

        Assert.Throws<ArgumentException>(() => Train(5f, float.NaN));
        Assert.Equal(3f, Predict());
    }

    // Verifies that unsuccessful retraining preserves the global mean fallback.
    [Fact]
    public void TrainInvalidDatasetPreservesGlobalMeanFallback()
    {
        Train(2f, 4f);

        var invalidRatings = CreateMediaRatings(Guid.NewGuid(), 5f, float.NaN);

        Assert.Throws<ArgumentException>(() => _model.Train(invalidRatings));
        Assert.Equal(3f, Predict(Guid.NewGuid()));
    }

    // Data integrity

    // Verifies that training does not modify the input ratings.
    [Fact]
    public void TrainValidRatingsDoesNotModifyInput()
    {
        var ratings = CreateMediaRatings(Guid.NewGuid(), 5f, 2f)
            .Concat(CreateMediaRatings(Guid.NewGuid(), 4f, 1f)).ToList();

        var original = ratings.ToArray();

        _model.Train(ratings);

        Assert.Equal(original, ratings.ToArray());
    }

    // Verifies that training data order does not influence predictions.
    [Fact]
    public void TrainDifferentRatingOrderReturnsSamePredictions()
    {
        Guid mediaA = Guid.NewGuid();
        Guid mediaB = Guid.NewGuid();

        var ratings = CreateMediaRatings(mediaA, 5f, 2f)
            .Concat(CreateMediaRatings(mediaB, 4f, 1f)).ToList();

        _model.Train(ratings);

        float firstA = Predict(mediaA);
        float firstB = Predict(mediaB);

        _model.Train(ratings.AsEnumerable().Reverse().ToArray());

        Assert.Equal(firstA, Predict(mediaA), 5);
        Assert.Equal(firstB, Predict(mediaB), 5);
    }

    // Scalability and numerical correctness

    // Verifies that 10,000 ratings produce correct media and global means.
    [Fact]
    public void TrainLargeDatasetReturnsCorrectMeans()
    {
        Guid mediaA = Guid.NewGuid();
        Guid mediaB = Guid.NewGuid();

        var ratings = CreateMediaRatings(mediaA, Enumerable.Repeat(5f, 5000).ToArray())
            .Concat(CreateMediaRatings(mediaB, Enumerable.Repeat(1f, 5000).ToArray()))
            .ToList();

        _model.Train(ratings);

        Assert.Equal(5f, Predict(mediaA));
        Assert.Equal(1f, Predict(mediaB));
        Assert.Equal(3f, Predict(Guid.NewGuid()));
    }

    // Verifies that 1,000 media maintain their correct individual means.
    [Fact]
    public void TrainManyMediaReturnsCorrectIndividualMeans()
    {
        var ratings = new List<RatingObservation>();
        var expectedMeans = new Dictionary<Guid, float>();

        for (int i = 0; i < 1000; i++)
        {
            Guid mediaId = Guid.NewGuid();
            float rating = (i % 5) + 1f;

            ratings.AddRange(CreateMediaRatings(mediaId, rating, rating));
            expectedMeans[mediaId] = rating;
        }

        _model.Train(ratings);

        foreach (var entry in expectedMeans)
            Assert.Equal(entry.Value, Predict(entry.Key));
    }

    // Recommendation ranking

    // Verifies that higher-rated media receive higher predictions.
    [Fact]
    public void PredictHigherRatedMediaReceivesHigherPrediction()
    {
        Guid mediaA = Guid.NewGuid();
        Guid mediaB = Guid.NewGuid();

        var ratings = CreateMediaRatings(mediaA, 5f, 4f, 5f)
            .Concat(CreateMediaRatings(mediaB, 2f, 1f, 3f)).ToList();

        _model.Train(ratings);

        Guid userId = Guid.NewGuid();

        Assert.True(_model.Predict(userId, mediaA) >
            _model.Predict(userId, mediaB));
    }

    // Verifies that equal media means produce identical predictions.
    [Fact]
    public void PredictEqualMediaMeansReturnsEqualPredictions()
    {
        Guid mediaA = Guid.NewGuid();
        Guid mediaB = Guid.NewGuid();

        var ratings = CreateMediaRatings(mediaA, 1f, 5f)
            .Concat(CreateMediaRatings(mediaB, 3f, 3f, 3f)).ToList();

        _model.Train(ratings);

        Guid userId = Guid.NewGuid();

        Assert.Equal(3f, _model.Predict(userId, mediaA));
        Assert.Equal(3f, _model.Predict(userId, mediaB));
    }

    // Verifies that higher means rank higher regardless of rating count.
    [Fact]
    public void PredictHigherMeanWithFewerRatingsRanksHigher()
    {
        Guid mediaA = Guid.NewGuid();
        Guid mediaB = Guid.NewGuid();

        var ratings = CreateMediaRatings(mediaA, 5f)
            .Concat(CreateMediaRatings(mediaB, Enumerable.Repeat(4f, 100).ToArray()))
            .ToList();

        _model.Train(ratings);

        Guid userId = Guid.NewGuid();

        Assert.True(_model.Predict(userId, mediaA) >
            _model.Predict(userId, mediaB));
    }
}