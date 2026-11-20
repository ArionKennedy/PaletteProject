
using System;
using System.Collections.Generic;
using System.Linq;
using Palette.Api.MachineLearning;
using Palette.Api.MachineLearning.Baselines;
using Xunit;

public class GlobalMeanModelTests
{
    private readonly GlobalMeanModel _model = new();

    // Creates rating observations from the supplied values.
    private static IReadOnlyList<RatingObservation> CreateRatings(params float[] values)
    {
        return values.Select(value =>
            new RatingObservation(Guid.NewGuid(), Guid.NewGuid(), value)).ToList();
    }

    // Trains the model using the supplied rating values.
    private void Train(params float[] values)
    {
        _model.Train(CreateRatings(values));
    }

    // Predicts a rating using randomly generated user and media identifiers.
    private float Predict()
    {
        return _model.Predict(Guid.NewGuid(), Guid.NewGuid());
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

    // Prediction consistency

    // Verifies that different users receive identical predictions.
    [Fact]
    public void PredictDifferentUsersReturnsSamePrediction()
    {
        Train(2f, 4f);

        Guid mediaId = Guid.NewGuid();
        float first = _model.Predict(Guid.NewGuid(), mediaId);
        float second = _model.Predict(Guid.NewGuid(), mediaId);

        Assert.Equal(3f, first);
        Assert.Equal(first, second);
    }

    // Verifies that different media receive identical predictions.
    [Fact]
    public void PredictDifferentMediaReturnsSamePrediction()
    {
        Train(2f, 4f);

        Guid userId = Guid.NewGuid();
        float first = _model.Predict(userId, Guid.NewGuid());
        float second = _model.Predict(userId, Guid.NewGuid());

        Assert.Equal(3f, first);
        Assert.Equal(first, second);
    }

    // Verifies that known and unknown pairs receive identical predictions.
    [Fact]
    public void PredictKnownAndUnknownPairsReturnsSamePrediction()
    {
        var ratings = CreateRatings(2f, 4f);
        _model.Train(ratings);

        float known = _model.Predict(ratings[0].UserId, ratings[0].MediaId);

        Assert.Equal(3f, known);
        Assert.Equal(known, Predict());
    }

    // Verifies that 100 repeated predictions produce identical results.
    [Fact]
    public void PredictRepeatedCallsReturnsSamePrediction()
    {
        Train(1f, 3f, 5f);

        Guid userId = Guid.NewGuid();
        Guid mediaId = Guid.NewGuid();
        float expected = _model.Predict(userId, mediaId);

        for (int i = 0; i < 100; i++)
            Assert.Equal(expected, _model.Predict(userId, mediaId));
    }

    // Retraining

    // Verifies that retraining replaces the previous global mean.
    [Fact]
    public void TrainNewDatasetReplacesPreviousMean()
    {
        Train(1f, 2f, 3f);
        float first = Predict();

        Train(3f, 4f, 5f);
        float second = Predict();

        Assert.Equal(2f, first);
        Assert.Equal(4f, second);
    }

    // Verifies that retraining with the same dataset preserves the mean.
    [Fact]
    public void TrainSameDatasetProducesSameMean()
    {
        var ratings = CreateRatings(1f, 3f, 4f, 5f);

        _model.Train(ratings);
        float first = Predict();

        _model.Train(ratings);

        Assert.Equal(first, Predict());
    }

    // Verifies that unsuccessful retraining preserves the previous mean.
    [Fact]
    public void TrainInvalidDatasetPreservesPreviousMean()
    {
        Train(2f, 4f);

        Assert.Throws<ArgumentException>(() => Train(5f, float.NaN));
        Assert.Equal(3f, Predict());
    }

    // Data integrity

    // Verifies that training does not modify the input ratings.
    [Fact]
    public void TrainValidRatingsDoesNotModifyInput()
    {
        var ratings = CreateRatings(5f, 2f, 4f, 1f);
        var original = ratings.ToArray();

        _model.Train(ratings);

        Assert.Equal(original, ratings.ToArray());
    }

    // Verifies that training data order does not affect the global mean.
    [Fact]
    public void TrainDifferentRatingOrderReturnsSameMean()
    {
        var ratings = CreateRatings(5f, 2f, 4f, 1f);

        _model.Train(ratings);
        float first = Predict();

        _model.Train(ratings.Reverse().ToArray());

        Assert.Equal(first, Predict(), 5);
    }

    // Scalability and numerical correctness

    // Verifies that training on 10,000 ratings produces the correct mean.
    [Fact]
    public void TrainLargeDatasetReturnsCorrectMean()
    {
        var values = Enumerable.Repeat(5f, 5000)
            .Concat(Enumerable.Repeat(1f, 5000)).ToArray();

        Train(values);

        Assert.Equal(3f, Predict(), 5);
    }
}