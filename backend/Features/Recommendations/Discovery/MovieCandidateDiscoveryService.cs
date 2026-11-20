
using Backend.Providers.Tmdb;

namespace Backend.Features.Recommendations.Discovery;

public sealed class MovieCandidateDiscoveryService
{
    private readonly TmdbClient _tmdbClient;

    public MovieCandidateDiscoveryService(TmdbClient tmdbClient)
    {
        ArgumentNullException.ThrowIfNull(tmdbClient);
        _tmdbClient = tmdbClient;
    }

    // Discovers movie candidates from a user's preferred genres.
    // Candidates are not persisted to PostgreSQL.
    public async Task<IReadOnlyList<TmdbMovieSummary>> DiscoverAsync(
        IEnumerable<string> preferredGenres,
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(preferredGenres);

        if (page < 1 || page > 500)
        {
            throw new ArgumentOutOfRangeException(nameof(page));
        }

        var genres = preferredGenres
            .Where(genre => !string.IsNullOrWhiteSpace(genre))
            .Select(genre => genre.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(3)
            .ToArray();

        if (genres.Length == 0)
        {
            return [];
        }

        // Convert genre names to TMDB genre IDs.
        var genreIds = await _tmdbClient.ResolveMovieGenreIdsAsync(
            genres,
            cancellationToken);

        if (genreIds.Count == 0)
        {
            return [];
        }

        var candidates = new Dictionary<int, TmdbMovieSummary>();

        // Discover movies for each preferred genre separately.
        // This allows a movie to match any preferred genre.
        foreach (var genreId in genreIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var response = await _tmdbClient.DiscoverMoviesAsync(
                new[] { genreId },
                page,
                cancellationToken);

            foreach (var movie in response.Results)
            {
                if (movie.Id <= 0)
                {
                    continue;
                }

                // Prevent the same TMDB movie from appearing twice.
                candidates.TryAdd(movie.Id, movie);
            }
        }

        return candidates.Values.ToArray();
    }
}
