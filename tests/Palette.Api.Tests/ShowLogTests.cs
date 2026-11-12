using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Palette.Api.Data;
using Palette.Api.Logging;
using Palette.Api.Models;
using Palette.Api.Enums;
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
        builder.UseSetting("ConnectionStrings:DefaultConnection", ConnectionString);
        builder.UseSetting("Tmdb:ReadAccessToken", "logging-tests-no-external-access");
        builder.UseSetting("Tmdb:BaseUrl", "http://127.0.0.1:1/");
    }

    public async Task<TestViewer> NewUserClient()
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
        return new TestViewer(client, subject);
    }
}

public sealed record TestViewer(HttpClient Client, string UserId) : IDisposable
{
    public void Dispose() => Client.Dispose();
}

public sealed class ShowLogTests : IClassFixture<LoggingApiFactory>
{
    private readonly LoggingApiFactory factory;
    public ShowLogTests(LoggingApiFactory factory) => this.factory = factory;

    private async Task<Guid> AddTitle(TestViewer client, MediaType type = MediaType.Movie,
        bool reviewed = true, Guid? mediaId = null, DateTime? createdAt = null)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PaletteDbContext>();
        var subject = client.UserId;
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

    private static async Task<ShowLogPage> ReadHistory(TestViewer viewer, int page = 1)
    {
        using var response = await viewer.Client.PostAsJsonAsync("/api/me/shows/query",
            new ShowLogQuery(viewer.UserId, page));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ShowLogPage>())!;
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task MissingUserIdIsRejected(string? userId)
    {
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.PostAsJsonAsync("/api/me/shows/query", new ShowLogQuery(userId))).StatusCode);
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
        var result = await ReadHistory(client);
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
        var movie = await AddTitle(first, MediaType.Movie);
        var tv = await AddTitle(first, MediaType.TVShow);
        await AddTitle(first, reviewed: false);
        await AddTitle(second, MediaType.Book);
        var page = await ReadHistory(first);
        Assert.Equal(2, page!.Items.Count);
        Assert.Contains(page.Items, item => item.MediaId == movie && item.MediaType == "Movie");
        Assert.Contains(page.Items, item => item.MediaId == tv && item.MediaType == "TVShow");
        Assert.Single((await ReadHistory(second))!.Items);

    }

    [Fact]
    public async Task RepeatedReviewsProduceOneTitleWithLatestDate()
    {
        using var client = await factory.NewUserClient();
        var latest = DateTime.UtcNow.AddMinutes(-1);
        var first = await AddTitle(client, createdAt: latest.AddDays(-2));
        var second = await AddTitle(client, createdAt: latest.AddDays(-1));
        await AddTitle(client, mediaId: first, createdAt: latest);
        var page = await ReadHistory(client);
        Assert.Equal(2, page!.Items.Count);
        Assert.Equal(first, page.Items[0].MediaId);
        Assert.Equal(second, page.Items[1].MediaId);
        Assert.True(Math.Abs((page.Items[0].LastReviewedAt - latest).TotalMilliseconds) < 1);
        Assert.Equal(first, (await ReadHistory(client))!.Items[0].MediaId);
    }

    [Fact]
    public async Task RemovingLastReviewRemovesTitleFromHistory()
    {
        using var client = await factory.NewUserClient();
        var id = await AddTitle(client);
        Assert.Single((await ReadHistory(client))!.Items);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PaletteDbContext>();
        // Only this test's review in the dedicated test database is removed.
        db.Reviews.Remove(await db.Reviews.SingleAsync(review => review.MediaId == id));
        await db.SaveChangesAsync();
        Assert.Empty((await ReadHistory(client))!.Items);
    }

    [Fact]
    public async Task PagesAreBoundedAndDoNotOverlap()
    {
        using var client = await factory.NewUserClient();
        for (var i = 0; i < 51; i++) await AddTitle(client);
        var first = await ReadHistory(client);
        var second = await ReadHistory(client, 2);
        Assert.Equal(50, first!.Items.Count);
        Assert.True(first.HasMore);
        Assert.Single(second!.Items);
        Assert.False(second.HasMore);
        Assert.DoesNotContain(second.Items[0].MediaId, first.Items.Select(item => item.MediaId));
        Assert.Equal(HttpStatusCode.BadRequest, (await client.Client.PostAsJsonAsync("/api/me/shows/query", new ShowLogQuery(client.UserId, 0))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.Client.PostAsJsonAsync("/api/me/shows/query", new ShowLogQuery(client.UserId, 10001))).StatusCode);
    }

    [Fact]
    public async Task UnknownProfileIsNotSilentlyCreated()
    {
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        Assert.Equal(HttpStatusCode.NotFound,
            (await client.PostAsJsonAsync("/api/me/shows/query",
                new ShowLogQuery($"unprovisioned|{Guid.NewGuid()}"))).StatusCode);
    }

    [Fact]
    public async Task TemporaryContractDoesNotVerifyCallerIdentity()
    {
        using var first = await factory.NewUserClient();
        using var second = await factory.NewUserClient();
        var id = await AddTitle(first);
        // This documents a known temporary limitation, NOT a security guarantee.
        // Shared authentication must prevent this before the endpoint is released.
        using var response = await second.Client.PostAsJsonAsync("/api/me/shows/query",
            new ShowLogQuery(first.UserId));
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ShowLogPage>();
        Assert.Equal(id, Assert.Single(result!.Items).MediaId);
    }
}
