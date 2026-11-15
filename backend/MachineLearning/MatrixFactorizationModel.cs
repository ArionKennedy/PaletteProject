using Palette.Api.MachineLearning.LinearAlgebra;
namespace Palette.Api.MachineLearning;

public class MatrixFactorizationModel
{
    private const float VECTORMAX = 0.01f;
    
    // The count of numbers that represent each user and movie
    private readonly int _factorCount;

    // Mapping User ID and Media ID to their respective vectors
    private readonly Dictionary<Guid, float[]> _userFactors = new();
    private readonly Dictionary<Guid, float[]> _movieFactors = new();

    // Fixed seed for reproduced outputs
    private readonly Random _random = new(42);

    public MatrixFactorizationModel(int factorCount = 8)
    {
        if (factorCount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(factorCount),
                "Factor count must be positive."
            );
        }

        _factorCount = factorCount;
    }

    // This creates an array of randmized numbers 
    // to avoid identical representations for each 
    // user and movie
    private float[] CreateRandomVector()
    {
        var vector = new float[_factorCount];

        for (int i = 0; i < _factorCount; i++)
        {
            vector[i] = (float)(_random.NextDouble() * VECTORMAX);
        }

        return vector;
    }

    // This checks if a vector exists for an established user
    // Otherwise it creates a new random one
    private float[] GetOrCreateUserFactors(Guid userId)
    {
        if (!_userFactors.TryGetValue(userId, out var factors))
        {
            factors = CreateRandomVector();
            _userFactors[userId] = factors;
        }

        return factors;
    }

    // This checks if a vector exists for an established movie
    // Otherwise it creates a new random one
    private float[] GetOrCreateMovieFactors(Guid mediaId)
    {
        if (!_movieFactors.TryGetValue(mediaId, out var factors))
        {
            factors = CreateRandomVector();
            _movieFactors[mediaId] = factors;
        }

        return factors;
    }

    // This will attempt to predict a rating for a user and a movie
    public float Predict(Guid userId, Guid mediaId)
    {
        // Check if the user has been trained yet.
        if (!_userFactors.TryGetValue(userId, out var userFactors))
        {
            throw new InvalidOperationException(
                "User has not been trained."
            );
        }

        // Check if the movie has been trained yet.
        if (!_movieFactors.TryGetValue(mediaId, out var movieFactors))
        {
            throw new InvalidOperationException(
                "Movie has not been trained."
            );
        }

        // Output the dot product as the prediction
        return VectorMath.DotProduct(userFactors, movieFactors);
    }

    // This determines the deviation between the actual and predicted rating
    private static float CalculateError(float actualRating, float predictedRating)
    {
        return actualRating - predictedRating;
    }
    
    public void Train(IReadOnlyList<RatingObservation> ratings,
        int epochs = 100,
        float learningRate = 0.01f,
        float regularization = 0.02f)
    {
        if (ratings.Count == 0)
            throw new ArgumentException("No training ratings provided.");

        if (epochs <= 0)
            throw new ArgumentOutOfRangeException(nameof(epochs));

        if (learningRate <= 0)
            throw new ArgumentOutOfRangeException(nameof(learningRate));

        if (regularization < 0)
            throw new ArgumentOutOfRangeException(nameof(regularization));

        // Shuffle observations during each epoch.
        var shuffledRatings = ratings.ToArray();

        // For every epoch, we shuffle the rating and
        // update the user and movie factors based on the
        // error between the actual and predicted rating
        for (int epoch = 0; epoch < epochs; epoch++)
        {
            _random.Shuffle(shuffledRatings);

            foreach (var rating in shuffledRatings)
            {
                // Ratings must be within range
                if (rating.Rating < 1 || rating.Rating > 5)
                    throw new ArgumentOutOfRangeException(
                        nameof(ratings), "Ratings must be between 1 and 5.");

                // Get the factors for the user and moive
                float[] user = GetOrCreateUserFactors(rating.UserId);
                float[] movie = GetOrCreateMovieFactors(rating.MediaId);

                // Get the dot product and calculate the error (Actual - Predicted)
                float predictionProduct = VectorMath.DotProduct(user, movie);
                float error = CalculateError(rating.Rating, predictionProduct);

                // Update the user and movie factors based on the error
                for (int k = 0; k < _factorCount; k++)
                {
                    // Preserve the old values for simultaneous updates.
                    float oldUser = user[k];
                    float oldMovie = movie[k];

                    user[k] += learningRate *
                        (error * oldMovie - regularization * oldUser);

                    movie[k] += learningRate *
                        (error * oldUser - regularization * oldMovie);
                }
            }
        }
    }


}
