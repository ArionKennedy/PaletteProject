namespace Backend.Shared.Errors;

public static class ErrorHandlingServiceCollectionExtensions
{
    /// <summary>
    /// Registers ProblemDetails and <see cref="ApiExceptionHandler"/>. The app must also call
    /// app.UseExceptionHandler(), because handlers only run inside that middleware.
    /// </summary>
    public static IServiceCollection AddApiErrorHandling(this IServiceCollection services)
    {
        services.AddProblemDetails();
        services.AddExceptionHandler<ApiExceptionHandler>();

        return services;
    }
}
