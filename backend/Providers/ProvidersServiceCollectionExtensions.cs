namespace Backend.Providers;

public static class ProvidersServiceCollectionExtensions
{

    public static IServiceCollection AddMediaProviders(this IServiceCollection services)
    {
        services.AddScoped<MediaProviderResolver>();

        return services;
    }
}
