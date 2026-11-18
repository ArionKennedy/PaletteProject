namespace Palette.Api.MachineLearning.Evaluation;

public static class ModelEvaluator
{

    // WIP, not done
    // public static double CalculateRMSEForAllAttr(int input, int[] actual, int[] predicted)
    // {
    //     const int power = 2;
    //     int result = 0;

    //     if (actual.Length != predicted.Length)
    //         throw new ArgumentException("Arrays must have the same length");

    //     for (int value = 0; value < actual.Length; value++)
    //     {
    //         result += Math.Pow(actual[value] - predicted[value], power);
    //     }

    //     return Math.Sqrt((1/actual.Length)*(result));
    // }

    // Test Calculation Recommendation
    public static double CalculateRmse(IReadOnlyList<RatingObservation> ratings, Func<Guid, Guid, float> predict)
    {
        if (ratings.Count == 0)
        {
            throw new ArgumentException("Ratings cannot be empty.", nameof(ratings));
        }

        double squaredErrorSum = 0;

        foreach (var rating in ratings)
        {
            float prediction = predict(rating.UserId,rating.MediaId);

            double error = rating.Rating - prediction;
            squaredErrorSum += error * error;
        }

        return Math.Sqrt(squaredErrorSum / ratings.Count);
    }

}
