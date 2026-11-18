namespace Palette.Api.Logging;

public static class LoggingServiceCollectionExtensions
{
    public static IServiceCollection AddLoggingFeature(this IServiceCollection services)
    {
        services.AddScoped<ShowLogService>();

        return services;
    }
}
