using System.Net;
using System.Text;
using System.Text.Json;
using Backend.Providers.Igdb;

namespace Palette.Tests.Unit.Igdb;

public class IgdbClientTests
{
    private static readonly Uri BaseUrl = new("https://igdb.example/v4/");

    // ---------- Search ----------

    [Fact]
    public async Task SearchPostsTheQueryBodyToTheGamesEndpointAndParsesResponse()
    {
        HttpMethod? method = null;
        string? path = null;
        string? contentType = null;
        string? body = null;

        using var http = CreateHttpClient(async (request, ct) =>
        {
            method = request.Method;
            path = request.RequestUri!.AbsolutePath;
            contentType = request.Content!.Headers.ContentType?.MediaType;
            body = await request.Content.ReadAsStringAsync(ct);
            return JsonResponse(HttpStatusCode.OK, """
            [
              {
                "id": 1942,
                "name": "The Witcher 3",
                "first_release_date": 1431993600,
                "cover": { "id": 5, "image_id": "co1wyy" }
              }
            ]
            """);
        });

        var games = await new IgdbClient(http).SearchAsync("witcher", 7, CancellationToken.None);

        Assert.Equal(HttpMethod.Post, method);
        Assert.Equal("/v4/games", path);
        Assert.Equal("text/plain", contentType);
        Assert.Equal(
            "search \"witcher\"; fields name,first_release_date,cover.image_id; limit 7;",
            body);

        var game = Assert.Single(games);
        Assert.Equal(1942, game.Id);
        Assert.Equal("The Witcher 3", game.Name);
        Assert.Equal(1431993600L, game.FirstReleaseDate);
        Assert.Equal("co1wyy", game.Cover!.ImageId);
        Assert.Null(game.Summary);
    }

    [Fact]
    public async Task SearchEscapesQuotesAndBackslashesInTheQuery()
    {
        string? body = null;

        using var http = CreateHttpClient(async (request, ct) =>
        {
            body = await request.Content!.ReadAsStringAsync(ct);
            return JsonResponse(HttpStatusCode.OK, "[]");
        });

        await new IgdbClient(http).SearchAsync("say \"hi\" \\ there", 5, CancellationToken.None);

        Assert.StartsWith("""search "say \"hi\" \\ there";""", body);
    }

    [Fact]
    public async Task SearchWithNoMatchesReturnsAnEmptyList()
    {
        using var http = CreateHttpClient(HttpStatusCode.OK, "[]");

        var games = await new IgdbClient(http).SearchAsync("zzzz", 5, CancellationToken.None);

        Assert.Empty(games);
    }

    [Fact]
    public async Task GameWithoutOptionalFieldsDeserializes()
    {
        using var http = CreateHttpClient(HttpStatusCode.OK, """[{ "id": 7, "name": "Bare Game" }]""");

        var game = Assert.Single(
            await new IgdbClient(http).SearchAsync("bare", 5, CancellationToken.None));

        Assert.Null(game.Summary);
        Assert.Null(game.FirstReleaseDate);
        Assert.Null(game.Cover);
    }

    // ---------- Lookup ----------

    [Fact]
    public async Task LookupPostsAWhereClauseForTheIdAndParsesResponse()
    {
        string? path = null;
        string? body = null;

        using var http = CreateHttpClient(async (request, ct) =>
        {
            path = request.RequestUri!.AbsolutePath;
            body = await request.Content!.ReadAsStringAsync(ct);
            return JsonResponse(HttpStatusCode.OK, """
            [{ "id": 1942, "name": "The Witcher 3", "summary": "An RPG." }]
            """);
        });

        var game = await new IgdbClient(http).LookupAsync(1942, CancellationToken.None);

        Assert.Equal("/v4/games", path);
        Assert.Equal(
            "fields name,first_release_date,summary,cover.image_id; where id = 1942;",
            body);

        Assert.NotNull(game);
        Assert.Equal(1942, game.Id);
        Assert.Equal("An RPG.", game.Summary);
    }

    [Fact]
    public async Task LookupReturnsNullWhenIgdbReturnsAnEmptyArray()
    {
        // IGDB answers 200 with [] for an id that does not exist.
        using var http = CreateHttpClient(HttpStatusCode.OK, "[]");

        var game = await new IgdbClient(http).LookupAsync(99999, CancellationToken.None);

        Assert.Null(game);
    }

    // ---------- Failures ----------

    [Theory]
    [InlineData("search", HttpStatusCode.BadRequest)]
    [InlineData("search", HttpStatusCode.Unauthorized)]
    [InlineData("search", HttpStatusCode.TooManyRequests)]
    [InlineData("search", HttpStatusCode.InternalServerError)]
    [InlineData("lookup", HttpStatusCode.Unauthorized)]
    [InlineData("lookup", HttpStatusCode.NotFound)]
    [InlineData("lookup", HttpStatusCode.InternalServerError)]
    public async Task NonSuccessStatusThrowsHttpRequestException(string operation, HttpStatusCode status)
    {
        using var http = CreateHttpClient(status, "{}");
        var client = new IgdbClient(http);

        var error = await Assert.ThrowsAsync<HttpRequestException>(() => operation == "search"
            ? client.SearchAsync("q", 5, CancellationToken.None)
            : client.LookupAsync(1, CancellationToken.None));

        Assert.Equal(status, error.StatusCode);
    }

    [Theory]
    [InlineData("search")]
    [InlineData("lookup")]
    public async Task NullBodyThrowsJsonException(string operation)
    {
        using var http = CreateHttpClient(HttpStatusCode.OK, "null");
        var client = new IgdbClient(http);

        var error = await Assert.ThrowsAsync<JsonException>(() => operation == "search"
            ? client.SearchAsync("q", 5, CancellationToken.None)
            : client.LookupAsync(1, CancellationToken.None));

        Assert.Equal("IGDB returned an empty response body.", error.Message);
    }

    [Theory]
    [InlineData("not json at all")]
    [InlineData("""{ "message": "an object, not an array" }""")]
    public async Task UnexpectedBodyShapeThrowsJsonException(string body)
    {
        using var http = CreateHttpClient(HttpStatusCode.OK, body);

        await Assert.ThrowsAsync<JsonException>(() =>
            new IgdbClient(http).SearchAsync("q", 5, CancellationToken.None));
    }

    [Fact]
    public async Task GameWithANullNameThrowsJsonException()
    {
        using var http = CreateHttpClient(HttpStatusCode.OK, """[{ "id": 1, "name": null }]""");

        await Assert.ThrowsAsync<JsonException>(() =>
            new IgdbClient(http).SearchAsync("q", 5, CancellationToken.None));
    }

    [Fact]
    public async Task SearchPropagatesCancellation()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        using var http = CreateHttpClient((_, ct) =>
        {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(JsonResponse(HttpStatusCode.OK, "[]"));
        });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new IgdbClient(http).SearchAsync("q", 5, cts.Token));
    }

    // ---------- helpers ----------

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
