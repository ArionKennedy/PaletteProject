
using System;
using System.Collections.Generic;
using System.Linq;
using Palette.Api.MachineLearning.ContentBased;
using Palette.Api.MachineLearning.ImplicitFeedback;
using Xunit;

public class InteractionScorerTests
{
    private readonly InteractionScorer _scorer = new();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _mediaId = Guid.NewGuid();

    // Creates a configurable user interaction.
    private static UserInteraction CreateInteraction(
        Guid userId, Guid mediaId, InteractionType type,
        DateTimeOffset? occurredAt = null)
    {
        return new UserInteraction(
            userId, mediaId, type, occurredAt ?? DateTimeOffset.UnixEpoch);
    }

    // Creates interactions for the default user and media.
    private UserInteraction[] CreateInteractions(params InteractionType[] types)
    {
        return types.Select(type =>
            CreateInteraction(_userId, _mediaId, type)).ToArray();
    }

    // Creates configurable movie features for testing.
    private static MediaFeatures CreateMedia(
        Guid mediaId, string[]? genres = null, int? runtime = null)
    {
        return new MediaFeatures(
            mediaId, "movie", genres ?? [], [], [], [], [], runtime);
    }

    // Interaction weight retrieval

    // Verifies that each interaction type has the correct default weight.
    [Theory]
    [InlineData(InteractionType.Search, 0.5f)]
    [InlineData(InteractionType.SearchSelection, 1f)]
    [InlineData(InteractionType.Stored, 2f)]
    [InlineData(InteractionType.Completed, 3f)]
    [InlineData(InteractionType.PositiveReview, 5f)]
    public void GetWeightReturnsDefaultInteractionWeight(InteractionType type, float expected)
    {
        Assert.Equal(expected, _scorer.GetWeight(type));
    }

    // Verifies that custom interaction weights are respected.
    [Fact]
    public void GetWeightReturnsCustomInteractionWeights()
    {
        var scorer = new InteractionScorer(new InteractionWeights(
            Search: 1f, SearchSelection: 2f, Stored: 4f,
            Completed: 6f, PositiveReview: 10f));

        Assert.Equal(1f, scorer.GetWeight(InteractionType.Search));
        Assert.Equal(2f, scorer.GetWeight(InteractionType.SearchSelection));
        Assert.Equal(4f, scorer.GetWeight(InteractionType.Stored));
        Assert.Equal(6f, scorer.GetWeight(InteractionType.Completed));
        Assert.Equal(10f, scorer.GetWeight(InteractionType.PositiveReview));
    }

    // Verifies that unknown interaction types are rejected.
    [Fact]
    public void GetWeightUnknownInteractionTypeThrowsException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _scorer.GetWeight((InteractionType)999));
    }

    // Interaction scoring

    // Verifies that empty interactions produce an empty score dictionary.
    [Fact]
    public void ScoreEmptyInteractionsReturnsEmptyDictionary()
    {
        Assert.Empty(_scorer.Score(_userId, []));
    }

    // Verifies that a single interaction contributes its correct weight.
    [Fact]
    public void ScoreSingleInteractionReturnsCorrectWeight()
    {
        var result = _scorer.Score(_userId,
            CreateInteractions(InteractionType.Stored));

        Assert.Single(result);
        Assert.Equal(2f, result[_mediaId]);
    }

    // Verifies that different interaction types accumulate their weights.
    [Fact]
    public void ScoreDifferentInteractionTypesAccumulatesWeights()
    {
        var result = _scorer.Score(_userId,
            CreateInteractions(InteractionType.Stored, InteractionType.Completed));

        Assert.Equal(5f, result[_mediaId]);
    }

    // Verifies that all interaction types produce the maximum default weight.
    [Fact]
    public void ScoreAllInteractionTypesReturnsMaximumDefaultWeight()
    {
        var result = _scorer.Score(_userId,
            CreateInteractions(Enum.GetValues<InteractionType>()));

        Assert.Equal(11.5f, result[_mediaId]);
    }

    // Verifies that interaction weights remain separate for different media.
    [Fact]
    public void ScoreMultipleMediaSeparatesWeights()
    {
        Guid secondMedia = Guid.NewGuid();

        var result = _scorer.Score(_userId,
        [
            CreateInteraction(_userId, _mediaId, InteractionType.Stored),
            CreateInteraction(_userId, _mediaId, InteractionType.Completed),
            CreateInteraction(_userId, secondMedia, InteractionType.Search)
        ]);

        Assert.Equal(2, result.Count);
        Assert.Equal(5f, result[_mediaId]);
        Assert.Equal(0.5f, result[secondMedia]);
    }

    // Verifies that interactions belonging to other users are ignored.
    [Fact]
    public void ScoreIgnoresInteractionsFromOtherUsers()
    {
        var result = _scorer.Score(_userId,
        [
            CreateInteraction(_userId, _mediaId, InteractionType.Stored),
            CreateInteraction(Guid.NewGuid(), _mediaId, InteractionType.PositiveReview)
        ]);

        Assert.Single(result);
        Assert.Equal(2f, result[_mediaId]);
    }

    // Verifies that users without matching interactions receive no scores.
    [Fact]
    public void ScoreUnknownUserReturnsEmptyDictionary()
    {
        Assert.Empty(_scorer.Score(Guid.NewGuid(),
            CreateInteractions(InteractionType.Completed)));
    }

    // Verifies that zero-weight interactions are excluded.
    [Fact]
    public void ScoreZeroWeightedInteractionIsExcluded()
    {
        var scorer = new InteractionScorer(new InteractionWeights(Search: 0f));

        Assert.Empty(scorer.Score(_userId,
            CreateInteractions(InteractionType.Search)));
    }

    // Duplicate interaction handling

    // Verifies that identical interactions contribute their weight only once.
    [Fact]
    public void ScoreDuplicateInteractionsAreCountedOnce()
    {
        var interaction = CreateInteraction(_userId, _mediaId, InteractionType.Stored);
        var result = _scorer.Score(_userId, [interaction, interaction, interaction]);

        Assert.Equal(2f, result[_mediaId]);
    }

    // Verifies that 100 searches at different times do not inflate weights.
    [Fact]
    public void ScoreRepeatedSearchesDoNotInflateWeight()
    {
        var interactions = Enumerable.Range(0, 100)
            .Select(index => CreateInteraction(
                _userId, _mediaId, InteractionType.Search,
                DateTimeOffset.UnixEpoch.AddMinutes(index)))
            .ToArray();

        Assert.Equal(0.5f, _scorer.Score(_userId, interactions)[_mediaId]);
    }

    // Verifies that identical interaction types count separately across media.
    [Fact]
    public void ScoreSameInteractionTypeForDifferentMediaCountsSeparately()
    {
        Guid secondMedia = Guid.NewGuid();

        var result = _scorer.Score(_userId,
        [
            CreateInteraction(_userId, _mediaId, InteractionType.Stored),
            CreateInteraction(_userId, secondMedia, InteractionType.Stored)
        ]);

        Assert.Equal(2, result.Count);
        Assert.Equal(2f, result[_mediaId]);
        Assert.Equal(2f, result[secondMedia]);
    }

    // Verifies that repeated mixed interactions remain capped per media.
    [Fact]
    public void ScoreRepeatedMixedInteractionsRemainCapped()
    {
        var interactions = new List<UserInteraction>();

        for (int i = 0; i < 100; i++)
            interactions.AddRange(CreateInteractions(Enum.GetValues<InteractionType>()));

        Assert.Equal(11.5f, _scorer.Score(_userId, interactions)[_mediaId]);
    }

    // Weighted media interaction generation

    // Verifies that weighted interactions preserve media references and weights.
    [Fact]
    public void BuildWeightedInteractionsReturnsCorrectMediaAndWeight()
    {
        var media = CreateMedia(_mediaId);
        var features = new Dictionary<Guid, MediaFeatures> { [_mediaId] = media };

        var result = _scorer.BuildWeightedInteractions(_userId,
            CreateInteractions(InteractionType.Stored, InteractionType.Completed),
            features);

        var entry = Assert.Single(result);

        Assert.Same(media, entry.Media);
        Assert.Equal(5f, entry.Weight);
    }

    // Verifies that interactions without corresponding metadata are ignored.
    [Fact]
    public void BuildWeightedInteractionsMissingMetadataIsIgnored()
    {
        var result = _scorer.BuildWeightedInteractions(_userId,
            CreateInteractions(InteractionType.Stored),
            new Dictionary<Guid, MediaFeatures>());

        Assert.Empty(result);
    }

    // Verifies that only interactions with available metadata are included.
    [Fact]
    public void BuildWeightedInteractionsPartiallyMissingMetadataIsIgnored()
    {
        var interactions = new[]
        {
            CreateInteraction(_userId, _mediaId, InteractionType.Stored),
            CreateInteraction(_userId, Guid.NewGuid(), InteractionType.Completed)
        };

        var features = new Dictionary<Guid, MediaFeatures>
        {
            [_mediaId] = CreateMedia(_mediaId)
        };

        var entry = Assert.Single(
            _scorer.BuildWeightedInteractions(_userId, interactions, features));

        Assert.Equal(_mediaId, entry.Media.MediaId);
        Assert.Equal(2f, entry.Weight);
    }

    // Verifies that metadata without corresponding interactions is ignored.
    [Fact]
    public void BuildWeightedInteractionsUnusedMetadataIsIgnored()
    {
        var features = new Dictionary<Guid, MediaFeatures>
        {
            [_mediaId] = CreateMedia(_mediaId)
        };

        Assert.Empty(_scorer.BuildWeightedInteractions(_userId, [], features));
    }

    // Verifies that weighted interactions include multiple distinct media.
    [Fact]
    public void BuildWeightedInteractionsCombinesMultipleMedia()
    {
        Guid secondMedia = Guid.NewGuid();

        var interactions = new[]
        {
            CreateInteraction(_userId, _mediaId, InteractionType.Stored),
            CreateInteraction(_userId, secondMedia, InteractionType.Completed)
        };

        var features = new Dictionary<Guid, MediaFeatures>
        {
            [_mediaId] = CreateMedia(_mediaId),
            [secondMedia] = CreateMedia(secondMedia)
        };

        var result = _scorer.BuildWeightedInteractions(_userId, interactions, features);

        Assert.Equal(2, result.Count);
        Assert.Equal(2f, result.Single(entry => entry.Media.MediaId == _mediaId).Weight);
        Assert.Equal(3f, result.Single(entry => entry.Media.MediaId == secondMedia).Weight);
    }

    // Verifies that mismatched metadata identifiers are rejected.
    [Fact]
    public void BuildWeightedInteractionsMismatchedMediaIdThrowsException()
    {
        var features = new Dictionary<Guid, MediaFeatures>
        {
            [_mediaId] = CreateMedia(Guid.NewGuid())
        };

        Assert.Throws<ArgumentException>(() =>
            _scorer.BuildWeightedInteractions(_userId,
                CreateInteractions(InteractionType.Stored), features));
    }

    // Verifies that null media metadata is rejected.
    [Fact]
    public void BuildWeightedInteractionsNullMediaThrowsException()
    {
        var features = new Dictionary<Guid, MediaFeatures> { [_mediaId] = null! };

        Assert.Throws<ArgumentNullException>(() =>
            _scorer.BuildWeightedInteractions(_userId,
                CreateInteractions(InteractionType.Stored), features));
    }

    // Configuration validation

    // Verifies that negative and nonfinite interaction weights are rejected.
    [Theory]
    [InlineData(-1f)]
    [InlineData(-0.01f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    public void ConstructorInvalidInteractionWeightThrowsException(float weight)
    {
        Assert.Throws<ArgumentException>(() =>
            new InteractionScorer(new InteractionWeights(Search: weight)));
    }

    // Verifies that at least one interaction type must have a positive weight.
    [Fact]
    public void ConstructorAllZeroWeightsThrowsException()
    {
        var weights = new InteractionWeights(
            Search: 0f, SearchSelection: 0f, Stored: 0f,
            Completed: 0f, PositiveReview: 0f);

        Assert.Throws<ArgumentException>(() => new InteractionScorer(weights));
    }

    // Verifies that every interaction weight is independently validated.
    [Fact]
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
            Assert.Throws<ArgumentException>(() => new InteractionScorer(configuration));
    }

    // Input validation

    // Verifies that a null interaction collection is rejected.
    [Fact]
    public void ScoreNullInteractionsThrowsException()
    {
        Assert.Throws<ArgumentNullException>(() => _scorer.Score(_userId, null!));
    }

    // Verifies that null interactions within a collection are rejected.
    [Fact]
    public void ScoreNullInteractionThrowsException()
    {
        Assert.Throws<ArgumentNullException>(() => _scorer.Score(_userId, [null!]));
    }

    // Verifies that a null metadata dictionary is rejected.
    [Fact]
    public void BuildWeightedInteractionsNullMetadataThrowsException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            _scorer.BuildWeightedInteractions(_userId, [], null!));
    }

    // Verifies that overflowing accumulated interaction weights are rejected.
    [Fact]
    public void ScoreAccumulatedWeightOverflowThrowsException()
    {
        var scorer = new InteractionScorer(new InteractionWeights(
            Stored: float.MaxValue, Completed: float.MaxValue));

        Assert.Throws<OverflowException>(() => scorer.Score(_userId,
            CreateInteractions(InteractionType.Stored, InteractionType.Completed)));
    }

    // Algorithm properties

    // Verifies that interaction ordering does not influence accumulated weights.
    [Fact]
    public void ScoreReorderedInteractionsProducesSameWeights()
    {
        var interactions = CreateInteractions(
            InteractionType.Search, InteractionType.Stored, InteractionType.Completed);

        var original = _scorer.Score(_userId, interactions);
        var reordered = _scorer.Score(_userId, interactions.Reverse().ToArray());

        Assert.Equal(original[_mediaId], reordered[_mediaId]);
    }

    // Verifies that 100 repeated scoring calls do not accumulate state.
    [Fact]
    public void ScoreRepeatedCallsDoNotAccumulateState()
    {
        var interactions = CreateInteractions(InteractionType.Completed);

        for (int i = 0; i < 100; i++)
            Assert.Equal(3f, _scorer.Score(_userId, interactions)[_mediaId]);
    }

    // Verifies that scoring does not modify the input interactions.
    [Fact]
    public void ScoreDoesNotModifyInputInteractions()
    {
        var interactions = CreateInteractions(
            InteractionType.Stored, InteractionType.Search);

        var original = interactions.ToArray();

        _scorer.Score(_userId, interactions);

        Assert.Equal(original, interactions);
    }

    // Verifies that the returned score dictionary is read-only.
    [Fact]
    public void ScoreReturnsReadOnlyDictionary()
    {
        var result = _scorer.Score(_userId, []);
        var dictionary = Assert.IsAssignableFrom<IDictionary<Guid, float>>(result);

        Assert.Throws<NotSupportedException>(() =>
            dictionary.Add(Guid.NewGuid(), 5f));
    }

    // Verifies that 10,000 random interactions remain within default weight bounds.
    [Fact]
    public void ScoreRandomInteractionsRemainWithinDefaultBounds()
    {
        var random = new Random(42);
        var mediaIds = Enumerable.Range(0, 100)
            .Select(_ => Guid.NewGuid()).ToArray();

        var types = Enum.GetValues<InteractionType>();
        var interactions = new List<UserInteraction>();

        for (int i = 0; i < 10000; i++)
        {
            interactions.Add(CreateInteraction(_userId,
                mediaIds[random.Next(mediaIds.Length)],
                types[random.Next(types.Length)]));
        }

        var result = _scorer.Score(_userId, interactions);

        Assert.NotEmpty(result);
        Assert.InRange(result.Count, 1, 100);

        foreach (var weight in result.Values)
        {
            Assert.True(float.IsFinite(weight));
            Assert.InRange(weight, 0.5f, 11.5f);
        }
    }

    // System integration

    // Verifies that weighted interactions generate the expected preference profile.
    [Fact]
    public void BuildWeightedInteractionsGeneratesExpectedUserProfile()
    {
        Guid secondMedia = Guid.NewGuid();

        var interactions = new[]
        {
            CreateInteraction(_userId, _mediaId, InteractionType.Completed),
            CreateInteraction(_userId, _mediaId, InteractionType.PositiveReview),
            CreateInteraction(_userId, secondMedia, InteractionType.Stored)
        };

        var features = new Dictionary<Guid, MediaFeatures>
        {
            [_mediaId] = CreateMedia(_mediaId, ["Action"], runtime: 100),
            [secondMedia] = CreateMedia(secondMedia, ["Drama"], runtime: 200)
        };

        var weighted = _scorer.BuildWeightedInteractions(_userId, interactions, features);
        var profile = UserProfileBuilder.Build(_userId, weighted);

        Assert.Equal(8f, profile.GenrePreferences["Action"]);
        Assert.Equal(2f, profile.GenrePreferences["Drama"]);
        Assert.Equal(120f, profile.PreferredRuntimeMinutes);
    }

    // Verifies that weighted interactions produce personalized media predictions.
    [Fact]
    public void BuildWeightedInteractionsProducesPersonalizedPredictions()
    {
        var model = new ContentBasedModel();
        Guid secondMedia = Guid.NewGuid();

        var interactions = new[]
        {
            CreateInteraction(_userId, _mediaId, InteractionType.Completed),
            CreateInteraction(_userId, _mediaId, InteractionType.PositiveReview),
            CreateInteraction(_userId, secondMedia, InteractionType.Search)
        };

        var features = new Dictionary<Guid, MediaFeatures>
        {
            [_mediaId] = CreateMedia(_mediaId, ["Action"], runtime: 150),
            [secondMedia] = CreateMedia(secondMedia, ["Romance"], runtime: 150)
        };

        var weighted = _scorer.BuildWeightedInteractions(_userId, interactions, features);
        var profile = UserProfileBuilder.Build(_userId, weighted);

        var relevant = CreateMedia(Guid.NewGuid(), ["Action"], runtime: 150);
        var lessRelevant = CreateMedia(Guid.NewGuid(), ["Romance"], runtime: 150);

        Assert.True(model.Predict(profile, relevant) >
            model.Predict(profile, lessRelevant));
    }
}