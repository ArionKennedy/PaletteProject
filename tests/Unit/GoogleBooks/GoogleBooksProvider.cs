using System.Net;
using System.Text;
using System.Text.Json;
using Backend.Constants;
using Backend.Providers.GoogleBooks;
using Backend.Shared.Errors;
using Backend.Shared.Models;

namespace Palette.Tests.Unit.GoogleBooks;

public class GoogleBooksProviderTests
{
    private static readonly Uri BaseUrl = new("https://books.example/books/v1/");

    private const string SearchResponse = """
        {
          "items": [
            {
              "id": "id-1",
              "volumeInfo": {
                "title": "First Book",
                "publishedDate": "2024-03-15",
                "imageLinks": { "thumbnail": "http://img.example/first.jpg" }
              }
            },
            {
              "id": "id-2",
              "volumeInfo": { "title": "Second Book", "publishedDate": "2023" }
            }
          ]
        }
        """;

    private const string LookupResponse = """
        {
          "id": "id-1",
          "volumeInfo": {
            "title": "Example Book",
            "publishedDate": "2024-03-15",
            "description": "<p>A book &amp; its story.</p>",
            "imageLinks": { "thumbnail": "http://img.example/book.jpg" },
            "industryIdentifiers": [
              { "type": "ISBN_10", "identifier": "0441013597" },
              { "type": "ISBN_13", "identifier": "9780441013593" }
            ]
          }
        }
        """;

    // ---------- MediaType ----------

    [Fact]
    public void MediaTypeIsBook()
    {
        using var http = CreateHttpClient(HttpStatusCode.OK, "{}");

        Assert.Equal("book", CreateProvider(http).MediaType);
    }

    // ---------- Search ----------

    [Fact]
    public async Task SearchMapsResultsAndPreservesProviderOrder()
    {
        using var http = CreateHttpClient(HttpStatusCode.OK, SearchResponse);

        var results = await CreateProvider(http).SearchAsync("book", 10, CancellationToken.None);

        Assert.Collection(
            results,
            first =>
            {
                Assert.Equal("id-1", first.ExternalId);
                Assert.Equal("book", first.MediaType);
                Assert.Equal("First Book", first.Title);
                Assert.Equal(new DateOnly(2024, 3, 15), first.ReleaseDate);
                Assert.Equal("https://img.example/first.jpg", first.PosterUrl);
            },
            second =>
            {
                Assert.Equal("id-2", second.ExternalId);
                Assert.Equal("Second Book", second.Title);
                Assert.Equal(new DateOnly(2023, 1, 1), second.ReleaseDate);
                Assert.Null(second.PosterUrl);
            });
    }

    [Fact]
    public async Task SearchForwardsQueryAndMaxResultsToTheClient()
    {
        HttpRequestMessage? captured = null;

        using var http = CreateHttpClient((request, _) =>
        {
            captured = request;
            return Task.FromResult(JsonResponse(HttpStatusCode.OK, SearchResponse));
        });

        await CreateProvider(http).SearchAsync("dune", 7, CancellationToken.None);

        Assert.NotNull(captured);
        Assert.Equal("?q=dune&maxResults=7", captured.RequestUri!.Query);
    }

    [Fact]
    public async Task SearchWithNoItemsReturnsEmptyList()
    {
        using var http = CreateHttpClient(HttpStatusCode.OK, """{ "totalItems": 0 }""");

        var results = await CreateProvider(http).SearchAsync("nothing", 10, CancellationToken.None);

        Assert.Empty(results);
    }

    // ---------- Lookup ----------

    [Fact]
    public async Task LookupReturnsMappedBookDetails()
    {
        using var http = CreateHttpClient(HttpStatusCode.OK, LookupResponse);

        var result = await CreateProvider(http).LookupAsync("id-1", CancellationToken.None);

        var book = Assert.IsType<BookLookupResult>(result);
        Assert.Equal("id-1", book.ExternalId);
        Assert.Equal("Example Book", book.Title);
        Assert.Equal(new DateOnly(2024, 3, 15), book.ReleaseDate);
        Assert.Equal("A book & its story.", book.Description);
        Assert.Equal("https://img.example/book.jpg", book.PosterUrl);
        Assert.Equal("9780441013593", book.ISBN);
    }

    [Fact]
    public async Task LookupOfUnknownVolumeBecomesMediaNotFound()
    {
        // Google Books answers 503 for unknown or malformed ids; the client maps that to null.
        using var http = CreateHttpClient(HttpStatusCode.ServiceUnavailable, "");

        var exception = await Assert.ThrowsAsync<MediaNotFoundException>(
            () => CreateProvider(http).LookupAsync("missing", CancellationToken.None));

        Assert.Equal(404, exception.StatusCode);
        Assert.Contains("book", exception.Detail);
        Assert.Contains("missing", exception.Detail);
    }

    // ---------- Failure conversion (ProviderCallGuard) ----------

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task HttpFailureBecomesProviderUnavailable(bool lookup)
    {
        using var http = CreateHttpClient(HttpStatusCode.InternalServerError, "{}");

        var exception = await Assert.ThrowsAsync<ProviderUnavailableException>(
            () => ExecuteAsync(CreateProvider(http), lookup));

        Assert.Equal(GoogleBooksConstants.PROVIDER_NAME, exception.ProviderName);
        var inner = Assert.IsType<HttpRequestException>(exception.InnerException);
        Assert.Equal(HttpStatusCode.InternalServerError, inner.StatusCode);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InvalidJsonBecomesProviderUnavailable(bool lookup)
    {
        using var http = CreateHttpClient(HttpStatusCode.OK, "This is not JSON.");

        var exception = await Assert.ThrowsAsync<ProviderUnavailableException>(
            () => ExecuteAsync(CreateProvider(http), lookup));

        Assert.Equal(GoogleBooksConstants.PROVIDER_NAME, exception.ProviderName);
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
        GoogleBooksProvider provider,
        bool lookup,
        CancellationToken cancellationToken = default) =>
        lookup
            ? provider.LookupAsync("id-1", cancellationToken)
            : provider.SearchAsync("book", 10, cancellationToken);

    private static GoogleBooksProvider CreateProvider(HttpClient http) =>
        new(new GoogleBooksClient(http));

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
