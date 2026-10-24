using Backend.Shared.Errors;

namespace Backend.Providers;

public sealed class MediaProviderResolver
{
    private readonly Dictionary<string, IMediaProvider> _providersByType;

    public MediaProviderResolver(IEnumerable<IMediaProvider> providers)
    {
        var registered = providers.ToList();

        // Two providers for one type would make the choice ambiguous, so fail loudly.
        var duplicate = registered
            .GroupBy(provider => provider.MediaType, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicate is not null)
        {
            throw new InvalidOperationException(
                $"More than one provider is registered for media type '{duplicate.Key}'.");
        }

        _providersByType = registered.ToDictionary(
            provider => provider.MediaType,
            StringComparer.OrdinalIgnoreCase);
    }


    public IMediaProvider Resolve(string mediaType)
    {
        if (_providersByType.TryGetValue(mediaType, out var provider))
        {
            return provider;
        }

        throw new UnknownMediaTypeException(mediaType, _providersByType.Keys.Order());
    }
}