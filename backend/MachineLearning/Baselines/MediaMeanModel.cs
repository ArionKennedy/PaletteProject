using System;
using System.Collections.Generic;

namespace Palette.Api.MachineLearning.Baselines;

public sealed class MediaMeanModel
{
    private Dictionary<Guid, float> _mediaMeans = new();

    private readonly GlobalMeanModel _globalMeanModel = new();

    private bool _isTrained;

    public void Train(
        IReadOnlyList<RatingObservation> ratings)
    {
        _globalMeanModel.Train(ratings);

        var mediaTotals = CalculateMediaTotals(ratings);

        _mediaMeans = CalculateMediaMeans(mediaTotals);

        _isTrained = true;
    }

    public float Predict(Guid userId, Guid mediaId)
    {
        if (!_isTrained)
        {
            throw new InvalidOperationException(
                "The model has not been trained.");
        }

        if (_mediaMeans.TryGetValue(mediaId, out float mean))
        {
            return mean;
        }

        return _globalMeanModel.Predict(userId, mediaId);
    }

    // Accumulates the rating sum and count for each media item.
    private static Dictionary<Guid, (double Sum, int Count)>
        CalculateMediaTotals(
            IReadOnlyList<RatingObservation> ratings)
    {
        var totals =
            new Dictionary<Guid, (double Sum, int Count)>();

        foreach (var rating in ratings)
        {
            totals.TryGetValue(rating.MediaId, out var current);

            totals[rating.MediaId] = (
                current.Sum + rating.Rating,
                current.Count + 1
            );
        }

        return totals;
    }

    // Calculates the average rating for each media item.
    private static Dictionary<Guid, float> CalculateMediaMeans(
        Dictionary<Guid, (double Sum, int Count)> totals)
    {
        var means = new Dictionary<Guid, float>();

        foreach (var entry in totals)
        {
            means[entry.Key] =
                (float)(entry.Value.Sum / entry.Value.Count);
        }

        return means;
    }
}