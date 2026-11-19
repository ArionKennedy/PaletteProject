
using System;
using System.Collections.Generic;
using Palette.Api.MachineLearning.ContentBased;
using Xunit;

public class CategoricalSimilarityScorerTests
{
    // ==================================================
    // Mathematical Accuracy Tests
    // ==================================================

    [Fact]
    // Verifies that scoring with identical single feature returns one.
    public void ScoreIdenticalSingleFeatureReturnsOne()
    {
        var preferences = new Dictionary<string, float>
        {
            ["Action"] = 1f
        };

        string[] features = { "Action" };

        float result = CategoricalSimilarityScorer.Score(
            preferences, features);

        Assert.Equal(1f, result, 6);
    }

    [Fact]
    // Verifies that scoring with identical multiple features returns one.
    public void ScoreIdenticalMultipleFeaturesReturnsOne()
    {
        var preferences = new Dictionary<string, float>
        {
            ["Action"] = 1f,
            ["Adventure"] = 1f,
            ["Drama"] = 1f
        };

        string[] features = { "Action", "Adventure", "Drama" };

        float result = CategoricalSimilarityScorer.Score(
            preferences, features);

        Assert.Equal(1f, result, 6);
    }

    [Fact]
    // Verifies that scoring with unrelated features returns zero.
    public void ScoreUnrelatedFeaturesReturnsZero()
    {
        var preferences = new Dictionary<string, float>
        {
            ["Action"] = 3f,
            ["Adventure"] = 2f
        };

        string[] features = { "Romance", "Drama" };

        float result = CategoricalSimilarityScorer.Score(
            preferences, features);

        Assert.Equal(0f, result, 6);
    }

    [Fact]
    // Verifies that scoring with partially matching features returns the expected result.
    public void ScorePartiallyMatchingFeaturesReturnsExpectedResult()
    {
        var preferences = new Dictionary<string, float>
        {
            ["Action"] = 1f,
            ["Drama"] = 1f
        };

        string[] features = { "Action", "Adventure" };

        float result = CategoricalSimilarityScorer.Score(
            preferences, features);

        Assert.Equal(0.5f, result, 6);
    }

    [Fact]
    // Verifies that scoring with weighted preferences returns the expected result.
    public void ScoreWeightedPreferencesReturnsExpectedResult()
    {
        var preferences = new Dictionary<string, float>
        {
            ["Science Fiction"] = 2f,
            ["Adventure"] = 2f,
            ["Action"] = 1f,
            ["Drama"] = 1f
        };

        string[] features =
        {
            "Science Fiction", "Adventure", "Action"
        };

        float expected = 5f / MathF.Sqrt(30f);

        float result = CategoricalSimilarityScorer.Score(
            preferences, features);

        Assert.Equal(expected, result, 6);
    }

    [Fact]
    // Verifies that a more heavily weighted matching feature increases similarity.
    public void ScoreHigherWeightedFeatureRanksHigher()
    {
        var preferences = new Dictionary<string, float>
        {
            ["Action"] = 5f,
            ["Romance"] = 1f
        };

        float action = CategoricalSimilarityScorer.Score(
            preferences, new[] { "Action" });

        float romance = CategoricalSimilarityScorer.Score(
            preferences, new[] { "Romance" });

        Assert.True(action > romance);
    }

    [Fact]
    // Verifies that scoring with unknown candidate feature reduces similarity.
    public void ScoreUnknownCandidateFeatureReducesSimilarity()
    {
        var preferences = new Dictionary<string, float>
        {
            ["Action"] = 1f
        };

        float exact = CategoricalSimilarityScorer.Score(
            preferences, new[] { "Action" });

        float partial = CategoricalSimilarityScorer.Score(
            preferences, new[] { "Action", "Romance" });

        Assert.True(exact > partial);
        Assert.Equal(1f / MathF.Sqrt(2f), partial, 6);
    }

    [Fact]
    // Verifies that scoring with unmatched preference reduces similarity.
    public void ScoreUnmatchedPreferenceReducesSimilarity()
    {
        var preferences = new Dictionary<string, float>
        {
            ["Action"] = 1f,
            ["Adventure"] = 1f
        };

        float result = CategoricalSimilarityScorer.Score(
            preferences, new[] { "Action" });

        Assert.Equal(1f / MathF.Sqrt(2f), result, 6);
    }

    [Fact]
    // Verifies that scoring with fractional weights returns the expected result.
    public void ScoreFractionalWeightsReturnsExpectedResult()
    {
        var preferences = new Dictionary<string, float>
        {
            ["Action"] = 0.3f,
            ["Drama"] = 0.4f
        };

        float result = CategoricalSimilarityScorer.Score(
            preferences, new[] { "Drama" });

        Assert.Equal(0.8f, result, 6);
    }

    // ==================================================
    // Empty Inputs and Zero Weights
    // ==================================================

    [Fact]
    // Verifies that scoring with empty preferences returns zero.
    public void ScoreEmptyPreferencesReturnsZero()
    {
        var preferences = new Dictionary<string, float>();

        float result = CategoricalSimilarityScorer.Score(
            preferences, new[] { "Action" });

        Assert.Equal(0f, result);
    }

    [Fact]
    // Verifies that scoring with empty features returns zero.
    public void ScoreEmptyFeaturesReturnsZero()
    {
        var preferences = new Dictionary<string, float>
        {
            ["Action"] = 1f
        };

        float result = CategoricalSimilarityScorer.Score(
            preferences, Array.Empty<string>());

        Assert.Equal(0f, result);
    }

    [Fact]
    // Verifies that scoring with both inputs empty returns zero.
    public void ScoreBothInputsEmptyReturnsZero()
    {
        float result = CategoricalSimilarityScorer.Score(
            new Dictionary<string, float>(),
            Array.Empty<string>());

        Assert.Equal(0f, result);
    }

    [Fact]
    // Verifies that scoring with all zero weights returns zero.
    public void ScoreAllZeroWeightsReturnsZero()
    {
        var preferences = new Dictionary<string, float>
        {
            ["Action"] = 0f,
            ["Drama"] = 0f
        };

        float result = CategoricalSimilarityScorer.Score(
            preferences, new[] { "Action", "Drama" });

        Assert.Equal(0f, result);
    }

    [Fact]
    // Verifies that scoring with matching zero weight feature returns zero.
    public void ScoreMatchingZeroWeightFeatureReturnsZero()
    {
        var preferences = new Dictionary<string, float>
        {
            ["Action"] = 5f,
            ["Drama"] = 0f
        };

        float result = CategoricalSimilarityScorer.Score(
            preferences, new[] { "Drama" });

        Assert.Equal(0f, result);
    }

    // ==================================================
    // Feature Normalization Tests
    // ==================================================

    [Fact]
    // Verifies that scoring with different capitalization still matches .
    public void ScoreDifferentCapitalizationStillMatches()
    {
        var preferences = new Dictionary<string, float>
        {
            ["ACTION"] = 1f
        };

        float result = CategoricalSimilarityScorer.Score(
            preferences, new[] { "action" });

        Assert.Equal(1f, result, 6);
    }

    [Fact]
    // Verifies that scoring with extra whitespace still matches .
    public void ScoreExtraWhitespaceStillMatches()
    {
        var preferences = new Dictionary<string, float>
        {
            ["  Action  "] = 1f
        };

        float result = CategoricalSimilarityScorer.Score(
            preferences, new[] { " Action " });

        Assert.Equal(1f, result, 6);
    }

    [Fact]
    // Verifies that scoring with duplicate features does not increase weight.
    public void ScoreDuplicateFeaturesDoesNotIncreaseWeight()
    {
        var preferences = new Dictionary<string, float>
        {
            ["Action"] = 1f,
            ["Drama"] = 1f
        };

        float original = CategoricalSimilarityScorer.Score(
            preferences, new[] { "Action", "Drama" });

        float duplicate = CategoricalSimilarityScorer.Score(
            preferences,
            new[] { "Action", "Action", "ACTION", "Drama" });

        Assert.Equal(original, duplicate, 6);
    }

    [Fact]
    // Verifies that scoring with duplicate normalized preferences combines weights.
    public void ScoreDuplicateNormalizedPreferencesCombinesWeights()
    {
        var preferences = new Dictionary<string, float>
        {
            ["Action"] = 1f,
            ["ACTION"] = 2f,
            [" Action "] = 3f,
            ["Drama"] = 2f
        };

        var combined = new Dictionary<string, float>
        {
            ["Action"] = 6f,
            ["Drama"] = 2f
        };

        string[] features = { "Action" };

        float first = CategoricalSimilarityScorer.Score(
            preferences, features);

        float second = CategoricalSimilarityScorer.Score(
            combined, features);

        Assert.Equal(second, first, 6);
    }

    [Fact]
    // Verifies that blank candidate features are ignored.
    public void ScoreBlankFeaturesAreIgnored()
    {
        var preferences = new Dictionary<string, float>
        {
            ["Action"] = 1f
        };

        string[] features = { "Action", "", " ", "\t" };

        float result = CategoricalSimilarityScorer.Score(
            preferences, features);

        Assert.Equal(1f, result, 6);
    }

    [Fact]
    // Verifies that blank preference keys are ignored.
    public void ScoreBlankPreferenceKeysAreIgnored()
    {
        var preferences = new Dictionary<string, float>
        {
            ["Action"] = 1f,
            [""] = 3f,
            [" "] = 4f
        };

        float result = CategoricalSimilarityScorer.Score(
            preferences, new[] { "Action" });

        Assert.Equal(1f, result, 6);
    }

    // ==================================================
    // Invalid Input Tests
    // ==================================================

    [Fact]
    // Verifies that scoring with null preferences throws ArgumentNullException.
    public void ScoreNullPreferencesThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            CategoricalSimilarityScorer.Score(
                null!, new[] { "Action" }));
    }

    [Fact]
    // Verifies that scoring with null features throws ArgumentNullException.
    public void ScoreNullFeaturesThrowsArgumentNullException()
    {
        var preferences = new Dictionary<string, float>
        {
            ["Action"] = 1f
        };

        Assert.Throws<ArgumentNullException>(() =>
            CategoricalSimilarityScorer.Score(
                preferences, null!));
    }

    [Theory]
    [InlineData(-1f)]
    [InlineData(-0.01f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    // Verifies that scoring with invalid weight throws ArgumentException.
    public void ScoreInvalidWeightThrowsArgumentException(
        float weight)
    {
        var preferences = new Dictionary<string, float>
        {
            ["Action"] = weight
        };

        Assert.Throws<ArgumentException>(() =>
            CategoricalSimilarityScorer.Score(
                preferences, new[] { "Action" }));
    }

    // ==================================================
    // Algorithm Property Tests
    // ==================================================

    [Fact]
    // Verifies that scoring with scaled preferences returns the same result.
    public void ScoreScaledPreferencesReturnsSameResult()
    {
        var original = new Dictionary<string, float>
        {
            ["Action"] = 2f,
            ["Drama"] = 3f
        };

        var scaled = new Dictionary<string, float>
        {
            ["Action"] = 20f,
            ["Drama"] = 30f
        };

        string[] features = { "Action" };

        float first = CategoricalSimilarityScorer.Score(
            original, features);

        float second = CategoricalSimilarityScorer.Score(
            scaled, features);

        Assert.Equal(first, second, 6);
    }

    [Fact]
    // Verifies that scoring with reordered features returns the same result.
    public void ScoreReorderedFeaturesReturnsSameResult()
    {
        var preferences = new Dictionary<string, float>
        {
            ["Action"] = 3f,
            ["Drama"] = 2f,
            ["Adventure"] = 1f
        };

        float first = CategoricalSimilarityScorer.Score(
            preferences, new[] { "Action", "Drama", "Adventure" });

        float second = CategoricalSimilarityScorer.Score(
            preferences, new[] { "Adventure", "Action", "Drama" });

        Assert.Equal(first, second, 6);
    }

    [Fact]
    // Verifies that scoring with repeated calls returns the same result.
    public void ScoreRepeatedCallsReturnsSameResult()
    {
        var preferences = new Dictionary<string, float>
        {
            ["Action"] = 3f,
            ["Drama"] = 2f
        };

        string[] features = { "Action", "Adventure" };

        float first = CategoricalSimilarityScorer.Score(
            preferences, features);

        for (int i = 0; i < 100; i++)
        {
            float result = CategoricalSimilarityScorer.Score(
                preferences, features);

            Assert.Equal(first, result);
        }
    }

    [Fact]
    // Verifies that scoring does not modify inputs.
    public void ScoreDoesNotModifyInputs()
    {
        var preferences = new Dictionary<string, float>
        {
            [" Action "] = 3f,
            ["DRAMA"] = 2f
        };

        string[] features = { "action", "Drama" };

        var originalPreferences =
            new Dictionary<string, float>(preferences);

        string[] originalFeatures = (string[])features.Clone();

        CategoricalSimilarityScorer.Score(preferences, features);

        Assert.Equal(originalPreferences.Count, preferences.Count);

        foreach (var preference in originalPreferences)
        {
            Assert.True(preferences.ContainsKey(preference.Key));
            Assert.Equal(
                preference.Value, preferences[preference.Key]);
        }

        Assert.Equal(originalFeatures, features);
    }

    [Fact]
    // Verifies that scoring with random preferences remains in range.
    public void ScoreRandomPreferencesRemainsInRange()
    {
        Random random = new Random(42);

        string[] vocabulary =
        {
            "Action", "Drama", "Adventure", "Romance",
            "Comedy", "Horror", "Science Fiction", "Fantasy"
        };

        for (int iteration = 0; iteration < 1000; iteration++)
        {
            var preferences = new Dictionary<string, float>();
            var features = new List<string>();

            foreach (string feature in vocabulary)
            {
                preferences[feature] =
                    (float)(random.NextDouble() * 10);

                if (random.NextDouble() > 0.5)
                {
                    features.Add(feature);
                }
            }

            float result = CategoricalSimilarityScorer.Score(
                preferences, features);

            Assert.True(float.IsFinite(result));
            Assert.InRange(result, 0f, 1f);
        }
    }
}
