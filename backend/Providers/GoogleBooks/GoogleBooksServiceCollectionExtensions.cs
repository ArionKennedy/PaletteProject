using System.Net.Http.Headers;
using Backend.Constants;
using Backend.Providers.GoogleBooks;
using Microsoft.Extensions.Options;

namespace Backend.Providers.Tmdb;

public static class GoogleBooksServiceCollectionExtensions
{

    public static IServiceCollection AddGoogleBooksProvider(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<GoogleBooksOptions>()
            .Bind(configuration.GetSection(GoogleBooksConstants.CONFIG_SECTION))
            .ValidateDataAnnotations()
            .ValidateOnStart();


        services.AddHttpClient<GoogleBooksClient>((serviceProvider, httpClient) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<GoogleBooksOptions>>().Value;

            httpClient.BaseAddress = new Uri(options.BaseUrl);
            httpClient.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            if (!string.IsNullOrWhiteSpace(options.ApiKey))
                httpClient.DefaultRequestHeaders.Add(GoogleBooksConstants.API_KEY_HEADER, options.ApiKey);
        });
        services.AddScoped<IMediaProvider, GoogleBooksProvider>();


        return services;
    }
}
