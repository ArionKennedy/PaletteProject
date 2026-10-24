namespace Backend.Shared.Errors;

/// <summary>
/// Base type for every failure the API reports to its callers on purpose.
/// <see cref="ApiExceptionHandler"/> turns these into ProblemDetails responses.
/// Any exception that is NOT an ApiException is treated as a bug (HTTP 500).
/// </summary>
public abstract class ApiException(
    int statusCode,
    string title,
    string detail,
    Exception? innerException = null) : Exception(detail, innerException)
{
    /// <summary>HTTP status code to send.</summary>
    public int StatusCode { get; } = statusCode;

    /// <summary>Short, human-readable name of the problem.</summary>
    public string Title { get; } = title;

    /// <summary>Client-safe explanation. Never put upstream or internal details here.</summary>
    public string Detail => Message;
}
