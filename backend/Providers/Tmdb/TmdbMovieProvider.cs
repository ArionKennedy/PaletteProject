using System.Globalization;
using Backend.Constants;
using Backend.Shared.Errors;
using Backend.Shared.Models;
using Microsoft.Extensions.Options;

namespace Backend.Providers.Tmdb;

public sealed class TmdbMovieProvider(TmdbClient client, IOptions<TmdbOptions> options)
    : IMediaProvider
{
    public string MediaType => MediaConstants.MEDIA_TYPE_MOVIE;

    public async Task<IReadOnlyList<MediaSearchResult>> SearchAsync(
        string query,
        int maxResults,
        CancellationToken cancellationToken)
    {
        var response = await ProviderCallGuard.RunAsync(
            TmdbConstants.PROVIDER_NAME,
            () => client.SearchMoviesAsync(query, cancellationToken),
            cancellationToken);

        var imageBaseUrl = options.Value.ImageBaseUrl;

        // TMDB returns 20 per page; we keep only the first maxResults and ignore the rest.
        return response.Results
            .Take(maxResults)
            .Select(movie => TmdbMapper.ToSearchResult(movie, imageBaseUrl))
            .ToList();
    }

    public async Task<MediaLookupResult> LookupAsync(
        string externalId,
        CancellationToken cancellationToken)
    {
        // Validate here: TMDB answers 404 for "abc" just like for an unknown id,
        // and a malformed id is the caller's mistake (400), not a missing movie.
        var movieId = ParseMovieId(externalId);

        var details = await ProviderCallGuard.RunAsync(
            TmdbConstants.PROVIDER_NAME,
            () => client.LookupMovieAsync(movieId, cancellationToken),
            cancellationToken);

        if (details is null)
        {
            throw new MediaNotFoundException(MediaType, externalId);
        }

        return TmdbMapper.ToLookupResult(details, options.Value.ImageBaseUrl);
    }

    private static int ParseMovieId(string externalId)
    {
        var isValid = int.TryParse(
            externalId,
            NumberStyles.None,
            CultureInfo.InvariantCulture,
            out var movieId);

        if (!isValid || movieId <= 0)
        {
            throw new InvalidRequestException($"'{externalId}' is not a valid movie id.");
        }

        return movieId;
    }
}
