using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Palette.Api.Data;
using Palette.Api.Logging;
using Palette.Api.Models;
using Palette.Api.Enums;
using Palette.Tests.Infrastructure;
using Xunit;

namespace Palette.Tests.Integration.Api;

[Collection("Postgres Integration Tests")]
public sealed class ShowLogTests : IAsyncLifetime
{
    private readonly PaletteApplicationFactory factory;
    private readonly List<string> identities = [];
    private readonly List<Guid> mediaIds = [];

    public ShowLogTests(PostgresFixture fixture)
    {
        factory = new PaletteApplicationFactory(fixture.ConnectionString);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        try
        {
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<PaletteDbContext>();
            await db.Users.Where(user => identities.Contains(user.ClerkId)).ExecuteDeleteAsync();
            await db.Media.Where(media => mediaIds.Contains(media.MediaId)).ExecuteDeleteAsync();
        }
        finally { await factory.DisposeAsync(); }
    }

    private async Task<TestViewer> NewUserClient()
    {
        var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        var subject = $"logging-{Guid.NewGuid():N}";
        identities.Add(subject);
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserHeader, subject);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PaletteDbContext>();
        db.Users.Add(new User
        {
            UserId = Guid.NewGuid(), ClerkId = subject, Username = "Test viewer",
            Email = $"{Guid.NewGuid()}@palette.invalid", DateJoined = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();
        return new TestViewer(client, subject);
    }

    private async Task<Guid> AddTitle(TestViewer client, MediaType type = MediaType.Movie,
        bool reviewed = true, Guid? mediaId = null, DateTime? createdAt = null)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PaletteDbContext>();
        var subject = client.UserId;
        var user = await db.Users.SingleAsync(user => user.ClerkId == subject);
        var id = mediaId ?? Guid.NewGuid();
        if (mediaId is null)
        {
            mediaIds.Add(id);
            db.Media.Add(new Media { MediaId = id, Title = "Review test " + id, MediaType = type });
        }
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
            new ShowLogQuery(page));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ShowLogPage>())!;
    }

    [Fact]
    public async Task UnauthenticatedRequestsAreRejectedEvenWithAnotherUserId()
    {
        using var viewer = await NewUserClient();
        await AddTitle(viewer);
        using var anonymous = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        using var response = await anonymous.PostAsJsonAsync("/api/me/shows/query",
            new { userId = viewer.UserId, page = 1 });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task EmptyHistoryDoesNotWriteRecords()
    {
        using var client = await NewUserClient();
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
        using var first = await NewUserClient();
        using var second = await NewUserClient();
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
        using var client = await NewUserClient();
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
        using var client = await NewUserClient();
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
        using var client = await NewUserClient();
        for (var i = 0; i < 51; i++) await AddTitle(client);
        var first = await ReadHistory(client);
        var second = await ReadHistory(client, 2);
        Assert.Equal(50, first!.Items.Count);
        Assert.True(first.HasMore);
        Assert.Single(second!.Items);
        Assert.False(second.HasMore);
        Assert.DoesNotContain(second.Items[0].MediaId, first.Items.Select(item => item.MediaId));
        Assert.Equal(HttpStatusCode.BadRequest, (await client.Client.PostAsJsonAsync("/api/me/shows/query", new ShowLogQuery(0))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.Client.PostAsJsonAsync("/api/me/shows/query", new ShowLogQuery(10001))).StatusCode);
    }

    [Fact]
    public async Task NewAuthenticatedUserIsProvisionedBySharedMiddleware()
    {
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        var subject = $"logging-new-{Guid.NewGuid():N}";
        identities.Add(subject);
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserHeader, subject);
        using var response = await client.PostAsJsonAsync("/api/me/shows/query", new ShowLogQuery());
        response.EnsureSuccessStatusCode();
        var page = await response.Content.ReadFromJsonAsync<ShowLogPage>();
        Assert.Empty(page!.Items);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PaletteDbContext>();
        Assert.Equal(1, await db.Users.CountAsync(user => user.ClerkId == subject));
    }

    [Fact]
    public async Task SuppliedUserIdCannotReadAnotherUsersHistory()
    {
        using var first = await NewUserClient();
        using var second = await NewUserClient();
        var firstTitle = await AddTitle(first);
        var secondTitle = await AddTitle(second);
        using var response = await second.Client.PostAsJsonAsync("/api/me/shows/query",
            new { userId = first.UserId, page = 1 });
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ShowLogPage>();
        Assert.Equal(secondTitle, Assert.Single(result!.Items).MediaId);
        Assert.DoesNotContain(result.Items, item => item.MediaId == firstTitle);
    }
}

public sealed record TestViewer(HttpClient Client, string UserId) : IDisposable
{
    public void Dispose() => Client.Dispose();
}
