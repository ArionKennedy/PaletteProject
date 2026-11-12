
using Backend.Shared.Errors;
using Backend.Shared.Models;
using Backend.Constants;

namespace Backend.Providers.GoogleBooks;

public sealed class GoogleBooksProvider(GoogleBooksClient client) : IMediaProvider
{
    public string MediaType => MediaConstants.MEDIA_TYPE_BOOK;


    public async Task<IReadOnlyList<MediaSearchResult>> SearchAsync(
        string query,
        int maxResults,
        CancellationToken cancellationToken)
    {
        var response = await ProviderCallGuard.RunAsync(
            GoogleBooksConstants.PROVIDER_NAME,
            () => client.SearchAsync(query, maxResults, cancellationToken),
            cancellationToken);

        return (response.Items ?? [])
            .Select(GoogleBooksMapper.ToSearchResult)
            .ToList();
    }

    public async Task<MediaLookupResult> LookupAsync(
        string externalId,
        CancellationToken cancellationToken)
    {
        var volume = await ProviderCallGuard.RunAsync(
            GoogleBooksConstants.PROVIDER_NAME,
            () => client.LookupAsync(externalId, cancellationToken),
            cancellationToken);

        if (volume is null) throw new MediaNotFoundException(MediaType, externalId);

        return GoogleBooksMapper.ToLookupResult(volume);
    }

}