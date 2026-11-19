
using System;
using System.Collections.Generic;
using System.Linq;
using Palette.Api.MachineLearning.ContentBased;
using Palette.Api.MachineLearning.Hybrid;
using Palette.Api.MachineLearning.ImplicitFeedback;
using Xunit;

public class HybridRecommendationEngineTests
{
    // ==================================================
    // Helper Methods
    // ==================================================

    // Creates a user-preference profile fixture.
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
            Guid.NewGuid(),
            genres ?? new Dictionary<string, float>(),
            authors ?? new Dictionary<string, float>(),
            directors ?? new Dictionary<string, float>(),
            studios ?? new Dictionary<string, float>(),
            publishers ?? new Dictionary<string, float>(),
            runtime,
            pageCount,
            seasons);
    }

    // Creates a recommendation candidate fixture.
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
            mediaId ?? Guid.NewGuid(),
            mediaType,
            genres ?? Array.Empty<string>(),
            authors ?? Array.Empty<string>(),
            directors ?? Array.Empty<string>(),
            studios ?? Array.Empty<string>(),
            publishers ?? Array.Empty<string>(),
            runtime,
            pageCount,
            seasons);

        return new RecommendationCandidate(
            media,
            reviewCount,
            averageRating);
    }

    // Creates a recommendation context fixture.
    private static RecommendationContext CreateContext(
        UserPreferenceProfile? profile = null,
        IReadOnlySet<Guid>? excluded = null,
        int maxReviews = 9999,
        float globalAverage = 3f,
        float priorWeight = 50f)
    {
        return new RecommendationContext(
            profile ?? CreateProfile(),
            excluded ?? new HashSet<Guid>(),
            maxReviews,
            globalAverage,
            priorWeight);
    }

    // Creates test data for action profile.
    private static UserPreferenceProfile CreateActionProfile()
    {
        return CreateProfile(
            genres: new Dictionary<string, float>
            {
                ["Action"] = 5f
            });
    }

    // ==================================================
    // Hybrid Scoring Accuracy Tests (8)
    // ==================================================

    [Fact]
    // Verifies that scoring returns the expected weighted average.
    public void ScoreReturnsExpectedWeightedAverage()
    {
        var engine = new HybridRecommendationEngine();

        var context = CreateContext(CreateActionProfile());

        var candidate = CreateCandidate(
            genres: new[] { "Action" },
            reviewCount: 99,
            averageRating: 3f);

        var result = engine.Score(context, candidate);

        // Content = 1
        // Popularity = ln(100) / ln(10000) = 0.5
        // Rating quality = (3 - 1) / 4 = 0.5
        // Final = 0.7 + 0.05 + 0.1 = 0.85

        Assert.Equal(1f, result.ContentScore!.Value, 6);
        Assert.Equal(0.5f, result.PopularityScore, 6);
        Assert.Equal(0.5f, result.RatingQualityScore, 6);
        Assert.Equal(0.85f, result.FinalScore, 6);
    }

    [Fact]
    // Verifies that unrelated media receives a zero content score.
    public void ScoreUnrelatedMediaReceivesZeroContentScore()
    {
        var engine = new HybridRecommendationEngine();

        var result = engine.Score(
            CreateContext(CreateActionProfile()),
            CreateCandidate(
                genres: new[] { "Romance" },
                reviewCount: 99,
                averageRating: 3f));

        Assert.Equal(0f, result.ContentScore!.Value);
        Assert.Equal(0.15f, result.FinalScore, 6);
    }

    [Fact]
    // Verifies that perfect component scores produce a final score of one.
    public void ScorePerfectComponentsReturnOne()
    {
        var engine = new HybridRecommendationEngine();

        var result = engine.Score(
            CreateContext(
                CreateActionProfile(),
                globalAverage: 5f),
            CreateCandidate(
                genres: new[] { "Action" },
                reviewCount: 9999,
                averageRating: 5f));

        Assert.Equal(1f, result.FinalScore, 6);
    }

    [Fact]
    // Verifies that zero component scores produce a final score of zero.
    public void ScoreZeroComponentsReturnZero()
    {
        var engine = new HybridRecommendationEngine();

        var result = engine.Score(
            CreateContext(
                CreateActionProfile(),
                globalAverage: 1f),
            CreateCandidate(
                genres: new[] { "Romance" },
                reviewCount: 0,
                averageRating: null));

        Assert.Equal(0f, result.FinalScore);
    }

    [Fact]
    // Verifies that scoring supports custom weights.
    public void ScoreSupportsCustomWeights()
    {
        var engine = new HybridRecommendationEngine(
            new RecommendationWeights(2f, 1f, 1f));

        var result = engine.Score(
            CreateContext(CreateActionProfile()),
            CreateCandidate(
                genres: new[] { "Action" },
                reviewCount: 99,
                averageRating: 3f));

        // (2 * 1 + 1 * 0.5 + 1 * 0.5) / 4

        Assert.Equal(0.75f, result.FinalScore, 6);
    }

    [Fact]
    // Verifies that scoring with content only uses content similarity.
    public void ScoreContentOnlyUsesContentSimilarity()
    {
        var engine = new HybridRecommendationEngine(
            new RecommendationWeights(1f, 0f, 0f));

        var result = engine.Score(
            CreateContext(CreateActionProfile()),
            CreateCandidate(
                genres: new[] { "Action" }));

        Assert.Equal(
            result.ContentScore!.Value,
            result.FinalScore);
    }

    [Fact]
    // Verifies that scoring with popularity only uses popularity.
    public void ScorePopularityOnlyUsesPopularity()
    {
        var engine = new HybridRecommendationEngine(
            new RecommendationWeights(0f, 1f, 0f));

        var result = engine.Score(
            CreateContext(),
            CreateCandidate());

        Assert.Equal(
            result.PopularityScore,
            result.FinalScore);
    }

    [Fact]
    // Verifies that scoring with quality only uses rating quality.
    public void ScoreQualityOnlyUsesRatingQuality()
    {
        var engine = new HybridRecommendationEngine(
            new RecommendationWeights(0f, 0f, 1f));

        var result = engine.Score(
            CreateContext(),
            CreateCandidate());

        Assert.Equal(
            result.RatingQualityScore,
            result.FinalScore);
    }

    // ==================================================
    // Cold-Start Handling Tests (7)
    // ==================================================

    [Fact]
    // Verifies that scoring with empty profile returns null content score.
    public void ScoreEmptyProfileReturnsNullContentScore()
    {
        var engine = new HybridRecommendationEngine();

        var result = engine.Score(
            CreateContext(),
            CreateCandidate(
                genres: new[] { "Action" }));

        Assert.Null(result.ContentScore);
    }

    [Fact]
    // Verifies that cold-start scoring redistributes weight when content is unavailable.
    public void ScoreColdStartRedistributesRemainingWeights()
    {
        var engine = new HybridRecommendationEngine();

        var result = engine.Score(
            CreateContext(),
            CreateCandidate(
                reviewCount: 99,
                averageRating: 3f));

        Assert.Null(result.ContentScore);
        Assert.Equal(0.5f, result.FinalScore, 6);
    }

    [Fact]
    // Verifies that scoring with missing candidate metadata returns null content.
    public void ScoreMissingCandidateMetadataReturnsNullContent()
    {
        var engine = new HybridRecommendationEngine();

        var result = engine.Score(
            CreateContext(CreateActionProfile()),
            CreateCandidate());

        Assert.Null(result.ContentScore);
    }

    [Fact]
    // Verifies that missing content is distinct from a zero similarity score.
    public void ScoreMissingContentDiffersFromZeroContent()
    {
        var engine = new HybridRecommendationEngine();
        var context = CreateContext(CreateActionProfile());

        var missing = engine.Score(
            context,
            CreateCandidate());

        var unrelated = engine.Score(
            context,
            CreateCandidate(
                genres: new[] { "Romance" }));

        Assert.Null(missing.ContentScore);
        Assert.Equal(0f, unrelated.ContentScore!.Value);

        Assert.True(
            missing.FinalScore > unrelated.FinalScore);
    }

    [Fact]
    // Verifies that scoring with content only without preferences returns zero.
    public void ScoreContentOnlyWithoutPreferencesReturnsZero()
    {
        var engine = new HybridRecommendationEngine(
            new RecommendationWeights(1f, 0f, 0f));

        var result = engine.Score(
            CreateContext(),
            CreateCandidate());

        Assert.Null(result.ContentScore);
        Assert.Equal(0f, result.FinalScore);
    }

    [Fact]
    // Verifies that scoring with empty review database uses global prior.
    public void ScoreEmptyReviewDatabaseUsesGlobalPrior()
    {
        var engine = new HybridRecommendationEngine();

        var result = engine.Score(
            CreateContext(maxReviews: 0),
            CreateCandidate(
                reviewCount: 0,
                averageRating: null));

        // (0.1 * 0 + 0.2 * 0.5) / 0.3

        Assert.Null(result.ContentScore);
        Assert.Equal(0f, result.PopularityScore);
        Assert.Equal(0.5f, result.RatingQualityScore);
        Assert.Equal(1f / 3f, result.FinalScore, 6);
    }

    [Fact]
    // Verifies that scoring with zero weighted feature does not provide evidence.
    public void ScoreZeroWeightedFeatureDoesNotProvideEvidence()
    {
        var engine = new HybridRecommendationEngine(
            contentWeights: new ContentBasedWeights(
                Genre: 0f));

        var result = engine.Score(
            CreateContext(CreateActionProfile()),
            CreateCandidate(
                genres: new[] { "Action" }));

        Assert.Null(result.ContentScore);
    }

    // ==================================================
    // Media-Specific Personalization Tests (5)
    // ==================================================

    [Fact]
    // Verifies that scoring with movie uses director preferences.
    public void ScoreMovieUsesDirectorPreferences()
    {
        var engine = new HybridRecommendationEngine();

        var profile = CreateProfile(
            directors: new Dictionary<string, float>
            {
                ["Christopher Nolan"] = 5f
            });

        var result = engine.Score(
            CreateContext(profile),
            CreateCandidate(
                directors: new[] { "Christopher Nolan" }));

        Assert.Equal(1f, result.ContentScore!.Value, 6);
    }

    [Fact]
    // Verifies that scoring with book uses author preferences.
    public void ScoreBookUsesAuthorPreferences()
    {
        var engine = new HybridRecommendationEngine();

        var profile = CreateProfile(
            authors: new Dictionary<string, float>
            {
                ["J.R.R. Tolkien"] = 5f
            });

        var result = engine.Score(
            CreateContext(profile),
            CreateCandidate(
                mediaType: "book",
                authors: new[] { "J.R.R. Tolkien" }));

        Assert.Equal(1f, result.ContentScore!.Value, 6);
    }

    [Fact]
    // Verifies that scoring with tv uses season count preferences.
    public void ScoreTvUsesSeasonCountPreferences()
    {
        var engine = new HybridRecommendationEngine();

        var result = engine.Score(
            CreateContext(CreateProfile(seasons: 3f)),
            CreateCandidate(
                mediaType: "tv",
                seasons: 5));

        Assert.Equal(
            (float)Math.Exp(-0.5),
            result.ContentScore!.Value,
            6);
    }

    [Fact]
    // Verifies that scoring with game uses studio preferences.
    public void ScoreGameUsesStudioPreferences()
    {
        var engine = new HybridRecommendationEngine();

        var profile = CreateProfile(
            studios: new Dictionary<string, float>
            {
                ["FromSoftware"] = 5f
            });

        var result = engine.Score(
            CreateContext(profile),
            CreateCandidate(
                mediaType: "game",
                studios: new[] { "FromSoftware" }));

        Assert.Equal(1f, result.ContentScore!.Value, 6);
    }

    [Fact]
    // Verifies that scoring with movie ignores book specific preferences.
    public void ScoreMovieIgnoresBookSpecificPreferences()
    {
        var engine = new HybridRecommendationEngine();

        var result = engine.Score(
            CreateContext(CreateProfile(pageCount: 300f)),
            CreateCandidate(
                mediaType: "movie",
                pageCount: 300));

        Assert.Null(result.ContentScore);
    }

    // ==================================================
    // Ranking and Filtering Tests (10)
    // ==================================================

    [Fact]
    // Verifies that generating recommendations returns descending scores.
    public void RecommendReturnsDescendingScores()
    {
        var engine = new HybridRecommendationEngine();

        var candidates = new[]
        {
            CreateCandidate(reviewCount: 10),
            CreateCandidate(reviewCount: 1000),
            CreateCandidate(reviewCount: 100)
        };

        var results = engine.Recommend(
            CreateContext(),
            candidates);

        Assert.Equal(3, results.Count);

        for (int i = 1; i < results.Count; i++)
        {
            Assert.True(
                results[i - 1].FinalScore >=
                results[i].FinalScore);
        }
    }

    [Fact]
    // Verifies that generating recommendations returns requested limit.
    public void RecommendReturnsRequestedLimit()
    {
        var engine = new HybridRecommendationEngine();

        var candidates = Enumerable.Range(0, 30)
            .Select(_ => CreateCandidate())
            .ToArray();

        var results = engine.Recommend(
            CreateContext(),
            candidates,
            limit: 5);

        Assert.Equal(5, results.Count);
    }

    [Fact]
    // Verifies that generating recommendations with default limit is twenty.
    public void RecommendDefaultLimitIsTwenty()
    {
        var engine = new HybridRecommendationEngine();

        var candidates = Enumerable.Range(0, 30)
            .Select(_ => CreateCandidate())
            .ToArray();

        Assert.Equal(
            20,
            engine.Recommend(
                CreateContext(),
                candidates).Count);
    }

    [Fact]
    // Verifies that generating recommendations with limit above candidate count returns all.
    public void RecommendLimitAboveCandidateCountReturnsAll()
    {
        var engine = new HybridRecommendationEngine();

        var candidates = new[]
        {
            CreateCandidate(),
            CreateCandidate()
        };

        Assert.Equal(
            2,
            engine.Recommend(
                CreateContext(),
                candidates,
                limit: 100).Count);
    }

    [Fact]
    // Verifies that generating recommendations excludes previously consumed media.
    public void RecommendExcludesPreviouslyConsumedMedia()
    {
        var engine = new HybridRecommendationEngine();

        var consumed = CreateCandidate();
        var unseen = CreateCandidate();

        var context = CreateContext(
            excluded: new HashSet<Guid>
            {
                consumed.Media.MediaId
            });

        var results = engine.Recommend(
            context,
            new[] { consumed, unseen });

        Assert.Single(results);

        Assert.Equal(
            unseen.Media.MediaId,
            results[0].MediaId);
    }

    [Fact]
    // Verifies that generating recommendations with all candidates excluded returns empty.
    public void RecommendAllCandidatesExcludedReturnsEmpty()
    {
        var engine = new HybridRecommendationEngine();
        var candidate = CreateCandidate();

        var context = CreateContext(
            excluded: new HashSet<Guid>
            {
                candidate.Media.MediaId
            });

        Assert.Empty(
            engine.Recommend(
                context,
                new[] { candidate }));
    }

    [Fact]
    // Verifies that generating recommendations with empty candidates returns empty.
    public void RecommendEmptyCandidatesReturnsEmpty()
    {
        var engine = new HybridRecommendationEngine();

        Assert.Empty(
            engine.Recommend(
                CreateContext(),
                Array.Empty<RecommendationCandidate>()));
    }

    [Fact]
    // Verifies that duplicate candidate media IDs are rejected.
    public void RecommendDuplicateCandidatesThrowException()
    {
        var engine = new HybridRecommendationEngine();
        var candidate = CreateCandidate();

        Assert.Throws<ArgumentException>(() =>
            engine.Recommend(
                CreateContext(),
                new[] { candidate, candidate }));
    }

    [Fact]
    // Verifies that equal recommendation scores are ordered by media ID.
    public void RecommendEqualScoresAreOrderedByMediaId()
    {
        var engine = new HybridRecommendationEngine();

        Guid first = Guid.Parse(
            "00000000-0000-0000-0000-000000000001");

        Guid second = Guid.Parse(
            "00000000-0000-0000-0000-000000000002");

        var results = engine.Recommend(
            CreateContext(),
            new[]
            {
                CreateCandidate(mediaId: second),
                CreateCandidate(mediaId: first)
            });

        Assert.Equal(first, results[0].MediaId);
        Assert.Equal(second, results[1].MediaId);
    }

    [Fact]
    // Verifies that excluded candidates are filtered before the result limit is applied.
    public void RecommendFiltersBeforeApplyingLimit()
    {
        var engine = new HybridRecommendationEngine();

        var consumed = CreateCandidate(
            reviewCount: 9999,
            averageRating: 5f);

        var unseen = CreateCandidate(
            reviewCount: 1,
            averageRating: 1f);

        var context = CreateContext(
            excluded: new HashSet<Guid>
            {
                consumed.Media.MediaId
            });

        var result = Assert.Single(
            engine.Recommend(
                context,
                new[] { consumed, unseen },
                limit: 1));

        Assert.Equal(unseen.Media.MediaId, result.MediaId);
    }

    // ==================================================
    // Input and Configuration Validation Tests (20)
    // ==================================================

    [Fact]
    // Verifies constructing a model for invalid weights throw exception.
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
        {
            Assert.Throws<ArgumentException>(() =>
                new HybridRecommendationEngine(weights));
        }
    }

    [Fact]
    // Verifies that scoring with null context throws an exception.
    public void ScoreNullContextThrowsException()
    {
        var engine = new HybridRecommendationEngine();

        Assert.Throws<ArgumentNullException>(() =>
            engine.Score(null!, CreateCandidate()));
    }

    [Fact]
    // Verifies that scoring with null candidate throws an exception.
    public void ScoreNullCandidateThrowsException()
    {
        var engine = new HybridRecommendationEngine();

        Assert.Throws<ArgumentNullException>(() =>
            engine.Score(CreateContext(), null!));
    }

    [Fact]
    // Verifies that scoring with null media throws an exception.
    public void ScoreNullMediaThrowsException()
    {
        var engine = new HybridRecommendationEngine();

        var candidate = new RecommendationCandidate(
            null!, 10, 4f);

        Assert.Throws<ArgumentNullException>(() =>
            engine.Score(CreateContext(), candidate));
    }

    [Fact]
    // Verifies that scoring with null profile throws an exception.
    public void ScoreNullProfileThrowsException()
    {
        var engine = new HybridRecommendationEngine();

        var context = CreateContext() with
        {
            Profile = null!
        };

        Assert.Throws<ArgumentNullException>(() =>
            engine.Score(context, CreateCandidate()));
    }

    [Fact]
    // Verifies that scoring with null excluded set throws an exception.
    public void ScoreNullExcludedSetThrowsException()
    {
        var engine = new HybridRecommendationEngine();

        var context = CreateContext() with
        {
            ExcludedMediaIds = null!
        };

        Assert.Throws<ArgumentNullException>(() =>
            engine.Score(context, CreateCandidate()));
    }

    [Fact]
    // Verifies that scoring with negative maximum reviews throws an exception.
    public void ScoreNegativeMaximumReviewsThrowsException()
    {
        var engine = new HybridRecommendationEngine();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            engine.Score(
                CreateContext(maxReviews: -1),
                CreateCandidate()));
    }

    [Fact]
    // Verifies that scoring with invalid global average throws an exception.
    public void ScoreInvalidGlobalAverageThrowsException()
    {
        var engine = new HybridRecommendationEngine();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            engine.Score(
                CreateContext(globalAverage: 6f),
                CreateCandidate()));
    }

    [Fact]
    // Verifies that scoring with invalid prior throws an exception.
    public void ScoreInvalidPriorThrowsException()
    {
        var engine = new HybridRecommendationEngine();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            engine.Score(
                CreateContext(priorWeight: 0f),
                CreateCandidate()));
    }

    [Fact]
    // Verifies that generating recommendations with null candidates throws an exception.
    public void RecommendNullCandidatesThrowsException()
    {
        var engine = new HybridRecommendationEngine();

        Assert.Throws<ArgumentNullException>(() =>
            engine.Recommend(CreateContext(), null!));
    }

    [Fact]
    // Verifies that generating recommendations with invalid limit throws an exception.
    public void RecommendInvalidLimitThrowsException()
    {
        var engine = new HybridRecommendationEngine();

        foreach (int limit in new[] { 0, -1 })
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                engine.Recommend(
                    CreateContext(),
                    new[] { CreateCandidate() },
                    limit));
        }
    }

    [Fact]
    // Verifies that scoring with negative review count throws an exception.
    public void ScoreNegativeReviewCountThrowsException()
    {
        var engine = new HybridRecommendationEngine();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            engine.Score(
                CreateContext(),
                CreateCandidate(reviewCount: -1)));
    }

    [Fact]
    // Verifies that scoring with missing average with reviews throws an exception.
    public void ScoreMissingAverageWithReviewsThrowsException()
    {
        var engine = new HybridRecommendationEngine();

        Assert.Throws<ArgumentException>(() =>
            engine.Score(
                CreateContext(),
                CreateCandidate(
                    reviewCount: 100,
                    averageRating: null)));
    }

    // ==================================================
    // Algorithm Property Tests (6)
    // ==================================================

    [Fact]
    // Verifies that scoring with uniform weight scaling does not change result.
    public void ScoreUniformWeightScalingDoesNotChangeResult()
    {
        var first = new HybridRecommendationEngine(
            new RecommendationWeights(0.7f, 0.1f, 0.2f));

        var second = new HybridRecommendationEngine(
            new RecommendationWeights(70f, 10f, 20f));

        var context = CreateContext(CreateActionProfile());

        var candidate = CreateCandidate(
            genres: new[] { "Action" });

        Assert.Equal(
            first.Score(context, candidate).FinalScore,
            second.Score(context, candidate).FinalScore,
            6);
    }

    [Fact]
    // Verifies that repeated scoring calls return identical results.
    public void ScoreRepeatedCallsReturnIdenticalResults()
    {
        var engine = new HybridRecommendationEngine();
        var context = CreateContext(CreateActionProfile());

        var candidate = CreateCandidate(
            genres: new[] { "Action" });

        var expected = engine.Score(context, candidate);

        for (int i = 0; i < 100; i++)
        {
            Assert.Equal(
                expected,
                engine.Score(context, candidate));
        }
    }

    [Fact]
    // Verifies that generating recommendations with input order does not change ranking.
    public void RecommendInputOrderDoesNotChangeRanking()
    {
        var engine = new HybridRecommendationEngine();
        var context = CreateContext();

        var candidates = Enumerable.Range(0, 30)
            .Select(i => CreateCandidate(
                reviewCount: i + 1))
            .ToArray();

        var first = engine.Recommend(
            context, candidates);

        var second = engine.Recommend(
            context,
            candidates.Reverse().ToArray());

        Assert.Equal(
            first.Select(result => result.MediaId).ToArray(),
            second.Select(result => result.MediaId).ToArray());
    }

    [Fact]
    // Verifies that generating recommendations does not modify input candidates.
    public void RecommendDoesNotModifyInputCandidates()
    {
        var engine = new HybridRecommendationEngine();

        var candidates = Enumerable.Range(0, 30)
            .Select(_ => CreateCandidate())
            .ToArray();

        var original = candidates.ToArray();

        engine.Recommend(CreateContext(), candidates);

        Assert.Equal(original, candidates);
    }

    [Fact]
    // Verifies that scores for random valid candidates remain within [0, 1].
    public void ScoreRandomValidCandidatesRemainWithinBounds()
    {
        var engine = new HybridRecommendationEngine();
        var context = CreateContext(CreateActionProfile());

        var random = new Random(42);

        for (int i = 0; i < 1000; i++)
        {
            var candidate = CreateCandidate(
                genres: new[]
                {
                    random.Next(2) == 0
                        ? "Action"
                        : "Romance"
                },
                reviewCount: random.Next(1, 10000),
                averageRating:
                    1f + (float)random.NextDouble() * 4f);

            var result = engine.Score(context, candidate);

            Assert.True(float.IsFinite(result.FinalScore));
            Assert.InRange(result.FinalScore, 0f, 1f);
        }
    }

    [Fact]
    // Verifies that very large weights produce a finite score without overflow.
    public void ScoreVeryLargeWeightsDoNotOverflow()
    {
        var engine = new HybridRecommendationEngine(
            new RecommendationWeights(
                float.MaxValue,
                float.MaxValue,
                float.MaxValue));

        var result = engine.Score(
            CreateContext(CreateActionProfile()),
            CreateCandidate(
                genres: new[] { "Action" }));

        Assert.True(float.IsFinite(result.FinalScore));
        Assert.InRange(result.FinalScore, 0f, 1f);
    }

    // ==================================================
    // End-to-End Integration Tests (4)
    // ==================================================

    [Fact]
    // Verifies that generating recommendations with personalized media outranks popular mismatch.
    public void RecommendPersonalizedMediaOutranksPopularMismatch()
    {
        var engine = new HybridRecommendationEngine();

        var relevant = CreateCandidate(
            genres: new[] { "Action" },
            reviewCount: 10,
            averageRating: 4f);

        var popular = CreateCandidate(
            genres: new[] { "Romance" },
            reviewCount: 9999,
            averageRating: 5f);

        var results = engine.Recommend(
            CreateContext(CreateActionProfile()),
            new[] { popular, relevant });

        Assert.Equal(
            relevant.Media.MediaId,
            results[0].MediaId);
    }

    [Fact]
    // Verifies that community scores determine ranking when no profile is available.
    public void RecommendColdStartPrioritizesCommunityScores()
    {
        var engine = new HybridRecommendationEngine();

        var weak = CreateCandidate(
            reviewCount: 10,
            averageRating: 2f);

        var strong = CreateCandidate(
            reviewCount: 9999,
            averageRating: 5f);

        var results = engine.Recommend(
            CreateContext(),
            new[] { weak, strong });

        Assert.Equal(
            strong.Media.MediaId,
            results[0].MediaId);
    }

    [Fact]
    // Verifies that preferences from the profile builder affect recommendation ranking.
    public void RecommendProfileBuilderPreferencesInfluenceRanking()
    {
        Guid userId = Guid.NewGuid();

        var history = CreateCandidate(
            genres: new[] { "Action" },
            runtime: 120);

        var profile = UserProfileBuilder.Build(
            userId,
            new[]
            {
                new WeightedMediaInteraction(
                    history.Media, 5f)
            });

        var relevant = CreateCandidate(
            genres: new[] { "Action" },
            runtime: 120);

        var unrelated = CreateCandidate(
            genres: new[] { "Romance" },
            runtime: 180);

        var engine = new HybridRecommendationEngine();

        var results = engine.Recommend(
            CreateContext(profile),
            new[] { unrelated, relevant });

        Assert.Equal(
            relevant.Media.MediaId,
            results[0].MediaId);
    }

    [Fact]
    // Verifies that weighted interactions build preferences that rank matching unseen media higher while excluding consumed media.
    public void RecommendCompleteInteractionPipelineProducesPersonalizedRanking()
    {
        Guid userId = Guid.NewGuid();

        var action = CreateCandidate(
            genres: new[] { "Action" },
            runtime: 120);

        var romance = CreateCandidate(
            genres: new[] { "Romance" },
            runtime: 120);

        var interactions = new[]
        {
            new UserInteraction(
                userId,
                action.Media.MediaId,
                InteractionType.Completed,
                DateTimeOffset.UtcNow),

            new UserInteraction(
                userId,
                action.Media.MediaId,
                InteractionType.PositiveReview,
                DateTimeOffset.UtcNow),

            new UserInteraction(
                userId,
                romance.Media.MediaId,
                InteractionType.Search,
                DateTimeOffset.UtcNow)
        };

        var features = new Dictionary<Guid, MediaFeatures>
        {
            [action.Media.MediaId] = action.Media,
            [romance.Media.MediaId] = romance.Media
        };

        var interactionScorer = new InteractionScorer();

        var weightedInteractions =
            interactionScorer.BuildWeightedInteractions(
                userId,
                interactions,
                features);

        var profile = UserProfileBuilder.Build(
            userId,
            weightedInteractions);

        var unseenAction = CreateCandidate(
            genres: new[] { "Action" },
            runtime: 120);

        var unseenRomance = CreateCandidate(
            genres: new[] { "Romance" },
            runtime: 120);

        var context = CreateContext(
            profile,
            excluded: new HashSet<Guid>
            {
                action.Media.MediaId
            });

        var engine = new HybridRecommendationEngine();

        var results = engine.Recommend(
            context,
            new[]
            {
                unseenRomance,
                action,
                unseenAction
            });

        Assert.Equal(2, results.Count);

        Assert.Equal(
            unseenAction.Media.MediaId,
            results[0].MediaId);

        Assert.True(
            results[0].FinalScore >
            results[1].FinalScore);

        Assert.DoesNotContain(
            results,
            result =>
                result.MediaId == action.Media.MediaId);
    }
}