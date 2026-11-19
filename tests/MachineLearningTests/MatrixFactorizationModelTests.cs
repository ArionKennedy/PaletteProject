using System;
using System.Collections.Generic;
using System.Linq;
using Palette.Api.MachineLearning;
using Palette.Api.MachineLearning.Data;
using Palette.Api.MachineLearning.Evaluation;
using Xunit;

public class MatrixFactorizationModelTests
{
    // Initialization tests

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(8)]
    [InlineData(16)]
    // Verifies that constructing a model with valid factor count creates a model.
    public void ConstructorValidFactorCountCreatesModel(
        int factorCount)
    {
        // Act
        var model = new MatrixFactorizationModel(factorCount);

        // Assert
        Assert.NotNull(model);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    // Verifies that constructing a model with invalid factor count throws ArgumentException.
    public void ConstructorInvalidFactorCountThrowsArgumentException(
        int factorCount)
    {
        // Act & Assert
        Assert.ThrowsAny<ArgumentException>(() =>
            new MatrixFactorizationModel(factorCount));
    }

    // Training input validation tests

    [Fact]
    // Verifies that training with empty ratings throws ArgumentException.
    public void TrainEmptyRatingsThrowsArgumentException()
    {
        // Arrange
        var model = new MatrixFactorizationModel();

        // Act & Assert
        Assert.ThrowsAny<ArgumentException>(() =>
            model.Train(Array.Empty<RatingObservation>()));
    }

    [Fact]
    // Verifies that training with null ratings throws ArgumentNullException.
    public void TrainNullRatingsThrowsArgumentNullException()
    {
        // Arrange
        var model = new MatrixFactorizationModel();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            model.Train(null!));
    }

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
    // Verifies that training with invalid hyperparameters throws ArgumentException.
    public void TrainInvalidHyperparametersThrowsArgumentException(
        int epochs,
        float learningRate,
        float regularization)
    {
        // Arrange
        var model = new MatrixFactorizationModel();

        var ratings = new List<RatingObservation>
        {
            new(Guid.NewGuid(), Guid.NewGuid(), 4f)
        };

        // Act & Assert
        Assert.ThrowsAny<ArgumentException>(() =>
            model.Train(
                ratings,
                epochs,
                learningRate,
                regularization));
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(-1f)]
    [InlineData(0.99f)]
    [InlineData(5.01f)]
    [InlineData(6f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    // Verifies that training with invalid rating throws ArgumentException.
    public void TrainInvalidRatingThrowsArgumentException(
        float rating)
    {
        // Arrange
        var model = new MatrixFactorizationModel();

        var ratings = new List<RatingObservation>
        {
            new(Guid.NewGuid(), Guid.NewGuid(), rating)
        };

        // Act & Assert
        Assert.ThrowsAny<ArgumentException>(() =>
            model.Train(ratings));
    }

    // Training boundary tests

    [Theory]
    [InlineData(1f)]
    [InlineData(3f)]
    [InlineData(3.5f)]
    [InlineData(5f)]
    // Verifies that training with single observation returns the finite prediction.
    public void TrainSingleObservationReturnsFinitePrediction(
        float rating)
    {
        // Arrange
        var model = new MatrixFactorizationModel(factorCount: 1);

        var observation = new RatingObservation(
            Guid.NewGuid(), Guid.NewGuid(), rating);

        // Act
        model.Train(
            new[] { observation },
            epochs: 100);

        float prediction = model.Predict(
            observation.UserId,
            observation.MediaId);

        // Assert
        Assert.True(float.IsFinite(prediction));
    }

    [Fact]
    // Verifies that training with zero regularization returns the finite predictions.
    public void TrainZeroRegularizationReturnsFinitePredictions()
    {
        // Arrange
        var ratings = CreateRatings();
        var model = new MatrixFactorizationModel(3);

        // Act
        model.Train(ratings, regularization: 0f);

        // Assert
        AssertFinitePredictions(model, ratings);
    }

    [Fact]
    // Verifies that training with single epoch returns the finite predictions.
    public void TrainSingleEpochReturnsFinitePredictions()
    {
        // Arrange
        var ratings = CreateRatings();
        var model = new MatrixFactorizationModel(3);

        // Act
        model.Train(ratings, epochs: 1);

        // Assert
        AssertFinitePredictions(model, ratings);
    }

    [Fact]
    // Verifies that training with valid ratings does not modify input.
    public void TrainValidRatingsDoesNotModifyInput()
    {
        // Arrange
        var ratings = CreateRatings();
        var original = ratings.ToArray();

        var model = new MatrixFactorizationModel(3);

        // Act
        model.Train(ratings);

        // Assert
        Assert.Equal(original, ratings.ToArray());
    }

    // Prediction tests

    [Fact]
    // Verifies that predicting with untrained model throws InvalidOperationException.
    public void PredictUntrainedModelThrowsInvalidOperationException()
    {
        // Arrange
        var model = new MatrixFactorizationModel();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            model.Predict(Guid.NewGuid(), Guid.NewGuid()));
    }

    [Fact]
    // Verifies that predicting with unknown user throws InvalidOperationException.
    public void PredictUnknownUserThrowsInvalidOperationException()
    {
        // Arrange
        var ratings = CreateRatings();
        var model = new MatrixFactorizationModel(3);

        model.Train(ratings);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            model.Predict(
                Guid.NewGuid(),
                ratings[0].MediaId));
    }

    [Fact]
    // Verifies that predicting with unknown media throws InvalidOperationException.
    public void PredictUnknownMediaThrowsInvalidOperationException()
    {
        // Arrange
        var ratings = CreateRatings();
        var model = new MatrixFactorizationModel(3);

        model.Train(ratings);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            model.Predict(
                ratings[0].UserId,
                Guid.NewGuid()));
    }

    [Fact]
    // Verifies that predicting with repeated calls returns the identical prediction.
    public void PredictRepeatedCallsReturnsIdenticalPrediction()
    {
        // Arrange
        var ratings = CreateRatings();
        var model = new MatrixFactorizationModel(3);

        model.Train(ratings);

        var rating = ratings[0];

        // Act
        float first = model.Predict(
            rating.UserId, rating.MediaId);

        float second = model.Predict(
            rating.UserId, rating.MediaId);

        // Assert
        Assert.Equal(first, second);
    }

    [Fact]
    // Verifies that predicting with known entities with unseen pair returns the finite prediction.
    public void PredictKnownEntitiesWithUnseenPairReturnsFinitePrediction()
    {
        // Arrange
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();

        var movieA = Guid.NewGuid();
        var movieB = Guid.NewGuid();

        var ratings = new List<RatingObservation>
        {
            new(userA, movieA, 5f),
            new(userB, movieA, 4f),
            new(userB, movieB, 3f)
        };

        var model = new MatrixFactorizationModel(3);

        model.Train(ratings);

        // Act
        float prediction = model.Predict(userA, movieB);

        // Assert
        Assert.True(float.IsFinite(prediction));
    }

    // Learning tests

    [Fact]
    // Verifies that training with identical inputs produces identical predictions.
    public void TrainIdenticalInputsProducesIdenticalPredictions()
    {
        // Arrange
        var ratings = CreateRatings();

        var first = new MatrixFactorizationModel(3);
        var second = new MatrixFactorizationModel(3);

        // Act
        first.Train(ratings);
        second.Train(ratings);

        // Assert
        Assert.All(ratings, rating =>
        {
            Assert.Equal(
                first.Predict(rating.UserId, rating.MediaId),
                second.Predict(rating.UserId, rating.MediaId));
        });
    }

    [Fact]
    // Verifies that training with additional epochs reduces training error.
    public void TrainAdditionalEpochsReducesTrainingError()
    {
        // Arrange
        var ratings = DataGenerator.Generate(
            userCount: 20,
            movieCount: 40,
            ratingsPerUser: 20);

        var model = new MatrixFactorizationModel(3);

        model.Train(ratings, epochs: 1);

        double initialError = ModelEvaluator.CalculateRmse(
            ratings, model.Predict);

        // Act
        model.Train(ratings, epochs: 100);

        double finalError = ModelEvaluator.CalculateRmse(
            ratings, model.Predict);

        // Assert
        Assert.True(double.IsFinite(initialError));
        Assert.True(double.IsFinite(finalError));

        Assert.True(finalError < initialError,
            $"Initial RMSE: {initialError}, Final RMSE: {finalError}");
    }

    [Fact]
    // Verifies training for single observation learns expected rating.
    public void TrainSingleObservationLearnsExpectedRating()
    {
        // Arrange
        var observation = new RatingObservation(
            Guid.NewGuid(), Guid.NewGuid(), 4f);

        var model = new MatrixFactorizationModel(1);

        // Act
        model.Train(
            new[] { observation },
            epochs: 1000,
            learningRate: 0.01f,
            regularization: 0f);

        float prediction = model.Predict(
            observation.UserId,
            observation.MediaId);

        // Assert
        Assert.InRange(prediction, 3.95f, 4.05f);
    }

    // Integration tests

    [Fact]
    // Verifies that predicting with held out dataset returns the finite predictions.
    public void PredictHeldOutDatasetReturnsFinitePredictions()
    {
        // Arrange
        var ratings = DataGenerator.Generate();
        var split = DatasetSplitter.Split(ratings);

        var model = new MatrixFactorizationModel(3);
        model.Train(split.Training);

        // Act & Assert
        AssertFinitePredictions(model, split.Testing);
    }

    [Fact]
    // Verifies that training with valid dataset returns the finite validation rmse.
    public void TrainValidDatasetReturnsFiniteValidationRmse()
    {
        // Arrange
        var ratings = DataGenerator.Generate();
        var split = DatasetSplitter.Split(ratings);

        var model = new MatrixFactorizationModel(3);
        model.Train(split.Training);

        // Act
        double rmse = ModelEvaluator.CalculateRmse(
            split.Validation,
            model.Predict);

        // Assert
        Assert.True(double.IsFinite(rmse));
        Assert.True(rmse >= 0);
    }

    // Helper methods

    // Creates a rating set for the tests.
    private static IReadOnlyList<RatingObservation> CreateRatings()
    {
        return DataGenerator.Generate(
            userCount: 10,
            movieCount: 20,
            ratingsPerUser: 10);
    }

    // Asserts that predictions for all supplied ratings are finite.
    private static void AssertFinitePredictions(
        MatrixFactorizationModel model,
        IReadOnlyList<RatingObservation> ratings)
    {
        Assert.All(ratings, rating =>
        {
            float prediction = model.Predict(
                rating.UserId,
                rating.MediaId);

            Assert.True(float.IsFinite(prediction));
        });
    }
}