
using Backend.Constants;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Backend.Providers.Tmdb;

public sealed class TmdbClient(HttpClient httpClient)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        RespectNullableAnnotations = true,
    };

    public async Task<TmdbSearchResponse<TmdbMovieSummary>> SearchMoviesAsync(
        string query,
        CancellationToken cancellationToken)
    {
        var path =
            $"{TmdbConstants.SEARCH_MOVIE_PATH}?query={Uri.EscapeDataString(query)}";

        using var response = await httpClient.GetAsync(
            path, cancellationToken);

        response.EnsureSuccessStatusCode();

        return await ReadBodyAsync<TmdbSearchResponse<TmdbMovieSummary>>(
            response, cancellationToken);
    }

    public async Task<TmdbSearchResponse<TmdbMovieSummary>> DiscoverMoviesAsync(
        IEnumerable<int> genreIds,
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(genreIds);

        if (page < 1 || page > 500)
            throw new ArgumentOutOfRangeException(nameof(page));

        var ids = genreIds
            .Where(id => id > 0)
            .Distinct()
            .ToArray();

        if (ids.Length == 0)
        {
            throw new ArgumentException(
                "At least one valid genre ID is required.",
                nameof(genreIds));
        }

        var genres = string.Join(",", ids);

        var path =
            $"{TmdbConstants.DISCOVER_MOVIE_PATH}" +
            $"?with_genres={Uri.EscapeDataString(genres)}" +
            $"&page={page}" +
            "&include_adult=false";

        using var response = await httpClient.GetAsync(
            path, cancellationToken);

        response.EnsureSuccessStatusCode();

        return await ReadBodyAsync<TmdbSearchResponse<TmdbMovieSummary>>(
            response, cancellationToken);
    }

    // General discovery for users without a preference profile.
    public async Task<TmdbSearchResponse<TmdbMovieSummary>>
        DiscoverPopularMoviesAsync(
            int page = 1,
            CancellationToken cancellationToken = default)
    {
        if (page < 1 || page > 500)
            throw new ArgumentOutOfRangeException(nameof(page));

        var path =
            $"{TmdbConstants.DISCOVER_MOVIE_PATH}" +
            $"?page={page}" +
            "&sort_by=popularity.desc" +
            "&vote_count.gte=100" +
            "&include_adult=false" +
            "&include_video=false";

        using var response = await httpClient.GetAsync(
            path, cancellationToken);

        response.EnsureSuccessStatusCode();

        return await ReadBodyAsync<TmdbSearchResponse<TmdbMovieSummary>>(
            response, cancellationToken);
    }

    public async Task<TmdbGenreListResponse> GetMovieGenresAsync(
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync(
            TmdbConstants.MOVIE_GENRES_PATH,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        return await ReadBodyAsync<TmdbGenreListResponse>(
            response, cancellationToken);
    }

    public async Task<IReadOnlyList<int>> ResolveMovieGenreIdsAsync(
        IEnumerable<string> genreNames,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(genreNames);

        var names = genreNames
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (names.Count == 0)
            return [];

        var genreResponse = await GetMovieGenresAsync(
            cancellationToken);

        return genreResponse.Genres
            .Where(genre => names.Contains(genre.Name))
            .Select(genre => genre.Id)
            .Distinct()
            .ToArray();
    }

    public async Task<TmdbSearchResponse<TmdbTvSummary>> SearchTvAsync(
        string query,
        CancellationToken cancellationToken)
    {
        var path =
            $"{TmdbConstants.SEARCH_TV_PATH}?query={Uri.EscapeDataString(query)}";

        using var response = await httpClient.GetAsync(
            path, cancellationToken);

        response.EnsureSuccessStatusCode();

        return await ReadBodyAsync<TmdbSearchResponse<TmdbTvSummary>>(
            response, cancellationToken);
    }

    public async Task<TmdbMovieDetails?> LookupMovieAsync(
        int movieId,
        CancellationToken cancellationToken)
    {
        var path = $"{TmdbConstants.MOVIE_PATH}/{movieId}";

        using var response = await httpClient.GetAsync(
            path, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();

        return await ReadBodyAsync<TmdbMovieDetails>(
            response, cancellationToken);
    }

    public async Task<TmdbTvDetails?> LookupTvAsync(
        int tvId,
        CancellationToken cancellationToken)
    {
        var path = $"{TmdbConstants.TV_PATH}/{tvId}";

        using var response = await httpClient.GetAsync(
            path, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();

        return await ReadBodyAsync<TmdbTvDetails>(
            response, cancellationToken);
    }

    private static async Task<T> ReadBodyAsync<T>(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
        where T : class
    {
        var body = await response.Content.ReadFromJsonAsync<T>(
            JsonOptions, cancellationToken);

        return body ?? throw new JsonException(
            "TMDB returned an empty response body.");
    }
}
