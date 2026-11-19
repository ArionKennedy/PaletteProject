using Palette.Api.MachineLearning;
using Palette.Api.MachineLearning.Evaluation;

namespace Palette.Tests.ModelEvaluatorTests;

public class ModelEvaluatorTests
{
    [Fact]
    public void CalculateRmseEmptyRatingsThrowsArgumentException()
    {
        // Empty ratings list
        var ratings = new List<RatingObservation>();

        // Act & Assert if 
        Assert.Throws<ArgumentException>(() =>
            ModelEvaluator.CalculateRmse(ratings, (_, _) => 3f));
    }

    [Fact]
    public void CalculateRmseInaccuratePredictionsReturnsExpectedError()
    {
        // Active rating list
        var ratings = new List<RatingObservation>
        {
            new(Guid.NewGuid(), Guid.NewGuid(), 4f),
            new(Guid.NewGuid(), Guid.NewGuid(), 2f),
            new(Guid.NewGuid(), Guid.NewGuid(), 5f)
        };

        // Inaccurate prediction function that always returns 3
        double rmse = ModelEvaluator.CalculateRmse(
            ratings, (_, _) => 3f);

        // Assert if the RMSE is approximately equal to sqrt(2) for the given ratings and predictions
        Assert.Equal(Math.Sqrt(2), rmse, 6);
    }

    [Fact]
    public void CalculateRmsePerfectPredictionsReturnsZero()
    {
        // Active rating list
        var ratings = new List<RatingObservation>
        {
            new(Guid.NewGuid(), Guid.NewGuid(), 4f),
            new(Guid.NewGuid(), Guid.NewGuid(), 2f),
            new(Guid.NewGuid(), Guid.NewGuid(), 5f)
        };

        // Prediction function that returns the actual rating for each observation
        Func<Guid, Guid, float> predict = (userId, mediaId) =>
            ratings.Single(r =>
                r.UserId == userId &&
                r.MediaId == mediaId).Rating;

        // Accumulate RMSE using the ModelEvaluator
        double rmse = ModelEvaluator.CalculateRmse(ratings, predict);

        // Assert if the RMSE is zero for perfect predictions
        Assert.Equal(0.0, rmse);
    }
}