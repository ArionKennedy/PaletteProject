using Backend.Providers;
using Backend.Shared.Models;

namespace Palette.Tests.TestDoubles;

// Shared service-test seam: no HTTP, database, or authentication setup is needed.
internal sealed class StubMediaProvider(string mediaType = "movie") : IMediaProvider
{
    public string MediaType { get; } = mediaType;

    public Func<string, int, CancellationToken, Task<IReadOnlyList<MediaSearchResult>>>?
        SearchHandler { get; init; }
    public Func<string, CancellationToken, Task<MediaLookupResult>>? LookupHandler { get; init; }

    public List<(string Query, int MaxResults, CancellationToken CancellationToken)> SearchCalls { get; } = [];
    public List<(string ExternalId, CancellationToken CancellationToken)> LookupCalls { get; } = [];

    public Task<IReadOnlyList<MediaSearchResult>> SearchAsync(
        string query, int maxResults, CancellationToken cancellationToken)
    {
        SearchCalls.Add((query, maxResults, cancellationToken));
        return SearchHandler is { } handler
            ? handler(query, maxResults, cancellationToken)
            : throw new InvalidOperationException("Unexpected search call. Configure SearchHandler in the test.");
    }

    public Task<MediaLookupResult> LookupAsync(string externalId, CancellationToken cancellationToken)
    {
        LookupCalls.Add((externalId, cancellationToken));
        return LookupHandler is { } handler
            ? handler(externalId, cancellationToken)
            : throw new InvalidOperationException("Unexpected lookup call. Configure LookupHandler in the test.");
    }
}
