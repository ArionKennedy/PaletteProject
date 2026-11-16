using Palette.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
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
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Palette.Api.Data;
using Xunit;

namespace Palette.Tests.Integration.Api;

[Collection("Postgres Integration Tests")]
public class ApplicationStartupTests : IAsyncLifetime
{
    private readonly PaletteApplicationFactory _factory;
    private readonly string _identity = $"startup-{Guid.NewGuid():N}";

    public ApplicationStartupTests(
        PostgresFixture fixture)
    {
        _factory = new PaletteApplicationFactory(fixture.ConnectionString);
    }

    [Theory]
    [InlineData(
        "/api/test",
        "message",
        "Connected to backend!")]
    public async Task ApplicationStartsAndExposesPublicEndpoint(
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
        using var client = CreateClient(authenticated: true);

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
            "/api/test");

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
            "/api/test");

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
        using var client = CreateClient(authenticated: true);

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

    public Task InitializeAsync() => Task.CompletedTask;

    [Fact]
    public void BearerOptionsUseConfiguredClerkAuthorityAndValidationRules()
    {
        // Resolve the real named options without authenticating a token or
        // asking the configuration manager to download Clerk metadata.
        var options = _factory.Services
            .GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);

        Assert.Equal("https://clerk.example.test", options.Authority);
        Assert.True(options.RequireHttpsMetadata);
        Assert.False(options.TokenValidationParameters.ValidateAudience);
        Assert.True(options.TokenValidationParameters.ValidateIssuerSigningKey);
        Assert.True(options.TokenValidationParameters.ValidateLifetime);
        Assert.True(options.TokenValidationParameters.ValidateIssuer);
    }

    [Fact]
    public async Task BearerOptionsRejectMissingClerkAuthority()
    {
        await using var factory = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                // A null value masks earlier configuration providers, including
                // any machine-local Clerk setting, without changing the environment.
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Clerk:FrontendAPIURL"] = null
                });
            }));

        var options = factory.Services.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>();
        var error = Assert.Throws<InvalidOperationException>(() =>
            options.Get(JwtBearerDefaults.AuthenticationScheme));

        Assert.Equal("Clerk:FrontendAPIURL is not configured.", error.Message);
    }

    public async Task DisposeAsync()
    {
        try
        {
            using var scope = _factory.Services.CreateScope();
            await scope.ServiceProvider.GetRequiredService<PaletteDbContext>().Users
                .Where(u => u.ClerkId == _identity).ExecuteDeleteAsync();
        }
        finally { await _factory.DisposeAsync(); }
    }

    [Theory]
    [InlineData("/api/health")]
    [InlineData("/api/search/movie?q=Example")]
    [InlineData("/api/search/tv?q=Example")]
    [InlineData("/api/lookup/movie/42")]
    [InlineData("/api/lookup/tv/84")]
    [InlineData("/api/search/unregistered?q=Example")]
    public async Task ProtectedEndpointsChallengeAnonymousRequests(string path)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PaletteDbContext>();
        var before = await db.Users.CountAsync();
        using var client = CreateClient();
        using var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Null(response.Headers.Location);
        Assert.Equal(before, await db.Users.CountAsync());
    }

    [Fact]
    public async Task AuthenticatedHealthRequestProvisionsUserOnceAndKeepsExistingProfile()
    {
        var id = $"http-{Guid.NewGuid():N}";
        using var client = CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserHeader, id);
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PaletteDbContext>();
        try
        {
            using var first = await client.GetAsync("/api/health");
            Assert.Equal(HttpStatusCode.OK, first.StatusCode);
            var user = await db.Users.SingleAsync(u => u.ClerkId == id);
            Assert.NotEqual(Guid.Empty, user.UserId);
            Assert.Equal($"{id}@example.test", user.Email);
            Assert.Equal("test-user", user.Username);
            Assert.Equal(DateTimeKind.Utc, user.DateJoined.Kind);
            var userId = user.UserId;
            var joined = user.DateJoined;
            user.Username = "persisted-profile";
            await db.SaveChangesAsync();
            using var second = await client.GetAsync("/api/health");
            Assert.Equal(HttpStatusCode.OK, second.StatusCode);
            db.ChangeTracker.Clear();
            var saved = await db.Users.SingleAsync(u => u.ClerkId == id);
            Assert.Equal(userId, saved.UserId);
            Assert.Equal(joined, saved.DateJoined);
            Assert.Equal("persisted-profile", saved.Username);
        }
        finally
        {
            await db.Users.Where(u => u.ClerkId == id).ExecuteDeleteAsync();
        }
    }

    private HttpClient CreateClient(bool authenticated = false)
    {
        var client = _factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost"),
                AllowAutoRedirect = false,
            });
        if (authenticated) client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserHeader, _identity);
        return client;
    }
}

// PaletteDbContext identifies the application assembly.
// WebApplicationFactory executes that assembly's real Program.cs.
public sealed class PaletteApplicationFactory(string connectionString, Func<HttpMessageHandler>? transport = null)
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
                        // Real provisioning middleware uses the isolated test database.
                        ["ConnectionStrings:DefaultConnection"] =
                            connectionString,
                        ["Clerk:FrontendAPIURL"] = "https://clerk.example.test",

                        ["Tmdb:BaseUrl"] =
                            "https://tmdb.example/3/",

                        ["Tmdb:ImageBaseUrl"] =
                            "https://image.tmdb.org/t/p/w500",

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
            // Use controlled authentication; keep authorization and provisioning.
            TestAuthenticationHandler.Register(services);
            // Replace the external TMDB transport.
            services
                .AddHttpClient<TmdbClient>()
                .ConfigurePrimaryHttpMessageHandler(
                    () => transport?.Invoke() ?? new StubTmdbHandler());
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
