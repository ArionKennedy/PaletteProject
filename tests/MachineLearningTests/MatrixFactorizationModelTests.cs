
using Palette.Api.MachineLearning;
using Xunit;

namespace Palette.Tests.MachineLearning;

public class MatrixFactorizationModelTests
{

    // Test unseen ratings after training
    [Fact]
    public void Train_ShouldPredictUnseenRatings()
    {
        // Arrange
        var ratings = DataGenerator.Generate(
            userCount: 100,
            movieCount: 200,
            ratingsPerUser: 30
        );

        var random = new Random(42);
        var shuffled = ratings
            .OrderBy(_ => random.Next())
            .ToList();

        int trainingCount = (int)(shuffled.Count * 0.8);

        var trainingRatings = shuffled
            .Take(trainingCount)
            .ToList();

        var testRatings = shuffled
            .Skip(trainingCount)
            .ToList();

        var model = new MatrixFactorizationModel(
            factorCount: 3
        );

        // train the model on the training ratings
        model.Train(
            trainingRatings,
            epochs: 100,
            learningRate: 0.01f
        );

        // Assert
        foreach (var rating in testRatings)
        {
            float prediction = model.Predict(
                rating.UserId,
                rating.MediaId
            );

            Assert.True(float.IsFinite(prediction));
        }
    }

    // Test the Stochastic Gradient Descent (SGD) training method
    [Fact]
    public void ReduceTrainingErrorTest()
    {
        // Arrange
        var ratings = DataGenerator.Generate(
            userCount: 20,
            movieCount: 40,
            ratingsPerUser: 20
        );

        var model = new MatrixFactorizationModel(
            factorCount: 3
        );

        // Train the model once
        model.Train(
            ratings,
            epochs: 1,
            learningRate: 0.01f
        );

        float initialMse = CalculateMse(model, ratings);

        // Continue training the same model
        model.Train(
            ratings,
            epochs: 100,
            learningRate: 0.01f
        );

        float finalMse = CalculateMse(model, ratings);

        // Assert
        Assert.True(
            finalMse < initialMse,
            $"Expected MSE to decrease, but got {initialMse} -> {finalMse}"
        );
    }

    private static float CalculateMse(
        MatrixFactorizationModel model,
        List<RatingObservation> ratings)
    {
        float totalSquaredError = 0f;

        foreach (var rating in ratings)
        {
            float prediction = model.Predict(
                rating.UserId,
                rating.MediaId
            );

            float error = rating.Rating - prediction;

            totalSquaredError += error * error;
        }

        return totalSquaredError / ratings.Count;
    }
}
