
using Backend.Features.Users;
using Microsoft.EntityFrameworkCore;
using Palette.Api.Data;

namespace Backend.Features.Recommendations.Endpoints;

public static class RecommendationEndpoints
{
    public static IEndpointRouteBuilder MapRecommendationEndpoints(
        this IEndpointRouteBuilder app)
    {
        app.MapGet(
            "/api/recommendations/movies",
            GetMovieRecommendationsAsync
        )
        .WithTags("Recommendations")
        .RequireAuthorization()
        .Produces<IReadOnlyList<RecommendedMovie>>(
            StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> GetMovieRecommendationsAsync(
        int? limit,
        UserService userService,
        PaletteDbContext dbContext,
        RecommendationOrchestrator orchestrator,
        CancellationToken cancellationToken)
    {
        int requestedLimit = limit ?? 20;

        if (requestedLimit < 1 || requestedLimit > 50)
        {
            return Results.BadRequest(new
            {
                error = "Limit must be between 1 and 50."
            });
        }

        // Retrieve the authenticated Clerk user.
        var clerkId = userService.GetCurrentClerkId();

        if (string.IsNullOrWhiteSpace(clerkId))
        {
            return Results.Unauthorized();
        }

        // Resolve Clerk identity to the local Palette user ID.
        var userId = await dbContext.Users
            .AsNoTracking()
            .Where(user => user.ClerkId == clerkId)
            .Select(user => (Guid?)user.UserId)
            .SingleOrDefaultAsync(cancellationToken);

        if (userId is null)
        {
            return Results.NotFound(new
            {
                error = "Authenticated user was not found."
            });
        }

        // Execute the original recommendation engine
        // through the orchestration service.
        var recommendations =
            await orchestrator.RecommendMoviesAsync(
                userId.Value,
                requestedLimit,
                cancellationToken);

        return Results.Ok(recommendations);
    }
}