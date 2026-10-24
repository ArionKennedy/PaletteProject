using System.Net.Http.Headers;
using Backend.Constants;
using Microsoft.Extensions.Options;

namespace Backend.Providers.Tmdb;

public static class TmdbServiceCollectionExtensions
{
    //Everything the TMDB provider needs, in one call from Program.cs.
    public static IServiceCollection AddTmdbProvider(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<TmdbOptions>()
            .Bind(configuration.GetSection(TmdbConstants.CONFIG_SECTION))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // A "typed client": the factory builds TmdbClient with an HttpClient that already
        // has the address, timeout and Bearer token set, so nothing else touches the token.

        services.AddHttpClient<TmdbClient>((serviceProvider, httpClient) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<TmdbOptions>>().Value;

            httpClient.BaseAddress = new Uri(options.BaseUrl);
            httpClient.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(TmdbConstants.BEARER_SCHEME, options.ReadAccessToken);
        });

        // Registering as IMediaProvider is what makes the resolver find it.
        services.AddScoped<IMediaProvider, TmdbMovieProvider>();

        return services;
    }
}
