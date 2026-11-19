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
    public void ConstructorInvalidFactorCountThrowsArgumentException(
        int factorCount)
    {
        // Act & Assert
        Assert.ThrowsAny<ArgumentException>(() =>
            new MatrixFactorizationModel(factorCount));
    }

    // Training input validation tests

    [Fact]
    public void TrainEmptyRatingsThrowsArgumentException()
    {
        // Arrange
        var model = new MatrixFactorizationModel();

        // Act & Assert
        Assert.ThrowsAny<ArgumentException>(() =>
            model.Train(Array.Empty<RatingObservation>()));
    }

    [Fact]
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
    public void PredictUntrainedModelThrowsInvalidOperationException()
    {
        // Arrange
        var model = new MatrixFactorizationModel();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            model.Predict(Guid.NewGuid(), Guid.NewGuid()));
    }

    [Fact]
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

    private static IReadOnlyList<RatingObservation> CreateRatings()
    {
        return DataGenerator.Generate(
            userCount: 10,
            movieCount: 20,
            ratingsPerUser: 10);
    }

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