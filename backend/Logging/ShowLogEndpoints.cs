using Backend.Features.Users;
using Microsoft.EntityFrameworkCore;
using Palette.Api.Data;

namespace Palette.Api.Logging;

public sealed record ShowLogQuery(int Page = 1);

public static class ShowLogEndpoints
{
    public static void MapShowLogs(this WebApplication app)
    {
        // POST carries the query in JSON; this endpoint only reads existing reviews.
        app.MapPost("/api/me/shows/query", async (ShowLogQuery request, PaletteDbContext db,
            UserService users, ShowLogService service, CancellationToken cancellationToken) =>
        {
            var clerkId = users.GetCurrentClerkId();
            if (string.IsNullOrWhiteSpace(clerkId))
                return Results.Unauthorized();
            if (request.Page is < 1 or > 10000)
                return Results.BadRequest(new { message = "Page must be between 1 and 10000." });

            var userId = await db.Users.AsNoTracking()
                .Where(user => user.ClerkId == clerkId)
                .Select(user => (Guid?)user.UserId)
                .SingleOrDefaultAsync(cancellationToken);
            if (userId is null)
                return Results.NotFound(new { message = "Your Palette profile is not set up yet." });

            return Results.Ok(await service.GetPage(userId.Value, request.Page, cancellationToken));
        }).RequireAuthorization();
    }
}
