
using System;
using System.Collections.Generic;
using System.Linq;

namespace Palette.Api.MachineLearning.ContentBased;

public sealed class ContentBasedModel
{
    private readonly ContentBasedWeights _weights;

    // Creates a content scorer with validated feature weights.
    public ContentBasedModel(ContentBasedWeights? weights = null)
    {
        _weights = weights ?? new ContentBasedWeights();

        ValidateWeights(_weights);
    }

    // Returns a weighted similarity score for a candidate and profile.
    public float Predict(
        UserPreferenceProfile profile,
        MediaFeatures candidate)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(candidate);

        double weightedSum = 0;
        double totalWeight = 0;

        AddCategoricalScore(
            profile.GenrePreferences,
            candidate.Genres,
            _weights.Genre,
            ref weightedSum,
            ref totalWeight);

        string mediaType =
            candidate.MediaType.Trim().ToLowerInvariant();

        switch (mediaType)
        {
            case "movie":
            case "tv":
                AddCategoricalScore(
                    profile.DirectorPreferences,
                    candidate.Directors,
                    _weights.Director,
                    ref weightedSum,
                    ref totalWeight);

                AddCategoricalScore(
                    profile.StudioPreferences,
                    candidate.Studios,
                    _weights.Studio,
                    ref weightedSum,
                    ref totalWeight);
                break;

            case "book":
                AddCategoricalScore(
                    profile.AuthorPreferences,
                    candidate.Authors,
                    _weights.Author,
                    ref weightedSum,
                    ref totalWeight);

                AddCategoricalScore(
                    profile.PublisherPreferences,
                    candidate.Publishers,
                    _weights.Publisher,
                    ref weightedSum,
                    ref totalWeight);
                break;

            case "game":
                AddCategoricalScore(
                    profile.StudioPreferences,
                    candidate.Studios,
                    _weights.Studio,
                    ref weightedSum,
                    ref totalWeight);
                break;
        }

        switch (mediaType)
        {
            case "movie":
                AddLengthScore(
                    profile.PreferredRuntimeMinutes,
                    candidate.RuntimeMinutes,
                    _weights.RuntimeTolerance,
                    _weights.Length,
                    ref weightedSum,
                    ref totalWeight);
                break;

            case "book":
                AddLengthScore(
                    profile.PreferredPageCount,
                    candidate.PageCount,
                    _weights.PageCountTolerance,
                    _weights.Length,
                    ref weightedSum,
                    ref totalWeight);
                break;

            case "tv":
                AddLengthScore(
                    profile.PreferredNumberOfSeasons,
                    candidate.NumberOfSeasons,
                    _weights.SeasonCountTolerance,
                    _weights.Length,
                    ref weightedSum,
                    ref totalWeight);
                break;
        }

        if (totalWeight == 0)
        {
            return 0f;
        }

        return (float)Math.Clamp(
            weightedSum / totalWeight, 0d, 1d);
    }

    // Adds a categorical similarity when both inputs provide evidence.
    private static void AddCategoricalScore(
        IReadOnlyDictionary<string, float> preferences,
        IReadOnlyList<string> features,
        float weight,
        ref double weightedSum,
        ref double totalWeight)
    {
        ArgumentNullException.ThrowIfNull(preferences);
        ArgumentNullException.ThrowIfNull(features);

        if (weight == 0f)
        {
            return;
        }

        bool hasPreferences = preferences.Any(
            preference =>
                !string.IsNullOrWhiteSpace(preference.Key)
                && preference.Value > 0f);

        bool hasFeatures = features.Any(
            feature => !string.IsNullOrWhiteSpace(feature));

        if (!hasPreferences || !hasFeatures)
        {
            return;
        }

        float similarity =
            CategoricalSimilarityScorer.Score(
                preferences, features);

        weightedSum += (double)weight * similarity;
        totalWeight += weight;
    }

    // Adds a length similarity when both lengths are available.
    private static void AddLengthScore(
        float? preferredLength,
        float? candidateLength,
        float tolerance,
        float weight,
        ref double weightedSum,
        ref double totalWeight)
    {
        if (weight == 0f ||
            !preferredLength.HasValue ||
            !candidateLength.HasValue)
        {
            return;
        }

        float similarity = LengthSimilarityScorer.Score(
            preferredLength,
            candidateLength,
            tolerance);

        weightedSum += (double)weight * similarity;
        totalWeight += weight;
    }

    // Ensures feature weights and length tolerances are valid.
    private static void ValidateWeights(
        ContentBasedWeights weights)
    {
        float[] featureWeights =
        {
            weights.Genre,
            weights.Author,
            weights.Director,
            weights.Studio,
            weights.Publisher,
            weights.Length
        };

        foreach (float weight in featureWeights)
        {
            if (!float.IsFinite(weight) || weight < 0f)
            {
                throw new ArgumentException(
                    "Feature weights must be finite and non-negative.",
                    nameof(weights));
            }
        }

        if (!featureWeights.Any(weight => weight > 0f))
        {
            throw new ArgumentException(
                "At least one feature weight must be positive.",
                nameof(weights));
        }

        float[] tolerances =
        {
            weights.RuntimeTolerance,
            weights.PageCountTolerance,
            weights.SeasonCountTolerance
        };

        foreach (float tolerance in tolerances)
        {
            if (!float.IsFinite(tolerance) || tolerance <= 0f)
            {
                throw new ArgumentException(
                    "Length tolerances must be finite and positive.",
                    nameof(weights));
            }
        }
    }
}
