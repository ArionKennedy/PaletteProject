using System;
using System.Collections.Generic;

namespace Palette.Api.MachineLearning.Baselines;

public sealed class GlobalMeanModel
{
    private float _globalMean;
    private bool _isTrained;

    // Trains the model using the mean of all valid ratings.
    public void Train(
        IReadOnlyList<RatingObservation> ratings)
    {
        ValidateRatings(ratings);

        double sum = 0;

        foreach (var rating in ratings)
        {
            sum += rating.Rating;
        }

        _globalMean = (float)(sum / ratings.Count);
        _isTrained = true;
    }

    // Returns the global mean for any user and media pair.
    public float Predict(Guid userId, Guid mediaId)
    {
        if (!_isTrained)
        {
            throw new InvalidOperationException(
                "The model has not been trained.");
        }

        return _globalMean;
    }

    // Rejects missing, empty, or out-of-range rating data.
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