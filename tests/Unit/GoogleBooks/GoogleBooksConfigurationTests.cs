using System.Net.Http.Headers;
using Backend.Constants;
using Backend.Providers;
using Backend.Providers.GoogleBooks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Palette.Tests.Unit.GoogleBooks;

public class GoogleBooksConfigurationTests
{
    private const string BaseUrl = "https://books.example/books/v1/";
    private const string ApiKey = "configuration-test-key";

    // ---------- Options binding and startup validation ----------

    [Fact]
    public async Task ValidConfigurationAllowsHostToStartAndBindsOptions()
    {
        using var host = CreateBuilder(ValidSettings()).Build();

        await host.StartAsync();

        var options = host.Services.GetRequiredService<IOptions<GoogleBooksOptions>>().Value;

        Assert.Equal(BaseUrl, options.BaseUrl);
        Assert.Equal(ApiKey, options.ApiKey);
        Assert.Equal(25, options.TimeoutSeconds);

        await host.StopAsync();
    }

    [Fact]
    public async Task ApiKeyIsOptionalForStartup()
    {
        var settings = ValidSettings();
        settings.Remove("GoogleBooks:ApiKey");

        using var host = CreateBuilder(settings).Build();

        await host.StartAsync();

        var options = host.Services.GetRequiredService<IOptions<GoogleBooksOptions>>().Value;
        Assert.Null(options.ApiKey);

        await host.StopAsync();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task MissingOrBlankBaseUrlPreventsStartup(string? value)
    {
        var settings = ValidSettings();
        settings["GoogleBooks:BaseUrl"] = value;

        using var host = CreateBuilder(settings).Build();

        await Assert.ThrowsAsync<OptionsValidationException>(() => host.StartAsync());
    }

    [Fact]
    public async Task InvalidBaseUrlPreventsStartup()
    {
        var settings = ValidSettings();
        settings["GoogleBooks:BaseUrl"] = "not-a-url/";

        using var host = CreateBuilder(settings).Build();

        await Assert.ThrowsAsync<OptionsValidationException>(() => host.StartAsync());
    }

    [Fact]
    public async Task BaseUrlWithoutTrailingSlashPreventsStartup()
    {
        var settings = ValidSettings();
        settings["GoogleBooks:BaseUrl"] = "https://books.example/books/v1";

        using var host = CreateBuilder(settings).Build();

        await Assert.ThrowsAsync<OptionsValidationException>(() => host.StartAsync());
    }

    [Fact]
    public async Task MissingTimeoutUsesTheDefault()
    {
        var settings = ValidSettings();
        settings.Remove("GoogleBooks:TimeoutSeconds");

        using var host = CreateBuilder(settings).Build();

        await host.StartAsync();

        var options = host.Services.GetRequiredService<IOptions<GoogleBooksOptions>>().Value;
        Assert.Equal(GoogleBooksConstants.DEFAULT_TIMEOUT_SECONDS, options.TimeoutSeconds);

        await host.StopAsync();
    }

    // ---------- Typed HttpClient configuration ----------

    [Fact]
    public async Task TypedClientUsesConfiguredAddressTimeoutAndApiKeyHeader()
    {
        var observed = await ObserveTypedClientAsync(ValidSettings());

        Assert.Equal(new Uri(BaseUrl), observed.BaseAddress);
        Assert.Equal(TimeSpan.FromSeconds(25), observed.Timeout);

        var values = Assert.IsAssignableFrom<IEnumerable<string>>(observed.ApiKeyHeaderValues);
        Assert.Equal(ApiKey, Assert.Single(values));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task TypedClientOmitsApiKeyHeaderWhenKeyIsMissingOrBlank(string? apiKey)
    {
        var settings = ValidSettings();
        settings["GoogleBooks:ApiKey"] = apiKey;

        var observed = await ObserveTypedClientAsync(settings);

        Assert.Equal(new Uri(BaseUrl), observed.BaseAddress);
        Assert.Null(observed.ApiKeyHeaderValues);
    }

    // ---------- Provider registration ----------

    [Fact]
    public async Task BookProviderCanBeResolvedFromActualRegistrations()
    {
        using var host = CreateBuilder(ValidSettings()).Build();

        await host.StartAsync();

        using var scope = host.Services.CreateScope();
        var resolver = scope.ServiceProvider.GetRequiredService<MediaProviderResolver>();

        var provider = resolver.Resolve("book");

        Assert.IsType<GoogleBooksProvider>(provider);
        Assert.Equal("book", provider.MediaType);

        await host.StopAsync();
    }

    // ---------- helpers ----------

    private sealed record ObservedClient(
        Uri? BaseAddress,
        TimeSpan Timeout,
        IEnumerable<string>? ApiKeyHeaderValues);

    private static async Task<ObservedClient> ObserveTypedClientAsync(
        Dictionary<string, string?> settings)
    {
        var builder = CreateBuilder(settings);

        Uri? baseAddress = null;
        TimeSpan timeout = default;
        IEnumerable<string>? headerValues = null;

        // Appended after the production registration, so this callback sees the
        // settings that AddGoogleBooksProvider applied to the HttpClient.
        builder.Services
            .AddHttpClient<GoogleBooksClient>()
            .ConfigureHttpClient(client =>
            {
                baseAddress = client.BaseAddress;
                timeout = client.Timeout;
                client.DefaultRequestHeaders.TryGetValues(
                    GoogleBooksConstants.API_KEY_HEADER, out headerValues);
            });

        using var host = builder.Build();
        await host.StartAsync();

        // Resolving the typed client runs its configuration callbacks.
        // No HTTP request is sent.
        using var scope = host.Services.CreateScope();
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<GoogleBooksClient>());

        await host.StopAsync();

        return new ObservedClient(baseAddress, timeout, headerValues);
    }

    private static Dictionary<string, string?> ValidSettings() => new()
    {
        ["GoogleBooks:BaseUrl"] = BaseUrl,
        ["GoogleBooks:ApiKey"] = ApiKey,
        ["GoogleBooks:TimeoutSeconds"] = "25",
    };

    private static HostApplicationBuilder CreateBuilder(Dictionary<string, string?> settings)
    {
        var builder = Host.CreateApplicationBuilder();

        // Keep the tests independent of local appsettings and environment variables.
        builder.Configuration.Sources.Clear();
        builder.Configuration.AddInMemoryCollection(settings);

        builder.Logging.ClearProviders();

        // Use the real dependency-injection and options registrations.
        builder.Services.AddMediaProviders();
        builder.Services.AddGoogleBooksProvider(builder.Configuration);

        return builder;
    }
}
