using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Backend.Features.Lookup;
using Backend.Features.Search;
using Backend.Providers;
using Backend.Providers.Tmdb;
using Backend.Shared.Errors;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Palette.Tests.Integration.Api;

public class SearchAndLookupApiTests : IAsyncLifetime
{
    private WebApplication _app = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder(
            new WebApplicationOptions
            {
                EnvironmentName = "Testing",
            });

        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();

        builder.Configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["Tmdb:BaseUrl"] = "https://tmdb.example/3/",
                ["Tmdb:ImageBaseUrl"] =
                    "https://image.tmdb.org/t/p/w500",
                ["Tmdb:ReadAccessToken"] = "test-token",
            });

        // Use the real application registrations.
        builder.Services.AddApiErrorHandling();
        builder.Services.AddMediaProviders();
        builder.Services.AddTmdbProvider(builder.Configuration);
        builder.Services.AddSearchFeature();
        builder.Services.AddLookupFeature();

        // Replace only TMDB's external HTTP transport.
        builder.Services
            .AddHttpClient<TmdbClient>()
            .ConfigurePrimaryHttpMessageHandler(
                () => new StubTmdbHandler());

        _app = builder.Build();

        _app.UseExceptionHandler();
        _app.MapSearchEndpoints();
        _app.MapLookupEndpoints();

        await _app.StartAsync();

        _client = _app.GetTestClient();
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();

        if (_app is not null)
        {
            await _app.DisposeAsync();
        }
    }

    [Theory]
    [InlineData("movie", "42", "Movie 1")]
    [InlineData("tv", "84", "Series 1")]
    public async Task SearchReturnsMappedResults(
        string mediaType,
        string expectedId,
        string expectedTitle)
    {
        using var response = await _client.GetAsync(
            $"/api/search/{mediaType}?q=Example");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            "application/json",
            response.Content.Headers.ContentType?.MediaType);

        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        var results = document.RootElement.GetProperty("results");

        Assert.Equal(JsonValueKind.Array, results.ValueKind);
        Assert.Equal(2, results.GetArrayLength());

        var first = results[0];

        Assert.Equal(
            expectedId,
            first.GetProperty("externalId").GetString());
        Assert.Equal(
            mediaType,
            first.GetProperty("mediaType").GetString());
        Assert.Equal(
            expectedTitle,
            first.GetProperty("title").GetString());
        Assert.Equal(
            "2024-03-15",
            first.GetProperty("releaseDate").GetString());
        Assert.Equal(
            "https://image.tmdb.org/t/p/w500/poster.jpg",
            first.GetProperty("posterUrl").GetString());

        var second = results[1];

        Assert.Equal(
            JsonValueKind.Null,
            second.GetProperty("releaseDate").ValueKind);
        Assert.Equal(
            JsonValueKind.Null,
            second.GetProperty("posterUrl").ValueKind);
    }

    [Theory]
    [InlineData("movie")]
    [InlineData("tv")]
    public async Task SearchWithNoMatchesReturnsAnEmptyArray(
        string mediaType)
    {
        using var response = await _client.GetAsync(
            $"/api/search/{mediaType}?q=empty");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        var results = document.RootElement.GetProperty("results");

        Assert.Equal(JsonValueKind.Array, results.ValueKind);
        Assert.Equal(0, results.GetArrayLength());
    }

    [Theory]
    [InlineData("movie")]
    [InlineData("tv")]
    public async Task SearchLimitsResultsToTenItems(
        string mediaType)
    {
        // The simulated upstream response contains twelve items.
        using var response = await _client.GetAsync(
            $"/api/search/{mediaType}?q=many");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        var results = document.RootElement.GetProperty("results");

        Assert.Equal(10, results.GetArrayLength());

        var ids = results
            .EnumerateArray()
            .Select(item => item.GetProperty("externalId").GetString())
            .ToArray();

        Assert.Equal(10, ids.Distinct().Count());
    }

    [Theory]
    [InlineData("/api/search/movie")]
    [InlineData("/api/search/tv?q=")]
    [InlineData("/api/search/movie?q=%20%20")]
    [InlineData("/api/search/unregistered?q=Example")]
    public async Task InvalidSearchReturnsBadRequest(
        string path)
    {
        await AssertProblemAsync(
            path,
            HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("movie", 200)]
    [InlineData("tv", 200)]
    public async Task SearchAcceptsTwoHundredCharacterQuery(
        string mediaType,
        int length)
    {
        var query = new string('x', length);

        using var response = await _client.GetAsync(
            $"/api/search/{mediaType}?q={query}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("movie")]
    [InlineData("tv")]
    public async Task SearchRejectsQueryLongerThanTwoHundredCharacters(
        string mediaType)
    {
        var query = new string('x', 201);

        await AssertProblemAsync(
            $"/api/search/{mediaType}?q={query}",
            HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData(
        "movie",
        "42",
        "Example Movie",
        "runtime",
        120)]
    [InlineData(
        "tv",
        "84",
        "Example Series",
        "numberOfSeasons",
        5)]
    public async Task LookupIncludesCommonAndMediaSpecificFields(
        string mediaType,
        string id,
        string expectedTitle,
        string specificField,
        int expectedValue)
    {
        using var response = await _client.GetAsync(
            $"/api/lookup/{mediaType}/{id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            "application/json",
            response.Content.Headers.ContentType?.MediaType);

        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        var result = document.RootElement;

        Assert.Equal(
            id,
            result.GetProperty("externalId").GetString());
        Assert.Equal(
            expectedTitle,
            result.GetProperty("title").GetString());
        Assert.Equal(
            "2024-03-15",
            result.GetProperty("releaseDate").GetString());
        Assert.Equal(
            "A description.",
            result.GetProperty("description").GetString());
        Assert.Equal(
            "https://image.tmdb.org/t/p/w500/poster.jpg",
            result.GetProperty("posterUrl").GetString());

        // This checks serialization of the derived lookup result,
        // not just the common MediaLookupResult fields.
        Assert.Equal(
            expectedValue,
            result.GetProperty(specificField).GetInt32());

        var otherField = mediaType == "movie"
            ? "numberOfSeasons"
            : "runtime";

        Assert.False(result.TryGetProperty(otherField, out _));
    }

    [Theory]
    [InlineData("movie", "43", "runtime")]
    [InlineData("tv", "85", "numberOfSeasons")]
    public async Task LookupNormalizesMissingOptionalDataToNull(
        string mediaType,
        string id,
        string specificField)
    {
        using var response = await _client.GetAsync(
            $"/api/lookup/{mediaType}/{id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        var result = document.RootElement;

        Assert.Equal(
            id,
            result.GetProperty("externalId").GetString());
        Assert.Equal(
            JsonValueKind.Null,
            result.GetProperty("releaseDate").ValueKind);
        Assert.Equal(
            JsonValueKind.Null,
            result.GetProperty("description").ValueKind);
        Assert.Equal(
            JsonValueKind.Null,
            result.GetProperty("posterUrl").ValueKind);
        Assert.Equal(
            JsonValueKind.Null,
            result.GetProperty(specificField).ValueKind);
    }

    public static IEnumerable<object[]> InvalidLookupIds()
    {
        string[] mediaTypes = ["movie", "tv"];

        string[] ids =
        [
            "abc",
            "0",
            "-1",
            "2147483648",
        ];

        foreach (var mediaType in mediaTypes)
        {
            foreach (var id in ids)
            {
                yield return new object[] { mediaType, id };
            }
        }
    }

    [Theory]
    [MemberData(nameof(InvalidLookupIds))]
    public async Task InvalidLookupIdReturnsBadRequest(
        string mediaType,
        string id)
    {
        await AssertProblemAsync(
            $"/api/lookup/{mediaType}/{id}",
            HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task LookupWithUnknownMediaTypeReturnsBadRequest()
    {
        await AssertProblemAsync(
            "/api/lookup/unregistered/42",
            HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("movie")]
    [InlineData("tv")]
    public async Task MissingMediaReturnsNotFound(
        string mediaType)
    {
        await AssertProblemAsync(
            $"/api/lookup/{mediaType}/999",
            HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData("/api/search/movie?q=upstream-error")]
    [InlineData("/api/search/tv?q=upstream-error")]
    [InlineData("/api/lookup/movie/500")]
    [InlineData("/api/lookup/tv/500")]
    [InlineData("/api/search/movie?q=invalid-json")]
    [InlineData("/api/search/tv?q=invalid-json")]
    [InlineData("/api/lookup/movie/501")]
    [InlineData("/api/lookup/tv/501")]
    public async Task UpstreamFailureReturnsBadGatewayWithoutLeakingDetails(
        string path)
    {
        using var response = await _client.GetAsync(path);

        Assert.Equal(
            HttpStatusCode.BadGateway,
            response.StatusCode);

        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);

        var body = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain(
            "sensitive-upstream-marker",
            body);

        using var document = JsonDocument.Parse(body);

        var problem = document.RootElement;

        Assert.Equal(
            502,
            problem.GetProperty("status").GetInt32());
        Assert.Equal(
            "Provider unavailable",
            problem.GetProperty("title").GetString());

        Assert.False(string.IsNullOrWhiteSpace(
            problem.GetProperty("detail").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(
            problem.GetProperty("traceId").GetString()));
    }

    private async Task AssertProblemAsync(
        string path,
        HttpStatusCode expectedStatus)
    {
        using var response = await _client.GetAsync(path);

        Assert.Equal(expectedStatus, response.StatusCode);
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);

        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        var problem = document.RootElement;

        Assert.Equal(
            (int)expectedStatus,
            problem.GetProperty("status").GetInt32());

        Assert.False(string.IsNullOrWhiteSpace(
            problem.GetProperty("title").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(
            problem.GetProperty("detail").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(
            problem.GetProperty("traceId").GetString()));
    }

    private sealed class StubTmdbHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var uri = request.RequestUri
                ?? throw new InvalidOperationException(
                    "The HTTP request has no URI.");

            var path = uri.AbsolutePath;

            if (path is "/3/search/movie" or "/3/search/tv")
            {
                var query = Uri.UnescapeDataString(uri.Query);

                if (query == "?query=upstream-error")
                {
                    return Respond(
                        HttpStatusCode.ServiceUnavailable,
                        """
                        {
                          "status_message":
                            "sensitive-upstream-marker"
                        }
                        """);
                }

                if (query == "?query=invalid-json")
                {
                    return Respond(
                        HttpStatusCode.OK,
                        "sensitive-upstream-marker: invalid JSON");
                }

                var isTv = path == "/3/search/tv";

                var count = query switch
                {
                    "?query=empty" => 0,
                    "?query=many" => 12,
                    _ => 2,
                };

                return Respond(
                    HttpStatusCode.OK,
                    CreateSearchBody(isTv, count));
            }

            if (path.StartsWith(
                    "/3/movie/",
                    StringComparison.Ordinal)
                || path.StartsWith(
                    "/3/tv/",
                    StringComparison.Ordinal))
            {
                var isTv = path.StartsWith(
                    "/3/tv/",
                    StringComparison.Ordinal);

                var idText = path[(path.LastIndexOf('/') + 1)..];

                if (idText == "500")
                {
                    return Respond(
                        HttpStatusCode.ServiceUnavailable,
                        """
                        {
                          "status_message":
                            "sensitive-upstream-marker"
                        }
                        """);
                }

                if (idText == "501")
                {
                    return Respond(
                        HttpStatusCode.OK,
                        "sensitive-upstream-marker: invalid JSON");
                }

                if (idText == (isTv ? "84" : "42"))
                {
                    return Respond(
                        HttpStatusCode.OK,
                        CreateLookupBody(
                            isTv,
                            missingOptionalData: false));
                }

                if (idText == (isTv ? "85" : "43"))
                {
                    return Respond(
                        HttpStatusCode.OK,
                        CreateLookupBody(
                            isTv,
                            missingOptionalData: true));
                }

                return Respond(HttpStatusCode.NotFound, "{}");
            }

            throw new InvalidOperationException(
                $"Unexpected upstream request: {uri}");
        }

        private static string CreateSearchBody(
            bool isTv,
            int count)
        {
            var results = Enumerable
                .Range(0, count)
                .Select(index => new Dictionary<string, object?>
                {
                    ["id"] = (isTv ? 84 : 42) + index,
                    [isTv ? "name" : "title"] =
                        $"{(isTv ? "Series" : "Movie")} {index + 1}",
                    [isTv ? "first_air_date" : "release_date"] =
                        index == 0 ? "2024-03-15" : "",
                    ["poster_path"] =
                        index == 0 ? "/poster.jpg" : null,
                })
                .ToArray();

            return JsonSerializer.Serialize(new
            {
                page = 1,
                results,
                total_pages = count == 0 ? 0 : 1,
                total_results = count,
            });
        }

        private static string CreateLookupBody(
            bool isTv,
            bool missingOptionalData)
        {
            var id = isTv ? 84 : 42;

            if (missingOptionalData)
            {
                id++;
            }

            var body = new Dictionary<string, object?>
            {
                ["id"] = id,
                [isTv ? "name" : "title"] =
                    isTv ? "Example Series" : "Example Movie",
                ["overview"] =
                    missingOptionalData ? " " : "A description.",
                [isTv ? "first_air_date" : "release_date"] =
                    missingOptionalData ? "" : "2024-03-15",
                ["poster_path"] =
                    missingOptionalData ? null : "/poster.jpg",
                [isTv ? "number_of_seasons" : "runtime"] =
                    missingOptionalData ? 0 : (isTv ? 5 : 120),
            };

            return JsonSerializer.Serialize(body);
        }

        private static Task<HttpResponseMessage> Respond(
            HttpStatusCode statusCode,
            string body)
        {
            var response = new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(
                    body,
                    Encoding.UTF8,
                    "application/json"),
            };

            return Task.FromResult(response);
        }
    }
}
