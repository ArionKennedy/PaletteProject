

using System.Net;
using System.Text.Json;
using Backend.Constants;

namespace Backend.Providers.GoogleBooks;

public sealed class GoogleBooksClient(HttpClient httpClient)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        RespectNullableAnnotations = true
    };

    public async Task<GoogleBooksSearchResponse> SearchAsync(
        string query,
        int maxResults,
        CancellationToken cancellationToken)
    {
        var path = $"{GoogleBooksConstants.SEARCH_PATH}" + $"?q={Uri.EscapeDataString(query)}&maxResults={maxResults}";
        using var response = await httpClient.GetAsync(path, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await ReadBodyAsync<GoogleBooksSearchResponse>(response, cancellationToken);
    }

    public async Task<GoogleBooksVolume?> LookupAsync(
        string volumeId,
        CancellationToken cancellationToken)
    {
        var path = $"{GoogleBooksConstants.VOLUME_PATH}/{Uri.EscapeDataString(volumeId)}";
        using var response = await httpClient.GetAsync(path, cancellationToken);

        if (response.StatusCode == HttpStatusCode.ServiceUnavailable)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await ReadBodyAsync<GoogleBooksVolume>(response, cancellationToken);
    }

    private static async Task<T> ReadBodyAsync<T>(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
        where T : class
    {
        var body = await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);

        return body ?? throw new JsonException("TMDB returned an empty response body.");
    }
}