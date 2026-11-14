using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Palette.Api.Data;
using Testcontainers.PostgreSql;
using Xunit;

namespace Palette.Tests.Integration.Api;

public class DevelopmentStartupTests
    : IClassFixture<DevelopmentApplicationFixture>
{
    private readonly DevelopmentApplicationFixture _fixture;

    public DevelopmentStartupTests(
        DevelopmentApplicationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task DevelopmentStartupAppliesMigrationsToAFreshDatabase()
    {
        Assert.Empty(_fixture.MigrationsBeforeStartup);

        using var scope =
            _fixture.Factory.Services.CreateScope();

        var environment = scope.ServiceProvider
            .GetRequiredService<IHostEnvironment>();

        Assert.True(environment.IsDevelopment());

        var db = scope.ServiceProvider
            .GetRequiredService<PaletteDbContext>();

        var expected = db.Database
            .GetMigrations()
            .ToArray();

        var applied = (
            await db.Database.GetAppliedMigrationsAsync())
            .ToArray();

        Assert.NotEmpty(expected);
        Assert.Equal(expected, applied);

        // Verify that startup created usable application tables.
        Assert.Equal(0, await db.Users.CountAsync());
        Assert.Equal(0, await db.Media.CountAsync());
    }

    [Fact]
    public async Task DevelopmentExposesOpenApiDocument()
    {
        using var client =
            _fixture.CreateClient("https://localhost");

        using var response =
            await client.GetAsync("/openapi/v1.json");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.Equal(
            "application/json",
            response.Content.Headers.ContentType?.MediaType);

        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        var root = document.RootElement;

        Assert.StartsWith(
            "3.",
            root.GetProperty("openapi").GetString()!);

        var paths = root.GetProperty("paths");

        Assert.True(
            paths.TryGetProperty("/api/health", out _));

        Assert.True(
            paths.TryGetProperty(
                "/api/search/{type}",
                out _));

        Assert.True(
            paths.TryGetProperty(
                "/api/lookup/{type}/{id}",
                out _));
    }

    [Fact]
    public async Task DevelopmentRedirectsHttpToHttpsAndPreservesPathAndQuery()
    {
        using var client =
            _fixture.CreateClient("http://localhost");

        using var response =
            await client.GetAsync("/api/health?probe=1");

        Assert.Equal(
            HttpStatusCode.TemporaryRedirect,
            response.StatusCode);

        Assert.Equal(
            new Uri(
                "https://localhost:7443/api/health?probe=1"),
            response.Headers.Location);
    }

    [Fact]
    public async Task DevelopmentServesHttpsRequestsWithoutAnotherRedirect()
    {
        using var client =
            _fixture.CreateClient("https://localhost:7443");

        using var response =
            await client.GetAsync("/api/health");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.Null(response.Headers.Location);

        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        Assert.Equal(
            "ok",
            document.RootElement
                .GetProperty("status")
                .GetString());
    }
}

public sealed class DevelopmentApplicationFixture
    : IAsyncLifetime
{
    // Always use a fresh database for startup migration verification.
    // Do not reuse PostgresFixture: it applies migrations before startup.
    private readonly PostgreSqlContainer _container =
        new PostgreSqlBuilder("postgres:17")
            .WithDatabase("palette_development_tests")
            .WithUsername("development_test")
            .WithPassword(
                "temporary-development-test-password")
            .Build();

    public DevelopmentApplicationFactory Factory
    {
        get;
        private set;
    } = null!;

    public string[] MigrationsBeforeStartup
    {
        get;
        private set;
    } = [];

    public async Task InitializeAsync()
    {
        try
        {
            await _container.StartAsync();

            var connectionString =
                _container.GetConnectionString();

            var options =
                new DbContextOptionsBuilder<PaletteDbContext>()
                    .UseNpgsql(connectionString)
                    .Options;

            // Inspect the database without applying migrations.
            await using (var db = new PaletteDbContext(options))
            {
                MigrationsBeforeStartup = (
                    await db.Database.GetAppliedMigrationsAsync())
                    .ToArray();
            }

            Factory = new DevelopmentApplicationFactory(
                connectionString);

            // Creating the client starts the real Program.cs.
            // Its Development branch must apply the migrations.
            using var client =
                CreateClient("https://localhost");
        }
        catch
        {
            await DisposeAsync();
            throw;
        }
    }

    public HttpClient CreateClient(string baseAddress)
    {
        return Factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri(baseAddress),
                AllowAutoRedirect = false,
            });
    }

    public async Task DisposeAsync()
    {
        try
        {
            if (Factory is not null)
            {
                await Factory.DisposeAsync();
            }
        }
        finally
        {
            await _container.DisposeAsync();
        }
    }
}

public sealed class DevelopmentApplicationFactory(
    string connectionString)
    : WebApplicationFactory<PaletteDbContext>
{
    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration(
            (_, configuration) =>
            {
                configuration.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:DefaultConnection"] =
                            connectionString,

                        ["Tmdb:BaseUrl"] =
                            "https://tmdb.example/3/",

                        ["Tmdb:ImageBaseUrl"] =
                            "https://images.example/posters/",

                        ["Tmdb:ReadAccessToken"] =
                            "development-startup-test-token",

                        ["Tmdb:TimeoutSeconds"] = "10",
                    });
            });

        builder.ConfigureLogging(
            logging => logging.ClearProviders());

        builder.ConfigureServices(services =>
        {
            // TestServer has no real HTTPS listener.
            // Specify a deterministic redirect destination.
            services.Configure<HttpsRedirectionOptions>(
                options =>
                {
                    options.HttpsPort = 7443;
                });
        });
    }
}
