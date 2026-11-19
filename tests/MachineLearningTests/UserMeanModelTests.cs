using System;
using System.Collections.Generic;
using System.Linq;
using Palette.Api.MachineLearning;
using Palette.Api.MachineLearning.Baselines;
using Xunit;

public class UserMeanModelTests
{
    private readonly UserMeanModel _model = new();

    // Mathematical correctness

    [Fact]
    // Verifies that predicting with single rating returns the same rating.
    public void PredictSingleRatingReturnsSameRating()
    {
        Guid userId = Guid.NewGuid();

        _model.Train(CreateUserRatings(userId, 4f));

        Assert.Equal(4f,
            _model.Predict(userId, Guid.NewGuid()));
    }

    [Fact]
    // Verifies that predicting with multiple ratings returns the correct mean.
    public void PredictMultipleRatingsReturnsCorrectMean()
    {
        Guid userId = Guid.NewGuid();

        _model.Train(
            CreateUserRatings(userId, 5f, 4f, 2f, 3f, 4f));

        Assert.Equal(3.6f,
            _model.Predict(userId, Guid.NewGuid()), 5);
    }

    [Fact]
    // Verifies that predicting with fractional ratings returns the correct mean.
    public void PredictFractionalRatingsReturnsCorrectMean()
    {
        Guid userId = Guid.NewGuid();

        _model.Train(
            CreateUserRatings(userId, 2.5f, 3.5f, 4.5f));

        Assert.Equal(3.5f,
            _model.Predict(userId, Guid.NewGuid()), 5);
    }

    [Fact]
    // Verifies that predicting with multiple users returns individual means.
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

        Assert.Equal(14f / 3f,
            _model.Predict(userA, mediaId), 5);

        Assert.Equal(1.5f,
            _model.Predict(userB, mediaId), 5);

        Assert.Equal(3f,
            _model.Predict(userC, mediaId), 5);
    }

    [Fact]
    // Verifies that predicting with users rating same media returns individual means.
    public void PredictUsersRatingSameMediaReturnsIndividualMeans()
    {
        Guid userA = Guid.NewGuid();
        Guid userB = Guid.NewGuid();
        Guid mediaId = Guid.NewGuid();

        var ratings = new List<RatingObservation>
        {
            new(userA, mediaId, 5f),
            new(userB, mediaId, 1f)
        };

        _model.Train(ratings);

        Assert.Equal(5f, _model.Predict(userA, mediaId));
        Assert.Equal(1f, _model.Predict(userB, mediaId));
    }

    // Boundary values

    [Theory]
    [InlineData(1f)]
    [InlineData(3f)]
    [InlineData(5f)]
    // Verifies that predicting with identical ratings returns the same rating.
    public void PredictIdenticalRatingsReturnsSameRating(float rating)
    {
        Guid userId = Guid.NewGuid();

        _model.Train(
            CreateUserRatings(userId, rating, rating, rating));

        Assert.Equal(rating,
            _model.Predict(userId, Guid.NewGuid()));
    }

    [Fact]
    // Verifies that predicting with minimum and maximum ratings returns midpoint.
    public void PredictMinimumAndMaximumRatingsReturnsMidpoint()
    {
        Guid userId = Guid.NewGuid();

        _model.Train(CreateUserRatings(userId, 1f, 5f));

        Assert.Equal(3f,
            _model.Predict(userId, Guid.NewGuid()));
    }

    // Prediction consistency

    [Fact]
    // Verifies that predicting with same user different media returns the same prediction.
    public void PredictSameUserDifferentMediaReturnsSamePrediction()
    {
        Guid userId = Guid.NewGuid();

        _model.Train(CreateUserRatings(userId, 2f, 4f));

        float first = _model.Predict(userId, Guid.NewGuid());
        float second = _model.Predict(userId, Guid.NewGuid());

        Assert.Equal(3f, first);
        Assert.Equal(first, second);
    }

    [Fact]
    // Verifies that predicting with known and unknown media returns the same prediction.
    public void PredictKnownAndUnknownMediaReturnsSamePrediction()
    {
        Guid userId = Guid.NewGuid();

        var ratings = CreateUserRatings(userId, 2f, 4f);

        _model.Train(ratings);

        float known = _model.Predict(
            userId, ratings[0].MediaId);

        float unknown = _model.Predict(
            userId, Guid.NewGuid());

        Assert.Equal(3f, known);
        Assert.Equal(known, unknown);
    }

    [Fact]
    // Verifies that predicting with repeated calls returns the same prediction.
    public void PredictRepeatedCallsReturnsSamePrediction()
    {
        Guid userId = Guid.NewGuid();
        Guid mediaId = Guid.NewGuid();

        _model.Train(CreateUserRatings(userId, 1f, 3f, 5f));

        float expected = _model.Predict(userId, mediaId);

        for (int i = 0; i < 100; i++)
        {
            Assert.Equal(expected,
                _model.Predict(userId, mediaId));
        }
    }

    // Cold start behaviour

    [Fact]
    // Verifies that predicting with unknown user returns global mean.
    public void PredictUnknownUserReturnsGlobalMean()
    {
        var ratings = CreateUserRatings(Guid.NewGuid(), 5f, 4f)
            .Concat(CreateUserRatings(Guid.NewGuid(), 1f, 2f))
            .ToList();

        _model.Train(ratings);

        Assert.Equal(3f,
            _model.Predict(Guid.NewGuid(), Guid.NewGuid()));
    }

    [Fact]
    // Verifies that predicting with unknown user uses observation weighted global mean.
    public void PredictUnknownUserUsesObservationWeightedGlobalMean()
    {
        var ratings = CreateUserRatings(Guid.NewGuid(), 5f, 5f, 5f)
            .Concat(CreateUserRatings(Guid.NewGuid(), 1f))
            .ToList();

        _model.Train(ratings);

        Assert.Equal(4f,
            _model.Predict(Guid.NewGuid(), Guid.NewGuid()));
    }

    [Fact]
    // Verifies that predicting with unknown users returns the same global mean.
    public void PredictUnknownUsersReturnsSameGlobalMean()
    {
        _model.Train(
            CreateUserRatings(Guid.NewGuid(), 2f, 4f));

        Guid mediaId = Guid.NewGuid();

        float first = _model.Predict(Guid.NewGuid(), mediaId);
        float second = _model.Predict(Guid.NewGuid(), mediaId);

        Assert.Equal(3f, first);
        Assert.Equal(first, second);
    }

    [Fact]
    // Verifies that predicting with unknown user known media returns global mean.
    public void PredictUnknownUserKnownMediaReturnsGlobalMean()
    {
        Guid userId = Guid.NewGuid();

        var ratings = CreateUserRatings(userId, 1f, 5f);

        _model.Train(ratings);

        Assert.Equal(3f,
            _model.Predict(
                Guid.NewGuid(), ratings[0].MediaId));
    }

    // Input validation

    [Fact]
    // Verifies that training with null ratings throws ArgumentNullException.
    public void TrainNullRatingsThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            _model.Train(null!));
    }

    [Fact]
    // Verifies that training with empty ratings throws ArgumentException.
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
    // Verifies that training with invalid rating throws ArgumentException.
    public void TrainInvalidRatingThrowsArgumentException(
        float invalidRating)
    {
        var ratings = CreateUserRatings(
            Guid.NewGuid(), 3f, invalidRating, 4f);

        Assert.Throws<ArgumentException>(() =>
            _model.Train(ratings));
    }

    // Model state

    [Fact]
    // Verifies that predicting with before training throws InvalidOperationException.
    public void PredictBeforeTrainingThrowsInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(() =>
            _model.Predict(Guid.NewGuid(), Guid.NewGuid()));
    }

    [Fact]
    // Verifies training for invalid dataset leaves model untrained.
    public void TrainInvalidDatasetLeavesModelUntrained()
    {
        Guid userId = Guid.NewGuid();

        var ratings = CreateUserRatings(userId, 4f, float.NaN);

        Assert.Throws<ArgumentException>(() =>
            _model.Train(ratings));

        Assert.Throws<InvalidOperationException>(() =>
            _model.Predict(userId, Guid.NewGuid()));
    }

    // Retraining

    [Fact]
    // Verifies training for new ratings replaces previous user mean.
    public void TrainNewRatingsReplacesPreviousUserMean()
    {
        Guid userId = Guid.NewGuid();

        _model.Train(CreateUserRatings(userId, 1f, 2f, 3f));

        Assert.Equal(2f,
            _model.Predict(userId, Guid.NewGuid()));

        _model.Train(CreateUserRatings(userId, 4f, 5f));

        Assert.Equal(4.5f,
            _model.Predict(userId, Guid.NewGuid()));
    }

    [Fact]
    // Verifies training for new dataset removes previous users.
    public void TrainNewDatasetRemovesPreviousUsers()
    {
        Guid oldUser = Guid.NewGuid();
        Guid newUser = Guid.NewGuid();

        _model.Train(CreateUserRatings(oldUser, 1f));

        Assert.Equal(1f,
            _model.Predict(oldUser, Guid.NewGuid()));

        _model.Train(CreateUserRatings(newUser, 5f));

        // Old user is now unknown and receives the global mean.
        Assert.Equal(5f,
            _model.Predict(oldUser, Guid.NewGuid()));

        Assert.Equal(5f,
            _model.Predict(newUser, Guid.NewGuid()));
    }

    [Fact]
    // Verifies training for new dataset updates global mean fallback.
    public void TrainNewDatasetUpdatesGlobalMeanFallback()
    {
        Guid unknownUser = Guid.NewGuid();

        _model.Train(
            CreateUserRatings(Guid.NewGuid(), 1f, 3f));

        Assert.Equal(2f,
            _model.Predict(unknownUser, Guid.NewGuid()));

        _model.Train(
            CreateUserRatings(Guid.NewGuid(), 4f, 5f));

        Assert.Equal(4.5f,
            _model.Predict(unknownUser, Guid.NewGuid()));
    }

    [Fact]
    // Verifies that training with same dataset produces same predictions.
    public void TrainSameDatasetProducesSamePredictions()
    {
        Guid userId = Guid.NewGuid();

        var ratings = CreateUserRatings(userId, 1f, 3f, 5f);

        _model.Train(ratings);

        float first = _model.Predict(userId, Guid.NewGuid());

        _model.Train(ratings);

        float second = _model.Predict(userId, Guid.NewGuid());

        Assert.Equal(first, second);
    }

    // State preservation

    [Fact]
    // Verifies that training with invalid dataset preserves previous user mean.
    public void TrainInvalidDatasetPreservesPreviousUserMean()
    {
        Guid userId = Guid.NewGuid();

        _model.Train(CreateUserRatings(userId, 2f, 4f));

        var invalidRatings =
            CreateUserRatings(userId, 5f, float.NaN);

        Assert.Throws<ArgumentException>(() =>
            _model.Train(invalidRatings));

        Assert.Equal(3f,
            _model.Predict(userId, Guid.NewGuid()));
    }

    [Fact]
    // Verifies that training with invalid dataset preserves global mean fallback.
    public void TrainInvalidDatasetPreservesGlobalMeanFallback()
    {
        _model.Train(
            CreateUserRatings(Guid.NewGuid(), 2f, 4f));

        var invalidRatings =
            CreateUserRatings(Guid.NewGuid(), 5f, float.NaN);

        Assert.Throws<ArgumentException>(() =>
            _model.Train(invalidRatings));

        Assert.Equal(3f,
            _model.Predict(Guid.NewGuid(), Guid.NewGuid()));
    }

    // Data integrity

    [Fact]
    // Verifies that training with valid ratings does not modify input.
    public void TrainValidRatingsDoesNotModifyInput()
    {
        var ratings = CreateUserRatings(Guid.NewGuid(), 5f, 2f)
            .Concat(CreateUserRatings(Guid.NewGuid(), 4f, 1f))
            .ToList();

        var original = ratings.ToArray();

        _model.Train(ratings);

        Assert.Equal(original, ratings.ToArray());
    }

    [Fact]
    // Verifies that training with different rating order returns the same predictions.
    public void TrainDifferentRatingOrderReturnsSamePredictions()
    {
        Guid userA = Guid.NewGuid();
        Guid userB = Guid.NewGuid();

        var ratings = CreateUserRatings(userA, 5f, 2f)
            .Concat(CreateUserRatings(userB, 4f, 1f))
            .ToList();

        _model.Train(ratings);

        float firstA = _model.Predict(userA, Guid.NewGuid());
        float firstB = _model.Predict(userB, Guid.NewGuid());

        _model.Train(ratings.AsEnumerable().Reverse().ToArray());

        Assert.Equal(firstA,
            _model.Predict(userA, Guid.NewGuid()), 5);

        Assert.Equal(firstB,
            _model.Predict(userB, Guid.NewGuid()), 5);
    }

    // Scalability and numerical correctness

    [Fact]
    // Verifies that training with large dataset returns the correct means.
    public void TrainLargeDatasetReturnsCorrectMeans()
    {
        Guid userA = Guid.NewGuid();
        Guid userB = Guid.NewGuid();

        var ratings = CreateUserRatings(
                userA, Enumerable.Repeat(5f, 5000).ToArray())
            .Concat(CreateUserRatings(
                userB, Enumerable.Repeat(1f, 5000).ToArray()))
            .ToList();

        _model.Train(ratings);

        Assert.Equal(5f,
            _model.Predict(userA, Guid.NewGuid()));

        Assert.Equal(1f,
            _model.Predict(userB, Guid.NewGuid()));

        Assert.Equal(3f,
            _model.Predict(Guid.NewGuid(), Guid.NewGuid()));
    }

    [Fact]
    // Verifies that training with many users returns the correct individual means.
    public void TrainManyUsersReturnsCorrectIndividualMeans()
    {
        var ratings = new List<RatingObservation>();
        var expectedMeans = new Dictionary<Guid, float>();

        for (int i = 0; i < 1000; i++)
        {
            Guid userId = Guid.NewGuid();
            float rating = (i % 5) + 1f;

            ratings.AddRange(
                CreateUserRatings(userId, rating, rating));

            expectedMeans[userId] = rating;
        }

        _model.Train(ratings);

        foreach (var entry in expectedMeans)
        {
            Assert.Equal(entry.Value,
                _model.Predict(entry.Key, Guid.NewGuid()));
        }
    }

    // Helper method

    // Creates test data for user ratings.
    private static IReadOnlyList<RatingObservation> CreateUserRatings(
        Guid userId,
        params float[] values)
    {
        return values.Select(value =>
            new RatingObservation(
                userId,
                Guid.NewGuid(),
                value))
            .ToList();
    }
}