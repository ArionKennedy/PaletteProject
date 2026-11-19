using Backend.Features.Recommendations.Data;
using Backend.Features.Users;
using Microsoft.EntityFrameworkCore;
using Palette.Api.Data;

namespace Backend.Features.Recommendations.Endpoints;

public static class SearchInteractionEndpoints
{
    public sealed record SearchSelectionRequest(
        string MediaType,
        string ExternalId
    );

    public static IEndpointRouteBuilder MapSearchInteractionEndpoints(
        this IEndpointRouteBuilder app)
    {
        app.MapPost(
            "/api/recommendations/interactions/selection",
            RecordSelectionAsync
        )
        .WithTags("Recommendations")
        .RequireAuthorization()
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> RecordSelectionAsync(
        SearchSelectionRequest request,
        UserService userService,
        PaletteDbContext dbContext,
        SearchInteractionService interactionService,
        CancellationToken cancellationToken)
    {
        var clerkId = userService.GetCurrentClerkId();

        if (string.IsNullOrWhiteSpace(clerkId))
        {
            return Results.Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.MediaType) ||
            string.IsNullOrWhiteSpace(request.ExternalId))
        {
            return Results.BadRequest(
                new { error = "Media type and external ID are required." }
            );
        }

        var mediaType = request.MediaType.Trim().ToLowerInvariant();

        if (mediaType is not ("movie" or "tv" or "book" or "game"))
        {
            return Results.BadRequest(
                new { error = "Unsupported media type." }
            );
        }

        var userId = await dbContext.Users
            .Where(user => user.ClerkId == clerkId)
            .Select(user => (Guid?)user.UserId)
            .SingleOrDefaultAsync(cancellationToken);

        if (userId is null)
        {
            return Results.NotFound(
                new { error = "Authenticated user was not found." }
            );
        }

        await interactionService.RecordSelectionAsync(
            userId.Value,
            mediaType,
            request.ExternalId,
            cancellationToken
        );

        return Results.NoContent();
    }
}