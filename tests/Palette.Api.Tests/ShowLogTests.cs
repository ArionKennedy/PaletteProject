using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using Palette.Api.Data;
using Palette.Api.Logging;
using Palette.Api.Models;
using Xunit;

namespace Palette.Api.Tests;

public sealed class LoggingApiFactory : WebApplicationFactory<Program>
{
    public string ConnectionString { get; } = GetTestConnection();

    private static string GetTestConnection()
    {
        if (new[] { "POSTGRES_DB", "POSTGRES_USER", "POSTGRES_PASSWORD" }
            .Any(key => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key))))
            throw new InvalidOperationException("Unset POSTGRES_* overrides before running isolated logging tests.");
        var value = Environment.GetEnvironmentVariable("PALETTE_TEST_DATABASE")
            ?? throw new InvalidOperationException("Set PALETTE_TEST_DATABASE to a dedicated PostgreSQL test database.");
        var parsed = new NpgsqlConnectionStringBuilder(value);
        if (parsed.Database != "palette_logging_tests" || parsed.Host is not ("localhost" or "127.0.0.1"))
            throw new InvalidOperationException("Tests only accept a local database named palette_logging_tests.");
        return value;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Avoid the generated test manifest's path parsing issue with apostrophes
        // in directory names (for example, the user's iCloud project folder).
        builder.UseContentRoot(Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "../../../../../backend")));
        builder.UseEnvironment("Testing");
        builder.UseSetting("AUTH_CLERK_URL", "https://clerk.logging-tests.invalid");
        builder.UseSetting("ConnectionStrings:DefaultConnection", ConnectionString);
        builder.UseSetting("Tmdb:ReadAccessToken", "logging-tests-no-external-access");
        builder.UseSetting("Tmdb:BaseUrl", "http://127.0.0.1:1/");
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = ConnectionString,
                // Logging tests never call TMDB; satisfy provider startup validation locally.
                ["Tmdb:ReadAccessToken"] = "logging-tests-no-external-access",
                ["Tmdb:BaseUrl"] = "http://127.0.0.1:1/",
            }));
        builder.ConfigureServices(services =>
        {
            // Test-only identities; this handler is not part of the application project.
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "TestUser";
                options.DefaultChallengeScheme = "TestUser";
            }).AddScheme<AuthenticationSchemeOptions, TestUserHandler>("TestUser", _ => { });
        });
    }

    public async Task<HttpClient> NewUserClient()
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PaletteDbContext>();
        await db.Database.MigrateAsync();
        var subject = $"tests|{Guid.NewGuid()}";
        db.Users.Add(new User
        {
            UserId = Guid.NewGuid(), Auth0Id = subject, Username = "Test viewer",
            Email = $"{Guid.NewGuid()}@palette.invalid", DateJoined = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();
        client.DefaultRequestHeaders.Add("X-Test-Subject", subject);
        return client;
    }
}

public sealed class TestUserHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var subject = Request.Headers["X-Test-Subject"].ToString();
        if (string.IsNullOrEmpty(subject)) return Task.FromResult(AuthenticateResult.NoResult());
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(
            new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", subject)], Scheme.Name)), Scheme.Name)));
    }
}

public sealed class ShowLogTests : IClassFixture<LoggingApiFactory>
{
    private readonly LoggingApiFactory factory;
    public ShowLogTests(LoggingApiFactory factory) => this.factory = factory;

    private async Task<Guid> AddTitle(HttpClient client, string type = "movie",
        bool reviewed = true, Guid? mediaId = null, DateTime? createdAt = null)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PaletteDbContext>();
        var subject = client.DefaultRequestHeaders.GetValues("X-Test-Subject").Single();
        var user = await db.Users.SingleAsync(user => user.Auth0Id == subject);
        var id = mediaId ?? Guid.NewGuid();
        if (mediaId is null)
            db.Media.Add(new Media { MediaId = id, Title = "Review test " + id, MediaType = type });
        if (reviewed)
            db.Reviews.Add(new Review {
                ReviewId = Guid.NewGuid(), UserId = user.UserId, MediaId = id,
                Rating = 4, Body = "Test review", CreatedAt = createdAt ?? DateTime.UtcNow
            });
        else
            db.UserMediaLogs.Add(new UserMediaLog {
                LogId = Guid.NewGuid(), UserId = user.UserId, MediaId = id,
                Status = "watched", LoggedDate = DateOnly.FromDateTime(DateTime.UtcNow)
            });
        await db.SaveChangesAsync();
        return id;
    }

    [Fact]
    public async Task UnauthenticatedRequestsAreRejected()
    {
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/me/shows")).StatusCode);
        client.DefaultRequestHeaders.Authorization = new("Bearer", "invalid-token");
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/me/shows")).StatusCode);
    }

    [Fact]
    public async Task EmptyHistoryDoesNotWriteRecords()
    {
        using var client = await factory.NewUserClient();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PaletteDbContext>();
        var mediaCount = await db.Media.CountAsync();
        var logCount = await db.UserMediaLogs.CountAsync();
        var reviewCount = await db.Reviews.CountAsync();
        var result = await client.GetFromJsonAsync<ShowLogPage>("/api/me/shows");
        Assert.Empty(result!.Items);
        Assert.False(result.HasMore);
        Assert.Equal(mediaCount, await db.Media.CountAsync());
        Assert.Equal(logCount, await db.UserMediaLogs.CountAsync());
        Assert.Equal(reviewCount, await db.Reviews.CountAsync());
    }

    [Fact]
    public async Task OnlyOwnReviewedTitlesAppearAcrossMediaTypes()
    {
        using var first = await factory.NewUserClient();
        using var second = await factory.NewUserClient();
        var movie = await AddTitle(first, "movie");
        var tv = await AddTitle(first, "tv");
        await AddTitle(first, reviewed: false);
        await AddTitle(second, "book");
        var page = await first.GetFromJsonAsync<ShowLogPage>("/api/me/shows");
        Assert.Equal(2, page!.Items.Count);
        Assert.Contains(page.Items, item => item.MediaId == movie && item.MediaType == "movie");
        Assert.Contains(page.Items, item => item.MediaId == tv && item.MediaType == "tv");
        Assert.Single((await second.GetFromJsonAsync<ShowLogPage>("/api/me/shows"))!.Items);
        // Caller-supplied IDs must not change whose history is read.
        var spoofed = await second.GetFromJsonAsync<ShowLogPage>("/api/me/shows?user_id=" + first.DefaultRequestHeaders.GetValues("X-Test-Subject").Single());
        Assert.DoesNotContain(spoofed!.Items, item => item.MediaId == movie || item.MediaId == tv);
    }

    [Fact]
    public async Task RepeatedReviewsProduceOneTitleWithLatestDate()
    {
        using var client = await factory.NewUserClient();
        var latest = DateTime.UtcNow.AddMinutes(-1);
        var first = await AddTitle(client, createdAt: latest.AddDays(-2));
        var second = await AddTitle(client, createdAt: latest.AddDays(-1));
        await AddTitle(client, mediaId: first, createdAt: latest);
        var page = await client.GetFromJsonAsync<ShowLogPage>("/api/me/shows");
        Assert.Equal(2, page!.Items.Count);
        Assert.Equal(first, page.Items[0].MediaId);
        Assert.Equal(second, page.Items[1].MediaId);
        Assert.True(Math.Abs((page.Items[0].LastReviewedAt - latest).TotalMilliseconds) < 1);
        Assert.Equal(first, (await client.GetFromJsonAsync<ShowLogPage>("/api/me/shows"))!.Items[0].MediaId);
    }

    [Fact]
    public async Task RemovingLastReviewRemovesTitleFromHistory()
    {
        using var client = await factory.NewUserClient();
        var id = await AddTitle(client);
        Assert.Single((await client.GetFromJsonAsync<ShowLogPage>("/api/me/shows"))!.Items);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PaletteDbContext>();
        // Only this test's review in the dedicated test database is removed.
        db.Reviews.Remove(await db.Reviews.SingleAsync(review => review.MediaId == id));
        await db.SaveChangesAsync();
        Assert.Empty((await client.GetFromJsonAsync<ShowLogPage>("/api/me/shows"))!.Items);
    }

    [Fact]
    public async Task PagesAreBoundedAndDoNotOverlap()
    {
        using var client = await factory.NewUserClient();
        for (var i = 0; i < 51; i++) await AddTitle(client);
        var first = await client.GetFromJsonAsync<ShowLogPage>("/api/me/shows");
        var second = await client.GetFromJsonAsync<ShowLogPage>("/api/me/shows?page=2");
        Assert.Equal(50, first!.Items.Count);
        Assert.True(first.HasMore);
        Assert.Single(second!.Items);
        Assert.False(second.HasMore);
        Assert.DoesNotContain(second.Items[0].MediaId, first.Items.Select(item => item.MediaId));
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/me/shows?page=0")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/me/shows?page=10001")).StatusCode);
    }

    [Fact]
    public async Task UnknownAuthenticatedProfileIsNotSilentlyCreated()
    {
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        client.DefaultRequestHeaders.Add("X-Test-Subject", $"unprovisioned|{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/me/shows")).StatusCode);
    }

    [Fact]
    public async Task WatchedSaveEndpointIsNoLongerExposed()
    {
        using var client = await factory.NewUserClient();
        Assert.Equal(HttpStatusCode.MethodNotAllowed,
            (await client.PostAsJsonAsync("/api/me/shows", new { tmdbTvId = 1396, title = "Not a review" })).StatusCode);
    }
}
