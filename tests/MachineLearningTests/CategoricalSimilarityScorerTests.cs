
using System;
using System.Collections.Generic;
using Palette.Api.MachineLearning.ContentBased;
using Xunit;

public class CategoricalSimilarityScorerTests
{
    // Creates a dictionary containing the specified feature weights.
    private static Dictionary<string, float> CreatePreferences(
        params (string Feature, float Weight)[] entries)
    {
        var preferences = new Dictionary<string, float>();

        foreach (var (feature, weight) in entries)
            preferences.Add(feature, weight);

        return preferences;
    }

    // Mathematical accuracy

    // Verifies that identical single features produce a score of one.
    [Fact]
    public void ScoreIdenticalSingleFeatureReturnsOne()
    {
        var preferences = CreatePreferences(("Action", 1f));

        Assert.Equal(1f, CategoricalSimilarityScorer.Score(preferences, ["Action"]), 6);
    }

    // Verifies that identical sets of features produce a score of one.
    [Fact]
    public void ScoreIdenticalMultipleFeaturesReturnsOne()
    {
        var preferences = CreatePreferences(
            ("Action", 1f), ("Adventure", 1f), ("Drama", 1f));

        Assert.Equal(1f, CategoricalSimilarityScorer.Score(
            preferences, ["Action", "Adventure", "Drama"]), 6);
    }

    // Verifies that unrelated features produce a score of zero.
    [Fact]
    public void ScoreUnrelatedFeaturesReturnsZero()
    {
        var preferences = CreatePreferences(("Action", 3f), ("Adventure", 2f));

        Assert.Equal(0f, CategoricalSimilarityScorer.Score(
            preferences, ["Romance", "Drama"]), 6);
    }

    // Verifies that partially matching features produce the expected similarity.
    [Fact]
    public void ScorePartiallyMatchingFeaturesReturnsExpectedResult()
    {
        var preferences = CreatePreferences(("Action", 1f), ("Drama", 1f));

        Assert.Equal(0.5f, CategoricalSimilarityScorer.Score(
            preferences, ["Action", "Adventure"]), 6);
    }

    // Verifies that weighted preferences produce the expected cosine similarity.
    [Fact]
    public void ScoreWeightedPreferencesReturnsExpectedResult()
    {
        var preferences = CreatePreferences(
            ("Science Fiction", 2f), ("Adventure", 2f),
            ("Action", 1f), ("Drama", 1f));

        Assert.Equal(5f / MathF.Sqrt(30f), CategoricalSimilarityScorer.Score(
            preferences, ["Science Fiction", "Adventure", "Action"]), 6);
    }

    // Verifies that more heavily weighted matching features rank higher.
    [Fact]
    public void ScoreHigherWeightedFeatureRanksHigher()
    {
        var preferences = CreatePreferences(("Action", 5f), ("Romance", 1f));

        float action = CategoricalSimilarityScorer.Score(preferences, ["Action"]);
        float romance = CategoricalSimilarityScorer.Score(preferences, ["Romance"]);

        Assert.True(action > romance);
    }

    // Verifies that additional unmatched candidate features reduce similarity.
    [Fact]
    public void ScoreUnknownCandidateFeatureReducesSimilarity()
    {
        var preferences = CreatePreferences(("Action", 1f));

        float exact = CategoricalSimilarityScorer.Score(preferences, ["Action"]);
        float partial = CategoricalSimilarityScorer.Score(
            preferences, ["Action", "Romance"]);

        Assert.True(exact > partial);
        Assert.Equal(1f / MathF.Sqrt(2f), partial, 6);
    }

    // Verifies that unmatched user preferences reduce similarity.
    [Fact]
    public void ScoreUnmatchedPreferenceReducesSimilarity()
    {
        var preferences = CreatePreferences(("Action", 1f), ("Adventure", 1f));

        Assert.Equal(1f / MathF.Sqrt(2f), CategoricalSimilarityScorer.Score(
            preferences, ["Action"]), 6);
    }

    // Verifies that fractional preference weights are supported.
    [Fact]
    public void ScoreFractionalWeightsReturnsExpectedResult()
    {
        var preferences = CreatePreferences(("Action", 0.3f), ("Drama", 0.4f));

        Assert.Equal(0.8f, CategoricalSimilarityScorer.Score(preferences, ["Drama"]), 6);
    }

    // Empty inputs and zero weights

    // Verifies that empty preferences produce a score of zero.
    [Fact]
    public void ScoreEmptyPreferencesReturnsZero()
    {
        Assert.Equal(0f, CategoricalSimilarityScorer.Score(
            CreatePreferences(), ["Action"]));
    }

    // Verifies that empty candidate features produce a score of zero.
    [Fact]
    public void ScoreEmptyFeaturesReturnsZero()
    {
        var preferences = CreatePreferences(("Action", 1f));

        Assert.Equal(0f, CategoricalSimilarityScorer.Score(preferences, []));
    }

    // Verifies that two empty inputs produce a score of zero.
    [Fact]
    public void ScoreBothInputsEmptyReturnsZero()
    {
        Assert.Equal(0f, CategoricalSimilarityScorer.Score(CreatePreferences(), []));
    }

    // Verifies that exclusively zero-weight preferences produce a score of zero.
    [Fact]
    public void ScoreAllZeroWeightsReturnsZero()
    {
        var preferences = CreatePreferences(("Action", 0f), ("Drama", 0f));

        Assert.Equal(0f, CategoricalSimilarityScorer.Score(
            preferences, ["Action", "Drama"]));
    }

    // Verifies that matching a zero-weight preference produces a score of zero.
    [Fact]
    public void ScoreMatchingZeroWeightFeatureReturnsZero()
    {
        var preferences = CreatePreferences(("Action", 5f), ("Drama", 0f));

        Assert.Equal(0f, CategoricalSimilarityScorer.Score(preferences, ["Drama"]));
    }

    // Feature normalization

    // Verifies that feature matching is case insensitive.
    [Fact]
    public void ScoreDifferentCapitalizationStillMatches()
    {
        var preferences = CreatePreferences(("ACTION", 1f));

        Assert.Equal(1f, CategoricalSimilarityScorer.Score(preferences, ["action"]), 6);
    }

    // Verifies that surrounding whitespace does not affect feature matching.
    [Fact]
    public void ScoreExtraWhitespaceStillMatches()
    {
        var preferences = CreatePreferences(("  Action  ", 1f));

        Assert.Equal(1f, CategoricalSimilarityScorer.Score(preferences, [" Action "]), 6);
    }

    // Verifies that duplicate candidate features do not increase their weight.
    [Fact]
    public void ScoreDuplicateFeaturesDoesNotIncreaseWeight()
    {
        var preferences = CreatePreferences(("Action", 1f), ("Drama", 1f));

        float original = CategoricalSimilarityScorer.Score(
            preferences, ["Action", "Drama"]);
        float duplicate = CategoricalSimilarityScorer.Score(
            preferences, ["Action", "Action", "ACTION", "Drama"]);

        Assert.Equal(original, duplicate, 6);
    }

    // Verifies that equivalent normalized preferences combine their weights.
    [Fact]
    public void ScoreDuplicateNormalizedPreferencesCombinesWeights()
    {
        var preferences = CreatePreferences(
            ("Action", 1f), ("ACTION", 2f), (" Action ", 3f), ("Drama", 2f));
        var combined = CreatePreferences(("Action", 6f), ("Drama", 2f));

        float first = CategoricalSimilarityScorer.Score(preferences, ["Action"]);
        float second = CategoricalSimilarityScorer.Score(combined, ["Action"]);

        Assert.Equal(second, first, 6);
    }

    // Verifies that blank candidate features are ignored.
    [Fact]
    public void ScoreBlankFeaturesAreIgnored()
    {
        var preferences = CreatePreferences(("Action", 1f));

        Assert.Equal(1f, CategoricalSimilarityScorer.Score(
            preferences, ["Action", "", " ", "\t"]), 6);
    }

    // Verifies that blank preference keys are ignored.
    [Fact]
    public void ScoreBlankPreferenceKeysAreIgnored()
    {
        var preferences = CreatePreferences(("Action", 1f), ("", 3f), (" ", 4f));

        Assert.Equal(1f, CategoricalSimilarityScorer.Score(preferences, ["Action"]), 6);
    }

    // Input validation

    // Verifies that null preferences are rejected.
    [Fact]
    public void ScoreNullPreferencesThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            CategoricalSimilarityScorer.Score(null!, ["Action"]));
    }

    // Verifies that null candidate features are rejected.
    [Fact]
    public void ScoreNullFeaturesThrowsArgumentNullException()
    {
        var preferences = CreatePreferences(("Action", 1f));

        Assert.Throws<ArgumentNullException>(() =>
            CategoricalSimilarityScorer.Score(preferences, null!));
    }

    // Verifies that negative and nonfinite preference weights are rejected.
    [Theory]
    [InlineData(-1f)]
    [InlineData(-0.01f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    public void ScoreInvalidWeightThrowsArgumentException(float weight)
    {
        var preferences = CreatePreferences(("Action", weight));

        Assert.Throws<ArgumentException>(() =>
            CategoricalSimilarityScorer.Score(preferences, ["Action"]));
    }

    // Algorithm properties

    // Verifies that proportionally scaling preferences preserves similarity.
    [Fact]
    public void ScoreScaledPreferencesReturnsSameResult()
    {
        var original = CreatePreferences(("Action", 2f), ("Drama", 3f));
        var scaled = CreatePreferences(("Action", 20f), ("Drama", 30f));

        Assert.Equal(
            CategoricalSimilarityScorer.Score(original, ["Action"]),
            CategoricalSimilarityScorer.Score(scaled, ["Action"]), 6);
    }

    // Verifies that candidate feature ordering does not affect similarity.
    [Fact]
    public void ScoreReorderedFeaturesReturnsSameResult()
    {
        var preferences = CreatePreferences(
            ("Action", 3f), ("Drama", 2f), ("Adventure", 1f));

        Assert.Equal(
            CategoricalSimilarityScorer.Score(preferences, ["Action", "Drama", "Adventure"]),
            CategoricalSimilarityScorer.Score(preferences, ["Adventure", "Action", "Drama"]), 6);
    }

    // Verifies that 100 repeated calls produce identical scores.
    [Fact]
    public void ScoreRepeatedCallsReturnsSameResult()
    {
        var preferences = CreatePreferences(("Action", 3f), ("Drama", 2f));
        string[] features = ["Action", "Adventure"];

        float expected = CategoricalSimilarityScorer.Score(preferences, features);

        for (int i = 0; i < 100; i++)
            Assert.Equal(expected, CategoricalSimilarityScorer.Score(preferences, features));
    }

    // Verifies that scoring does not modify preferences or candidate features.
    [Fact]
    public void ScoreDoesNotModifyInputs()
    {
        var preferences = CreatePreferences((" Action ", 3f), ("DRAMA", 2f));
        string[] features = ["action", "Drama"];

        var originalPreferences = new Dictionary<string, float>(preferences);
        string[] originalFeatures = (string[])features.Clone();

        CategoricalSimilarityScorer.Score(preferences, features);

        Assert.Equal(originalPreferences.Count, preferences.Count);

        foreach (var preference in originalPreferences)
        {
            Assert.True(preferences.ContainsKey(preference.Key));
            Assert.Equal(preference.Value, preferences[preference.Key]);
        }

        Assert.Equal(originalFeatures, features);
    }

    // Verifies that 1,000 randomized inputs produce finite scores within [0, 1].
    [Fact]
    public void ScoreRandomPreferencesRemainsInRange()
    {
        var random = new Random(42);

        string[] vocabulary =
        [
            "Action", "Drama", "Adventure", "Romance",
            "Comedy", "Horror", "Science Fiction", "Fantasy"
        ];

        for (int iteration = 0; iteration < 1000; iteration++)
        {
            var preferences = new Dictionary<string, float>();
            var features = new List<string>();

            foreach (string feature in vocabulary)
            {
                preferences[feature] = (float)(random.NextDouble() * 10);

                if (random.NextDouble() > 0.5)
                    features.Add(feature);
            }

            float result = CategoricalSimilarityScorer.Score(preferences, features);

            Assert.True(float.IsFinite(result));
            Assert.InRange(result, 0f, 1f);
        }
    }
}
