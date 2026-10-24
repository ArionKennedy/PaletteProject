using Backend.Providers;
using Backend.Shared.Models;
using Backend.Shared.Errors;

namespace Backend.Features.Lookup;

public sealed class LookupService(MediaProviderResolver providerResolver)
{
    public async Task<MediaLookupResult> LookupAsync(
        string mediaType,
        string externalId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(externalId))
        {
            throw new InvalidRequestException("The media id must not be empty.");
        }

        var provider = providerResolver.Resolve(mediaType.Trim());

        return await provider.LookupAsync(externalId.Trim(), cancellationToken);
    }
}

