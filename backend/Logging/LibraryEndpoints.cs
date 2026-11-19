using Backend.Features.Users;
using Microsoft.EntityFrameworkCore;
using Palette.Api.Data;

namespace Palette.Api.Logging;

public static class LibraryEndpoints
{
    public static void MapLibrary(this WebApplication app)
    {
        var library = app.MapGroup("/api/me/library").RequireAuthorization();

        library.MapGet("", (PaletteDbContext db, UserService users, LibraryService service, CancellationToken ct) =>
            ForCurrentUser(db, users, async id => Results.Ok(await service.GetCounts(id, ct)), ct));

        library.MapGet("/previews", (PaletteDbContext db, UserService users, LibraryService service, CancellationToken ct) =>
            ForCurrentUser(db, users, async id => Results.Ok(await service.GetPreviews(id, ct)), ct));

        library.MapGet("/{type}", (string type, int? page, PaletteDbContext db, UserService users,
            LibraryService service, CancellationToken ct) =>
            ForCurrentUser(db, users, async id => Results.Ok(await service.GetPage(id, type, page ?? 1, ct)), ct));

        library.MapGet("/{type}/{externalId}", (string type, string externalId, PaletteDbContext db,
            UserService users, LibraryService service, CancellationToken ct) =>
            ForCurrentUser(db, users, async id => Results.Ok(await service.GetMembership(id, type, externalId, ct)), ct));

        library.MapPut("/{type}/{externalId}", (string type, string externalId, PaletteDbContext db,
            UserService users, LibraryService service, CancellationToken ct) =>
            ForCurrentUser(db, users, async id => Results.Ok(await service.SetMembership(id, type, externalId, true, ct)), ct));

        library.MapDelete("/{type}/{externalId}", (string type, string externalId, PaletteDbContext db,
            UserService users, LibraryService service, CancellationToken ct) =>
            ForCurrentUser(db, users, async id => Results.Ok(await service.SetMembership(id, type, externalId, false, ct)), ct));
    }

    private static async Task<IResult> ForCurrentUser(PaletteDbContext db, UserService users,
        Func<Guid, Task<IResult>> action, CancellationToken cancellationToken)
    {
        var clerkId = users.GetCurrentClerkId();
        if (string.IsNullOrWhiteSpace(clerkId))
            return Results.Unauthorized();

        var userId = await db.Users.AsNoTracking()
            .Where(user => user.ClerkId == clerkId)
            .Select(user => (Guid?)user.UserId)
            .SingleOrDefaultAsync(cancellationToken);

        return userId is { } id
            ? await action(id)
            : Results.NotFound(new { message = "Your Palette profile is not set up yet." });
    }
}
