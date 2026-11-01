using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Palette.Api.Contracts;
using Palette.Api.Services;

namespace Palette.Api.Endpoints;

public static class CurrentUserEndpoints
{
    public static IEndpointRouteBuilder MapCurrentUserEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/users/me")
            .RequireAuthorization();

        group.MapPut("/", async Task<Results<Created<UserResponse>, Ok<UserResponse>, Conflict<ProblemDetails>, UnauthorizedHttpResult>> (
            ClaimsPrincipal principal,
            CurrentUserRequest request,
            ICurrentUserService currentUserService,
            CancellationToken cancellationToken) =>
        {
            var clerkUserId = principal.FindFirstValue("sub");
            if (string.IsNullOrWhiteSpace(clerkUserId))
            {
                return TypedResults.Unauthorized();
            }

            var result = await currentUserService.SyncAsync(
                clerkUserId,
                request,
                cancellationToken);

            if (result.EmailInUse)
            {
                return TypedResults.Conflict(new ProblemDetails
                {
                    Title = "Email is already linked to another account",
                    Status = StatusCodes.Status409Conflict,
                });
            }

            if (result.User is null)
            {
                throw new InvalidOperationException("The current user profile could not be synchronized.");
            }

            return result.Created
                ? TypedResults.Created("/api/users/me", result.User)
                : TypedResults.Ok(result.User);
        })
        .WithName("SyncCurrentUser")
        .WithSummary("Create or update the authenticated user's database profile")
        .WithDescription("Uses the verified Clerk token subject as the account identity.")
        .Produces<UserResponse>(StatusCodes.Status201Created)
        .Produces<UserResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .Produces(StatusCodes.Status401Unauthorized);

        group.MapGet("/", async Task<Results<Ok<UserResponse>, NotFound, UnauthorizedHttpResult>> (
            ClaimsPrincipal principal,
            ICurrentUserService currentUserService,
            CancellationToken cancellationToken) =>
        {
            var clerkUserId = principal.FindFirstValue("sub");
            if (string.IsNullOrWhiteSpace(clerkUserId))
            {
                return TypedResults.Unauthorized();
            }

            var user = await currentUserService.GetAsync(clerkUserId, cancellationToken);
            return user is null ? TypedResults.NotFound() : TypedResults.Ok(user);
        })
        .WithName("GetCurrentUser")
        .WithSummary("Get the authenticated user's database profile")
        .Produces<UserResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status401Unauthorized);

        return endpoints;
    }
}
