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
    public void PredictSingleRatingReturnsSameRating()
    {
        var ratings = CreateRatings(4f);

        _model.Train(ratings);

        float prediction = _model.Predict(
            Guid.NewGuid(), Guid.NewGuid());

        Assert.Equal(4f, prediction);
    }

    [Fact]
    public void PredictMultipleRatingsReturnsCorrectMean()
    {
        var ratings = CreateRatings(5f, 4f, 2f, 3f, 4f);

        _model.Train(ratings);

        float prediction = _model.Predict(
            Guid.NewGuid(), Guid.NewGuid());

        Assert.Equal(3.6f, prediction, 5);
    }

    [Fact]
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
    public void TrainNullRatingsThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            _model.Train(null!));
    }

    [Fact]
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
    public void TrainInvalidRatingThrowsArgumentException(
        float invalidRating)
    {
        var ratings = CreateRatings(3f, invalidRating, 4f);

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
        var ratings = CreateRatings(4f, float.NaN);

        Assert.Throws<ArgumentException>(() =>
            _model.Train(ratings));

        Assert.Throws<InvalidOperationException>(() =>
            _model.Predict(Guid.NewGuid(), Guid.NewGuid()));
    }

    // Prediction consistency

    [Fact]
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
    public void TrainValidRatingsDoesNotModifyInput()
    {
        var ratings = CreateRatings(5f, 2f, 4f, 1f);

        var original = ratings.ToArray();

        _model.Train(ratings);

        Assert.Equal(original, ratings.ToArray());
    }

    [Fact]
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