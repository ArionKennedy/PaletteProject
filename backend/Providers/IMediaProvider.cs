using Backend.Shared.Models;

namespace Backend.Providers;

//Contract for implementers:

//Throw MediaNotFoundException when an id does not exist.
//Throw InvalidRequestException when an id is malformed for this provider.
//Throw ProviderUnavailableException when the upstream call fails; use "ProviderCallGuard" instead of writing your own try/catch.
//Return an empty list, not an error, when a search has no matches.

public interface IMediaProvider
{
    string MediaType { get; }

    Task<IReadOnlyList<MediaSearchResult>> SearchAsync(
        string query,
        int maxResults,
        CancellationToken cancellationToken
    );

    Task<MediaLookupResult> LookupAsync(
        string externalId,
        CancellationToken cancellationToken
    );
}