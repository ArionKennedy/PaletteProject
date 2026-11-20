using System;
using System.Collections.Generic;
using System.Linq;
using Palette.Api.MachineLearning.LinearAlgebra;

namespace Palette.Api.MachineLearning;

public sealed class MatrixFactorizationModel
{
    private readonly int _factorCount;

    private readonly Dictionary<Guid, float[]> _userFactors = new();
    private readonly Dictionary<Guid, float[]> _movieFactors = new();

    private readonly Random _random = new(42);

    // Creates a model with the requested number of latent factors.
    public MatrixFactorizationModel(int factorCount = 8)
    {
        if (factorCount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(factorCount),
                "Factor count must be greater than zero.");
        }

        _factorCount = factorCount;
    }

    // Predicts a rating from the trained user and media factor vectors.
    public float Predict(Guid userId, Guid mediaId)
    {
        if (!_userFactors.TryGetValue(userId, out var user))
        {
            throw new InvalidOperationException(
                $"Unknown user: {userId}");
        }

        if (!_movieFactors.TryGetValue(mediaId, out var movie))
        {
            throw new InvalidOperationException(
                $"Unknown media: {mediaId}");
        }

        return VectorMath.DotProduct(user, movie);
    }

    // Trains with stochastic gradient descent after validating all input data.
    public void Train(
        IReadOnlyList<RatingObservation> ratings,
        int epochs = 100,
        float learningRate = 0.01f,
        float regularization = 0.02f)
    {
        ValidateTrainingParameters(
            epochs, learningRate, regularization);

        ValidateRatings(ratings);

        var shuffledRatings = ratings.ToArray();

        for (int epoch = 0; epoch < epochs; epoch++)
        {
            _random.Shuffle(shuffledRatings);

            foreach (var rating in shuffledRatings)
            {
                TrainObservation(
                    rating,
                    learningRate,
                    regularization);
            }
        }
    }

    // Updates the latent factors for one rating observation.
    private void TrainObservation(
        RatingObservation rating,
        float learningRate,
        float regularization)
    {
        var user = GetOrCreateFactors(
            _userFactors, rating.UserId);

        var movie = GetOrCreateFactors(
            _movieFactors, rating.MediaId);

        float prediction = VectorMath.DotProduct(user, movie);
        float error = rating.Rating - prediction;

        UpdateFactors(
            user,
            movie,
            error,
            learningRate,
            regularization);
    }

    // Applies one regularized gradient step to both factor vectors.
    private void UpdateFactors(
        float[] user,
        float[] movie,
        float error,
        float learningRate,
        float regularization)
    {
        for (int k = 0; k < _factorCount; k++)
        {
            float oldUser = user[k];
            float oldMovie = movie[k];

            user[k] += learningRate *
                (error * oldMovie - regularization * oldUser);

            movie[k] += learningRate *
                (error * oldUser - regularization * oldMovie);
        }
    }

    // Returns an entity's latent vector, initializing it when first seen.
    private float[] GetOrCreateFactors(
        Dictionary<Guid, float[]> factors,
        Guid id)
    {
        if (!factors.TryGetValue(id, out var vector))
        {
            vector = CreateRandomVector();
            factors[id] = vector;
        }

        return vector;
    }

    // Creates a latent vector initialized with small random values.
    private float[] CreateRandomVector()
    {
        var vector = new float[_factorCount];

        for (int k = 0; k < _factorCount; k++)
        {
            vector[k] = (float)(_random.NextDouble() * 0.1);
        }

        return vector;
    }

    // Rejects invalid epoch, learning-rate, and regularization values.
    private static void ValidateTrainingParameters(
        int epochs,
        float learningRate,
        float regularization)
    {
        if (epochs <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(epochs),
                "Epochs must be greater than zero.");
        }

        if (!float.IsFinite(learningRate) ||
            learningRate <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(learningRate),
                "Learning rate must be finite and greater than zero.");
        }

        if (!float.IsFinite(regularization) ||
            regularization < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(regularization),
                "Regularization must be finite and non-negative.");
        }
    }

    // Rejects missing, empty, or out-of-range ratings before training.
    private static void ValidateRatings(
        IReadOnlyList<RatingObservation> ratings)
    {
        ArgumentNullException.ThrowIfNull(ratings);

        if (ratings.Count == 0)
        {
            throw new ArgumentException(
                "Ratings cannot be empty.",
                nameof(ratings));
        }

        foreach (var rating in ratings)
        {
            if (!float.IsFinite(rating.Rating) ||
                rating.Rating < 1f ||
                rating.Rating > 5f)
            {
                throw new ArgumentException(
                    "Ratings must be finite and between 1 and 5.",
                    nameof(ratings));
            }
        }
    }
}