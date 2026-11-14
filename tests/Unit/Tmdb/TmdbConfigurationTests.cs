using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Backend.Providers;
using Backend.Providers.Tmdb;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace Palette.Tests.Unit.Tmdb;

public class TmdbConfigurationTests
{
    private const string BaseUrl =
        "https://tmdb.example/3/";

    private const string ImageBaseUrl =
        "https://images.example/posters/";

    private const string ReadAccessToken =
        "configuration-test-token";

    [Fact]
    public async Task ValidConfigurationAllowsHostToStartAndBindsOptions()
    {
        var settings = ValidSettings();
        var builder = CreateBuilder(settings);

        using var host = builder.Build();

        await host.StartAsync();

        var options = host.Services
            .GetRequiredService<IOptions<TmdbOptions>>()
            .Value;

        Assert.Equal(BaseUrl, options.BaseUrl);
        Assert.Equal(ImageBaseUrl, options.ImageBaseUrl);
        Assert.Equal(ReadAccessToken, options.ReadAccessToken);
        Assert.Equal(25, options.TimeoutSeconds);

        await host.StopAsync();
    }

    [Theory]
    [InlineData("Tmdb:BaseUrl", null)]
    [InlineData("Tmdb:BaseUrl", "")]
    [InlineData("Tmdb:BaseUrl", " ")]
    [InlineData("Tmdb:ImageBaseUrl", null)]
    [InlineData("Tmdb:ImageBaseUrl", "")]
    [InlineData("Tmdb:ImageBaseUrl", " ")]
    [InlineData("Tmdb:ReadAccessToken", null)]
    [InlineData("Tmdb:ReadAccessToken", "")]
    [InlineData("Tmdb:ReadAccessToken", " ")]
    public async Task MissingOrBlankRequiredSettingPreventsStartup(
        string key,
        string? value)
    {
        var settings = ValidSettings();
        settings[key] = value;

        var builder = CreateBuilder(settings);

        using var host = builder.Build();

        await Assert.ThrowsAsync<OptionsValidationException>(
            () => host.StartAsync());
    }

    [Theory]
    [InlineData("Tmdb:BaseUrl", "not-a-url/")]
    [InlineData("Tmdb:ImageBaseUrl", "not-a-url")]
    public async Task InvalidUrlPreventsStartup(
        string key,
        string value)
    {
        var settings = ValidSettings();
        settings[key] = value;

        var builder = CreateBuilder(settings);

        using var host = builder.Build();

        await Assert.ThrowsAsync<OptionsValidationException>(
            () => host.StartAsync());
    }

    [Fact]
    public async Task BaseUrlWithoutTrailingSlashPreventsStartup()
    {
        var settings = ValidSettings();
        settings["Tmdb:BaseUrl"] = "https://tmdb.example/3";

        var builder = CreateBuilder(settings);

        using var host = builder.Build();

        await Assert.ThrowsAsync<OptionsValidationException>(
            () => host.StartAsync());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(121)]
    public async Task TimeoutOutsideAllowedRangePreventsStartup(
        int timeoutSeconds)
    {
        var settings = ValidSettings();

        settings["Tmdb:TimeoutSeconds"] =
            timeoutSeconds.ToString(
                System.Globalization.CultureInfo.InvariantCulture);

        var builder = CreateBuilder(settings);

        using var host = builder.Build();

        await Assert.ThrowsAsync<OptionsValidationException>(
            () => host.StartAsync());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(120)]
    public async Task TimeoutBoundaryValuesAreAccepted(
        int timeoutSeconds)
    {
        var settings = ValidSettings();

        settings["Tmdb:TimeoutSeconds"] =
            timeoutSeconds.ToString(
                System.Globalization.CultureInfo.InvariantCulture);

        var builder = CreateBuilder(settings);

        using var host = builder.Build();

        await host.StartAsync();

        var options = host.Services
            .GetRequiredService<IOptions<TmdbOptions>>()
            .Value;

        Assert.Equal(timeoutSeconds, options.TimeoutSeconds);

        await host.StopAsync();
    }

    [Fact]
    public async Task MissingTimeoutUsesDefaultOfTenSeconds()
    {
        var settings = ValidSettings();
        settings.Remove("Tmdb:TimeoutSeconds");

        var builder = CreateBuilder(settings);

        using var host = builder.Build();

        await host.StartAsync();

        var options = host.Services
            .GetRequiredService<IOptions<TmdbOptions>>()
            .Value;

        Assert.Equal(10, options.TimeoutSeconds);

        await host.StopAsync();
    }

    [Fact]
    public async Task TypedClientUsesConfiguredAddressTimeoutAndBearerToken()
    {
        var builder = CreateBuilder(ValidSettings());

        Uri? observedBaseAddress = null;
        TimeSpan observedTimeout = default;
        AuthenticationHeaderValue? observedAuthorization = null;

        // Appended after the production registration, so this callback
        // observes the HttpClient settings applied by AddTmdbProvider.
        builder.Services
            .AddHttpClient<TmdbClient>()
            .ConfigureHttpClient(client =>
            {
                observedBaseAddress = client.BaseAddress;
                observedTimeout = client.Timeout;
                observedAuthorization =
                    client.DefaultRequestHeaders.Authorization;
            });

        using var host = builder.Build();

        await host.StartAsync();

        using var scope = host.Services.CreateScope();

        // Resolving the typed client executes its configuration callbacks.
        // No HTTP request is sent.
        var client = scope.ServiceProvider
            .GetRequiredService<TmdbClient>();

        Assert.NotNull(client);
        Assert.Equal(new Uri(BaseUrl), observedBaseAddress);
        Assert.Equal(
            TimeSpan.FromSeconds(25),
            observedTimeout);

        Assert.NotNull(observedAuthorization);
        Assert.Equal("Bearer", observedAuthorization.Scheme);
        Assert.Equal(
            ReadAccessToken,
            observedAuthorization.Parameter);

        await host.StopAsync();
    }

    [Fact]
    public async Task MovieAndTvProvidersCanBeResolvedFromActualRegistrations()
    {
        var builder = CreateBuilder(ValidSettings());

        using var host = builder.Build();

        await host.StartAsync();

        using var scope = host.Services.CreateScope();

        var resolver = scope.ServiceProvider
            .GetRequiredService<MediaProviderResolver>();

        var movieProvider = resolver.Resolve("movie");
        var tvProvider = resolver.Resolve("tv");

        Assert.IsType<TmdbMovieProvider>(movieProvider);
        Assert.IsType<TmdbTvProvider>(tvProvider);

        Assert.Equal("movie", movieProvider.MediaType);
        Assert.Equal("tv", tvProvider.MediaType);

        await host.StopAsync();
    }

    private static Dictionary<string, string?> ValidSettings()
    {
        return new Dictionary<string, string?>
        {
            ["Tmdb:BaseUrl"] = BaseUrl,
            ["Tmdb:ImageBaseUrl"] = ImageBaseUrl,
            ["Tmdb:ReadAccessToken"] = ReadAccessToken,
            ["Tmdb:TimeoutSeconds"] = "25",
        };
    }

    private static HostApplicationBuilder CreateBuilder(
        Dictionary<string, string?> settings)
    {
        var builder = Host.CreateApplicationBuilder();

        // Keep tests independent from local appsettings and environment
        // variables. Only the supplied test configuration is used.
        builder.Configuration.Sources.Clear();
        builder.Configuration.AddInMemoryCollection(settings);

        builder.Logging.ClearProviders();

        // Use the real dependency-injection and options registrations.
        builder.Services.AddMediaProviders();
        builder.Services.AddTmdbProvider(builder.Configuration);

        return builder;
    }
}
