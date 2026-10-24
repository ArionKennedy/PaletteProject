using Backend.Constants;
using System.Net;
using System.Text.Json;

namespace Backend.Providers.Tmdb;

public sealed class TmdbClient(HttpClient httpClient)
{
    //to turn camelcase to pascalcase.
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        RespectNullableAnnotations = true,
    };

    public async Task<TmdbSearchResponse> SearchMoviesAsync(
        string query,
        CancellationToken cancellationToken)
    {
        var path = $"{TmdbConstants.SEARCH_MOVIE_PATH}?query={Uri.EscapeDataString(query)}";

        using var response = await httpClient.GetAsync(path, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await ReadBodyAsync<TmdbSearchResponse>(response, cancellationToken);
    }

    public async Task<TmdbMovieDetails?> LookupMovieAsync(
        int movieId,
        CancellationToken cancellationToken)
    {
        var path = $"{TmdbConstants.MOVIE_PATH}/{movieId}";

        using var response = await httpClient.GetAsync(path, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        return await ReadBodyAsync<TmdbMovieDetails>(response, cancellationToken);
    }

    private static async Task<T> ReadBodyAsync<T>(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
        where T : class
    {
        var body = await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);

        return body ?? throw new JsonException("TMDB returned an empty response body.");
    }
}