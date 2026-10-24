using Backend.Constants;

namespace Backend.Features.Health;

public static class HealthEndpoints
{
    //we define this static method which accepts the builder app, attaches the endpoints and returns it
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(
            ApiConstants.HEALTH_ROUTE,
            () => Results.Ok(new { status = ApiConstants.HEALTH_STATUS_OK }))
            .WithTags("Health");

        return app;
    }
}