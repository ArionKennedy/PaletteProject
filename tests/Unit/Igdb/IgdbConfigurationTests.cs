using Backend.Constants;
using Backend.Providers;
using Backend.Providers.Igdb;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Palette.Tests.Unit.Igdb;

public class IgdbConfigurationTests
{
    private const string BaseUrl = "https://igdb.example/v4/";
    private const string ImageBaseUrl = "https://images.igdb.example/upload/";
    private const string ClientId = "configuration-test-client-id";
    private const string AccessToken = "configuration-test-access-token";

    // ---------- Options binding and startup validation ----------

    [Fact]
    public async Task ValidConfigurationAllowsHostToStartAndBindsOptions()
    {
        using var host = CreateBuilder(ValidSettings()).Build();

        await host.StartAsync();

        var options = host.Services.GetRequiredService<IOptions<IgdbOptions>>().Value;

        Assert.Equal(BaseUrl, options.BaseUrl);
        Assert.Equal(ImageBaseUrl, options.ImageBaseUrl);
        Assert.Equal(ClientId, options.ClientId);
        Assert.Equal(AccessToken, options.AccessToken);
        Assert.Equal(25, options.TimeoutSeconds);

        await host.StopAsync();
    }

    [Theory]
    [InlineData("Igdb:ClientId", null)]
    [InlineData("Igdb:ClientId", "")]
    [InlineData("Igdb:ClientId", "  ")]
    [InlineData("Igdb:AccessToken", null)]
    [InlineData("Igdb:AccessToken", "")]
    [InlineData("Igdb:AccessToken", "  ")]
    [InlineData("Igdb:BaseUrl", null)]
    [InlineData("Igdb:BaseUrl", " ")]
    [InlineData("Igdb:ImageBaseUrl", null)]
    [InlineData("Igdb:ImageBaseUrl", " ")]
    public async Task MissingOrBlankRequiredSettingPreventsStartup(string key, string? value)
    {
        var settings = ValidSettings();
        settings[key] = value;

        using var host = CreateBuilder(settings).Build();

        await Assert.ThrowsAsync<OptionsValidationException>(() => host.StartAsync());
    }

    [Theory]
    [InlineData("Igdb:BaseUrl", "not-a-url/")]
    [InlineData("Igdb:BaseUrl", "https://igdb.example/v4")]
    [InlineData("Igdb:ImageBaseUrl", "not-a-url/")]
    [InlineData("Igdb:ImageBaseUrl", "https://images.igdb.example/upload")]
    public async Task InvalidOrSlashlessUrlPreventsStartup(string key, string value)
    {
        var settings = ValidSettings();
        settings[key] = value;

        using var host = CreateBuilder(settings).Build();

        await Assert.ThrowsAsync<OptionsValidationException>(() => host.StartAsync());
    }

    [Fact]
    public async Task MissingTimeoutUsesTheDefault()
    {
        var settings = ValidSettings();
        settings.Remove("Igdb:TimeoutSeconds");

        using var host = CreateBuilder(settings).Build();

        await host.StartAsync();

        var options = host.Services.GetRequiredService<IOptions<IgdbOptions>>().Value;
        Assert.Equal(IgdbConstants.DEFAULT_TIMEOUT_SECONDS, options.TimeoutSeconds);

        await host.StopAsync();
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("121")]
    public async Task OutOfRangeTimeoutPreventsStartup(string value)
    {
        var settings = ValidSettings();
        settings["Igdb:TimeoutSeconds"] = value;

        using var host = CreateBuilder(settings).Build();

        await Assert.ThrowsAsync<OptionsValidationException>(() => host.StartAsync());
    }

    // ---------- Typed HttpClient configuration ----------

    [Fact]
    public async Task TypedClientUsesConfiguredAddressTimeoutAndAuthHeaders()
    {
        var observed = await ObserveTypedClientAsync(ValidSettings());

        Assert.Equal(new Uri(BaseUrl), observed.BaseAddress);
        Assert.Equal(TimeSpan.FromSeconds(25), observed.Timeout);

        var clientId = Assert.IsAssignableFrom<IEnumerable<string>>(observed.ClientIdHeaderValues);
        Assert.Equal(ClientId, Assert.Single(clientId));

        Assert.NotNull(observed.Authorization);
        Assert.Equal(IgdbConstants.BEARER_SCHEME, observed.Authorization.Scheme);
        Assert.Equal(AccessToken, observed.Authorization.Parameter);
    }

    // ---------- Provider registration ----------

    [Fact]
    public async Task GameProviderCanBeResolvedFromActualRegistrations()
    {
        using var host = CreateBuilder(ValidSettings()).Build();

        await host.StartAsync();

        using var scope = host.Services.CreateScope();
        var resolver = scope.ServiceProvider.GetRequiredService<MediaProviderResolver>();

        var provider = resolver.Resolve("game");

        Assert.IsType<IgdbProvider>(provider);
        Assert.Equal("game", provider.MediaType);

        await host.StopAsync();
    }

    // ---------- helpers ----------

    private sealed record ObservedClient(
        Uri? BaseAddress,
        TimeSpan Timeout,
        IEnumerable<string>? ClientIdHeaderValues,
        System.Net.Http.Headers.AuthenticationHeaderValue? Authorization);

    private static async Task<ObservedClient> ObserveTypedClientAsync(
        Dictionary<string, string?> settings)
    {
        var builder = CreateBuilder(settings);

        Uri? baseAddress = null;
        TimeSpan timeout = default;
        IEnumerable<string>? clientIdValues = null;
        System.Net.Http.Headers.AuthenticationHeaderValue? authorization = null;

        // Appended after the production registration, so this callback sees the
        // settings that AddIgdbProvider applied to the HttpClient.
        builder.Services
            .AddHttpClient<IgdbClient>()
            .ConfigureHttpClient(client =>
            {
                baseAddress = client.BaseAddress;
                timeout = client.Timeout;
                client.DefaultRequestHeaders.TryGetValues(
                    IgdbConstants.CLIENT_ID_HEADER, out clientIdValues);
                authorization = client.DefaultRequestHeaders.Authorization;
            });

        using var host = builder.Build();
        await host.StartAsync();

        // Resolving the typed client runs its configuration callbacks.
        // No HTTP request is sent.
        using var scope = host.Services.CreateScope();
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IgdbClient>());

        await host.StopAsync();

        return new ObservedClient(baseAddress, timeout, clientIdValues, authorization);
    }

    private static Dictionary<string, string?> ValidSettings() => new()
    {
        ["Igdb:BaseUrl"] = BaseUrl,
        ["Igdb:ImageBaseUrl"] = ImageBaseUrl,
        ["Igdb:ClientId"] = ClientId,
        ["Igdb:AccessToken"] = AccessToken,
        ["Igdb:TimeoutSeconds"] = "25",
    };

    private static HostApplicationBuilder CreateBuilder(Dictionary<string, string?> settings)
    {
        var builder = Host.CreateApplicationBuilder();

        // Keep the tests independent of local appsettings, user-secrets and env variables.
        builder.Configuration.Sources.Clear();
        builder.Configuration.AddInMemoryCollection(settings);

        builder.Logging.ClearProviders();

        // Use the real dependency-injection and options registrations.
        builder.Services.AddMediaProviders();
        builder.Services.AddIgdbProvider(builder.Configuration);

        return builder;
    }
}
