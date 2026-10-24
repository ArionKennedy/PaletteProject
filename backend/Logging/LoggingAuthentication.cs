using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Palette.Api.Data;
using Palette.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Palette.Api.Logging;

public static class LoggingAuthentication
{
    public const string DemoToken = "palette-local-demo";
    public const string DemoSubject = "development|palette-logging-demo";

    public static IServiceCollection AddLoggingFeature(this IServiceCollection services,
        IConfiguration configuration, IHostEnvironment environment)
    {
        // Temporary local testing only. Replace this registration with the team's real
        // authentication scheme when it is ready. All non-demo requests fail closed.
        services.AddAuthentication("LocalLoggingDemo")
            .AddScheme<AuthenticationSchemeOptions, LocalDemoHandler>("LocalLoggingDemo", _ => { });
        services.AddAuthorization();
        services.AddScoped<ShowLogService>();
        return services;
    }
}

// This handler cannot authenticate production traffic, even if the demo flag is accidentally set.
// No caller-supplied user ID is accepted: local demo requests all use one explicit demo identity.
public sealed class LocalDemoHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger,
    UrlEncoder encoder, IHostEnvironment environment, IConfiguration configuration)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var remoteAddress = Context.Connection.RemoteIpAddress;
        if (!environment.IsDevelopment() || !configuration.GetValue<bool>("LoggingDemo:Enabled")
            || remoteAddress is null || !IPAddress.IsLoopback(remoteAddress)
            || Request.Headers.Authorization != $"Bearer {LoggingAuthentication.DemoToken}")
            return Task.FromResult(AuthenticateResult.NoResult());

        var identity = new ClaimsIdentity(
            [new Claim("sub", LoggingAuthentication.DemoSubject)], Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(
            new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
    }
}

public static class ShowLogEndpoints
{
    public static void MapShowLogs(this WebApplication app)
    {
        var group = app.MapGroup("/api/me/shows").RequireAuthorization();

        group.MapGet("", async (HttpContext context, PaletteDbContext db,
            ShowLogService service, int? page, CancellationToken cancellationToken) =>
        {
            if (page is < 1 or > 10000)
                return Results.BadRequest(new { message = "Page must be between 1 and 10000." });
            var user = await ResolveUser(context, db, cancellationToken);
            if (user is null)
                return Results.NotFound(new { message = "Your Palette profile is not set up yet." });
            return Results.Ok(await service.GetPage(user.UserId, page ?? 1, cancellationToken));
        });

        group.MapPost("", async (LogShowRequest request, HttpContext context,
            PaletteDbContext db, ShowLogService service, CancellationToken cancellationToken) =>
        {
            if (request.TmdbTvId <= 0 || string.IsNullOrWhiteSpace(request.Title)
                || request.Title.Trim().Length > 300)
                return Results.BadRequest(new { message = "Select a show with a valid ID and a title of at most 300 characters." });
            var user = await ResolveUser(context, db, cancellationToken);
            if (user is null)
                return Results.NotFound(new { message = "Your Palette profile is not set up yet." });
            var result = await service.SaveWatched(user.UserId, request, cancellationToken);
            return Results.Ok(result);
        });
    }

    private static async Task<User?> ResolveUser(HttpContext context, PaletteDbContext db,
        CancellationToken cancellationToken)
    {
        var subject = context.User.FindFirstValue("sub");
        if (string.IsNullOrWhiteSpace(subject)) return null;
        if (subject == LoggingAuthentication.DemoSubject
            && context.User.Identity?.AuthenticationType == "LocalLoggingDemo")
        {
            // Seed only the demo profile, never a media catalogue. Parameterized and safe under
            // concurrent initial requests. The model's Auth0Id unique index prevents duplicates.
            var id = Guid.NewGuid();
            var joined = DateTime.UtcNow;
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO "USERS" ("UserId", "Auth0Id", "Username", "Email", "DateJoined")
                VALUES ({id}, {subject}, {"Logging demo"}, {"logging-demo@palette.invalid"}, {joined})
                ON CONFLICT ("Auth0Id") DO NOTHING
                """, cancellationToken);
        }
        return await db.Users.AsNoTracking()
            .SingleOrDefaultAsync(user => user.Auth0Id == subject, cancellationToken);
    }
}
