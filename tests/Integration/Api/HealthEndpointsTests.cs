using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Backend.Features.Health;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Palette.Tests.Integration.Api;

public class HealthEndpointsTests : IAsyncLifetime
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

        // This endpoint reports application liveness.
        // It does not require PostgreSQL or an external TMDB connection.
        _app = builder.Build();

        _app.MapHealthEndpoints();

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

    [Fact]
    public async Task HealthEndpointReturnsOkJsonWithoutExternalServices()
    {
        using var response = await _client.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        Assert.Equal(
            "application/json",
            response.Content.Headers.ContentType?.MediaType);

        var body = await response.Content.ReadAsStringAsync();

        using var document = JsonDocument.Parse(body);

        Assert.Equal(
            JsonValueKind.Object,
            document.RootElement.ValueKind);

        Assert.Equal(
            "ok",
            document.RootElement.GetProperty("status").GetString());
    }
}
