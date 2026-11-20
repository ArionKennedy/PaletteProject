
using Palette.Api.MachineLearning;
using Palette.Api.MachineLearning.Evaluation;

namespace Palette.Tests.ModelEvaluatorTests;

public class ModelEvaluatorTests
{
    // Creates rating observations from the supplied values.
    private static List<RatingObservation> CreateRatings(params float[] values)
    {
        return values.Select(value =>
            new RatingObservation(Guid.NewGuid(), Guid.NewGuid(), value)).ToList();
    }

    // Verifies that empty rating datasets are rejected.
    [Fact]
    public void CalculateRmseEmptyRatingsThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            ModelEvaluator.CalculateRmse([], (_, _) => 3f));
    }

    // Verifies that inaccurate predictions produce the expected RMSE.
    [Fact]
    public void CalculateRmseInaccuratePredictionsReturnsExpectedError()
    {
        double rmse = ModelEvaluator.CalculateRmse(
            CreateRatings(4f, 2f, 5f), (_, _) => 3f);

        Assert.Equal(Math.Sqrt(2), rmse, 6);
    }

    // Verifies that perfect predictions produce zero RMSE.
    [Fact]
    public void CalculateRmsePerfectPredictionsReturnsZero()
    {
        var ratings = CreateRatings(4f, 2f, 5f);

        double rmse = ModelEvaluator.CalculateRmse(ratings, (userId, mediaId) =>
            ratings.Single(r => r.UserId == userId &&
                r.MediaId == mediaId).Rating);

        Assert.Equal(0.0, rmse);
    }
}