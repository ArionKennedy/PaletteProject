using Backend.Constants;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Shared.Errors;

/// <summary>
/// The single place where exceptions become HTTP responses.
/// Endpoints and services just throw; they never build error responses themselves.
/// </summary>
public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not ApiException apiException)
        {
            // An unexpected bug: return false so the default handler answers with a plain 500.
            return false;
        }

        LogFailure(apiException);

        var problem = new ProblemDetails
        {
            Status = apiException.StatusCode,
            Title = apiException.Title,
            Detail = apiException.Detail,
        };
        problem.Extensions[ApiConstants.TRACE_ID_KEY] = httpContext.TraceIdentifier;

        httpContext.Response.StatusCode = apiException.StatusCode;
        await httpContext.Response.WriteAsJsonAsync(
            problem,
            options: null,
            contentType: ApiConstants.PROBLEM_JSON_CONTENT_TYPE,
            cancellationToken: cancellationToken);

        // Returning true tells the middleware the response is complete.
        return true;
    }

    // Since .NET 10 the middleware no longer logs exceptions that a handler reports as
    // handled, so this handler has to do the logging itself.
    private void LogFailure(ApiException exception)
    {
        if (exception.StatusCode >= StatusCodes.Status500InternalServerError)
        {
            // Keep the whole exception, including the inner one with the upstream details.
            logger.LogError(
                exception,
                "Request failed with {StatusCode}: {Title}",
                exception.StatusCode,
                exception.Title);
        }
        else
        {
            logger.LogInformation(
                "Request rejected with {StatusCode}: {Detail}",
                exception.StatusCode,
                exception.Detail);
        }
    }
}
