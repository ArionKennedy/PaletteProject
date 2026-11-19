
using System;
using System.Collections.Generic;
using System.Linq;
using Palette.Api.MachineLearning.ContentBased;
using Palette.Api.MachineLearning.ImplicitFeedback;
using Xunit;

public class InteractionScorerTests
{
    // Creates a user-interaction fixture.
    private static UserInteraction CreateInteraction(
        Guid userId,
        Guid mediaId,
        InteractionType type,
        DateTimeOffset? occurredAt = null)
    {
        return new UserInteraction(
            UserId: userId,
            MediaId: mediaId,
            Type: type,
            OccurredAt: occurredAt ?? DateTimeOffset.UnixEpoch
        );
    }

    // Creates media-feature test data.
    private static MediaFeatures CreateMedia(
        Guid mediaId,
        string[]? genres = null,
        int? runtime = null)
    {
        return new MediaFeatures(
            MediaId: mediaId,
            MediaType: "movie",
            Genres: genres ?? Array.Empty<string>(),
            Authors: Array.Empty<string>(),
            Directors: Array.Empty<string>(),
            Studios: Array.Empty<string>(),
            Publishers: Array.Empty<string>(),
            RuntimeMinutes: runtime
        );
    }

    // ==================================================
    // Interaction Weight Retrieval Tests (7)
    // ==================================================

    [Theory]
    [InlineData(InteractionType.Search, 0.5f)]
    [InlineData(InteractionType.SearchSelection, 1f)]
    [InlineData(InteractionType.Stored, 2f)]
    [InlineData(InteractionType.Completed, 3f)]
    [InlineData(InteractionType.PositiveReview, 5f)]
    // Verifies that looking up interaction weight returns default interaction weight.
    public void GetWeightReturnsDefaultInteractionWeight(
        InteractionType type,
        float expected)
    {
        var scorer = new InteractionScorer();

        Assert.Equal(expected, scorer.GetWeight(type));
    }

    [Fact]
    // Verifies that looking up interaction weight returns custom interaction weights.
    public void GetWeightReturnsCustomInteractionWeights()
    {
        var scorer = new InteractionScorer(
            new InteractionWeights(
                Search: 1f,
                SearchSelection: 2f,
                Stored: 4f,
                Completed: 6f,
                PositiveReview: 10f));

        Assert.Equal(1f, scorer.GetWeight(InteractionType.Search));
        Assert.Equal(2f, scorer.GetWeight(InteractionType.SearchSelection));
        Assert.Equal(4f, scorer.GetWeight(InteractionType.Stored));
        Assert.Equal(6f, scorer.GetWeight(InteractionType.Completed));
        Assert.Equal(10f, scorer.GetWeight(InteractionType.PositiveReview));
    }

    [Fact]
    // Verifies that looking up interaction weight with unknown interaction type throws an exception.
    public void GetWeightUnknownInteractionTypeThrowsException()
    {
        var scorer = new InteractionScorer();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            scorer.GetWeight((InteractionType)999));
    }

    // ==================================================
    // Interaction Scoring Tests (8)
    // ==================================================

    [Fact]
    // Verifies that scoring with empty interactions returns empty dictionary.
    public void ScoreEmptyInteractionsReturnsEmptyDictionary()
    {
        var scorer = new InteractionScorer();

        var result = scorer.Score(
            Guid.NewGuid(),
            Array.Empty<UserInteraction>());

        Assert.Empty(result);
    }

    [Fact]
    // Verifies that scoring with single interaction returns the correct weight.
    public void ScoreSingleInteractionReturnsCorrectWeight()
    {
        var scorer = new InteractionScorer();

        Guid userId = Guid.NewGuid();
        Guid mediaId = Guid.NewGuid();

        var interactions = new[]
        {
            CreateInteraction(
                userId, mediaId, InteractionType.Stored)
        };

        var result = scorer.Score(userId, interactions);

        Assert.Single(result);
        Assert.Equal(2f, result[mediaId]);
    }

    [Fact]
    // Verifies that different interaction types accumulate their weights.
    public void ScoreDifferentInteractionTypesAccumulatesWeights()
    {
        var scorer = new InteractionScorer();

        Guid userId = Guid.NewGuid();
        Guid mediaId = Guid.NewGuid();

        var interactions = new[]
        {
            CreateInteraction(
                userId, mediaId, InteractionType.Stored),

            CreateInteraction(
                userId, mediaId, InteractionType.Completed)
        };

        var result = scorer.Score(userId, interactions);

        Assert.Equal(5f, result[mediaId]);
    }

    [Fact]
    // Verifies that scoring with all interaction types returns maximum default weight.
    public void ScoreAllInteractionTypesReturnsMaximumDefaultWeight()
    {
        var scorer = new InteractionScorer();

        Guid userId = Guid.NewGuid();
        Guid mediaId = Guid.NewGuid();

        var interactions = Enum.GetValues<InteractionType>()
            .Select(type =>
                CreateInteraction(userId, mediaId, type))
            .ToArray();

        var result = scorer.Score(userId, interactions);

        Assert.Equal(11.5f, result[mediaId]);
    }

    [Fact]
    // Verifies that scores are kept separate for different media items.
    public void ScoreMultipleMediaSeparatesWeights()
    {
        var scorer = new InteractionScorer();

        Guid userId = Guid.NewGuid();
        Guid firstMedia = Guid.NewGuid();
        Guid secondMedia = Guid.NewGuid();

        var interactions = new[]
        {
            CreateInteraction(
                userId, firstMedia, InteractionType.Stored),

            CreateInteraction(
                userId, firstMedia, InteractionType.Completed),

            CreateInteraction(
                userId, secondMedia, InteractionType.Search)
        };

        var result = scorer.Score(userId, interactions);

        Assert.Equal(2, result.Count);
        Assert.Equal(5f, result[firstMedia]);
        Assert.Equal(0.5f, result[secondMedia]);
    }

    [Fact]
    // Verifies that scoring ignores interactions from other users.
    public void ScoreIgnoresInteractionsFromOtherUsers()
    {
        var scorer = new InteractionScorer();

        Guid userId = Guid.NewGuid();
        Guid otherUser = Guid.NewGuid();
        Guid mediaId = Guid.NewGuid();

        var interactions = new[]
        {
            CreateInteraction(
                userId, mediaId, InteractionType.Stored),

            CreateInteraction(
                otherUser, mediaId, InteractionType.PositiveReview)
        };

        var result = scorer.Score(userId, interactions);

        Assert.Single(result);
        Assert.Equal(2f, result[mediaId]);
    }

    [Fact]
    // Verifies that scoring with unknown user returns empty dictionary.
    public void ScoreUnknownUserReturnsEmptyDictionary()
    {
        var scorer = new InteractionScorer();

        var interactions = new[]
        {
            CreateInteraction(
                Guid.NewGuid(),
                Guid.NewGuid(),
                InteractionType.Completed)
        };

        var result = scorer.Score(
            Guid.NewGuid(), interactions);

        Assert.Empty(result);
    }

    [Fact]
    // Verifies that scoring with zero weighted interaction is excluded.
    public void ScoreZeroWeightedInteractionIsExcluded()
    {
        var scorer = new InteractionScorer(
            new InteractionWeights(Search: 0f));

        Guid userId = Guid.NewGuid();
        Guid mediaId = Guid.NewGuid();

        var interactions = new[]
        {
            CreateInteraction(
                userId, mediaId, InteractionType.Search)
        };

        var result = scorer.Score(userId, interactions);

        Assert.Empty(result);
    }

    // ==================================================
    // Duplicate Interaction Handling Tests (4)
    // ==================================================

    [Fact]
    // Verifies that duplicate interactions contribute their weight only once.
    public void ScoreDuplicateInteractionsAreCountedOnce()
    {
        var scorer = new InteractionScorer();

        Guid userId = Guid.NewGuid();
        Guid mediaId = Guid.NewGuid();

        var interaction = CreateInteraction(
            userId, mediaId, InteractionType.Stored);

        var interactions = new[]
        {
            interaction,
            interaction,
            interaction
        };

        var result = scorer.Score(userId, interactions);

        Assert.Equal(2f, result[mediaId]);
    }

    [Fact]
    // Verifies that repeated searches do not inflate the interaction weight.
    public void ScoreRepeatedSearchesDoNotInflateWeight()
    {
        var scorer = new InteractionScorer();

        Guid userId = Guid.NewGuid();
        Guid mediaId = Guid.NewGuid();

        var interactions = Enumerable.Range(0, 100)
            .Select(index => CreateInteraction(
                userId,
                mediaId,
                InteractionType.Search,
                DateTimeOffset.UnixEpoch.AddMinutes(index)))
            .ToArray();

        var result = scorer.Score(userId, interactions);

        Assert.Equal(0.5f, result[mediaId]);
    }

    [Fact]
    // Verifies that the same interaction type is counted separately for each media item.
    public void ScoreSameInteractionTypeForDifferentMediaCountsSeparately()
    {
        var scorer = new InteractionScorer();

        Guid userId = Guid.NewGuid();
        Guid firstMedia = Guid.NewGuid();
        Guid secondMedia = Guid.NewGuid();

        var interactions = new[]
        {
            CreateInteraction(
                userId, firstMedia, InteractionType.Stored),

            CreateInteraction(
                userId, secondMedia, InteractionType.Stored)
        };

        var result = scorer.Score(userId, interactions);

        Assert.Equal(2, result.Count);
        Assert.Equal(2f, result[firstMedia]);
        Assert.Equal(2f, result[secondMedia]);
    }

    [Fact]
    // Verifies that repeated mixed interactions remain capped per media and type.
    public void ScoreRepeatedMixedInteractionsRemainCapped()
    {
        var scorer = new InteractionScorer();

        Guid userId = Guid.NewGuid();
        Guid mediaId = Guid.NewGuid();

        var interactions = new List<UserInteraction>();

        for (int i = 0; i < 100; i++)
        {
            foreach (var type in Enum.GetValues<InteractionType>())
            {
                interactions.Add(
                    CreateInteraction(userId, mediaId, type));
            }
        }

        var result = scorer.Score(userId, interactions);

        Assert.Equal(11.5f, result[mediaId]);
    }

    // ==================================================
    // Weighted Media Interaction Generation Tests (7)
    // ==================================================

    [Fact]
    // Verifies that building weighted interactions returns the correct media and weight.
    public void BuildWeightedInteractionsReturnsCorrectMediaAndWeight()
    {
        var scorer = new InteractionScorer();

        Guid userId = Guid.NewGuid();
        Guid mediaId = Guid.NewGuid();

        var media = CreateMedia(mediaId);

        var interactions = new[]
        {
            CreateInteraction(
                userId, mediaId, InteractionType.Stored),

            CreateInteraction(
                userId, mediaId, InteractionType.Completed)
        };

        var features = new Dictionary<Guid, MediaFeatures>
        {
            [mediaId] = media
        };

        var result = scorer.BuildWeightedInteractions(
            userId, interactions, features);

        var entry = Assert.Single(result);

        Assert.Same(media, entry.Media);
        Assert.Equal(5f, entry.Weight);
    }

    [Fact]
    // Verifies that building weighted interactions with missing metadata is ignored.
    public void BuildWeightedInteractionsMissingMetadataIsIgnored()
    {
        var scorer = new InteractionScorer();

        Guid userId = Guid.NewGuid();

        var interactions = new[]
        {
            CreateInteraction(
                userId,
                Guid.NewGuid(),
                InteractionType.Stored)
        };

        var result = scorer.BuildWeightedInteractions(
            userId,
            interactions,
            new Dictionary<Guid, MediaFeatures>());

        Assert.Empty(result);
    }

    [Fact]
    // Verifies that building weighted interactions with partially missing metadata is ignored.
    public void BuildWeightedInteractionsPartiallyMissingMetadataIsIgnored()
    {
        var scorer = new InteractionScorer();

        Guid userId = Guid.NewGuid();
        Guid knownMedia = Guid.NewGuid();
        Guid unknownMedia = Guid.NewGuid();

        var interactions = new[]
        {
            CreateInteraction(
                userId, knownMedia, InteractionType.Stored),

            CreateInteraction(
                userId, unknownMedia, InteractionType.Completed)
        };

        var features = new Dictionary<Guid, MediaFeatures>
        {
            [knownMedia] = CreateMedia(knownMedia)
        };

        var result = scorer.BuildWeightedInteractions(
            userId, interactions, features);

        var entry = Assert.Single(result);

        Assert.Equal(knownMedia, entry.Media.MediaId);
        Assert.Equal(2f, entry.Weight);
    }

    [Fact]
    // Verifies that building weighted interactions with unused metadata is ignored.
    public void BuildWeightedInteractionsUnusedMetadataIsIgnored()
    {
        var scorer = new InteractionScorer();

        Guid userId = Guid.NewGuid();
        Guid mediaId = Guid.NewGuid();

        var features = new Dictionary<Guid, MediaFeatures>
        {
            [mediaId] = CreateMedia(mediaId)
        };

        var result = scorer.BuildWeightedInteractions(
            userId,
            Array.Empty<UserInteraction>(),
            features);

        Assert.Empty(result);
    }

    [Fact]
    // Verifies that building weighted interactions combines multiple media.
    public void BuildWeightedInteractionsCombinesMultipleMedia()
    {
        var scorer = new InteractionScorer();

        Guid userId = Guid.NewGuid();
        Guid firstMedia = Guid.NewGuid();
        Guid secondMedia = Guid.NewGuid();

        var interactions = new[]
        {
            CreateInteraction(
                userId, firstMedia, InteractionType.Stored),

            CreateInteraction(
                userId, secondMedia, InteractionType.Completed)
        };

        var features = new Dictionary<Guid, MediaFeatures>
        {
            [firstMedia] = CreateMedia(firstMedia),
            [secondMedia] = CreateMedia(secondMedia)
        };

        var result = scorer.BuildWeightedInteractions(
            userId, interactions, features);

        Assert.Equal(2, result.Count);

        Assert.Equal(
            2f,
            result.Single(
                entry => entry.Media.MediaId == firstMedia).Weight);

        Assert.Equal(
            3f,
            result.Single(
                entry => entry.Media.MediaId == secondMedia).Weight);
    }

    [Fact]
    // Verifies that building weighted interactions with mismatched media id throws an exception.
    public void BuildWeightedInteractionsMismatchedMediaIdThrowsException()
    {
        var scorer = new InteractionScorer();

        Guid userId = Guid.NewGuid();
        Guid mediaId = Guid.NewGuid();

        var interactions = new[]
        {
            CreateInteraction(
                userId, mediaId, InteractionType.Stored)
        };

        var features = new Dictionary<Guid, MediaFeatures>
        {
            [mediaId] = CreateMedia(Guid.NewGuid())
        };

        Assert.Throws<ArgumentException>(() =>
            scorer.BuildWeightedInteractions(
                userId, interactions, features));
    }

    [Fact]
    // Verifies that building weighted interactions with null media throws an exception.
    public void BuildWeightedInteractionsNullMediaThrowsException()
    {
        var scorer = new InteractionScorer();

        Guid userId = Guid.NewGuid();
        Guid mediaId = Guid.NewGuid();

        var interactions = new[]
        {
            CreateInteraction(
                userId, mediaId, InteractionType.Stored)
        };

        var features = new Dictionary<Guid, MediaFeatures>
        {
            [mediaId] = null!
        };

        Assert.Throws<ArgumentNullException>(() =>
            scorer.BuildWeightedInteractions(
                userId, interactions, features));
    }

    // ==================================================
    // Configuration Validation Tests (7)
    // ==================================================

    [Theory]
    [InlineData(-1f)]
    [InlineData(-0.01f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    // Verifies that constructing a model with invalid interaction weight throws an exception.
    public void ConstructorInvalidInteractionWeightThrowsException(
        float weight)
    {
        Assert.Throws<ArgumentException>(() =>
            new InteractionScorer(
                new InteractionWeights(Search: weight)));
    }

    [Fact]
    // Verifies that constructing a model with all zero weights throws an exception.
    public void ConstructorAllZeroWeightsThrowsException()
    {
        var weights = new InteractionWeights(
            Search: 0f,
            SearchSelection: 0f,
            Stored: 0f,
            Completed: 0f,
            PositiveReview: 0f);

        Assert.Throws<ArgumentException>(() =>
            new InteractionScorer(weights));
    }

    [Fact]
    // Verifies that constructing a model validates all interaction weights.
    public void ConstructorValidatesAllInteractionWeights()
    {
        var configurations = new[]
        {
            new InteractionWeights(SearchSelection: -1f),
            new InteractionWeights(Stored: -1f),
            new InteractionWeights(Completed: -1f),
            new InteractionWeights(PositiveReview: -1f)
        };

        foreach (var configuration in configurations)
        {
            Assert.Throws<ArgumentException>(() =>
                new InteractionScorer(configuration));
        }
    }

    // ==================================================
    // Invalid Input Tests (4)
    // ==================================================

    [Fact]
    // Verifies that scoring with null interactions throws an exception.
    public void ScoreNullInteractionsThrowsException()
    {
        var scorer = new InteractionScorer();

        Assert.Throws<ArgumentNullException>(() =>
            scorer.Score(Guid.NewGuid(), null!));
    }

    [Fact]
    // Verifies that scoring with null interaction throws an exception.
    public void ScoreNullInteractionThrowsException()
    {
        var scorer = new InteractionScorer();

        var interactions = new UserInteraction[]
        {
            null!
        };

        Assert.Throws<ArgumentNullException>(() =>
            scorer.Score(Guid.NewGuid(), interactions));
    }

    [Fact]
    // Verifies that building weighted interactions with null metadata throws an exception.
    public void BuildWeightedInteractionsNullMetadataThrowsException()
    {
        var scorer = new InteractionScorer();

        Assert.Throws<ArgumentNullException>(() =>
            scorer.BuildWeightedInteractions(
                Guid.NewGuid(),
                Array.Empty<UserInteraction>(),
                null!));
    }

    [Fact]
    // Verifies that scoring with accumulated weight overflow throws an exception.
    public void ScoreAccumulatedWeightOverflowThrowsException()
    {
        var scorer = new InteractionScorer(
            new InteractionWeights(
                Stored: float.MaxValue,
                Completed: float.MaxValue));

        Guid userId = Guid.NewGuid();
        Guid mediaId = Guid.NewGuid();

        var interactions = new[]
        {
            CreateInteraction(
                userId, mediaId, InteractionType.Stored),

            CreateInteraction(
                userId, mediaId, InteractionType.Completed)
        };

        Assert.Throws<OverflowException>(() =>
            scorer.Score(userId, interactions));
    }

    // ==================================================
    // Algorithm Property Tests (5)
    // ==================================================

    [Fact]
    // Verifies that scoring with reordered interactions produces same weights.
    public void ScoreReorderedInteractionsProducesSameWeights()
    {
        var scorer = new InteractionScorer();

        Guid userId = Guid.NewGuid();
        Guid mediaId = Guid.NewGuid();

        var interactions = new[]
        {
            CreateInteraction(
                userId, mediaId, InteractionType.Search),

            CreateInteraction(
                userId, mediaId, InteractionType.Stored),

            CreateInteraction(
                userId, mediaId, InteractionType.Completed)
        };

        var original = scorer.Score(userId, interactions);

        var reordered = scorer.Score(
            userId, interactions.Reverse().ToArray());

        Assert.Equal(original[mediaId], reordered[mediaId]);
    }

    [Fact]
    // Verifies that repeated scoring calls do not accumulate state.
    public void ScoreRepeatedCallsDoNotAccumulateState()
    {
        var scorer = new InteractionScorer();

        Guid userId = Guid.NewGuid();
        Guid mediaId = Guid.NewGuid();

        var interactions = new[]
        {
            CreateInteraction(
                userId, mediaId, InteractionType.Completed)
        };

        for (int i = 0; i < 100; i++)
        {
            var result = scorer.Score(userId, interactions);

            Assert.Equal(3f, result[mediaId]);
        }
    }

    [Fact]
    // Verifies that scoring does not modify input interactions.
    public void ScoreDoesNotModifyInputInteractions()
    {
        var scorer = new InteractionScorer();

        Guid userId = Guid.NewGuid();
        Guid mediaId = Guid.NewGuid();

        var interactions = new[]
        {
            CreateInteraction(
                userId, mediaId, InteractionType.Stored),

            CreateInteraction(
                userId, mediaId, InteractionType.Search)
        };

        var original = interactions.ToArray();

        scorer.Score(userId, interactions);

        Assert.Equal(original, interactions);
    }

    [Fact]
    // Verifies that scoring returns read only dictionary.
    public void ScoreReturnsReadOnlyDictionary()
    {
        var scorer = new InteractionScorer();

        var result = scorer.Score(
            Guid.NewGuid(),
            Array.Empty<UserInteraction>());

        var dictionary =
            Assert.IsAssignableFrom<IDictionary<Guid, float>>(
                result);

        Assert.Throws<NotSupportedException>(() =>
            dictionary.Add(Guid.NewGuid(), 5f));
    }

    [Fact]
    // Verifies that random interactions stay within the default weight bounds.
    public void ScoreRandomInteractionsRemainWithinDefaultBounds()
    {
        var scorer = new InteractionScorer();
        var random = new Random(42);

        Guid userId = Guid.NewGuid();

        var mediaIds = Enumerable.Range(0, 100)
            .Select(_ => Guid.NewGuid())
            .ToArray();

        var types = Enum.GetValues<InteractionType>();

        var interactions = new List<UserInteraction>();

        for (int i = 0; i < 10000; i++)
        {
            interactions.Add(CreateInteraction(
                userId,
                mediaIds[random.Next(mediaIds.Length)],
                types[random.Next(types.Length)]));
        }

        var result = scorer.Score(userId, interactions);

        Assert.NotEmpty(result);
        Assert.InRange(result.Count, 1, 100);

        foreach (var weight in result.Values)
        {
            Assert.True(float.IsFinite(weight));
            Assert.InRange(weight, 0.5f, 11.5f);
        }
    }

    // ==================================================
    // System Integration Tests (2)
    // ==================================================

    [Fact]
    // Verifies building weighted interactions for generates expected user profile.
    public void BuildWeightedInteractionsGeneratesExpectedUserProfile()
    {
        var scorer = new InteractionScorer();

        Guid userId = Guid.NewGuid();
        Guid firstMedia = Guid.NewGuid();
        Guid secondMedia = Guid.NewGuid();

        var interactions = new[]
        {
            CreateInteraction(
                userId, firstMedia, InteractionType.Completed),

            CreateInteraction(
                userId, firstMedia, InteractionType.PositiveReview),

            CreateInteraction(
                userId, secondMedia, InteractionType.Stored)
        };

        var features = new Dictionary<Guid, MediaFeatures>
        {
            [firstMedia] = CreateMedia(
                firstMedia,
                new[] { "Action" },
                runtime: 100),

            [secondMedia] = CreateMedia(
                secondMedia,
                new[] { "Drama" },
                runtime: 200)
        };

        var weightedInteractions =
            scorer.BuildWeightedInteractions(
                userId, interactions, features);

        var profile = UserProfileBuilder.Build(
            userId, weightedInteractions);

        Assert.Equal(8f, profile.GenrePreferences["Action"]);
        Assert.Equal(2f, profile.GenrePreferences["Drama"]);
        Assert.Equal(120f, profile.PreferredRuntimeMinutes);
    }

    [Fact]
    // Verifies that building weighted interactions produces personalized predictions.
    public void BuildWeightedInteractionsProducesPersonalizedPredictions()
    {
        var scorer = new InteractionScorer();
        var model = new ContentBasedModel();

        Guid userId = Guid.NewGuid();
        Guid firstMedia = Guid.NewGuid();
        Guid secondMedia = Guid.NewGuid();

        var interactions = new[]
        {
            CreateInteraction(
                userId, firstMedia, InteractionType.Completed),

            CreateInteraction(
                userId, firstMedia, InteractionType.PositiveReview),

            CreateInteraction(
                userId, secondMedia, InteractionType.Search)
        };

        var features = new Dictionary<Guid, MediaFeatures>
        {
            [firstMedia] = CreateMedia(
                firstMedia,
                new[] { "Action" },
                runtime: 150),

            [secondMedia] = CreateMedia(
                secondMedia,
                new[] { "Romance" },
                runtime: 150)
        };

        var weightedInteractions =
            scorer.BuildWeightedInteractions(
                userId, interactions, features);

        var profile = UserProfileBuilder.Build(
            userId, weightedInteractions);

        var relevant = CreateMedia(
            Guid.NewGuid(),
            new[] { "Action" },
            runtime: 150);

        var lessRelevant = CreateMedia(
            Guid.NewGuid(),
            new[] { "Romance" },
            runtime: 150);

        Assert.True(
            model.Predict(profile, relevant) >
            model.Predict(profile, lessRelevant));
    }
}