using Backend.Constants;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Backend.Features.Search;

public static class SearchEndpoints
{
    public static IEndpointRouteBuilder MapSearchEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(SearchConstants.SEARCH_ROUTE, SearchAsync)
            .WithTags("Search")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status502BadGateway);

        return app;
    }

    // type is bound from the {type} part of the route, q from the query string;
    // the service is supplied by dependency injection.
    private static async Task<Ok<SearchResponse>> SearchAsync(
        string type,
        string? q,
        SearchService searchService,
        CancellationToken cancellationToken)
    {
        var response = await searchService.SearchAsync(type, q, cancellationToken);

        return TypedResults.Ok(response);
    }
}
