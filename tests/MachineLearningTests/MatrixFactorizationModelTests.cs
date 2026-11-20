
using System;
using System.Collections.Generic;
using System.Linq;
using Palette.Api.MachineLearning;
using Palette.Api.MachineLearning.Data;
using Palette.Api.MachineLearning.Evaluation;
using Xunit;

public class MatrixFactorizationModelTests
{
    private readonly MatrixFactorizationModel _model = new(3);

    // Creates a synthetic rating dataset for testing.
    private static IReadOnlyList<RatingObservation> CreateRatings()
    {
        return DataGenerator.Generate(10, 20, 10);
    }

    // Creates an individual rating observation.
    private static RatingObservation CreateRating(float rating)
    {
        return new RatingObservation(Guid.NewGuid(), Guid.NewGuid(), rating);
    }

    // Predicts a rating for the supplied observation.
    private static float Predict(
        MatrixFactorizationModel model, RatingObservation observation)
    {
        return model.Predict(observation.UserId, observation.MediaId);
    }

    // Verifies that predictions for all supplied observations are finite.
    private static void AssertFinitePredictions(
        MatrixFactorizationModel model, IReadOnlyList<RatingObservation> ratings)
    {
        Assert.All(ratings, rating =>
            Assert.True(float.IsFinite(Predict(model, rating))));
    }

    // Initialization tests

    // Verifies that valid factor counts successfully initialize a model.
    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(8)]
    [InlineData(16)]
    public void ConstructorValidFactorCountCreatesModel(int factorCount)
    {
        Assert.NotNull(new MatrixFactorizationModel(factorCount));
    }

    // Verifies that invalid factor counts are rejected.
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ConstructorInvalidFactorCountThrowsArgumentException(int factorCount)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            new MatrixFactorizationModel(factorCount));
    }

    // Training input validation

    // Verifies that empty training datasets are rejected.
    [Fact]
    public void TrainEmptyRatingsThrowsArgumentException()
    {
        var model = new MatrixFactorizationModel();

        Assert.ThrowsAny<ArgumentException>(() => model.Train([]));
    }

    // Verifies that null training datasets are rejected.
    [Fact]
    public void TrainNullRatingsThrowsArgumentNullException()
    {
        var model = new MatrixFactorizationModel();

        Assert.Throws<ArgumentNullException>(() => model.Train(null!));
    }

    // Verifies that invalid and nonfinite hyperparameters are rejected.
    [Theory]
    [InlineData(0, 0.01f, 0.02f)]
    [InlineData(-1, 0.01f, 0.02f)]
    [InlineData(100, 0f, 0.02f)]
    [InlineData(100, -0.01f, 0.02f)]
    [InlineData(100, 0.01f, -0.02f)]
    [InlineData(100, float.NaN, 0.02f)]
    [InlineData(100, float.PositiveInfinity, 0.02f)]
    [InlineData(100, float.NegativeInfinity, 0.02f)]
    [InlineData(100, 0.01f, float.NaN)]
    [InlineData(100, 0.01f, float.PositiveInfinity)]
    [InlineData(100, 0.01f, float.NegativeInfinity)]
    public void TrainInvalidHyperparametersThrowsArgumentException(
        int epochs, float learningRate, float regularization)
    {
        var model = new MatrixFactorizationModel();

        Assert.ThrowsAny<ArgumentException>(() =>
            model.Train([CreateRating(4f)], epochs, learningRate, regularization));
    }

    // Verifies that out-of-range and nonfinite ratings are rejected.
    [Theory]
    [InlineData(0f)]
    [InlineData(-1f)]
    [InlineData(0.99f)]
    [InlineData(5.01f)]
    [InlineData(6f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    public void TrainInvalidRatingThrowsArgumentException(float rating)
    {
        var model = new MatrixFactorizationModel();

        Assert.ThrowsAny<ArgumentException>(() =>
            model.Train([CreateRating(rating)]));
    }

    // Training boundary tests

    // Verifies that training on a single observation produces a finite prediction.
    [Theory]
    [InlineData(1f)]
    [InlineData(3f)]
    [InlineData(3.5f)]
    [InlineData(5f)]
    public void TrainSingleObservationReturnsFinitePrediction(float rating)
    {
        var model = new MatrixFactorizationModel(1);
        var observation = CreateRating(rating);

        model.Train([observation], epochs: 100);

        Assert.True(float.IsFinite(Predict(model, observation)));
    }

    // Verifies that training without regularization produces finite predictions.
    [Fact]
    public void TrainZeroRegularizationReturnsFinitePredictions()
    {
        var ratings = CreateRatings();

        _model.Train(ratings, regularization: 0f);

        AssertFinitePredictions(_model, ratings);
    }

    // Verifies that training for a single epoch produces finite predictions.
    [Fact]
    public void TrainSingleEpochReturnsFinitePredictions()
    {
        var ratings = CreateRatings();

        _model.Train(ratings, epochs: 1);

        AssertFinitePredictions(_model, ratings);
    }

    // Verifies that training does not modify the input observations.
    [Fact]
    public void TrainValidRatingsDoesNotModifyInput()
    {
        var ratings = CreateRatings();
        var original = ratings.ToArray();

        _model.Train(ratings);

        Assert.Equal(original, ratings.ToArray());
    }

    // Prediction tests

    // Verifies that predictions cannot be made before training.
    [Fact]
    public void PredictUntrainedModelThrowsInvalidOperationException()
    {
        var model = new MatrixFactorizationModel();

        Assert.Throws<InvalidOperationException>(() =>
            model.Predict(Guid.NewGuid(), Guid.NewGuid()));
    }

    // Verifies that predictions for unknown users are rejected.
    [Fact]
    public void PredictUnknownUserThrowsInvalidOperationException()
    {
        var ratings = CreateRatings();
        _model.Train(ratings);

        Assert.Throws<InvalidOperationException>(() =>
            _model.Predict(Guid.NewGuid(), ratings[0].MediaId));
    }

    // Verifies that predictions for unknown media are rejected.
    [Fact]
    public void PredictUnknownMediaThrowsInvalidOperationException()
    {
        var ratings = CreateRatings();
        _model.Train(ratings);

        Assert.Throws<InvalidOperationException>(() =>
            _model.Predict(ratings[0].UserId, Guid.NewGuid()));
    }

    // Verifies that repeated predictions produce identical results.
    [Fact]
    public void PredictRepeatedCallsReturnsIdenticalPrediction()
    {
        var ratings = CreateRatings();
        _model.Train(ratings);

        Assert.Equal(Predict(_model, ratings[0]), Predict(_model, ratings[0]));
    }

    // Verifies that known users and media produce finite predictions for unseen pairs.
    [Fact]
    public void PredictKnownEntitiesWithUnseenPairReturnsFinitePrediction()
    {
        Guid userA = Guid.NewGuid();
        Guid userB = Guid.NewGuid();
        Guid movieA = Guid.NewGuid();
        Guid movieB = Guid.NewGuid();

        _model.Train(
        [
            new RatingObservation(userA, movieA, 5f),
            new RatingObservation(userB, movieA, 4f),
            new RatingObservation(userB, movieB, 3f)
        ]);

        Assert.True(float.IsFinite(_model.Predict(userA, movieB)));
    }

    // Learning tests

    // Verifies that identical training inputs produce identical predictions.
    [Fact]
    public void TrainIdenticalInputsProducesIdenticalPredictions()
    {
        var ratings = CreateRatings();
        var second = new MatrixFactorizationModel(3);

        _model.Train(ratings);
        second.Train(ratings);

        Assert.All(ratings, rating =>
            Assert.Equal(Predict(_model, rating), Predict(second, rating)));
    }

    // Verifies that training with additional epochs reduces training error.
    [Fact]
    public void TrainAdditionalEpochsReducesTrainingError()
    {
        var ratings = DataGenerator.Generate(20, 40, 20);

        _model.Train(ratings, epochs: 1);
        double initialError = ModelEvaluator.CalculateRmse(ratings, _model.Predict);

        _model.Train(ratings, epochs: 100);
        double finalError = ModelEvaluator.CalculateRmse(ratings, _model.Predict);

        Assert.True(double.IsFinite(initialError));
        Assert.True(double.IsFinite(finalError));

        Assert.True(finalError < initialError,
            $"Initial RMSE: {initialError}, Final RMSE: {finalError}");
    }

    // Verifies that training learns the expected rating for a single observation.
    [Fact]
    public void TrainSingleObservationLearnsExpectedRating()
    {
        var observation = CreateRating(4f);
        var model = new MatrixFactorizationModel(1);

        model.Train([observation], epochs: 1000,
            learningRate: 0.01f, regularization: 0f);

        Assert.InRange(Predict(model, observation), 3.95f, 4.05f);
    }

    // Integration tests

    // Verifies that predictions on held-out testing observations are finite.
    [Fact]
    public void PredictHeldOutDatasetReturnsFinitePredictions()
    {
        var split = DatasetSplitter.Split(DataGenerator.Generate());
        _model.Train(split.Training);

        AssertFinitePredictions(_model, split.Testing);
    }

    // Verifies that validation RMSE is finite and nonnegative.
    [Fact]
    public void TrainValidDatasetReturnsFiniteValidationRmse()
    {
        var split = DatasetSplitter.Split(DataGenerator.Generate());
        _model.Train(split.Training);

        double rmse = ModelEvaluator.CalculateRmse(split.Validation, _model.Predict);

        Assert.True(double.IsFinite(rmse));
        Assert.True(rmse >= 0);
    }
}