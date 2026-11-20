using System.Globalization;
using Backend.Constants;
using Backend.Shared.Errors;
using Backend.Shared.Models;
using Microsoft.Extensions.Options;

namespace Backend.Providers.Igdb;

public sealed class IgdbProvider(IgdbClient client, IOptions<IgdbOptions> options)
    : IMediaProvider
{
    public string MediaType => MediaConstants.MEDIA_TYPE_GAME;

    public async Task<IReadOnlyList<MediaSearchResult>> SearchAsync(
        string query,
        int maxResults,
        CancellationToken cancellationToken)
    {
        var games = await ProviderCallGuard.RunAsync(
            IgdbConstants.PROVIDER_NAME,
            () => client.SearchAsync(query, maxResults, cancellationToken),
            cancellationToken);

        var imageBaseUrl = options.Value.ImageBaseUrl;

        return games
            .Select(game => IgdbMapper.ToSearchResult(game, imageBaseUrl))
            .ToList();
    }

    public async Task<MediaLookupResult> LookupAsync(
        string externalId,
        CancellationToken cancellationToken)
    {
        // A malformed id is the caller's mistake (400), not a missing game (404).
        var gameId = ParseGameId(externalId);

        var game = await ProviderCallGuard.RunAsync(
            IgdbConstants.PROVIDER_NAME,
            () => client.LookupAsync(gameId, cancellationToken),
            cancellationToken);

        if (game is null)
        {
            throw new MediaNotFoundException(MediaType, externalId);
        }

        return IgdbMapper.ToLookupResult(game, options.Value.ImageBaseUrl);
    }

    private static int ParseGameId(string externalId)
    {
        if (!int.TryParse(externalId, NumberStyles.None, CultureInfo.InvariantCulture, out var id)
            || id <= 0)
        {
            throw new InvalidRequestException($"'{externalId}' is not a valid IGDB game id.");
        }

        return id;
    }
}
