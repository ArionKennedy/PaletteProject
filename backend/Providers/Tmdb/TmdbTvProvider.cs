using System.Globalization;
using Backend.Constants;
using Backend.Shared.Errors;
using Backend.Shared.Models;
using Microsoft.Extensions.Options;

namespace Backend.Providers.Tmdb;

public sealed class TmdbTvProvider(TmdbClient client, IOptions<TmdbOptions> options)
    : IMediaProvider
{
    public string MediaType => MediaConstants.MEDIA_TYPE_TV;

    public async Task<IReadOnlyList<MediaSearchResult>> SearchAsync(
        string query,
        int maxResults,
        CancellationToken cancellationToken)
    {
        var response = await ProviderCallGuard.RunAsync(
            TmdbConstants.PROVIDER_NAME,
            () => client.SearchTvAsync(query, cancellationToken),
            cancellationToken);

        var imageBaseUrl = options.Value.ImageBaseUrl;

        // TMDB returns 20 per page; we keep only the first maxResults and ignore the rest.
        return response.Results
            .Take(maxResults)
            .Select(tv => TmdbMapper.ToSearchResult(tv, imageBaseUrl))
            .ToList();
    }

    public async Task<MediaLookupResult> LookupAsync(
        string externalId,
        CancellationToken cancellationToken)
    {
        // Validate here: TMDB answers 404 for "abc" just like for an unknown id,
        // and a malformed id is the caller's mistake (400), not a missing movie.
        var tvId = ParseTvId(externalId);

        var details = await ProviderCallGuard.RunAsync(
            TmdbConstants.PROVIDER_NAME,
            () => client.LookupTvAsync(tvId, cancellationToken),
            cancellationToken);

        if (details is null)
        {
            throw new MediaNotFoundException(MediaType, externalId);
        }

        return TmdbMapper.ToLookupResult(details, options.Value.ImageBaseUrl);
    }

    private static int ParseTvId(string externalId)
    {
        var isValid = int.TryParse(
            externalId,
            NumberStyles.None,
            CultureInfo.InvariantCulture,
            out var tvId);

        if (!isValid || tvId <= 0)
        {
            throw new InvalidRequestException($"'{externalId}' is not a valid tv id.");
        }

        return tvId;
    }
}
