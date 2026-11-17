using System.Net;
using System.Text;
using System.Text.Json;
using Backend.Providers.GoogleBooks;

namespace Palette.Tests.Unit.GoogleBooks;

public class GoogleBooksClientTests
{
    private static readonly Uri BaseUrl = new("https://books.example/books/v1/");

    // All the paths for GoogleBooksClient

    [Fact]
    public async Task SearchUsesEscapedQueryAndMaxResultsAndParsesResponse()
    {
        HttpRequestMessage? captured = null;

        using var http = CreateHttpClient((request, _) =>
        {
            captured = request;
            return Task.FromResult(JsonResponse(HttpStatusCode.OK, """
            {
              "items": [
                {
                  "id": "abc123",
                  "volumeInfo": {
                    "title": "Dune",
                    "publishedDate": "1965",
                    "imageLinks": { "thumbnail": "http://img/dune.jpg" }
                  }
                }
              ]
            }
            """));
        });

        var response = await new GoogleBooksClient(http)
            .SearchAsync("Dune & more?", 7, CancellationToken.None);

        Assert.NotNull(captured);
        Assert.Equal(HttpMethod.Get, captured.Method);
        Assert.Equal("/books/v1/volumes", captured.RequestUri!.AbsolutePath);
        Assert.Equal("?q=Dune%20%26%20more%3F&maxResults=7", captured.RequestUri.Query);

        var volume = Assert.Single(response.Items!);
        Assert.Equal("abc123", volume.Id);
        Assert.Equal("Dune", volume.VolumeInfo.Title);
        Assert.Equal("1965", volume.VolumeInfo.PublishedDate);
        Assert.Equal("http://img/dune.jpg", volume.VolumeInfo.ImageLinks!.Thumbnail);
    }

    [Fact]
    public async Task SearchWithNoMatchesReturnsNullItems()
    {
        using var http = CreateHttpClient(HttpStatusCode.OK, """{ "totalItems": 0 }""");

        var response = await new GoogleBooksClient(http)
            .SearchAsync("zzzz", 5, CancellationToken.None);

        Assert.Null(response.Items);
    }

    [Fact]
    public async Task LookupUsesEscapedVolumeIdAndParsesResponse()
    {
        HttpRequestMessage? captured = null;

        using var http = CreateHttpClient((request, _) =>
        {
            captured = request;
            return Task.FromResult(JsonResponse(HttpStatusCode.OK, """
            {
              "id": "a/b c",
              "volumeInfo": {
                "title": "Example Book",
                "description": "<p>Hi</p>",
                "industryIdentifiers": [
                  { "type": "ISBN_13", "identifier": "9780000000002" }
                ]
              }
            }
            """));
        });

        var volume = await new GoogleBooksClient(http)
            .LookupAsync("a/b c", CancellationToken.None);

        Assert.NotNull(captured);
        Assert.Equal(HttpMethod.Get, captured.Method);
        Assert.Equal("/books/v1/volumes/a%2Fb%20c", captured.RequestUri!.AbsolutePath);

        Assert.NotNull(volume);
        Assert.Equal("Example Book", volume.VolumeInfo.Title);
        Assert.Equal("<p>Hi</p>", volume.VolumeInfo.Description);
        var id = Assert.Single(volume.VolumeInfo.IndustryIdentifiers!);
        Assert.Equal("ISBN_13", id.Type);
    }

    [Fact]
    public async Task LookupReturnsNullWhenServiceUnavailable()
    {
        using var http = CreateHttpClient(HttpStatusCode.ServiceUnavailable, "");

        var volume = await new GoogleBooksClient(http)
            .LookupAsync("bad-id", CancellationToken.None);

        Assert.Null(volume);
    }

    [Theory]
    [InlineData("search", HttpStatusCode.BadRequest)]
    [InlineData("search", HttpStatusCode.InternalServerError)]
    [InlineData("search", HttpStatusCode.ServiceUnavailable)]
    [InlineData("lookup", HttpStatusCode.NotFound)]
    [InlineData("lookup", HttpStatusCode.InternalServerError)]
    public async Task NonSuccessStatusThrowsHttpRequestException(string operation, HttpStatusCode status)
    {
        using var http = CreateHttpClient(status, "{}");
        var client = new GoogleBooksClient(http);

        var error = await Assert.ThrowsAsync<HttpRequestException>(() => operation == "search"
            ? client.SearchAsync("q", 5, CancellationToken.None)
            : client.LookupAsync("id", CancellationToken.None));

        Assert.Equal(status, error.StatusCode);
    }

    [Theory]
    [InlineData("search")]
    [InlineData("lookup")]
    public async Task NullBodyThrowsJsonException(string operation)
    {
        using var http = CreateHttpClient(HttpStatusCode.OK, "null");
        var client = new GoogleBooksClient(http);

        var error = await Assert.ThrowsAsync<JsonException>(() => operation == "search"
            ? client.SearchAsync("q", 5, CancellationToken.None)
            : client.LookupAsync("id", CancellationToken.None));

        Assert.Equal("GoogleBooks returned an empty response body.", error.Message);
    }

    [Theory]
    [InlineData("""{ "volumeInfo": { "title": "No Id" } }""")]
    [InlineData("""{ "id": "x" }""")]
    [InlineData("""{ "id": "x", "volumeInfo": { } }""")]
    public async Task LookupWithMissingRequiredFieldsThrowsJsonException(string body)
    {
        using var http = CreateHttpClient(HttpStatusCode.OK, body);

        await Assert.ThrowsAsync<JsonException>(() =>
            new GoogleBooksClient(http).LookupAsync("x", CancellationToken.None));
    }

    [Fact]
    public async Task SearchPropagatesCancellation()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        using var http = CreateHttpClient((_, ct) =>
        {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(JsonResponse(HttpStatusCode.OK, "{}"));
        });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new GoogleBooksClient(http).SearchAsync("q", 5, cts.Token));
    }


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
