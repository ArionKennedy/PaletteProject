using Microsoft.EntityFrameworkCore;
using Palette.Api.Data;

namespace Palette.Api.Logging;

// UserId is Clerk's useAuth().userId, not the database UUID.
public sealed record ShowLogQuery(string? UserId, int Page = 1);

public static class ShowLogEndpoints
{
    public static void MapShowLogs(this WebApplication app)
    {
        // Temporary team contract: shared authentication must verify UserId before release.
        // POST carries the query in JSON; this endpoint only reads existing reviews.
        app.MapPost("/api/me/shows/query", async (ShowLogQuery request, PaletteDbContext db,
            ShowLogService service, CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(request.UserId))
                return Results.BadRequest(new { message = "A Clerk userId is required." });
            if (request.Page is < 1 or > 10000)
                return Results.BadRequest(new { message = "Page must be between 1 and 10000." });

            var userId = await db.Users.AsNoTracking()
                .Where(user => user.Auth0Id == request.UserId)
                .Select(user => (Guid?)user.UserId)
                .SingleOrDefaultAsync(cancellationToken);
            if (userId is null)
                return Results.NotFound(new { message = "Your Palette profile is not set up yet." });

            return Results.Ok(await service.GetPage(userId.Value, request.Page, cancellationToken));
        });
    }
}
