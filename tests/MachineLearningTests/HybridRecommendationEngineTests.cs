
using System;
using System.Collections.Generic;
using System.Linq;
using Palette.Api.MachineLearning.ContentBased;
using Palette.Api.MachineLearning.Hybrid;
using Palette.Api.MachineLearning.ImplicitFeedback;
using Xunit;

public class HybridRecommendationEngineTests
{
    private readonly HybridRecommendationEngine _engine = new();

    // Creates a configurable user preference profile.
    private static UserPreferenceProfile CreateProfile(
        Dictionary<string, float>? genres = null,
        Dictionary<string, float>? authors = null,
        Dictionary<string, float>? directors = null,
        Dictionary<string, float>? studios = null,
        Dictionary<string, float>? publishers = null,
        float? runtime = null,
        float? pageCount = null,
        float? seasons = null)
    {
        return new UserPreferenceProfile(
            Guid.NewGuid(), genres ?? new(), authors ?? new(),
            directors ?? new(), studios ?? new(), publishers ?? new(),
            runtime, pageCount, seasons);
    }

    // Creates a configurable recommendation candidate.
    private static RecommendationCandidate CreateCandidate(
        Guid? mediaId = null,
        string mediaType = "movie",
        string[]? genres = null,
        string[]? authors = null,
        string[]? directors = null,
        string[]? studios = null,
        string[]? publishers = null,
        int? runtime = null,
        int? pageCount = null,
        int? seasons = null,
        int reviewCount = 100,
        float? averageRating = 4f)
    {
        var media = new MediaFeatures(
            mediaId ?? Guid.NewGuid(), mediaType,
            genres ?? [], authors ?? [], directors ?? [],
            studios ?? [], publishers ?? [],
            runtime, pageCount, seasons);

        return new RecommendationCandidate(media, reviewCount, averageRating);
    }

    // Creates the specified number of default candidates.
    private static RecommendationCandidate[] CreateCandidates(int count)
    {
        return Enumerable.Range(0, count)
            .Select(_ => CreateCandidate()).ToArray();
    }

    // Creates a configurable recommendation context.
    private static RecommendationContext CreateContext(
        UserPreferenceProfile? profile = null,
        IReadOnlySet<Guid>? excluded = null,
        int maxReviews = 9999,
        float globalAverage = 3f,
        float priorWeight = 50f)
    {
        return new RecommendationContext(
            profile ?? CreateProfile(), excluded ?? new HashSet<Guid>(),
            maxReviews, globalAverage, priorWeight);
    }

    // Creates a profile with an action genre preference.
    private static UserPreferenceProfile CreateActionProfile()
    {
        return CreateProfile(genres: new() { ["Action"] = 5f });
    }

    // Hybrid scoring accuracy

    // Verifies that component scores produce the expected weighted average.
    [Fact]
    public void ScoreReturnsExpectedWeightedAverage()
    {
        var result = _engine.Score(
            CreateContext(CreateActionProfile()),
            CreateCandidate(genres: ["Action"], reviewCount: 99, averageRating: 3f));

        // Final = 0.7(1) + 0.1(0.5) + 0.2(0.5) = 0.85
        Assert.Equal(1f, result.ContentScore!.Value, 6);
        Assert.Equal(0.5f, result.PopularityScore, 6);
        Assert.Equal(0.5f, result.RatingQualityScore, 6);
        Assert.Equal(0.85f, result.FinalScore, 6);
    }

    // Verifies that unrelated media receive a zero content score.
    [Fact]
    public void ScoreUnrelatedMediaReceivesZeroContentScore()
    {
        var result = _engine.Score(
            CreateContext(CreateActionProfile()),
            CreateCandidate(genres: ["Romance"], reviewCount: 99, averageRating: 3f));

        Assert.Equal(0f, result.ContentScore!.Value);
        Assert.Equal(0.15f, result.FinalScore, 6);
    }

    // Verifies that perfect component scores produce a final score of one.
    [Fact]
    public void ScorePerfectComponentsReturnOne()
    {
        var result = _engine.Score(
            CreateContext(CreateActionProfile(), globalAverage: 5f),
            CreateCandidate(genres: ["Action"], reviewCount: 9999, averageRating: 5f));

        Assert.Equal(1f, result.FinalScore, 6);
    }

    // Verifies that zero component scores produce a final score of zero.
    [Fact]
    public void ScoreZeroComponentsReturnZero()
    {
        var result = _engine.Score(
            CreateContext(CreateActionProfile(), globalAverage: 1f),
            CreateCandidate(genres: ["Romance"], reviewCount: 0, averageRating: null));

        Assert.Equal(0f, result.FinalScore);
    }

    // Verifies that custom weights are normalized correctly.
    [Fact]
    public void ScoreSupportsCustomWeights()
    {
        var engine = new HybridRecommendationEngine(
            new RecommendationWeights(2f, 1f, 1f));

        var result = engine.Score(
            CreateContext(CreateActionProfile()),
            CreateCandidate(genres: ["Action"], reviewCount: 99, averageRating: 3f));

        Assert.Equal(0.75f, result.FinalScore, 6);
    }

    // Verifies that content-only scoring uses content similarity.
    [Fact]
    public void ScoreContentOnlyUsesContentSimilarity()
    {
        var engine = new HybridRecommendationEngine(
            new RecommendationWeights(1f, 0f, 0f));

        var result = engine.Score(
            CreateContext(CreateActionProfile()),
            CreateCandidate(genres: ["Action"]));

        Assert.Equal(result.ContentScore!.Value, result.FinalScore);
    }

    // Verifies that popularity-only scoring uses popularity.
    [Fact]
    public void ScorePopularityOnlyUsesPopularity()
    {
        var engine = new HybridRecommendationEngine(
            new RecommendationWeights(0f, 1f, 0f));

        var result = engine.Score(CreateContext(), CreateCandidate());

        Assert.Equal(result.PopularityScore, result.FinalScore);
    }

    // Verifies that quality-only scoring uses Bayesian rating quality.
    [Fact]
    public void ScoreQualityOnlyUsesRatingQuality()
    {
        var engine = new HybridRecommendationEngine(
            new RecommendationWeights(0f, 0f, 1f));

        var result = engine.Score(CreateContext(), CreateCandidate());

        Assert.Equal(result.RatingQualityScore, result.FinalScore);
    }

    // Cold-start handling

    // Verifies that an empty profile produces a null content score.
    [Fact]
    public void ScoreEmptyProfileReturnsNullContentScore()
    {
        var result = _engine.Score(
            CreateContext(), CreateCandidate(genres: ["Action"]));

        Assert.Null(result.ContentScore);
    }

    // Verifies that unavailable content redistributes the remaining weights.
    [Fact]
    public void ScoreColdStartRedistributesRemainingWeights()
    {
        var result = _engine.Score(CreateContext(),
            CreateCandidate(reviewCount: 99, averageRating: 3f));

        Assert.Null(result.ContentScore);
        Assert.Equal(0.5f, result.FinalScore, 6);
    }

    // Verifies that missing candidate metadata produces a null content score.
    [Fact]
    public void ScoreMissingCandidateMetadataReturnsNullContent()
    {
        var result = _engine.Score(
            CreateContext(CreateActionProfile()), CreateCandidate());

        Assert.Null(result.ContentScore);
    }

    // Verifies that missing content differs from unrelated content.
    [Fact]
    public void ScoreMissingContentDiffersFromZeroContent()
    {
        var context = CreateContext(CreateActionProfile());

        var missing = _engine.Score(context, CreateCandidate());
        var unrelated = _engine.Score(context, CreateCandidate(genres: ["Romance"]));

        Assert.Null(missing.ContentScore);
        Assert.Equal(0f, unrelated.ContentScore!.Value);
        Assert.True(missing.FinalScore > unrelated.FinalScore);
    }

    // Verifies that content-only scoring returns zero without preferences.
    [Fact]
    public void ScoreContentOnlyWithoutPreferencesReturnsZero()
    {
        var engine = new HybridRecommendationEngine(
            new RecommendationWeights(1f, 0f, 0f));

        var result = engine.Score(CreateContext(), CreateCandidate());

        Assert.Null(result.ContentScore);
        Assert.Equal(0f, result.FinalScore);
    }

    // Verifies that media without community reviews use the global prior.
    [Fact]
    public void ScoreEmptyReviewDatabaseUsesGlobalPrior()
    {
        var result = _engine.Score(
            CreateContext(maxReviews: 0),
            CreateCandidate(reviewCount: 0, averageRating: null));

        Assert.Null(result.ContentScore);
        Assert.Equal(0f, result.PopularityScore);
        Assert.Equal(0.5f, result.RatingQualityScore);
        Assert.Equal(1f / 3f, result.FinalScore, 6);
    }

    // Verifies that zero-weight features do not provide content evidence.
    [Fact]
    public void ScoreZeroWeightedFeatureDoesNotProvideEvidence()
    {
        var engine = new HybridRecommendationEngine(
            contentWeights: new ContentBasedWeights(Genre: 0f));

        var result = engine.Score(
            CreateContext(CreateActionProfile()),
            CreateCandidate(genres: ["Action"]));

        Assert.Null(result.ContentScore);
    }

    // Media-specific personalization

    // Verifies that movie recommendations use director preferences.
    [Fact]
    public void ScoreMovieUsesDirectorPreferences()
    {
        var profile = CreateProfile(
            directors: new() { ["Christopher Nolan"] = 5f });

        var result = _engine.Score(CreateContext(profile),
            CreateCandidate(directors: ["Christopher Nolan"]));

        Assert.Equal(1f, result.ContentScore!.Value, 6);
    }

    // Verifies that book recommendations use author preferences.
    [Fact]
    public void ScoreBookUsesAuthorPreferences()
    {
        var profile = CreateProfile(
            authors: new() { ["J.R.R. Tolkien"] = 5f });

        var result = _engine.Score(CreateContext(profile),
            CreateCandidate(mediaType: "book", authors: ["J.R.R. Tolkien"]));

        Assert.Equal(1f, result.ContentScore!.Value, 6);
    }

    // Verifies that TV recommendations use season count similarity.
    [Fact]
    public void ScoreTvUsesSeasonCountPreferences()
    {
        var result = _engine.Score(
            CreateContext(CreateProfile(seasons: 3f)),
            CreateCandidate(mediaType: "tv", seasons: 5));

        Assert.Equal((float)Math.Exp(-0.5), result.ContentScore!.Value, 6);
    }

    // Verifies that game recommendations use studio preferences.
    [Fact]
    public void ScoreGameUsesStudioPreferences()
    {
        var profile = CreateProfile(
            studios: new() { ["FromSoftware"] = 5f });

        var result = _engine.Score(CreateContext(profile),
            CreateCandidate(mediaType: "game", studios: ["FromSoftware"]));

        Assert.Equal(1f, result.ContentScore!.Value, 6);
    }

    // Verifies that movies ignore book-specific preferences.
    [Fact]
    public void ScoreMovieIgnoresBookSpecificPreferences()
    {
        var result = _engine.Score(
            CreateContext(CreateProfile(pageCount: 300f)),
            CreateCandidate(mediaType: "movie", pageCount: 300));

        Assert.Null(result.ContentScore);
    }

    // Ranking and filtering

    // Verifies that recommendations are ordered by descending scores.
    [Fact]
    public void RecommendReturnsDescendingScores()
    {
        var results = _engine.Recommend(CreateContext(),
        [
            CreateCandidate(reviewCount: 10),
            CreateCandidate(reviewCount: 1000),
            CreateCandidate(reviewCount: 100)
        ]);

        Assert.Equal(3, results.Count);

        for (int i = 1; i < results.Count; i++)
            Assert.True(results[i - 1].FinalScore >= results[i].FinalScore);
    }

    // Verifies that the requested recommendation limit is respected.
    [Fact]
    public void RecommendReturnsRequestedLimit()
    {
        Assert.Equal(5, _engine.Recommend(
            CreateContext(), CreateCandidates(30), limit: 5).Count);
    }

    // Verifies that the default recommendation limit is twenty.
    [Fact]
    public void RecommendDefaultLimitIsTwenty()
    {
        Assert.Equal(20, _engine.Recommend(
            CreateContext(), CreateCandidates(30)).Count);
    }

    // Verifies that limits above the candidate count return all candidates.
    [Fact]
    public void RecommendLimitAboveCandidateCountReturnsAll()
    {
        Assert.Equal(2, _engine.Recommend(
            CreateContext(), CreateCandidates(2), limit: 100).Count);
    }

    // Verifies that previously consumed media are excluded.
    [Fact]
    public void RecommendExcludesPreviouslyConsumedMedia()
    {
        var consumed = CreateCandidate();
        var unseen = CreateCandidate();

        var context = CreateContext(
            excluded: new HashSet<Guid> { consumed.Media.MediaId });

        var results = _engine.Recommend(context, [consumed, unseen]);

        Assert.Single(results);
        Assert.Equal(unseen.Media.MediaId, results[0].MediaId);
    }

    // Verifies that excluding every candidate produces no recommendations.
    [Fact]
    public void RecommendAllCandidatesExcludedReturnsEmpty()
    {
        var candidate = CreateCandidate();
        var context = CreateContext(
            excluded: new HashSet<Guid> { candidate.Media.MediaId });

        Assert.Empty(_engine.Recommend(context, [candidate]));
    }

    // Verifies that an empty candidate collection produces no recommendations.
    [Fact]
    public void RecommendEmptyCandidatesReturnsEmpty()
    {
        Assert.Empty(_engine.Recommend(CreateContext(), []));
    }

    // Verifies that duplicate candidate identifiers are rejected.
    [Fact]
    public void RecommendDuplicateCandidatesThrowException()
    {
        var candidate = CreateCandidate();

        Assert.Throws<ArgumentException>(() =>
            _engine.Recommend(CreateContext(), [candidate, candidate]));
    }

    // Verifies that equal scores are ordered deterministically by media ID.
    [Fact]
    public void RecommendEqualScoresAreOrderedByMediaId()
    {
        Guid first = Guid.Parse("00000000-0000-0000-0000-000000000001");
        Guid second = Guid.Parse("00000000-0000-0000-0000-000000000002");

        var results = _engine.Recommend(CreateContext(),
        [
            CreateCandidate(mediaId: second),
            CreateCandidate(mediaId: first)
        ]);

        Assert.Equal(first, results[0].MediaId);
        Assert.Equal(second, results[1].MediaId);
    }

    // Verifies that excluded candidates are removed before applying the limit.
    [Fact]
    public void RecommendFiltersBeforeApplyingLimit()
    {
        var consumed = CreateCandidate(reviewCount: 9999, averageRating: 5f);
        var unseen = CreateCandidate(reviewCount: 1, averageRating: 1f);

        var context = CreateContext(
            excluded: new HashSet<Guid> { consumed.Media.MediaId });

        var result = Assert.Single(
            _engine.Recommend(context, [consumed, unseen], limit: 1));

        Assert.Equal(unseen.Media.MediaId, result.MediaId);
    }

    // Input and configuration validation

    // Verifies that invalid recommendation weight configurations are rejected.
    [Fact]
    public void ConstructorInvalidWeightsThrowException()
    {
        var invalidWeights = new[]
        {
            new RecommendationWeights(-1f, 0.1f, 0.2f),
            new RecommendationWeights(float.NaN, 0.1f, 0.2f),
            new RecommendationWeights(float.PositiveInfinity, 0.1f, 0.2f),
            new RecommendationWeights(float.NegativeInfinity, 0.1f, 0.2f),
            new RecommendationWeights(1f, -1f, 1f),
            new RecommendationWeights(1f, 1f, -1f),
            new RecommendationWeights(0f, 0f, 0f)
        };

        foreach (var weights in invalidWeights)
            Assert.Throws<ArgumentException>(() =>
                new HybridRecommendationEngine(weights));
    }

    // Verifies that null recommendation contexts are rejected.
    [Fact]
    public void ScoreNullContextThrowsException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            _engine.Score(null!, CreateCandidate()));
    }

    // Verifies that null recommendation candidates are rejected.
    [Fact]
    public void ScoreNullCandidateThrowsException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            _engine.Score(CreateContext(), null!));
    }

    // Verifies that candidates containing null media are rejected.
    [Fact]
    public void ScoreNullMediaThrowsException()
    {
        var candidate = new RecommendationCandidate(null!, 10, 4f);

        Assert.Throws<ArgumentNullException>(() =>
            _engine.Score(CreateContext(), candidate));
    }

    // Verifies that contexts containing null profiles are rejected.
    [Fact]
    public void ScoreNullProfileThrowsException()
    {
        var context = CreateContext() with { Profile = null! };

        Assert.Throws<ArgumentNullException>(() =>
            _engine.Score(context, CreateCandidate()));
    }

    // Verifies that contexts containing null exclusion sets are rejected.
    [Fact]
    public void ScoreNullExcludedSetThrowsException()
    {
        var context = CreateContext() with { ExcludedMediaIds = null! };

        Assert.Throws<ArgumentNullException>(() =>
            _engine.Score(context, CreateCandidate()));
    }

    // Verifies that negative maximum review counts are rejected.
    [Fact]
    public void ScoreNegativeMaximumReviewsThrowsException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _engine.Score(CreateContext(maxReviews: -1), CreateCandidate()));
    }

    // Verifies that invalid global average ratings are rejected.
    [Fact]
    public void ScoreInvalidGlobalAverageThrowsException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _engine.Score(CreateContext(globalAverage: 6f), CreateCandidate()));
    }

    // Verifies that nonpositive Bayesian prior weights are rejected.
    [Fact]
    public void ScoreInvalidPriorThrowsException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _engine.Score(CreateContext(priorWeight: 0f), CreateCandidate()));
    }

    // Verifies that null candidate collections are rejected.
    [Fact]
    public void RecommendNullCandidatesThrowsException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            _engine.Recommend(CreateContext(), null!));
    }

    // Verifies that zero and negative recommendation limits are rejected.
    [Fact]
    public void RecommendInvalidLimitThrowsException()
    {
        foreach (int limit in new[] { 0, -1 })
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                _engine.Recommend(CreateContext(), [CreateCandidate()], limit));
    }

    // Verifies that negative candidate review counts are rejected.
    [Fact]
    public void ScoreNegativeReviewCountThrowsException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _engine.Score(CreateContext(), CreateCandidate(reviewCount: -1)));
    }

    // Verifies that reviewed media require an average rating.
    [Fact]
    public void ScoreMissingAverageWithReviewsThrowsException()
    {
        Assert.Throws<ArgumentException>(() =>
            _engine.Score(CreateContext(),
                CreateCandidate(reviewCount: 100, averageRating: null)));
    }

    // Algorithm properties

    // Verifies that proportionally scaling weights preserves final scores.
    [Fact]
    public void ScoreUniformWeightScalingDoesNotChangeResult()
    {
        var first = new HybridRecommendationEngine(
            new RecommendationWeights(0.7f, 0.1f, 0.2f));

        var second = new HybridRecommendationEngine(
            new RecommendationWeights(70f, 10f, 20f));

        var context = CreateContext(CreateActionProfile());
        var candidate = CreateCandidate(genres: ["Action"]);

        Assert.Equal(first.Score(context, candidate).FinalScore,
            second.Score(context, candidate).FinalScore, 6);
    }

    // Verifies that 100 repeated scoring calls produce identical results.
    [Fact]
    public void ScoreRepeatedCallsReturnIdenticalResults()
    {
        var context = CreateContext(CreateActionProfile());
        var candidate = CreateCandidate(genres: ["Action"]);

        var expected = _engine.Score(context, candidate);

        for (int i = 0; i < 100; i++)
            Assert.Equal(expected, _engine.Score(context, candidate));
    }

    // Verifies that candidate input order does not affect ranking.
    [Fact]
    public void RecommendInputOrderDoesNotChangeRanking()
    {
        var context = CreateContext();
        var candidates = Enumerable.Range(0, 30)
            .Select(i => CreateCandidate(reviewCount: i + 1)).ToArray();

        var first = _engine.Recommend(context, candidates);
        var second = _engine.Recommend(context, candidates.Reverse().ToArray());

        Assert.Equal(first.Select(result => result.MediaId).ToArray(),
            second.Select(result => result.MediaId).ToArray());
    }

    // Verifies that generating recommendations does not modify input candidates.
    [Fact]
    public void RecommendDoesNotModifyInputCandidates()
    {
        var candidates = CreateCandidates(30);
        var original = candidates.ToArray();

        _engine.Recommend(CreateContext(), candidates);

        Assert.Equal(original, candidates);
    }

    // Verifies that 1,000 random candidate scores remain finite and within [0, 1].
    [Fact]
    public void ScoreRandomValidCandidatesRemainWithinBounds()
    {
        var random = new Random(42);
        var context = CreateContext(CreateActionProfile());

        for (int i = 0; i < 1000; i++)
        {
            var candidate = CreateCandidate(
                genres: [random.Next(2) == 0 ? "Action" : "Romance"],
                reviewCount: random.Next(1, 10000),
                averageRating: 1f + (float)random.NextDouble() * 4f);

            var result = _engine.Score(context, candidate);

            Assert.True(float.IsFinite(result.FinalScore));
            Assert.InRange(result.FinalScore, 0f, 1f);
        }
    }

    // Verifies that maximum floating-point weights do not cause overflow.
    [Fact]
    public void ScoreVeryLargeWeightsDoNotOverflow()
    {
        var engine = new HybridRecommendationEngine(new RecommendationWeights(
            float.MaxValue, float.MaxValue, float.MaxValue));

        var result = engine.Score(
            CreateContext(CreateActionProfile()),
            CreateCandidate(genres: ["Action"]));

        Assert.True(float.IsFinite(result.FinalScore));
        Assert.InRange(result.FinalScore, 0f, 1f);
    }

    // End-to-end integration

    // Verifies that relevant media outrank popular but unrelated media.
    [Fact]
    public void RecommendPersonalizedMediaOutranksPopularMismatch()
    {
        var relevant = CreateCandidate(
            genres: ["Action"], reviewCount: 10, averageRating: 4f);

        var popular = CreateCandidate(
            genres: ["Romance"], reviewCount: 9999, averageRating: 5f);

        var results = _engine.Recommend(
            CreateContext(CreateActionProfile()), [popular, relevant]);

        Assert.Equal(relevant.Media.MediaId, results[0].MediaId);
    }

    // Verifies that community scores determine cold-start rankings.
    [Fact]
    public void RecommendColdStartPrioritizesCommunityScores()
    {
        var weak = CreateCandidate(reviewCount: 10, averageRating: 2f);
        var strong = CreateCandidate(reviewCount: 9999, averageRating: 5f);

        var results = _engine.Recommend(CreateContext(), [weak, strong]);

        Assert.Equal(strong.Media.MediaId, results[0].MediaId);
    }

    // Verifies that generated user preferences influence recommendation rankings.
    [Fact]
    public void RecommendProfileBuilderPreferencesInfluenceRanking()
    {
        var history = CreateCandidate(genres: ["Action"], runtime: 120);

        var profile = UserProfileBuilder.Build(Guid.NewGuid(),
            [new WeightedMediaInteraction(history.Media, 5f)]);

        var relevant = CreateCandidate(genres: ["Action"], runtime: 120);
        var unrelated = CreateCandidate(genres: ["Romance"], runtime: 180);

        var results = _engine.Recommend(
            CreateContext(profile), [unrelated, relevant]);

        Assert.Equal(relevant.Media.MediaId, results[0].MediaId);
    }

    // Verifies the complete interaction, personalization and filtering pipeline.
    [Fact]
    public void RecommendCompleteInteractionPipelineProducesPersonalizedRanking()
    {
        Guid userId = Guid.NewGuid();

        var action = CreateCandidate(genres: ["Action"], runtime: 120);
        var romance = CreateCandidate(genres: ["Romance"], runtime: 120);

        var interactions = new[]
        {
            new UserInteraction(userId, action.Media.MediaId,
                InteractionType.Completed, DateTimeOffset.UtcNow),

            new UserInteraction(userId, action.Media.MediaId,
                InteractionType.PositiveReview, DateTimeOffset.UtcNow),

            new UserInteraction(userId, romance.Media.MediaId,
                InteractionType.Search, DateTimeOffset.UtcNow)
        };

        var features = new Dictionary<Guid, MediaFeatures>
        {
            [action.Media.MediaId] = action.Media,
            [romance.Media.MediaId] = romance.Media
        };

        var scorer = new InteractionScorer();
        var weighted = scorer.BuildWeightedInteractions(userId, interactions, features);
        var profile = UserProfileBuilder.Build(userId, weighted);

        var unseenAction = CreateCandidate(genres: ["Action"], runtime: 120);
        var unseenRomance = CreateCandidate(genres: ["Romance"], runtime: 120);

        var context = CreateContext(profile,
            excluded: new HashSet<Guid> { action.Media.MediaId });

        var results = _engine.Recommend(
            context, [unseenRomance, action, unseenAction]);

        Assert.Equal(2, results.Count);
        Assert.Equal(unseenAction.Media.MediaId, results[0].MediaId);
        Assert.True(results[0].FinalScore > results[1].FinalScore);
        Assert.DoesNotContain(results, result => result.MediaId == action.Media.MediaId);
    }
}