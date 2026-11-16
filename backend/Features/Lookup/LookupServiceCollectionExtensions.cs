namespace Backend.Features.Lookup;

public static class LookupServiceCollectionExtensions
{
    public static IServiceCollection AddLookupFeature(this IServiceCollection services)
    {
        services.AddScoped<LookupService>();

        return services;
    }
}
