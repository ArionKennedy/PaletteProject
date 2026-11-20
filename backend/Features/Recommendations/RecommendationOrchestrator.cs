
using System.Security.Cryptography;
using System.Text;
using Backend.Features.Recommendations.Data;
using Backend.Features.Recommendations.Discovery;
using Backend.Providers.Tmdb;
using Palette.Api.MachineLearning.ContentBased;
using Palette.Api.MachineLearning.Hybrid;
using Palette.Api.MachineLearning.ImplicitFeedback;

namespace Backend.Features.Recommendations;

public sealed record RecommendedMovie(
    int TmdbMovieId,
    string Title,
    string? PosterPath,
    string? ReleaseDate,
    float FinalScore,
    float? ContentScore,
    float PopularityScore,
    float RatingQualityScore
);

public sealed class RecommendationOrchestrator
{
    private readonly UserInteractionService _interactions;
    private readonly MediaMetadataService _metadata;
    private readonly MovieCandidateDiscoveryService _discovery;
    private readonly MovieCandidateFilterService _filter;
    private readonly TmdbClient _tmdb;

    public RecommendationOrchestrator(
        UserInteractionService interactions,
        MediaMetadataService metadata,
        MovieCandidateDiscoveryService discovery,
        MovieCandidateFilterService filter,
        TmdbClient tmdb)
    {
        _interactions = interactions;
        _metadata = metadata;
        _discovery = discovery;
        _filter = filter;
        _tmdb = tmdb;
    }

    public async Task<IReadOnlyList<RecommendedMovie>> RecommendMoviesAsync(
        Guid userId,
        int limit = 20,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException(
                "Invalid user ID.",
                nameof(userId));
        }

        if (limit < 1 || limit > 50)
        {
            throw new ArgumentOutOfRangeException(
                nameof(limit));
        }

        Console.WriteLine(
            "[Recommendations] Starting recommendation request.");

        // STEP 1: Retrieve user interactions.
        var interactions = await _interactions.GetInteractionsAsync(
            userId,
            cancellationToken);

        Console.WriteLine(
            $"[Recommendations] User interactions: {interactions.Count}");

        var mediaIds = interactions
            .Select(i => i.MediaId)
            .Distinct()
            .ToArray();

        // STEP 2: Retrieve metadata for interacted media.
        var features = await _metadata.GetMediaFeaturesAsync(
            mediaIds,
            cancellationToken);

        Console.WriteLine(
            $"[Recommendations] Media features: {features.Count}");

        var featureLookup = features.ToDictionary(
            f => f.MediaId);

        // STEP 3: Calculate interaction weights.
        var interactionScorer = new InteractionScorer();

        var weightedInteractions =
            interactionScorer.BuildWeightedInteractions(
                userId,
                interactions,
                featureLookup);

        Console.WriteLine(
            $"[Recommendations] Weighted interactions: {weightedInteractions.Count}");

        // STEP 4: Build user preference profile.
        var profile = UserProfileBuilder.Build(
            userId,
            weightedInteractions);

        var preferredGenres = profile.GenrePreferences
            .Where(g => g.Value > 0)
            .OrderByDescending(g => g.Value)
            .Select(g => g.Key)
            .Take(3)
            .ToArray();

        Console.WriteLine(
            $"[Recommendations] Preferred genres: {string.Join(", ", preferredGenres)}");

        // No preferred genres: general recommendations.
        if (preferredGenres.Length == 0)
        {
            Console.WriteLine(
                "[Recommendations] Using general fallback: No preferred genres.");

            return await GetGeneralMoviesAsync(
                userId,
                limit,
                cancellationToken);
        }

        // STEP 5: Discover movies matching preferences.
        var discovered = await _discovery.DiscoverAsync(
            preferredGenres,
            cancellationToken: cancellationToken);

        Console.WriteLine(
            $"[Recommendations] Discovered movies: {discovered.Count}");

        // Remove movies the user has completed.
        var filtered = await _filter.FilterInteractedAsync(
            userId,
            discovered,
            cancellationToken);

        Console.WriteLine(
            $"[Recommendations] Movies after filtering: {filtered.Count}");

        var selected = filtered
            .DistinctBy(movie => movie.Id)
            .Take(30)
            .ToArray();

        Console.WriteLine(
            $"[Recommendations] Selected candidates: {selected.Length}");

        if (selected.Length == 0)
        {
            Console.WriteLine(
                "[Recommendations] Using general fallback: No eligible candidates.");

            return await GetGeneralMoviesAsync(
                userId,
                limit,
                cancellationToken);
        }

        // STEP 6: Build candidates for the hybrid engine.
        var candidates = new List<RecommendationCandidate>();

        var candidateLookup =
            new Dictionary<Guid, TmdbMovieSummary>();

        foreach (var movie in selected)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var details = await _tmdb.LookupMovieAsync(
                movie.Id,
                cancellationToken);

            if (details == null)
            {
                Console.WriteLine(
                    $"[Recommendations] TMDB details unavailable: {movie.Id}");

                continue;
            }

            var candidateId = CreateCandidateId(movie.Id);

            var genres = details.Genres?
                .Select(g => g.Name)
                .ToArray() ?? [];

            var mediaFeatures = new MediaFeatures(
                MediaId: candidateId,
                MediaType: "movie",
                Genres: genres,
                Authors: [],
                Directors: [],
                Studios: [],
                Publishers: [],
                RuntimeMinutes: details.Runtime);

            // TMDB vote count provides an external
            // popularity signal.
            int voteCount = Math.Max(
                0,
                details.VoteCount);

            // Convert TMDB's 0-10 rating to 1-5.
            float? averageRating = ConvertTmdbRating(
                details.VoteAverage);

            // BayesianRatingScorer requires a valid rating
            // whenever the vote count is positive.
            if (!averageRating.HasValue)
            {
                voteCount = 0;
            }

            candidates.Add(new RecommendationCandidate(
                Media: mediaFeatures,
                ReviewCount: voteCount,
                AverageRating: averageRating));

            candidateLookup[candidateId] = movie;
        }

        Console.WriteLine(
            $"[Recommendations] Valid hybrid candidates: {candidates.Count}");

        if (candidates.Count == 0)
        {
            Console.WriteLine(
                "[Recommendations] Using general fallback: No valid TMDB candidates.");

            return await GetGeneralMoviesAsync(
                userId,
                limit,
                cancellationToken);
        }

        // STEP 7: Calculate population scoring statistics.
        int maxReviewCount = candidates.Max(
            candidate => candidate.ReviewCount);

        var ratedCandidates = candidates
            .Where(candidate =>
                candidate.ReviewCount > 0 &&
                candidate.AverageRating.HasValue)
            .ToArray();

        float globalAverageRating = 3f;

        if (ratedCandidates.Length > 0)
        {
            double totalVotes = ratedCandidates.Sum(
                candidate => (double)candidate.ReviewCount);

            double weightedRatingSum = ratedCandidates.Sum(
                candidate =>
                    (double)candidate.ReviewCount *
                    candidate.AverageRating!.Value);

            globalAverageRating = (float)(
                weightedRatingSum / totalVotes);

            globalAverageRating = Math.Clamp(
                globalAverageRating,
                1f,
                5f);
        }

        Console.WriteLine(
            $"[Recommendations] Maximum vote count: {maxReviewCount}");

        Console.WriteLine(
            $"[Recommendations] Global average rating: {globalAverageRating:F2}");

        // STEP 8: Run the original hybrid recommendation engine.
        var context = new RecommendationContext(
            Profile: profile,
            ExcludedMediaIds: new HashSet<Guid>(),
            MaxReviewCount: maxReviewCount,
            GlobalAverageRating: globalAverageRating);

        var engine = new HybridRecommendationEngine();

        var ranked = engine.Recommend(
            context,
            candidates,
            Math.Min(limit, candidates.Count));

        Console.WriteLine(
            $"[Recommendations] Hybrid engine results: {ranked.Count}");

        if (ranked.Count == 0)
        {
            Console.WriteLine(
                "[Recommendations] Using general fallback: Hybrid engine returned no results.");

            return await GetGeneralMoviesAsync(
                userId,
                limit,
                cancellationToken);
        }

        // STEP 9: Return ranked movies to Explore.
        var recommendations = ranked.Select(result =>
        {
            var movie = candidateLookup[result.MediaId];

            return new RecommendedMovie(
                TmdbMovieId: movie.Id,
                Title: movie.Title,
                PosterPath: movie.PosterPath,
                ReleaseDate: movie.ReleaseDate,
                FinalScore: result.FinalScore,
                ContentScore: result.ContentScore,
                PopularityScore: result.PopularityScore,
                RatingQualityScore: result.RatingQualityScore);
        }).ToArray();

        foreach (var recommendation in recommendations.Take(5))
        {
            Console.WriteLine(
                $"[Recommendations] {recommendation.Title} | " +
                $"Final: {recommendation.FinalScore:F3} | " +
                $"Content: {recommendation.ContentScore?.ToString("F3") ?? "N/A"} | " +
                $"Popularity: {recommendation.PopularityScore:F3} | " +
                $"Quality: {recommendation.RatingQualityScore:F3}");
        }

        Console.WriteLine(
            $"[Recommendations] Returning {recommendations.Length} personalized movies.");

        return recommendations;
    }

    // General recommendations for users without preferences
    // or without eligible personalized candidates.
    private async Task<IReadOnlyList<RecommendedMovie>>
        GetGeneralMoviesAsync(
            Guid userId,
            int limit,
            CancellationToken cancellationToken)
    {
        Console.WriteLine(
            "[Recommendations] Loading general TMDB movies.");

        var results = new List<TmdbMovieSummary>();
        var seenIds = new HashSet<int>();

        for (int page = 1;
             page <= 5 && results.Count < limit;
             page++)
        {
            var response = await _tmdb.DiscoverPopularMoviesAsync(
                page,
                cancellationToken);

            var filtered = await _filter.FilterInteractedAsync(
                userId,
                response.Results,
                cancellationToken);

            foreach (var movie in filtered)
            {
                if (seenIds.Add(movie.Id))
                {
                    results.Add(movie);
                }

                if (results.Count >= limit)
                {
                    break;
                }
            }

            if (page >= response.TotalPages)
            {
                break;
            }
        }

        var generalRecommendations = results
            .Take(limit)
            .Select(movie => new RecommendedMovie(
                TmdbMovieId: movie.Id,
                Title: movie.Title,
                PosterPath: movie.PosterPath,
                ReleaseDate: movie.ReleaseDate,
                FinalScore: 0,
                ContentScore: null,
                PopularityScore: 0,
                RatingQualityScore: 0))
            .ToArray();

        Console.WriteLine(
            $"[Recommendations] Returning {generalRecommendations.Length} general movies.");

        return generalRecommendations;
    }

    // Converts TMDB's 0-10 scale to the 1-5 range
    // expected by BayesianRatingScorer.
    private static float? ConvertTmdbRating(float? rating)
    {
        if (!rating.HasValue ||
            !float.IsFinite(rating.Value) ||
            rating.Value < 2f ||
            rating.Value > 10f)
        {
            return null;
        }

        return rating.Value / 2f;
    }

    // Generates a deterministic ID for each TMDB movie.
    private static Guid CreateCandidateId(int tmdbId)
    {
        var bytes = SHA256.HashData(
            Encoding.UTF8.GetBytes($"tmdb:movie:{tmdbId}"));

        return new Guid(bytes.AsSpan(0, 16));
    }
}
