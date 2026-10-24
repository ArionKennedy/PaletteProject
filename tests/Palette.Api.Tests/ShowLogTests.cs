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
    private static LogShowRequest NewShow() => new(Random.Shared.Next(10000000, int.MaxValue), "Integration show");

    [Fact]
    public async Task UnauthenticatedRequestsAreRejected()
    {
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/me/shows")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.PostAsJsonAsync("/api/me/shows", NewShow())).StatusCode);
        client.DefaultRequestHeaders.Authorization = new("Bearer", LoggingAuthentication.DemoToken);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/me/shows")).StatusCode);
    }

    [Fact]
    public async Task EmptyHistoryDoesNotPopulateMedia()
    {
        using var client = await factory.NewUserClient();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PaletteDbContext>();
        var before = await db.Media.CountAsync();
        var result = await client.GetFromJsonAsync<ShowLogPage>("/api/me/shows");
        Assert.Empty(result!.Items);
        Assert.False(result.HasMore);
        Assert.Equal(before, await db.Media.CountAsync());
    }

    [Fact]
    public async Task SavePersistsAndAnotherUserCannotSeeIt()
    {
        using var first = await factory.NewUserClient();
        using var second = await factory.NewUserClient();
        var show = NewShow();
        var response = await first.PostAsJsonAsync("/api/me/shows", show);
        response.EnsureSuccessStatusCode();
        var saved = await response.Content.ReadFromJsonAsync<ShowLogItem>();
        var history = await first.GetFromJsonAsync<ShowLogPage>("/api/me/shows");
        Assert.Equal(saved!.LogId, Assert.Single(history!.Items).LogId);
        Assert.Equal("watched", saved.Status);
        // Each GET uses a fresh server context, so this verifies persisted data, not component state.
        Assert.Single((await first.GetFromJsonAsync<ShowLogPage>("/api/me/shows"))!.Items);
        Assert.Empty((await second.GetFromJsonAsync<ShowLogPage>("/api/me/shows"))!.Items);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PaletteDbContext>();
        Assert.True(await db.TvShows.AnyAsync(item => item.TmdbTvId == show.TmdbTvId));
        Assert.False(await db.Reviews.AnyAsync(review => review.MediaId == saved.MediaId));
    }

    [Fact]
    public async Task ConcurrentSavesAreIdempotentAndMediaIsShared()
    {
        using var first = await factory.NewUserClient();
        using var second = await factory.NewUserClient();
        var show = NewShow();
        var responses = await Task.WhenAll(
            Enumerable.Range(0, 6).Select(_ => first.PostAsJsonAsync("/api/me/shows", show)));
        foreach (var response in responses) response.EnsureSuccessStatusCode();
        (await second.PostAsJsonAsync("/api/me/shows", show)).EnsureSuccessStatusCode();
        var firstItem = Assert.Single((await first.GetFromJsonAsync<ShowLogPage>("/api/me/shows"))!.Items);
        var secondItem = Assert.Single((await second.GetFromJsonAsync<ShowLogPage>("/api/me/shows"))!.Items);
        Assert.Equal(firstItem.MediaId, secondItem.MediaId);
        Assert.NotEqual(firstItem.LogId, secondItem.LogId);
        var repeated = await (await first.PostAsJsonAsync("/api/me/shows", show)).Content.ReadFromJsonAsync<ShowLogItem>();
        Assert.Equal(firstItem.LogId, repeated!.LogId);
        Assert.Equal(firstItem.LoggedDate, repeated.LoggedDate);
    }

    [Theory]
    [InlineData(0, "Invalid ID")]
    [InlineData(-1, "Invalid ID")]
    [InlineData(44, "  ")]
    [InlineData(44, null)]
    public async Task InvalidInputIsRejectedWithoutCreatingALog(int id, string? title)
    {
        using var client = await factory.NewUserClient();
        var response = await client.PostAsJsonAsync("/api/me/shows", new LogShowRequest(id, title));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty((await client.GetFromJsonAsync<ShowLogPage>("/api/me/shows"))!.Items);
    }

    [Fact]
    public async Task PagesAreBoundedAndDoNotOverlap()
    {
        using var client = await factory.NewUserClient();
        for (var index = 0; index < 51; index++)
            (await client.PostAsJsonAsync("/api/me/shows", NewShow())).EnsureSuccessStatusCode();
        var first = await client.GetFromJsonAsync<ShowLogPage>("/api/me/shows");
        var second = await client.GetFromJsonAsync<ShowLogPage>("/api/me/shows?page=2");
        Assert.Equal(50, first!.Items.Count);
        Assert.True(first.HasMore);
        Assert.Single(second!.Items);
        Assert.False(second.HasMore);
        Assert.DoesNotContain(second.Items[0].LogId, first.Items.Select(item => item.LogId));
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/me/shows?page=0")).StatusCode);
    }

    [Fact]
    public async Task UnknownAuthenticatedProfileIsNotSilentlyCreated()
    {
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        client.DefaultRequestHeaders.Add("X-Test-Subject", $"unprovisioned|{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/me/shows")).StatusCode);
    }
}
