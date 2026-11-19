using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Backend.Providers;
using Backend.Shared.Errors;
using Backend.Shared.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Palette.Api.Data;
using Palette.Api.Enums;
using Palette.Api.Logging;
using Palette.Api.Models;
using Palette.Tests.Infrastructure;
using Palette.Tests.TestDoubles;
using Xunit;

namespace Palette.Tests.Integration.Api;

[Collection("Postgres Integration Tests")]
public sealed class LibraryTests : IAsyncLifetime
{
    private readonly WebApplicationFactory<PaletteDbContext> factory;
    private readonly List<string> identities = [];
    private readonly List<Guid> mediaIds = [];
    private const string CoverUrl = "https://covers.example.test/library.jpg";
    private const string UnavailableVolumePrefix = "unavailable-volume-";
    private int providerFailures;

    public LibraryTests(PostgresFixture fixture)
    {
        factory = new PaletteApplicationFactory(fixture.ConnectionString).WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IMediaProvider>();
                foreach (var type in new[] { "movie", "tv", "book" })
                {
                    services.AddScoped<IMediaProvider>(_ => new StubMediaProvider(type)
                    {
                        LookupHandler = (id, _) =>
                        {
                            if (type == "book" && id.StartsWith(UnavailableVolumePrefix, StringComparison.Ordinal))
                            {
                                Interlocked.Increment(ref providerFailures);
                                throw new ProviderUnavailableException("Google Books",
                                    new HttpRequestException("Simulated provider outage"));
                            }

                            return Task.FromResult<MediaLookupResult>(type switch
                            {
                                "movie" => new MovieLookupResult(id, "Library Movie", null, null, CoverUrl, 120),
                                "tv" => new TvLookupResult(id, "Library Series", null, null, CoverUrl, 2),
                                _ => new BookLookupResult(id, "Library Book", null, null, CoverUrl, null)
                            });
                        }
                    });
                }
            }));
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

    [Theory]
    [InlineData("movie", "004321", "4321")]
    [InlineData("tv", "4321", "4321")]
    [InlineData("book", "library-test-volume", "library-test-volume")]
    public async Task LibraryAddAndRemoveAreIdempotentAndScopedToTheCurrentUser(
        string type, string inputId, string externalId)
    {
        using var first = await NewUserClient();
        using var second = await NewUserClient();
        var path = $"/api/me/library/{type}/{inputId}";
        using var anonymous = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PutAsync(path, null)).StatusCode);
        Assert.False((await first.Client.GetFromJsonAsync<LibraryMembership>(path))!.Saved);

        using var unsaved = await ReadDetails(first, type, inputId);
        Assert.False(unsaved.RootElement.GetProperty("saved").GetBoolean());

        var adds = await Task.WhenAll(first.Client.PutAsync(path, null), first.Client.PutAsync(path, null),
            second.Client.PutAsync(path, null));
        foreach (var response in adds)
        {
            using var ownedResponse = response;
            response.EnsureSuccessStatusCode();
            Assert.True((await response.Content.ReadFromJsonAsync<LibraryMembership>())!.Saved);
        }

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PaletteDbContext>();
        var title = type == "tv" ? "Library Series" : "Library " + (type == "movie" ? "Movie" : "Book");
        var media = await db.Media.SingleAsync(item => item.Title == title);
        mediaIds.Add(media.MediaId);
        Assert.Equal(2, await db.UserMediaLogs.CountAsync(log => log.MediaId == media.MediaId));

        var result = await first.Client.GetFromJsonAsync<LibraryPage>($"/api/me/library/{type}");
        var item = Assert.Single(result!.Items);
        Assert.Equal(type, item.MediaType);
        Assert.Equal(externalId, item.ExternalId);
        Assert.True((await first.Client.GetFromJsonAsync<LibraryMembership>($"/api/me/library/{type}/{externalId}"))!.Saved);

        using var details = await ReadDetails(first, type, externalId);
        Assert.True(details.RootElement.GetProperty("saved").GetBoolean());
        Assert.Equal(item.Title, details.RootElement.GetProperty("title").GetString());

        var previews = await first.Client.GetFromJsonAsync<LibraryPreviews>("/api/me/library/previews");
        Assert.NotNull(previews);
        var category = type switch
        {
            "movie" => previews.Movie,
            "tv" => previews.Tv,
            _ => previews.Book
        };
        var preview = Assert.Single(category);
        Assert.Single(previews.Movie.Concat(previews.Tv).Concat(previews.Book));
        Assert.Equal(item, preview);
        Assert.Equal(CoverUrl, preview.PosterUrl);

        var counts = await first.Client.GetFromJsonAsync<LibraryCounts>("/api/me/library");
        Assert.Equal(new LibraryCounts(type == "movie" ? 1 : 0, type == "tv" ? 1 : 0, type == "book" ? 1 : 0), counts);

        for (var i = 0; i < 2; i++)
        {
            using var removal = await first.Client.DeleteAsync(path);
            removal.EnsureSuccessStatusCode();
            Assert.False((await removal.Content.ReadFromJsonAsync<LibraryMembership>())!.Saved);
        }

        Assert.False((await first.Client.GetFromJsonAsync<LibraryMembership>(path))!.Saved);
        Assert.Empty((await first.Client.GetFromJsonAsync<LibraryPage>($"/api/me/library/{type}"))!.Items);
        var afterRemoval = await first.Client.GetFromJsonAsync<LibraryPreviews>("/api/me/library/previews");
        Assert.Empty(afterRemoval!.Movie.Concat(afterRemoval.Tv).Concat(afterRemoval.Book));
        Assert.True((await second.Client.GetFromJsonAsync<LibraryMembership>(path))!.Saved);
        using var firstDetails = await ReadDetails(first, type, externalId);
        using var secondDetails = await ReadDetails(second, type, externalId);
        Assert.False(firstDetails.RootElement.GetProperty("saved").GetBoolean());
        Assert.True(secondDetails.RootElement.GetProperty("saved").GetBoolean());
        Assert.True(await db.Media.AnyAsync(record => record.MediaId == media.MediaId));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    [InlineData("https://covers.example.test/existing.jpg")]
    public async Task SavingAnExistingBookFillsMissingCoversAndPreservesExistingOnes(string? previousCover)
    {
        using var viewer = await NewUserClient();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PaletteDbContext>();
        var externalId = $"volume-{Guid.NewGuid():N}";
        var mediaId = await SeedExistingBook(db, externalId, previousCover);

        using var response = await viewer.Client.PutAsync($"/api/me/library/book/{externalId}", null);
        response.EnsureSuccessStatusCode();

        var expected = string.IsNullOrWhiteSpace(previousCover) ? CoverUrl : previousCover;
        var storedCover = await db.Media.Where(media => media.MediaId == mediaId)
            .Select(media => media.PosterUrl).SingleAsync();
        var previews = await viewer.Client.GetFromJsonAsync<LibraryPreviews>("/api/me/library/previews");
        var book = Assert.Single(previews!.Book);
        Assert.Equal(externalId, book.ExternalId);
        Assert.Equal(expected, storedCover);
        Assert.Equal(expected, book.PosterUrl);
    }

    [Fact]
    public async Task SavingAnExistingBookSucceedsWhenItsMissingCoverCannotBeFetched()
    {
        using var viewer = await NewUserClient();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PaletteDbContext>();
        var externalId = $"{UnavailableVolumePrefix}{Guid.NewGuid():N}";
        var mediaId = await SeedExistingBook(db, externalId, posterUrl: null);

        using var response = await viewer.Client.PutAsync($"/api/me/library/book/{externalId}", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True((await response.Content.ReadFromJsonAsync<LibraryMembership>())!.Saved);
        Assert.Equal(1, providerFailures);
        Assert.Single(await db.UserMediaLogs.Where(log =>
            log.MediaId == mediaId && log.User.ClerkId == viewer.UserId).ToListAsync());

        var previews = await viewer.Client.GetFromJsonAsync<LibraryPreviews>("/api/me/library/previews");
        var book = Assert.Single(previews!.Book);
        Assert.Equal(externalId, book.ExternalId);
        Assert.Null(book.PosterUrl);
    }

    [Fact]
    public async Task CoverPreviewsAreLimitedWithoutTruncatingTheFullList()
    {
        using var viewer = await NewUserClient();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PaletteDbContext>();
        var userId = await db.Users.Where(user => user.ClerkId == viewer.UserId)
            .Select(user => user.UserId).SingleAsync();
        var newest = new DateOnly(2026, 11, 19);
        for (var i = 0; i < 13; i++)
        {
            var mediaId = Guid.NewGuid();
            mediaIds.Add(mediaId);
            db.Media.Add(new Media
            {
                MediaId = mediaId,
                Title = $"Preview book {i}",
                MediaType = MediaType.Book,
                PosterUrl = CoverUrl,
                Book = new Book { GoogleBooksId = $"preview-book-{mediaId}" }
            });
            db.UserMediaLogs.Add(new UserMediaLog
            {
                UserId = userId,
                MediaId = mediaId,
                LoggedDate = newest.AddDays(-i)
            });
        }
        await db.SaveChangesAsync();

        var previews = await viewer.Client.GetFromJsonAsync<LibraryPreviews>("/api/me/library/previews");
        var list = await viewer.Client.GetFromJsonAsync<LibraryPage>("/api/me/library/book");

        Assert.Equal(13, list!.Items.Count);
        Assert.Equal(12, previews!.Book.Count);
        Assert.Equal(list.Items.Take(12).ToArray(), previews.Book.ToArray());
        Assert.Equal("Preview book 0", previews.Book[0].Title);
        Assert.Empty(previews.Movie);
        Assert.Empty(previews.Tv);
    }

    private async Task<Guid> SeedExistingBook(PaletteDbContext db, string externalId, string? posterUrl)
    {
        var mediaId = Guid.NewGuid();
        mediaIds.Add(mediaId);
        db.Media.Add(new Media
        {
            MediaId = mediaId,
            MediaType = MediaType.Book,
            Title = "Existing library book",
            PosterUrl = posterUrl,
            Book = new Book { GoogleBooksId = externalId }
        });
        await db.SaveChangesAsync();
        return mediaId;
    }

    private static async Task<JsonDocument> ReadDetails(TestViewer viewer, string type, string externalId)
    {
        using var response = await viewer.Client.GetAsync($"/api/lookup/{type}/{externalId}?includeLibrary=true");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JsonDocument>())!;
    }

    private sealed record TestViewer(HttpClient Client, string UserId) : IDisposable
    {
        public void Dispose() => Client.Dispose();
    }
}
