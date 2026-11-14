using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Backend.Providers.Tmdb;
using Xunit;

namespace Palette.Tests.Unit.Tmdb;

public class TmdbClientTests
{
    private static readonly Uri BaseUrl =
        new("https://tmdb.example/3/");

    [Fact]
    public async Task MovieSearchUsesEncodedQueryAndParsesResponse()
    {
        HttpRequestMessage? capturedRequest = null;

        using var httpClient = CreateHttpClient((request, _) =>
        {
            capturedRequest = request;

            return Task.FromResult(JsonResponse(
                HttpStatusCode.OK,
                """
                {
                  "page": 1,
                  "results": [
                    {
                      "id": 42,
                      "title": "Example Movie",
                      "release_date": "2024-03-15",
                      "poster_path": "/movie.jpg"
                    }
                  ],
                  "total_pages": 2,
                  "total_results": 21
                }
                """));
        });

        var client = new TmdbClient(httpClient);

        var response = await client.SearchMoviesAsync(
            "Dune & more?",
            CancellationToken.None);

        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Get, capturedRequest.Method);
        Assert.Equal(
            "/3/search/movie",
            capturedRequest.RequestUri!.AbsolutePath);
        Assert.Equal(
            "?query=Dune%20%26%20more%3F",
            capturedRequest.RequestUri.Query);

        Assert.Equal(1, response.Page);
        Assert.Equal(2, response.TotalPages);
        Assert.Equal(21, response.TotalResults);

        var movie = Assert.Single(response.Results);

        Assert.Equal(42, movie.Id);
        Assert.Equal("Example Movie", movie.Title);
        Assert.Equal("2024-03-15", movie.ReleaseDate);
        Assert.Equal("/movie.jpg", movie.PosterPath);
    }

    [Fact]
    public async Task TvSearchUsesEncodedQueryAndParsesResponse()
    {
        HttpRequestMessage? capturedRequest = null;

        using var httpClient = CreateHttpClient((request, _) =>
        {
            capturedRequest = request;

            return Task.FromResult(JsonResponse(
                HttpStatusCode.OK,
                """
                {
                  "page": 1,
                  "results": [
                    {
                      "id": 84,
                      "name": "Example Series",
                      "first_air_date": "2023-09-20",
                      "poster_path": "/series.jpg"
                    }
                  ],
                  "total_pages": 1,
                  "total_results": 1
                }
                """));
        });

        var client = new TmdbClient(httpClient);

        var response = await client.SearchTvAsync(
            "Dark & more?",
            CancellationToken.None);

        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Get, capturedRequest.Method);
        Assert.Equal(
            "/3/search/tv",
            capturedRequest.RequestUri!.AbsolutePath);
        Assert.Equal(
            "?query=Dark%20%26%20more%3F",
            capturedRequest.RequestUri.Query);

        Assert.Equal(1, response.Page);
        Assert.Equal(1, response.TotalPages);
        Assert.Equal(1, response.TotalResults);

        var series = Assert.Single(response.Results);

        Assert.Equal(84, series.Id);
        Assert.Equal("Example Series", series.Name);
        Assert.Equal("2023-09-20", series.FirstAirDate);
        Assert.Equal("/series.jpg", series.PosterPath);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SearchWithNoMatchesReturnsEmptyResults(bool isTv)
    {
        using var httpClient = CreateHttpClient(
            HttpStatusCode.OK,
            """
            {
              "page": 1,
              "results": [],
              "total_pages": 0,
              "total_results": 0
            }
            """);

        var client = new TmdbClient(httpClient);

        if (isTv)
        {
            var response = await client.SearchTvAsync(
                "No matches",
                CancellationToken.None);

            Assert.Empty(response.Results);
            Assert.Equal(0, response.TotalResults);
        }
        else
        {
            var response = await client.SearchMoviesAsync(
                "No matches",
                CancellationToken.None);

            Assert.Empty(response.Results);
            Assert.Equal(0, response.TotalResults);
        }
    }

    [Fact]
    public async Task MovieLookupUsesMovieIdAndParsesDetails()
    {
        HttpRequestMessage? capturedRequest = null;

        using var httpClient = CreateHttpClient((request, _) =>
        {
            capturedRequest = request;

            return Task.FromResult(JsonResponse(
                HttpStatusCode.OK,
                """
                {
                  "id": 42,
                  "title": "Example Movie",
                  "overview": "A movie description.",
                  "release_date": "2024-03-15",
                  "poster_path": "/movie.jpg",
                  "runtime": 120
                }
                """));
        });

        var client = new TmdbClient(httpClient);

        var result = await client.LookupMovieAsync(
            42,
            CancellationToken.None);

        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Get, capturedRequest.Method);
        Assert.Equal(
            "/3/movie/42",
            capturedRequest.RequestUri!.AbsolutePath);
        Assert.Equal(string.Empty, capturedRequest.RequestUri.Query);

        Assert.NotNull(result);
        Assert.Equal(42, result.Id);
        Assert.Equal("Example Movie", result.Title);
        Assert.Equal("A movie description.", result.Overview);
        Assert.Equal("2024-03-15", result.ReleaseDate);
        Assert.Equal("/movie.jpg", result.PosterPath);
        Assert.Equal(120, result.Runtime);
    }

    [Fact]
    public async Task TvLookupUsesTvIdAndParsesDetails()
    {
        HttpRequestMessage? capturedRequest = null;

        using var httpClient = CreateHttpClient((request, _) =>
        {
            capturedRequest = request;

            return Task.FromResult(JsonResponse(
                HttpStatusCode.OK,
                """
                {
                  "id": 84,
                  "name": "Example Series",
                  "overview": "A series description.",
                  "first_air_date": "2023-09-20",
                  "poster_path": "/series.jpg",
                  "number_of_seasons": 5
                }
                """));
        });

        var client = new TmdbClient(httpClient);

        var result = await client.LookupTvAsync(
            84,
            CancellationToken.None);

        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Get, capturedRequest.Method);
        Assert.Equal(
            "/3/tv/84",
            capturedRequest.RequestUri!.AbsolutePath);
        Assert.Equal(string.Empty, capturedRequest.RequestUri.Query);

        Assert.NotNull(result);
        Assert.Equal(84, result.Id);
        Assert.Equal("Example Series", result.Name);
        Assert.Equal("A series description.", result.Overview);
        Assert.Equal("2023-09-20", result.FirstAirDate);
        Assert.Equal("/series.jpg", result.PosterPath);
        Assert.Equal(5, result.NumberOfSeasons);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LookupReturnsNullForNotFoundWithoutParsingBody(bool isTv)
    {
        using var httpClient = CreateHttpClient(
            HttpStatusCode.NotFound,
            "This deliberately is not JSON.");

        var client = new TmdbClient(httpClient);

        if (isTv)
        {
            var result = await client.LookupTvAsync(
                84,
                CancellationToken.None);

            Assert.Null(result);
        }
        else
        {
            var result = await client.LookupMovieAsync(
                42,
                CancellationToken.None);

            Assert.Null(result);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LookupAllowsMissingOptionalFields(bool isTv)
    {
        var body = isTv
            ? """{"id":84,"name":"Example Series"}"""
            : """{"id":42,"title":"Example Movie"}""";

        using var httpClient = CreateHttpClient(
            HttpStatusCode.OK,
            body);

        var client = new TmdbClient(httpClient);

        if (isTv)
        {
            var result = await client.LookupTvAsync(
                84,
                CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal("Example Series", result.Name);
            Assert.Null(result.Overview);
            Assert.Null(result.FirstAirDate);
            Assert.Null(result.PosterPath);
            Assert.Null(result.NumberOfSeasons);
        }
        else
        {
            var result = await client.LookupMovieAsync(
                42,
                CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal("Example Movie", result.Title);
            Assert.Null(result.Overview);
            Assert.Null(result.ReleaseDate);
            Assert.Null(result.PosterPath);
            Assert.Null(result.Runtime);
        }
    }

    [Theory]
    [InlineData("movie-search", 401)]
    [InlineData("movie-search", 500)]
    [InlineData("tv-search", 401)]
    [InlineData("tv-search", 500)]
    [InlineData("movie-lookup", 401)]
    [InlineData("movie-lookup", 500)]
    [InlineData("tv-lookup", 401)]
    [InlineData("tv-lookup", 500)]
    public async Task UnsuccessfulHttpStatusThrowsHttpRequestException(
        string operation,
        int statusCode)
    {
        var status = (HttpStatusCode)statusCode;

        using var httpClient = CreateHttpClient(
            status,
            """{"status_message":"Upstream failure"}""");

        var client = new TmdbClient(httpClient);

        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            () => ExecuteAsync(client, operation));

        Assert.Equal(status, exception.StatusCode);
    }

    [Theory]
    [InlineData("movie-search")]
    [InlineData("tv-search")]
    public async Task SearchNotFoundIsAnHttpFailureRatherThanAnEmptyResult(
        string operation)
    {
        using var httpClient = CreateHttpClient(
            HttpStatusCode.NotFound,
            "{}");

        var client = new TmdbClient(httpClient);

        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            () => ExecuteAsync(client, operation));

        Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
    }

    public static IEnumerable<object[]> InvalidResponseCases()
    {
        string[] operations =
        [
            "movie-search",
            "tv-search",
            "movie-lookup",
            "tv-lookup",
        ];

        string[] bodies =
        [
            "not-json",
            "",
            "null",
            "{}",
        ];

        foreach (var operation in operations)
        {
            foreach (var body in bodies)
            {
                yield return new object[] { operation, body };
            }
        }
    }

    [Theory]
    [MemberData(nameof(InvalidResponseCases))]
    public async Task InvalidOrMissingResponseDataThrowsJsonException(
        string operation,
        string body)
    {
        using var httpClient = CreateHttpClient(
            HttpStatusCode.OK,
            body);

        var client = new TmdbClient(httpClient);

        await Assert.ThrowsAsync<JsonException>(
            () => ExecuteAsync(client, operation));
    }

    [Theory]
    [InlineData("movie-search")]
    [InlineData("tv-search")]
    [InlineData("movie-lookup")]
    [InlineData("tv-lookup")]
    public async Task NetworkFailureIsPropagatedToTheProvider(
        string operation)
    {
        var original = new HttpRequestException(
            "The upstream server could not be reached.");

        using var httpClient = CreateHttpClient((_, _) =>
            Task.FromException<HttpResponseMessage>(original));

        var client = new TmdbClient(httpClient);

        var actual = await Assert.ThrowsAsync<HttpRequestException>(
            () => ExecuteAsync(client, operation));

        Assert.Same(original, actual);
    }

    [Theory]
    [InlineData("movie-search")]
    [InlineData("tv-search")]
    [InlineData("movie-lookup")]
    [InlineData("tv-lookup")]
    public async Task CallerCancellationReachesTheHttpHandler(
        string operation)
    {
        using var cancellation = new CancellationTokenSource();
        var handlerWasCalled = false;
        CancellationToken observedToken = default;

        using var httpClient = CreateHttpClient((_, token) =>
        {
            handlerWasCalled = true;
            observedToken = token;

            // Cancel after the request reaches the handler.
            // This avoids real delays and external requests.
            cancellation.Cancel();

            return Task.FromCanceled<HttpResponseMessage>(token);
        });

        var client = new TmdbClient(httpClient);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => ExecuteAsync(
                client,
                operation,
                cancellation.Token));

        Assert.True(handlerWasCalled);
        Assert.True(observedToken.CanBeCanceled);
        Assert.True(observedToken.IsCancellationRequested);
    }

    private static async Task ExecuteAsync(
        TmdbClient client,
        string operation,
        CancellationToken cancellationToken = default)
    {
        switch (operation)
        {
            case "movie-search":
                await client.SearchMoviesAsync(
                    "Example",
                    cancellationToken);
                break;

            case "tv-search":
                await client.SearchTvAsync(
                    "Example",
                    cancellationToken);
                break;

            case "movie-lookup":
                await client.LookupMovieAsync(
                    42,
                    cancellationToken);
                break;

            case "tv-lookup":
                await client.LookupTvAsync(
                    84,
                    cancellationToken);
                break;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(operation),
                    operation,
                    "Unknown test operation.");
        }
    }

    private static HttpClient CreateHttpClient(
        HttpStatusCode statusCode,
        string body)
    {
        return CreateHttpClient((_, _) =>
            Task.FromResult(JsonResponse(statusCode, body)));
    }

    private static HttpClient CreateHttpClient(
        Func<
            HttpRequestMessage,
            CancellationToken,
            Task<HttpResponseMessage>> handler)
    {
        return new HttpClient(new StubHttpMessageHandler(handler))
        {
            BaseAddress = BaseUrl,
        };
    }

    private static HttpResponseMessage JsonResponse(
        HttpStatusCode statusCode,
        string body)
    {
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(
                body,
                Encoding.UTF8,
                "application/json"),
        };
    }

    private sealed class StubHttpMessageHandler(
        Func<
            HttpRequestMessage,
            CancellationToken,
            Task<HttpResponseMessage>> handler)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return handler(request, cancellationToken);
        }
    }
}