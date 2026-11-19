using System;
using System.Collections.Generic;
using System.Linq;
using Palette.Api.MachineLearning;
using Palette.Api.MachineLearning.Baselines;
using Xunit;

public class GlobalMeanModelTests
{
    private readonly GlobalMeanModel _model = new();

    // Mathematical correctness

    [Fact]
    // Verifies that predicting with single rating returns the same rating.
    public void PredictSingleRatingReturnsSameRating()
    {
        var ratings = CreateRatings(4f);

        _model.Train(ratings);

        float prediction = _model.Predict(
            Guid.NewGuid(), Guid.NewGuid());

        Assert.Equal(4f, prediction);
    }

    [Fact]
    // Verifies that predicting with multiple ratings returns the correct mean.
    public void PredictMultipleRatingsReturnsCorrectMean()
    {
        var ratings = CreateRatings(5f, 4f, 2f, 3f, 4f);

        _model.Train(ratings);

        float prediction = _model.Predict(
            Guid.NewGuid(), Guid.NewGuid());

        Assert.Equal(3.6f, prediction, 5);
    }

    [Fact]
    // Verifies that predicting with fractional ratings returns the correct mean.
    public void PredictFractionalRatingsReturnsCorrectMean()
    {
        var ratings = CreateRatings(2.5f, 3.5f, 4.5f);

        _model.Train(ratings);

        float prediction = _model.Predict(
            Guid.NewGuid(), Guid.NewGuid());

        Assert.Equal(3.5f, prediction, 5);
    }

    // Boundary values

    [Theory]
    [InlineData(1f)]
    [InlineData(3f)]
    [InlineData(5f)]
    // Verifies that predicting with identical ratings returns the same rating.
    public void PredictIdenticalRatingsReturnsSameRating(
        float rating)
    {
        var ratings = CreateRatings(rating, rating, rating);

        _model.Train(ratings);

        float prediction = _model.Predict(
            Guid.NewGuid(), Guid.NewGuid());

        Assert.Equal(rating, prediction);
    }

    [Fact]
    // Verifies that predicting with minimum and maximum ratings returns midpoint.
    public void PredictMinimumAndMaximumRatingsReturnsMidpoint()
    {
        var ratings = CreateRatings(1f, 5f);

        _model.Train(ratings);

        float prediction = _model.Predict(
            Guid.NewGuid(), Guid.NewGuid());

        Assert.Equal(3f, prediction);
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
        var ratings = Array.Empty<RatingObservation>();

        Assert.Throws<ArgumentException>(() =>
            _model.Train(ratings));
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
        var ratings = CreateRatings(3f, invalidRating, 4f);

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
        var ratings = CreateRatings(4f, float.NaN);

        Assert.Throws<ArgumentException>(() =>
            _model.Train(ratings));

        Assert.Throws<InvalidOperationException>(() =>
            _model.Predict(Guid.NewGuid(), Guid.NewGuid()));
    }

    // Prediction consistency

    [Fact]
    // Verifies that predicting with different users returns the same prediction.
    public void PredictDifferentUsersReturnsSamePrediction()
    {
        _model.Train(CreateRatings(2f, 4f));

        Guid mediaId = Guid.NewGuid();

        float first = _model.Predict(
            Guid.NewGuid(), mediaId);

        float second = _model.Predict(
            Guid.NewGuid(), mediaId);

        Assert.Equal(3f, first);
        Assert.Equal(first, second);
    }

    [Fact]
    // Verifies that predicting with different media returns the same prediction.
    public void PredictDifferentMediaReturnsSamePrediction()
    {
        _model.Train(CreateRatings(2f, 4f));

        Guid userId = Guid.NewGuid();

        float first = _model.Predict(
            userId, Guid.NewGuid());

        float second = _model.Predict(
            userId, Guid.NewGuid());

        Assert.Equal(3f, first);
        Assert.Equal(first, second);
    }

    [Fact]
    // Verifies that predicting with known and unknown pairs returns the same prediction.
    public void PredictKnownAndUnknownPairsReturnsSamePrediction()
    {
        var ratings = CreateRatings(2f, 4f);

        _model.Train(ratings);

        float known = _model.Predict(
            ratings[0].UserId,
            ratings[0].MediaId);

        float unknown = _model.Predict(
            Guid.NewGuid(),
            Guid.NewGuid());

        Assert.Equal(3f, known);
        Assert.Equal(known, unknown);
    }

    [Fact]
    // Verifies that predicting with repeated calls returns the same prediction.
    public void PredictRepeatedCallsReturnsSamePrediction()
    {
        _model.Train(CreateRatings(1f, 3f, 5f));

        Guid userId = Guid.NewGuid();
        Guid mediaId = Guid.NewGuid();

        float expected = _model.Predict(userId, mediaId);

        for (int i = 0; i < 100; i++)
        {
            Assert.Equal(
                expected,
                _model.Predict(userId, mediaId));
        }
    }

    // Retraining

    [Fact]
    // Verifies training for new dataset replaces previous mean.
    public void TrainNewDatasetReplacesPreviousMean()
    {
        _model.Train(CreateRatings(1f, 2f, 3f));

        float first = _model.Predict(
            Guid.NewGuid(), Guid.NewGuid());

        _model.Train(CreateRatings(3f, 4f, 5f));

        float second = _model.Predict(
            Guid.NewGuid(), Guid.NewGuid());

        Assert.Equal(2f, first);
        Assert.Equal(4f, second);
    }

    [Fact]
    // Verifies that training with same dataset produces same mean.
    public void TrainSameDatasetProducesSameMean()
    {
        var ratings = CreateRatings(1f, 3f, 4f, 5f);

        _model.Train(ratings);

        float first = _model.Predict(
            Guid.NewGuid(), Guid.NewGuid());

        _model.Train(ratings);

        float second = _model.Predict(
            Guid.NewGuid(), Guid.NewGuid());

        Assert.Equal(first, second);
    }

    [Fact]
    // Verifies that training with invalid dataset preserves previous mean.
    public void TrainInvalidDatasetPreservesPreviousMean()
    {
        _model.Train(CreateRatings(2f, 4f));

        var invalidRatings = CreateRatings(5f, float.NaN);

        Assert.Throws<ArgumentException>(() =>
            _model.Train(invalidRatings));

        float prediction = _model.Predict(
            Guid.NewGuid(), Guid.NewGuid());

        Assert.Equal(3f, prediction);
    }

    // Data integrity

    [Fact]
    // Verifies that training with valid ratings does not modify input.
    public void TrainValidRatingsDoesNotModifyInput()
    {
        var ratings = CreateRatings(5f, 2f, 4f, 1f);

        var original = ratings.ToArray();

        _model.Train(ratings);

        Assert.Equal(original, ratings.ToArray());
    }

    [Fact]
    // Verifies that training with different rating order returns the same mean.
    public void TrainDifferentRatingOrderReturnsSameMean()
    {
        var ratings = CreateRatings(5f, 2f, 4f, 1f);

        _model.Train(ratings);

        float first = _model.Predict(
            Guid.NewGuid(), Guid.NewGuid());

        _model.Train(ratings.Reverse().ToArray());

        float second = _model.Predict(
            Guid.NewGuid(), Guid.NewGuid());

        Assert.Equal(first, second, 5);
    }

    // Scalability and numerical correctness

    [Fact]
    // Verifies that training with large dataset returns the correct mean.
    public void TrainLargeDatasetReturnsCorrectMean()
    {
        var values = Enumerable.Repeat(5f, 5000)
            .Concat(Enumerable.Repeat(1f, 5000))
            .ToArray();

        _model.Train(CreateRatings(values));

        float prediction = _model.Predict(
            Guid.NewGuid(), Guid.NewGuid());

        Assert.Equal(3f, prediction, 5);
    }

    // Helper method

    // Creates a rating set for the tests.
    private static IReadOnlyList<RatingObservation> CreateRatings(
        params float[] values)
    {
        return values.Select(value =>
            new RatingObservation(
                Guid.NewGuid(),
                Guid.NewGuid(),
                value))
            .ToList();
    }
}