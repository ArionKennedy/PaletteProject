using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Palette.Api.Data;
using Palette.Api.Models;

namespace Palette.Api.Logging;

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

    }

    private static async Task<User?> ResolveUser(HttpContext context, PaletteDbContext db,
        CancellationToken cancellationToken)
    {
        var subject = context.User.FindFirstValue("sub");
        if (string.IsNullOrWhiteSpace(subject)) return null;
        return await db.Users.AsNoTracking()
            .SingleOrDefaultAsync(user => user.Auth0Id == subject, cancellationToken);
    }
}
