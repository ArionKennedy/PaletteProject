namespace Backend.Features.Search;

public static class SearchServiceCollectionExtensions
{
    public static IServiceCollection AddSearchFeature(this IServiceCollection services)
    {
        services.AddScoped<SearchService>();

        return services;
    }
}
