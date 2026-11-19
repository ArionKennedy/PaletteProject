
using System;
using System.Collections.Generic;
using System.Linq;
using Palette.Api.MachineLearning;
using Palette.Api.MachineLearning.Baselines;
using Xunit;

public class UserMeanModelTests
{
    private readonly UserMeanModel _model = new();
    private readonly Guid _userId = Guid.NewGuid();

    // Creates rating observations for the specified user.
    private static IReadOnlyList<RatingObservation> CreateUserRatings(
        Guid userId, params float[] values)
    {
        return values.Select(value =>
            new RatingObservation(userId, Guid.NewGuid(), value)).ToList();
    }

    // Trains the model using ratings from the default user.
    private void Train(params float[] values)
    {
        _model.Train(CreateUserRatings(_userId, values));
    }

    // Predicts a rating for the default or specified user.
    private float Predict(Guid? userId = null)
    {
        return _model.Predict(userId ?? _userId, Guid.NewGuid());
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

    // Verifies that multiple users maintain individual rating means.
    [Fact]
    public void PredictMultipleUsersReturnsIndividualMeans()
    {
        Guid userA = Guid.NewGuid();
        Guid userB = Guid.NewGuid();
        Guid userC = Guid.NewGuid();

        var ratings = CreateUserRatings(userA, 5f, 4f, 5f)
            .Concat(CreateUserRatings(userB, 1f, 2f))
            .Concat(CreateUserRatings(userC, 3f, 4f, 2f))
            .ToList();

        _model.Train(ratings);

        Guid mediaId = Guid.NewGuid();

        Assert.Equal(14f / 3f, _model.Predict(userA, mediaId), 5);
        Assert.Equal(1.5f, _model.Predict(userB, mediaId), 5);
        Assert.Equal(3f, _model.Predict(userC, mediaId), 5);
    }

    // Verifies that users rating the same media maintain individual means.
    [Fact]
    public void PredictUsersRatingSameMediaReturnsIndividualMeans()
    {
        Guid userA = Guid.NewGuid();
        Guid userB = Guid.NewGuid();
        Guid mediaId = Guid.NewGuid();

        _model.Train(
        [
            new RatingObservation(userA, mediaId, 5f),
            new RatingObservation(userB, mediaId, 1f)
        ]);

        Assert.Equal(5f, _model.Predict(userA, mediaId));
        Assert.Equal(1f, _model.Predict(userB, mediaId));
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

    // Verifies that different media produce the same prediction for one user.
    [Fact]
    public void PredictSameUserDifferentMediaReturnsSamePrediction()
    {
        Train(2f, 4f);

        float first = Predict();
        float second = Predict();

        Assert.Equal(3f, first);
        Assert.Equal(first, second);
    }

    // Verifies that known and unknown media produce identical predictions.
    [Fact]
    public void PredictKnownAndUnknownMediaReturnsSamePrediction()
    {
        var ratings = CreateUserRatings(_userId, 2f, 4f);
        _model.Train(ratings);

        float known = _model.Predict(_userId, ratings[0].MediaId);

        Assert.Equal(3f, known);
        Assert.Equal(known, Predict());
    }

    // Verifies that 100 repeated predictions produce identical results.
    [Fact]
    public void PredictRepeatedCallsReturnsSamePrediction()
    {
        Train(1f, 3f, 5f);

        Guid mediaId = Guid.NewGuid();
        float expected = _model.Predict(_userId, mediaId);

        for (int i = 0; i < 100; i++)
            Assert.Equal(expected, _model.Predict(_userId, mediaId));
    }

    // Cold start behaviour

    // Verifies that unknown users receive the global mean.
    [Fact]
    public void PredictUnknownUserReturnsGlobalMean()
    {
        var ratings = CreateUserRatings(Guid.NewGuid(), 5f, 4f)
            .Concat(CreateUserRatings(Guid.NewGuid(), 1f, 2f)).ToList();

        _model.Train(ratings);

        Assert.Equal(3f, Predict(Guid.NewGuid()));
    }

    // Verifies that the global mean is weighted by individual observations.
    [Fact]
    public void PredictUnknownUserUsesObservationWeightedGlobalMean()
    {
        var ratings = CreateUserRatings(Guid.NewGuid(), 5f, 5f, 5f)
            .Concat(CreateUserRatings(Guid.NewGuid(), 1f)).ToList();

        _model.Train(ratings);

        Assert.Equal(4f, Predict(Guid.NewGuid()));
    }

    // Verifies that different unknown users receive identical global means.
    [Fact]
    public void PredictUnknownUsersReturnsSameGlobalMean()
    {
        Train(2f, 4f);

        Guid mediaId = Guid.NewGuid();
        float first = _model.Predict(Guid.NewGuid(), mediaId);
        float second = _model.Predict(Guid.NewGuid(), mediaId);

        Assert.Equal(3f, first);
        Assert.Equal(first, second);
    }

    // Verifies that unknown users receive the global mean for known media.
    [Fact]
    public void PredictUnknownUserKnownMediaReturnsGlobalMean()
    {
        var ratings = CreateUserRatings(_userId, 1f, 5f);
        _model.Train(ratings);

        Assert.Equal(3f, _model.Predict(Guid.NewGuid(), ratings[0].MediaId));
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

    // Verifies that retraining replaces the previous user mean.
    [Fact]
    public void TrainNewRatingsReplacesPreviousUserMean()
    {
        Train(1f, 2f, 3f);
        Assert.Equal(2f, Predict());

        Train(4f, 5f);
        Assert.Equal(4.5f, Predict());
    }

    // Verifies that retraining removes users from previous datasets.
    [Fact]
    public void TrainNewDatasetRemovesPreviousUsers()
    {
        Guid newUser = Guid.NewGuid();

        Train(1f);
        Assert.Equal(1f, Predict());

        _model.Train(CreateUserRatings(newUser, 5f));

        Assert.Equal(5f, Predict());
        Assert.Equal(5f, Predict(newUser));
    }

    // Verifies that retraining updates the global mean fallback.
    [Fact]
    public void TrainNewDatasetUpdatesGlobalMeanFallback()
    {
        Guid unknownUser = Guid.NewGuid();

        _model.Train(CreateUserRatings(Guid.NewGuid(), 1f, 3f));
        Assert.Equal(2f, Predict(unknownUser));

        _model.Train(CreateUserRatings(Guid.NewGuid(), 4f, 5f));
        Assert.Equal(4.5f, Predict(unknownUser));
    }

    // Verifies that training on the same dataset preserves predictions.
    [Fact]
    public void TrainSameDatasetProducesSamePredictions()
    {
        var ratings = CreateUserRatings(_userId, 1f, 3f, 5f);

        _model.Train(ratings);
        float first = Predict();

        _model.Train(ratings);

        Assert.Equal(first, Predict());
    }

    // State preservation

    // Verifies that unsuccessful retraining preserves the previous user mean.
    [Fact]
    public void TrainInvalidDatasetPreservesPreviousUserMean()
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

        var invalidRatings = CreateUserRatings(Guid.NewGuid(), 5f, float.NaN);

        Assert.Throws<ArgumentException>(() => _model.Train(invalidRatings));
        Assert.Equal(3f, Predict(Guid.NewGuid()));
    }

    // Data integrity

    // Verifies that training does not modify the input ratings.
    [Fact]
    public void TrainValidRatingsDoesNotModifyInput()
    {
        var ratings = CreateUserRatings(Guid.NewGuid(), 5f, 2f)
            .Concat(CreateUserRatings(Guid.NewGuid(), 4f, 1f)).ToList();

        var original = ratings.ToArray();

        _model.Train(ratings);

        Assert.Equal(original, ratings.ToArray());
    }

    // Verifies that training data order does not influence predictions.
    [Fact]
    public void TrainDifferentRatingOrderReturnsSamePredictions()
    {
        Guid userA = Guid.NewGuid();
        Guid userB = Guid.NewGuid();

        var ratings = CreateUserRatings(userA, 5f, 2f)
            .Concat(CreateUserRatings(userB, 4f, 1f)).ToList();

        _model.Train(ratings);

        float firstA = Predict(userA);
        float firstB = Predict(userB);

        _model.Train(ratings.AsEnumerable().Reverse().ToArray());

        Assert.Equal(firstA, Predict(userA), 5);
        Assert.Equal(firstB, Predict(userB), 5);
    }

    // Scalability and numerical correctness

    // Verifies that 10,000 ratings produce correct user and global means.
    [Fact]
    public void TrainLargeDatasetReturnsCorrectMeans()
    {
        Guid userA = Guid.NewGuid();
        Guid userB = Guid.NewGuid();

        var ratings = CreateUserRatings(userA, Enumerable.Repeat(5f, 5000).ToArray())
            .Concat(CreateUserRatings(userB, Enumerable.Repeat(1f, 5000).ToArray()))
            .ToList();

        _model.Train(ratings);

        Assert.Equal(5f, Predict(userA));
        Assert.Equal(1f, Predict(userB));
        Assert.Equal(3f, Predict(Guid.NewGuid()));
    }

    // Verifies that 1,000 users maintain their correct individual means.
    [Fact]
    public void TrainManyUsersReturnsCorrectIndividualMeans()
    {
        var ratings = new List<RatingObservation>();
        var expectedMeans = new Dictionary<Guid, float>();

        for (int i = 0; i < 1000; i++)
        {
            Guid userId = Guid.NewGuid();
            float rating = (i % 5) + 1f;

            ratings.AddRange(CreateUserRatings(userId, rating, rating));
            expectedMeans[userId] = rating;
        }

        _model.Train(ratings);

        foreach (var entry in expectedMeans)
            Assert.Equal(entry.Value, Predict(entry.Key));
    }
}