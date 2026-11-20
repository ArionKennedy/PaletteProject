/* 
    This Recommendation Engine will:
    - Calculate personalized content similarity.
    - Calculate popularity and Bayesian rating scores.
    - Combine scores using configurable weights.
    - Handle new users with no historical preferences.
    - Exclude previously consumed media.
    - Rank candidates and return the top N recommendations.
    - Providing individual score breakdowns.
*/
using System;
using System.Collections.Generic;
using System.Linq;
using Palette.Api.MachineLearning.ContentBased;
using Palette.Api.MachineLearning.Popularity;

namespace Palette.Api.MachineLearning.Hybrid;

public sealed class HybridRecommendationEngine
{
    private readonly RecommendationWeights _weights;
    private readonly ContentBasedWeights _contentWeights;
    private readonly ContentBasedModel _contentModel;

    // Creates a recommender with validated scoring and content weights.
    public HybridRecommendationEngine(
        RecommendationWeights? weights = null,
        ContentBasedWeights? contentWeights = null)
    {
        _weights = weights ?? new RecommendationWeights();

        _contentWeights =
            contentWeights ?? new ContentBasedWeights();

        ValidateWeights(_weights);

        _contentModel = new ContentBasedModel(_contentWeights);
    }

    // Scores one candidate and returns its component score breakdown.
    public RecommendationResult Score(
        RecommendationContext context,
        RecommendationCandidate candidate)
    {
        ValidateContext(context);

        return ScoreCandidate(context, candidate);
    }

    // Scores eligible candidates, sorts by score and media ID, and returns the top results.
    // Previously consumed items are excluded before the requested limit is applied.
    public IReadOnlyList<RecommendationResult> Recommend(
        RecommendationContext context,
        IReadOnlyList<RecommendationCandidate> candidates,
        int limit = 20)
    {
        ValidateContext(context);

        ArgumentNullException.ThrowIfNull(candidates);

        if (limit <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(limit),
                "Recommendation limit must be positive.");
        }

        var results = new List<RecommendationResult>();

        var processedMedia = new HashSet<Guid>();

        foreach (var candidate in candidates)
        {
            ArgumentNullException.ThrowIfNull(candidate);
            ArgumentNullException.ThrowIfNull(candidate.Media);

            Guid mediaId = candidate.Media.MediaId;

            if (context.ExcludedMediaIds.Contains(mediaId))
            {
                continue;
            }

            if (!processedMedia.Add(mediaId))
            {
                throw new ArgumentException(
                    "Recommendation candidates must have unique media IDs.",
                    nameof(candidates));
            }

            results.Add(ScoreCandidate(context, candidate));
        }

        return results
            .OrderByDescending(result => result.FinalScore)
            .ThenBy(result => result.MediaId)
            .Take(limit)
            .ToList()
            .AsReadOnly();
    }

    // Calculates available component scores and their weighted final score.
    private RecommendationResult ScoreCandidate(
        RecommendationContext context,
        RecommendationCandidate candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentNullException.ThrowIfNull(candidate.Media);

        float? contentScore = null;

        if (_weights.Content > 0f &&
            HasComparableFeatures(
                context.Profile, candidate.Media))
        {
            contentScore = _contentModel.Predict(
                context.Profile,
                candidate.Media);
        }

        float popularityScore = PopularityScorer.Score(
            candidate.ReviewCount,
            context.MaxReviewCount);

        float ratingQualityScore = BayesianRatingScorer.Score(
            candidate.ReviewCount,
            candidate.AverageRating,
            context.GlobalAverageRating,
            context.BayesianPriorWeight);

        float finalScore = CalculateFinalScore(
            contentScore,
            popularityScore,
            ratingQualityScore);

        return new RecommendationResult(
            MediaId: candidate.Media.MediaId,
            ContentScore: contentScore,
            PopularityScore: popularityScore,
            RatingQualityScore: ratingQualityScore,
            FinalScore: finalScore);
    }

    // Combines available component scores and clamps the result to [0, 1].
    private float CalculateFinalScore(
        float? contentScore,
        float popularityScore,
        float ratingQualityScore)
    {
        double weightedSum = 0d;
        double totalWeight = 0d;

        if (contentScore.HasValue)
        {
            weightedSum +=
                (double)_weights.Content * contentScore.Value;

            totalWeight += _weights.Content;
        }

        weightedSum +=
            (double)_weights.Popularity * popularityScore;

        totalWeight += _weights.Popularity;

        weightedSum +=
            (double)_weights.RatingQuality * ratingQualityScore;

        totalWeight += _weights.RatingQuality;

        if (totalWeight == 0d)
        {
            return 0f;
        }

        return (float)Math.Clamp(
            weightedSum / totalWeight,
            0d,
            1d);
    }

    // Checks whether the profile and media share a supported feature category.
    private bool HasComparableFeatures(
        UserPreferenceProfile profile,
        MediaFeatures media)
    {
        if (HasCategoricalEvidence(
            profile.GenrePreferences,
            media.Genres,
            _contentWeights.Genre))
        {
            return true;
        }

        string mediaType =
            media.MediaType.Trim().ToLowerInvariant();

        return mediaType switch
        {
            "movie" =>
                HasCategoricalEvidence(
                    profile.DirectorPreferences,
                    media.Directors,
                    _contentWeights.Director) ||

                HasCategoricalEvidence(
                    profile.StudioPreferences,
                    media.Studios,
                    _contentWeights.Studio) ||

                HasLengthEvidence(
                    profile.PreferredRuntimeMinutes,
                    media.RuntimeMinutes,
                    _contentWeights.Length),

            "tv" =>
                HasCategoricalEvidence(
                    profile.DirectorPreferences,
                    media.Directors,
                    _contentWeights.Director) ||

                HasCategoricalEvidence(
                    profile.StudioPreferences,
                    media.Studios,
                    _contentWeights.Studio) ||

                HasLengthEvidence(
                    profile.PreferredNumberOfSeasons,
                    media.NumberOfSeasons,
                    _contentWeights.Length),

            "book" =>
                HasCategoricalEvidence(
                    profile.AuthorPreferences,
                    media.Authors,
                    _contentWeights.Author) ||

                HasCategoricalEvidence(
                    profile.PublisherPreferences,
                    media.Publishers,
                    _contentWeights.Publisher) ||

                HasLengthEvidence(
                    profile.PreferredPageCount,
                    media.PageCount,
                    _contentWeights.Length),

            "game" =>
                HasCategoricalEvidence(
                    profile.StudioPreferences,
                    media.Studios,
                    _contentWeights.Studio),

            _ => false
        };
    }

    // Checks for a positive preference and at least one candidate feature.
    private static bool HasCategoricalEvidence(
        IReadOnlyDictionary<string, float> preferences,
        IReadOnlyList<string> features,
        float weight)
    {
        ArgumentNullException.ThrowIfNull(preferences);
        ArgumentNullException.ThrowIfNull(features);

        return weight > 0f &&
            preferences.Any(preference =>
                !string.IsNullOrWhiteSpace(preference.Key) &&
                preference.Value > 0f) &&

            features.Any(feature =>
                !string.IsNullOrWhiteSpace(feature));
    }

    // Checks whether weighted length scoring has both values to compare.
    private static bool HasLengthEvidence(
        float? preferredLength,
        int? candidateLength,
        float weight)
    {
        return weight > 0f &&
            preferredLength.HasValue &&
            candidateLength.HasValue;
    }

    // Ensures scoring weights are finite, non-negative, and not all zero.
    private static void ValidateWeights(
        RecommendationWeights weights)
    {
        float[] values =
        {
            weights.Content,
            weights.Popularity,
            weights.RatingQuality
        };

        foreach (float weight in values)
        {
            if (!float.IsFinite(weight) || weight < 0f)
            {
                throw new ArgumentException(
                    "Recommendation weights must be finite " +
                    "and non-negative.",
                    nameof(weights));
            }
        }

        if (!values.Any(weight => weight > 0f))
        {
            throw new ArgumentException(
                "At least one recommendation weight " +
                "must be positive.",
                nameof(weights));
        }
    }

    // Validates the recommendation context and its scoring parameters.
    private static void ValidateContext(
        RecommendationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(context.Profile);
        ArgumentNullException.ThrowIfNull(
            context.ExcludedMediaIds);

        if (context.MaxReviewCount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(context.MaxReviewCount));
        }

        if (!float.IsFinite(context.GlobalAverageRating) ||
            context.GlobalAverageRating < 1f ||
            context.GlobalAverageRating > 5f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(context.GlobalAverageRating));
        }

        if (!float.IsFinite(context.BayesianPriorWeight) ||
            context.BayesianPriorWeight <= 0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(context.BayesianPriorWeight));
        }
    }
}
