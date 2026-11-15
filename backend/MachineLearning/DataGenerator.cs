using Palette.Api.MachineLearning.LinearAlgebra;
namespace Palette.Api.MachineLearning;

public static class DataGenerator
{
    public static List<RatingObservation> Generate(
        int userCount = 100,
        int movieCount = 200,
        int ratingsPerUser = 30,
        int seed = 42)
    {
        if (userCount <= 0 || movieCount <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(userCount),
                "User and movie counts must be positive.");

        if (ratingsPerUser < 1 || ratingsPerUser > movieCount)
            throw new ArgumentOutOfRangeException(
                nameof(ratingsPerUser));

        var random = new Random(seed);

        var userIds = Enumerable.Range(0, userCount)
            .Select(_ => Guid.NewGuid())
            .ToArray();

        var movieIds = Enumerable.Range(0, movieCount)
            .Select(_ => Guid.NewGuid())
            .ToArray();

        // Hidden preference vectors used only by the generator.
        const int hiddenFactors = 3;

        float[][] userPreferences = Enumerable.Range(0, userCount)
            .Select(_ => CreatePreferenceVector(random, hiddenFactors))
            .ToArray();

        float[][] movieCharacteristics = Enumerable.Range(0, movieCount)
            .Select(_ => CreatePreferenceVector(random, hiddenFactors))
            .ToArray();

        var ratings = new List<RatingObservation>();

        for (int u = 0; u < userCount; u++)
        {
            // Each user rates a distinct random set of movies.
            int[] selectedMovies = Enumerable.Range(0, movieCount)
                .OrderBy(_ => random.Next())
                .Take(ratingsPerUser)
                .ToArray();

            foreach (int movieIndex in selectedMovies)
            {
                float similarity = VectorMath.DotProduct(
                    userPreferences[u],
                    movieCharacteristics[movieIndex]);

                // Transform similarity into a rating.
                float rawRating = 1f + 4f * similarity;

                // Add a small amount of random noise.
                rawRating += (float)(random.NextDouble() - 0.5) * 0.5f;

                float rating = System.Math.Clamp(
                    MathF.Round(rawRating),
                    1f,
                    5f
                );

                ratings.Add(new RatingObservation(
                    userIds[u],
                    movieIds[movieIndex],
                    rating));
            }
        }

        return ratings;
    }

    private static float[] CreatePreferenceVector(
        Random random,
        int factorCount)
    {
        var vector = new float[factorCount];

        float sum = 0f;

        for (int i = 0; i < factorCount; i++)
        {
            vector[i] = (float)random.NextDouble();
            sum += vector[i];
        }

        for (int i = 0; i < factorCount; i++)
        {
            vector[i] /= sum;
        }

        return vector;
    }
}
