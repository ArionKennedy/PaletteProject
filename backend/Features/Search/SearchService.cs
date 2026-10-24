using Backend.Constants;
using Backend.Providers;
using Backend.Shared.Errors;

namespace Backend.Features.Search;

public sealed class SearchService(MediaProviderResolver providerResolver)
{
    public async Task<SearchResponse> SearchAsync(
        string mediaType,
        string? query,
        CancellationToken cancellationToken)
    {
        var cleanQuery = ValidateQuery(query);
        var provider = providerResolver.Resolve(mediaType.Trim());

        var results = await provider.SearchAsync(
            cleanQuery,
            SearchConstants.MAX_SEARCH_RESULTS,
            cancellationToken);

        return new SearchResponse(results);
    }

    // A whitespace-only query counts as empty, so we trim before checking.
    private static string ValidateQuery(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new InvalidRequestException("Query parameter 'q' must not be empty.");
        }

        var trimmed = query.Trim();

        if (trimmed.Length > SearchConstants.MAX_QUERY_LENGTH)
        {
            throw new InvalidRequestException(
                "Query parameter 'q' must be at most " +
                $"{SearchConstants.MAX_QUERY_LENGTH} characters.");
        }

        return trimmed;
    }
}
