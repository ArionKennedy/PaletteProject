using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;

namespace Palette.Api.MachineLearning.ContentBased;

public static class UserProfileBuilder
{
    public static UserPreferenceProfile Build(
        Guid userId,
        IReadOnlyList<WeightedMediaInteraction> interactions)
    {
        ArgumentNullException.ThrowIfNull(interactions);

        var genres = CreatePreferenceDictionary();
        var authors = CreatePreferenceDictionary();
        var directors = CreatePreferenceDictionary();
        var studios = CreatePreferenceDictionary();
        var publishers = CreatePreferenceDictionary();

        double runtimeSum = 0;
        double runtimeWeight = 0;

        double pageCountSum = 0;
        double pageCountWeight = 0;

        double seasonSum = 0;
        double seasonWeight = 0;

        foreach (var interaction in interactions)
        {
            ArgumentNullException.ThrowIfNull(interaction);
            ArgumentNullException.ThrowIfNull(interaction.Media);

            float weight = interaction.Weight;

            if (!float.IsFinite(weight) || weight < 0f)
            {
                throw new ArgumentException(
                    "Interaction weights must be finite and non-negative.",
                    nameof(interactions));
            }

            if (weight == 0f)
            {
                continue;
            }

            MediaFeatures media = interaction.Media;

            AddPreferences(genres, media.Genres, weight);
            AddPreferences(authors, media.Authors, weight);
            AddPreferences(directors, media.Directors, weight);
            AddPreferences(studios, media.Studios, weight);
            AddPreferences(publishers, media.Publishers, weight);

            switch (media.MediaType.Trim().ToLowerInvariant())
            {
                case "movie":
                    AccumulateLength(
                        media.RuntimeMinutes,
                        weight,
                        ref runtimeSum,
                        ref runtimeWeight);
                    break;

                case "book":
                    AccumulateLength(
                        media.PageCount,
                        weight,
                        ref pageCountSum,
                        ref pageCountWeight);
                    break;

                case "tv":
                    AccumulateLength(
                        media.NumberOfSeasons,
                        weight,
                        ref seasonSum,
                        ref seasonWeight);
                    break;
            }
        }

        return new UserPreferenceProfile(
            UserId: userId,
            GenrePreferences: new ReadOnlyDictionary<string, float>(genres),
            AuthorPreferences: new ReadOnlyDictionary<string, float>(authors),
            DirectorPreferences: new ReadOnlyDictionary<string, float>(directors),
            StudioPreferences: new ReadOnlyDictionary<string, float>(studios),
            PublisherPreferences: new ReadOnlyDictionary<string, float>(publishers),
            PreferredRuntimeMinutes:
                CalculateWeightedAverage(runtimeSum, runtimeWeight),
            PreferredPageCount:
                CalculateWeightedAverage(pageCountSum, pageCountWeight),
            PreferredNumberOfSeasons:
                CalculateWeightedAverage(seasonSum, seasonWeight)
        );
    }

    private static Dictionary<string, float>
        CreatePreferenceDictionary()
    {
        return new Dictionary<string, float>(
            StringComparer.OrdinalIgnoreCase);
    }

    private static void AddPreferences(
        Dictionary<string, float> preferences,
        IReadOnlyList<string> features,
        float weight)
    {
        ArgumentNullException.ThrowIfNull(features);

        var uniqueFeatures = features
            .Where(feature =>
                !string.IsNullOrWhiteSpace(feature))
            .Select(feature => feature.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (string feature in uniqueFeatures)
        {
            float updatedWeight =
                preferences.GetValueOrDefault(feature) + weight;

            if (!float.IsFinite(updatedWeight))
            {
                throw new OverflowException(
                    "Accumulated preference weight is too large.");
            }

            preferences[feature] = updatedWeight;
        }
    }

    private static void AccumulateLength(
        int? length,
        float weight,
        ref double weightedSum,
        ref double totalWeight)
    {
        if (!length.HasValue || length.Value <= 0)
        {
            return;
        }

        weightedSum += (double)length.Value * weight;
        totalWeight += weight;
    }

    private static float? CalculateWeightedAverage(
        double weightedSum,
        double totalWeight)
    {
        if (totalWeight == 0)
        {
            return null;
        }

        return (float)(weightedSum / totalWeight);
    }
}
