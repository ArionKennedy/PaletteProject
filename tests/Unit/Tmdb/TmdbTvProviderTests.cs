using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Backend.Constants;
using Backend.Providers.Tmdb;
using Backend.Shared.Errors;
using Backend.Shared.Models;
using Microsoft.Extensions.Options;
using Xunit;

namespace Palette.Tests.Unit.Tmdb;

public class TmdbTvProviderTests
{
    private const string ImageBaseUrl =
        "https://image.tmdb.org/t/p/w500";

    private const string SearchResponse =
        """
        {
          "page": 1,
          "results": [
            {
              "id": 84,
              "name": "First Series",
              "first_air_date": "2024-03-15",
              "poster_path": "/first.jpg"
            },
            {
              "id": 85,
              "name": "Second Series",
              "first_air_date": "2023-09-20",
              "poster_path": "/second.jpg"
            },
            {
              "id": 86,
              "name": "Third Series",
              "first_air_date": "",
              "poster_path": null
            }
          ],
          "total_pages": 1,
          "total_results": 3
        }
        """;

    private const string LookupResponse =
        """
        {
          "id": 84,
          "name": "Example Series",
          "overview": "A series description.",
          "first_air_date": "2023-09-20",
          "poster_path": "/series.jpg",
          "number_of_seasons": 5
        }
        """;

    [Fact]
    public void MediaTypeIsTv()
    {
        using var httpClient = CreateHttpClient(
            HttpStatusCode.OK,
            SearchResponse);

        var provider = CreateProvider(httpClient);

        Assert.Equal("tv", provider.MediaType);
    }

    [Fact]
    public async Task SearchMapsResultsAndPreservesProviderOrder()
    {
        using var httpClient = CreateHttpClient(
            HttpStatusCode.OK,
            SearchResponse);

        var provider = CreateProvider(httpClient);

        var results = await provider.SearchAsync(
            "Example",
            10,
            CancellationToken.None);

        Assert.Collection(
            results,
            first =>
            {
                Assert.Equal("84", first.ExternalId);
                Assert.Equal("tv", first.MediaType);
                Assert.Equal("First Series", first.Title);
                Assert.Equal(
                    new DateOnly(2024, 3, 15),
                    first.ReleaseDate);
                Assert.Equal(
                    $"{ImageBaseUrl}/first.jpg",
                    first.PosterUrl);
            },
            second =>
            {
                Assert.Equal("85", second.ExternalId);
                Assert.Equal("Second Series", second.Title);
            },
            third =>
            {
                Assert.Equal("86", third.ExternalId);
                Assert.Equal("Third Series", third.Title);
                Assert.Null(third.ReleaseDate);
                Assert.Null(third.PosterUrl);
            });
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    [InlineData(3, 3)]
    [InlineData(10, 3)]
    public async Task SearchReturnsAtMostRequestedNumberOfResults(
        int maxResults,
        int expectedCount)
    {
        using var httpClient = CreateHttpClient(
            HttpStatusCode.OK,
            SearchResponse);

        var provider = CreateProvider(httpClient);

        var results = await provider.SearchAsync(
            "Example",
            maxResults,
            CancellationToken.None);

        Assert.Equal(expectedCount, results.Count);

        if (expectedCount > 0)
        {
            Assert.Equal("84", results[0].ExternalId);
        }
    }

    [Fact]
    public async Task SearchWithNoMatchesReturnsEmptyList()
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

        var provider = CreateProvider(httpClient);

        var results = await provider.SearchAsync(
            "No matches",
            10,
            CancellationToken.None);

        Assert.Empty(results);
    }

    [Fact]
    public async Task LookupReturnsMappedTvDetails()
    {
        using var httpClient = CreateHttpClient(
            HttpStatusCode.OK,
            LookupResponse);

        var provider = CreateProvider(httpClient);

        var result = await provider.LookupAsync(
            "84",
            CancellationToken.None);

        var series = Assert.IsType<TvLookupResult>(result);

        Assert.Equal("84", series.ExternalId);
        Assert.Equal("Example Series", series.Title);
        Assert.Equal(
            new DateOnly(2023, 9, 20),
            series.ReleaseDate);
        Assert.Equal(
            "A series description.",
            series.Description);
        Assert.Equal(
            $"{ImageBaseUrl}/series.jpg",
            series.PosterUrl);
        Assert.Equal(5, series.NumberOfSeasons);
    }

    [Fact]
    public async Task LookupWithMissingOptionalDataReturnsNullFields()
    {
        using var httpClient = CreateHttpClient(
            HttpStatusCode.OK,
            """
            {
              "id": 84,
              "name": "Example Series"
            }
            """);

        var provider = CreateProvider(httpClient);

        var result = await provider.LookupAsync(
            "84",
            CancellationToken.None);

        var series = Assert.IsType<TvLookupResult>(result);

        Assert.Equal("84", series.ExternalId);
        Assert.Equal("Example Series", series.Title);
        Assert.Null(series.ReleaseDate);
        Assert.Null(series.Description);
        Assert.Null(series.PosterUrl);
        Assert.Null(series.NumberOfSeasons);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("2147483648")]
    [InlineData("1.5")]
    [InlineData("+84")]
    [InlineData("")]
    public async Task InvalidTvIdIsRejectedBeforeSendingHttpRequest(
        string externalId)
    {
        var requestCount = 0;

        using var httpClient = CreateHttpClient((_, _) =>
        {
            requestCount++;

            return Task.FromResult(JsonResponse(
                HttpStatusCode.OK,
                LookupResponse));
        });

        var provider = CreateProvider(httpClient);

        await Assert.ThrowsAsync<InvalidRequestException>(
            () => provider.LookupAsync(
                externalId,
                CancellationToken.None));

        Assert.Equal(0, requestCount);
    }

    [Fact]
    public async Task LookupNotFoundBecomesMediaNotFound()
    {
        using var httpClient = CreateHttpClient(
            HttpStatusCode.NotFound,
            "{}");

        var provider = CreateProvider(httpClient);

        var exception = await Assert.ThrowsAsync<MediaNotFoundException>(
            () => provider.LookupAsync(
                "84",
                CancellationToken.None));

        Assert.Equal(404, exception.StatusCode);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task HttpFailureBecomesProviderUnavailable(
        bool lookup)
    {
        using var httpClient = CreateHttpClient(
            HttpStatusCode.InternalServerError,
            """{"status_message":"Upstream failure"}""");

        var provider = CreateProvider(httpClient);

        var exception =
            await Assert.ThrowsAsync<ProviderUnavailableException>(
                () => ExecuteAsync(provider, lookup));

        Assert.Equal(
            TmdbConstants.PROVIDER_NAME,
            exception.ProviderName);

        var inner = Assert.IsType<HttpRequestException>(
            exception.InnerException);

        Assert.Equal(
            HttpStatusCode.InternalServerError,
            inner.StatusCode);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InvalidJsonBecomesProviderUnavailable(
        bool lookup)
    {
        using var httpClient = CreateHttpClient(
            HttpStatusCode.OK,
            "This is not JSON.");

        var provider = CreateProvider(httpClient);

        var exception =
            await Assert.ThrowsAsync<ProviderUnavailableException>(
                () => ExecuteAsync(provider, lookup));

        Assert.Equal(
            TmdbConstants.PROVIDER_NAME,
            exception.ProviderName);

        Assert.IsType<JsonException>(exception.InnerException);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NetworkFailureBecomesProviderUnavailable(
        bool lookup)
    {
        var original = new HttpRequestException(
            "The upstream server is unreachable.");

        using var httpClient = CreateHttpClient((_, _) =>
            Task.FromException<HttpResponseMessage>(original));

        var provider = CreateProvider(httpClient);

        var exception =
            await Assert.ThrowsAsync<ProviderUnavailableException>(
                () => ExecuteAsync(provider, lookup));

        Assert.Equal(
            TmdbConstants.PROVIDER_NAME,
            exception.ProviderName);

        Assert.Same(original, exception.InnerException);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CallerCancellationIsNotConvertedToProviderFailure(
        bool lookup)
    {
        using var cancellation = new CancellationTokenSource();

        using var httpClient = CreateHttpClient((_, token) =>
        {
            cancellation.Cancel();

            return Task.FromCanceled<HttpResponseMessage>(token);
        });

        var provider = CreateProvider(httpClient);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => ExecuteAsync(
                provider,
                lookup,
                cancellation.Token));

        Assert.True(cancellation.IsCancellationRequested);
    }

    private static async Task ExecuteAsync(
        TmdbTvProvider provider,
        bool lookup,
        CancellationToken cancellationToken = default)
    {
        if (lookup)
        {
            await provider.LookupAsync("84", cancellationToken);
        }
        else
        {
            await provider.SearchAsync(
                "Example",
                10,
                cancellationToken);
        }
    }

    private static TmdbTvProvider CreateProvider(
        HttpClient httpClient)
    {
        var client = new TmdbClient(httpClient);

        var options = Options.Create(new TmdbOptions
        {
            ImageBaseUrl = ImageBaseUrl,
        });

        return new TmdbTvProvider(client, options);
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
            BaseAddress = new Uri("https://tmdb.example/3/"),
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
