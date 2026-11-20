using Backend.Constants;
using Backend.Features.Users;
using Backend.Shared.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Palette.Api.Logging;

namespace Backend.Features.Lookup;

public static class LookupEndpoints
{
    public static IEndpointRouteBuilder MapLookupEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(LookupConstants.LOOKUP_ROUTE, LookupAsync)
            .WithTags("Lookup")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status502BadGateway);

        return app;
    }

    // type and id are bound from the {type} and {id} parts of the route.
    private static async Task<Ok<MediaLookupResult>> LookupAsync(
        string type,
        string id,
        bool? includeLibrary,
        LookupService lookupService,
        LibraryService library,
        UserService users,
        CancellationToken cancellationToken)
    {
        var details = await lookupService.LookupAsync(type, id, cancellationToken);

        if (includeLibrary == true && users.GetCurrentClerkId() is { } clerkId)
        {
            var mediaType = type.Trim().ToLowerInvariant();
            var saved = await library.IsSavedBy(clerkId, mediaType, details.ExternalId, cancellationToken);
            details = details with { Saved = saved };
        }

        return TypedResults.Ok(details);
    }
}