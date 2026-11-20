using System.Net.Http.Headers;
using Backend.Constants;
using Microsoft.Extensions.Options;

namespace Backend.Providers.Igdb;

public static class IgdbServiceCollectionExtensions
{
    public static IServiceCollection AddIgdbProvider(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<IgdbOptions>()
            .Bind(configuration.GetSection(IgdbConstants.CONFIG_SECTION))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHttpClient<IgdbClient>((serviceProvider, httpClient) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<IgdbOptions>>().Value;

            httpClient.BaseAddress = new Uri(options.BaseUrl);
            httpClient.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            httpClient.DefaultRequestHeaders.Add(IgdbConstants.CLIENT_ID_HEADER, options.ClientId);
            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(IgdbConstants.BEARER_SCHEME, options.AccessToken);
        });

        services.AddScoped<IMediaProvider, IgdbProvider>();

        return services;
    }
}
