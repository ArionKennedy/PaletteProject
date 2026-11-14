using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Backend.Providers.Tmdb;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Palette.Api.Data;
using Xunit;

namespace Palette.Tests.Integration.Api;

public class ApplicationStartupTests
    : IClassFixture<PaletteApplicationFactory>
{
    private readonly PaletteApplicationFactory _factory;

    public ApplicationStartupTests(
        PaletteApplicationFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("/api/health", "status", "ok")]
    [InlineData(
        "/api/test",
        "message",
        "Connected to backend!")]
    public async Task ApplicationStartsAndExposesBasicEndpoints(
        string path,
        string property,
        string expectedValue)
    {
        using var client = CreateClient();

        using var response = await client.GetAsync(path);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.Equal(
            "application/json",
            response.Content.Headers.ContentType?.MediaType);

        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        Assert.Equal(
            expectedValue,
            document.RootElement
                .GetProperty(property)
                .GetString());
    }

    [Theory]
    [InlineData("movie", "Startup Movie")]
    [InlineData("tv", "Startup Series")]
    public async Task ApplicationRegistersSearchEndpointsAndProviders(
        string mediaType,
        string expectedTitle)
    {
        using var client = CreateClient();

        using var response = await client.GetAsync(
            $"/api/search/{mediaType}?q=Example");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        var results = document.RootElement
            .GetProperty("results");

        Assert.Equal(1, results.GetArrayLength());

        Assert.Equal(
            mediaType,
            results[0]
                .GetProperty("mediaType")
                .GetString());

        Assert.Equal(
            expectedTitle,
            results[0]
                .GetProperty("title")
                .GetString());
    }

    [Fact]
    public async Task AllowedFrontendOriginReceivesCorsHeader()
    {
        using var client = CreateClient();

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "/api/health");

        request.Headers.Add(
            "Origin",
            "http://localhost:8081");

        using var response = await client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.Equal(
            "http://localhost:8081",
            Assert.Single(
                response.Headers.GetValues(
                    "Access-Control-Allow-Origin")));
    }

    [Fact]
    public async Task OtherOriginDoesNotReceiveCorsPermission()
    {
        using var client = CreateClient();

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "/api/health");

        request.Headers.Add(
            "Origin",
            "https://other.example");

        using var response = await client.SendAsync(request);

        // CORS controls browser permission; it does not make this
        // public endpoint return an authorization error.
        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.False(
            response.Headers.Contains(
                "Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task AllowedFrontendPreflightIsHandledByCorsMiddleware()
    {
        using var client = CreateClient();

        using var request = new HttpRequestMessage(
            HttpMethod.Options,
            "/api/search/movie");

        request.Headers.Add(
            "Origin",
            "http://localhost:8081");

        request.Headers.Add(
            "Access-Control-Request-Method",
            "GET");

        request.Headers.Add(
            "Access-Control-Request-Headers",
            "X-Test-Header");

        using var response = await client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);

        Assert.Equal(
            "http://localhost:8081",
            Assert.Single(
                response.Headers.GetValues(
                    "Access-Control-Allow-Origin")));

        Assert.Contains(
            "GET",
            string.Join(
                ",",
                response.Headers.GetValues(
                    "Access-Control-Allow-Methods")));

        Assert.Contains(
            "x-test-header",
            string.Join(
                ",",
                response.Headers.GetValues(
                    "Access-Control-Allow-Headers"))
                .ToLowerInvariant());
    }

    [Fact]
    public async Task ApplicationUsesRegisteredExceptionHandlingMiddleware()
    {
        using var client = CreateClient();

        using var response = await client.GetAsync(
            "/api/search/unregistered?q=Example");

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);

        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        var problem = document.RootElement;

        Assert.Equal(
            400,
            problem.GetProperty("status").GetInt32());

        Assert.Equal(
            "Unknown media type",
            problem.GetProperty("title").GetString());

        Assert.False(
            string.IsNullOrWhiteSpace(
                problem.GetProperty("traceId").GetString()));
    }

    private HttpClient CreateClient()
    {
        return _factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost"),
                AllowAutoRedirect = false,
            });
    }
}

// PaletteDbContext identifies the application assembly.
// WebApplicationFactory executes that assembly's real Program.cs.
public sealed class PaletteApplicationFactory
    : WebApplicationFactory<PaletteDbContext>
{
    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        // Avoid development-only automatic migrations and redirects.
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration(
            (_, configuration) =>
            {
                configuration.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        // No endpoint exercised by these tests needs
                        // a database connection.
                        ["ConnectionStrings:DefaultConnection"] =
                            "Host=127.0.0.1;Port=1;" +
                            "Database=palette_startup_tests;" +
                            "Username=unused;Password=unused",

                        ["Tmdb:BaseUrl"] =
                            "https://tmdb.example/3/",

                        ["Tmdb:ImageBaseUrl"] =
                            "https://images.example/posters/",

                        ["Tmdb:ReadAccessToken"] =
                            "startup-test-token",

                        ["Tmdb:TimeoutSeconds"] = "10",
                    });
            });

        builder.ConfigureLogging(
            logging => logging.ClearProviders());

        builder.ConfigureServices(services =>
        {
            // Keep the production registrations and pipeline.
            // Replace only the external TMDB transport.
            services
                .AddHttpClient<TmdbClient>()
                .ConfigurePrimaryHttpMessageHandler(
                    () => new StubTmdbHandler());
        });
    }

    private sealed class StubTmdbHandler
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var body = request.RequestUri?.AbsolutePath switch
            {
                "/3/search/movie" =>
                    """
                    {
                      "page": 1,
                      "results": [
                        {
                          "id": 42,
                          "title": "Startup Movie",
                          "release_date": "2024-03-15",
                          "poster_path": null
                        }
                      ],
                      "total_pages": 1,
                      "total_results": 1
                    }
                    """,

                "/3/search/tv" =>
                    """
                    {
                      "page": 1,
                      "results": [
                        {
                          "id": 84,
                          "name": "Startup Series",
                          "first_air_date": "2024-03-15",
                          "poster_path": null
                        }
                      ],
                      "total_pages": 1,
                      "total_results": 1
                    }
                    """,

                _ => throw new InvalidOperationException(
                    $"Unexpected upstream request: {request.RequestUri}"),
            };

            return Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        body,
                        Encoding.UTF8,
                        "application/json"),
                });
        }
    }
}
