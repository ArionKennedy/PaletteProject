using System.Net;
using System.Text;
using System.Text.Json;
using Backend.Constants;
using Backend.Providers.Igdb;
using Backend.Shared.Errors;
using Backend.Shared.Models;
using Microsoft.Extensions.Options;

namespace Palette.Tests.Unit.Igdb;

public class IgdbProviderTests
{
    private static readonly Uri BaseUrl = new("https://igdb.example/v4/");
    private const string ImageBaseUrl = "https://images.igdb.example/upload/";

    private const string SearchResponse = """
        [
          {
            "id": 1,
            "name": "First Game",
            "first_release_date": 1431993600,
            "cover": { "id": 1, "image_id": "co1abc" }
          },
          { "id": 2, "name": "Second Game" }
        ]
        """;

    private const string LookupResponse = """
        [{
          "id": 1942,
          "name": "Example Game",
          "summary": "  A big adventure.  ",
          "first_release_date": 1431993600,
          "cover": { "id": 1, "image_id": "co1abc" }
        }]
        """;

    // ---------- MediaType ----------

    [Fact]
    public void MediaTypeIsGame()
    {
        using var http = CreateHttpClient(HttpStatusCode.OK, "[]");

        Assert.Equal("game", CreateProvider(http).MediaType);
    }

    // ---------- Search ----------

    [Fact]
    public async Task SearchMapsResultsAndPreservesProviderOrder()
    {
        using var http = CreateHttpClient(HttpStatusCode.OK, SearchResponse);

        var results = await CreateProvider(http).SearchAsync("game", 10, CancellationToken.None);

        Assert.Collection(
            results,
            first =>
            {
                Assert.Equal("1", first.ExternalId);
                Assert.Equal("game", first.MediaType);
                Assert.Equal("First Game", first.Title);
                Assert.Equal(new DateOnly(2015, 5, 19), first.ReleaseDate);
                Assert.Equal(ImageBaseUrl + "t_cover_big/co1abc.jpg", first.PosterUrl);
            },
            second =>
            {
                Assert.Equal("2", second.ExternalId);
                Assert.Equal("Second Game", second.Title);
                Assert.Null(second.ReleaseDate);
                Assert.Null(second.PosterUrl);
            });
    }

    [Fact]
    public async Task SearchForwardsQueryAndMaxResultsToTheClient()
    {
        string? body = null;

        using var http = CreateHttpClient(async (request, ct) =>
        {
            body = await request.Content!.ReadAsStringAsync(ct);
            return JsonResponse(HttpStatusCode.OK, SearchResponse);
        });

        await CreateProvider(http).SearchAsync("zelda", 7, CancellationToken.None);

        Assert.StartsWith("search \"zelda\";", body);
        Assert.EndsWith("limit 7;", body);
    }

    [Fact]
    public async Task SearchWithNoMatchesReturnsEmptyList()
    {
        using var http = CreateHttpClient(HttpStatusCode.OK, "[]");

        var results = await CreateProvider(http).SearchAsync("nothing", 10, CancellationToken.None);

        Assert.Empty(results);
    }

    // ---------- Lookup ----------

    [Fact]
    public async Task LookupReturnsMappedGameDetails()
    {
        using var http = CreateHttpClient(HttpStatusCode.OK, LookupResponse);

        var result = await CreateProvider(http).LookupAsync("1942", CancellationToken.None);

        var game = Assert.IsType<GameLookupResult>(result);
        Assert.Equal("1942", game.ExternalId);
        Assert.Equal("Example Game", game.Title);
        Assert.Equal(new DateOnly(2015, 5, 19), game.ReleaseDate);
        Assert.Equal("A big adventure.", game.Description);
        Assert.Equal(ImageBaseUrl + "t_cover_big/co1abc.jpg", game.PosterUrl);
    }

    [Fact]
    public async Task LookupOfUnknownGameBecomesMediaNotFound()
    {
        // IGDB answers 200 with [] for an unknown id; the client maps that to null.
        using var http = CreateHttpClient(HttpStatusCode.OK, "[]");

        var exception = await Assert.ThrowsAsync<MediaNotFoundException>(
            () => CreateProvider(http).LookupAsync("99999", CancellationToken.None));

        Assert.Equal(404, exception.StatusCode);
        Assert.Contains("game", exception.Detail);
        Assert.Contains("99999", exception.Detail);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("0")]
    [InlineData("-5")]
    [InlineData("+5")]
    [InlineData("19.5")]
    [InlineData(" 42")]
    [InlineData("99999999999")]   // does not fit in an int
    [InlineData("1; fields *;")]  // must never reach the query body
    public async Task MalformedIdIsRejectedWithoutCallingIgdb(string id)
    {
        var called = false;

        using var http = CreateHttpClient((_, _) =>
        {
            called = true;
            return Task.FromResult(JsonResponse(HttpStatusCode.OK, "[]"));
        });

        var exception = await Assert.ThrowsAsync<InvalidRequestException>(
            () => CreateProvider(http).LookupAsync(id, CancellationToken.None));

        Assert.Equal(400, exception.StatusCode);
        Assert.False(called);
    }

    // ---------- Failure conversion (ProviderCallGuard) ----------

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task HttpFailureBecomesProviderUnavailable(bool lookup)
    {
        // 401 is what an expired Twitch token looks like.
        using var http = CreateHttpClient(HttpStatusCode.Unauthorized, "{}");

        var exception = await Assert.ThrowsAsync<ProviderUnavailableException>(
            () => ExecuteAsync(CreateProvider(http), lookup));

        Assert.Equal(IgdbConstants.PROVIDER_NAME, exception.ProviderName);
        var inner = Assert.IsType<HttpRequestException>(exception.InnerException);
        Assert.Equal(HttpStatusCode.Unauthorized, inner.StatusCode);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InvalidJsonBecomesProviderUnavailable(bool lookup)
    {
        using var http = CreateHttpClient(HttpStatusCode.OK, "This is not JSON.");

        var exception = await Assert.ThrowsAsync<ProviderUnavailableException>(
            () => ExecuteAsync(CreateProvider(http), lookup));

        Assert.Equal(IgdbConstants.PROVIDER_NAME, exception.ProviderName);
        Assert.IsType<JsonException>(exception.InnerException);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NetworkFailureBecomesProviderUnavailable(bool lookup)
    {
        var original = new HttpRequestException("The upstream server is unreachable.");

        using var http = CreateHttpClient((_, _) =>
            Task.FromException<HttpResponseMessage>(original));

        var exception = await Assert.ThrowsAsync<ProviderUnavailableException>(
            () => ExecuteAsync(CreateProvider(http), lookup));

        Assert.Same(original, exception.InnerException);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CallerCancellationIsNotConvertedToProviderFailure(bool lookup)
    {
        using var cancellation = new CancellationTokenSource();

        using var http = CreateHttpClient((_, token) =>
        {
            cancellation.Cancel();
            return Task.FromCanceled<HttpResponseMessage>(token);
        });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => ExecuteAsync(CreateProvider(http), lookup, cancellation.Token));
    }

    // ---------- helpers ----------

    private static Task ExecuteAsync(
        IgdbProvider provider,
        bool lookup,
        CancellationToken cancellationToken = default) =>
        lookup
            ? provider.LookupAsync("1942", cancellationToken)
            : provider.SearchAsync("game", 10, cancellationToken);

    private static IgdbProvider CreateProvider(HttpClient http) =>
        new(new IgdbClient(http), Options.Create(new IgdbOptions { ImageBaseUrl = ImageBaseUrl }));

    private static HttpClient CreateHttpClient(HttpStatusCode statusCode, string body) =>
        CreateHttpClient((_, _) => Task.FromResult(JsonResponse(statusCode, body)));

    private static HttpClient CreateHttpClient(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) =>
        new(new StubHttpMessageHandler(handler)) { BaseAddress = BaseUrl };

    private static HttpResponseMessage JsonResponse(HttpStatusCode statusCode, string body) =>
        new(statusCode) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) =>
            handler(request, cancellationToken);
    }
}
