using System;
using System.Collections.Generic;
using System.Linq;
using Palette.Api.MachineLearning.LinearAlgebra;

namespace Palette.Api.MachineLearning.ContentBased;

public static class CategoricalSimilarityScorer
{
    public static float Score(
        IReadOnlyDictionary<string, float> preferences,
        IReadOnlyList<string> features)
    {
        ArgumentNullException.ThrowIfNull(preferences);
        ArgumentNullException.ThrowIfNull(features);

        var normalizedPreferences =
            new Dictionary<string, float>(
                StringComparer.OrdinalIgnoreCase);

        foreach (var preference in preferences)
        {
            if (!float.IsFinite(preference.Value)
                || preference.Value < 0f)
            {
                throw new ArgumentException(
                    "Preference weights must be finite and non-negative.");
            }

            if (string.IsNullOrWhiteSpace(preference.Key))
            {
                continue;
            }

            string key = preference.Key.Trim();

            normalizedPreferences[key] =
                normalizedPreferences.GetValueOrDefault(key)
                + preference.Value;
        }

        var featureSet = new HashSet<string>(
            features
                .Where(feature =>
                    !string.IsNullOrWhiteSpace(feature))
                .Select(feature => feature.Trim()),
            StringComparer.OrdinalIgnoreCase);

        var vocabulary = new HashSet<string>(
            normalizedPreferences.Keys,
            StringComparer.OrdinalIgnoreCase);

        vocabulary.UnionWith(featureSet);

        float[] userVector = new float[vocabulary.Count];
        float[] mediaVector = new float[vocabulary.Count];

        int index = 0;

        foreach (string feature in vocabulary)
        {
            userVector[index] =
                normalizedPreferences.GetValueOrDefault(feature);

            mediaVector[index] =
                featureSet.Contains(feature) ? 1f : 0f;

            index++;
        }

        return VectorMath.CosineSimilarity(
            userVector,
            mediaVector);
    }
}