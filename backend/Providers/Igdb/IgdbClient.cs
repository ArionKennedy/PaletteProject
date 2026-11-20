using System.Text;
using System.Text.Json;
using Backend.Constants;

namespace Backend.Providers.Igdb;

public sealed class IgdbClient(HttpClient httpClient)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        RespectNullableAnnotations = true
    };

    private const string SearchFields = "name,first_release_date,cover.image_id";
    private const string LookupFields = "name,first_release_date,summary,cover.image_id";

    public async Task<List<IgdbGame>> SearchAsync(
        string query,
        int maxResults,
        CancellationToken cancellationToken)
    {
        var body = $"search \"{EscapeQuery(query)}\"; fields {SearchFields}; limit {maxResults};";
        return await PostAsync(body, cancellationToken);
    }

    public async Task<IgdbGame?> LookupAsync(
        int gameId,
        CancellationToken cancellationToken)
    {
        var body = $"fields {LookupFields}; where id = {gameId};";
        var games = await PostAsync(body, cancellationToken);

        // IGDB answers 200 with [] for an unknown id, so "not found" is an empty list.
        return games.FirstOrDefault();
    }

    private async Task<List<IgdbGame>> PostAsync(string body, CancellationToken cancellationToken)
    {
        // IGDB reads the query (Apicalypse) from a plain-text body, not JSON.
        using var content = new StringContent(body, Encoding.UTF8, "text/plain");
        using var response = await httpClient.PostAsync(IgdbConstants.SEARCH_PATH, content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var games = await response.Content.ReadFromJsonAsync<List<IgdbGame>>(JsonOptions, cancellationToken);

        return games ?? throw new JsonException("IGDB returned an empty response body.");
    }

    // The query sits inside "..." in the request body, so a quote or backslash
    // from the user would break out of the string and produce a bad query.
    private static string EscapeQuery(string query) =>
        query.Replace("\\", "\\\\").Replace("\"", "\\\"");
}
