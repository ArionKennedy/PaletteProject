namespace Backend.Shared.Errors;

/// <summary>The caller sent something we cannot act on (HTTP 400).</summary>
public sealed class InvalidRequestException(string detail)
    : ApiException(StatusCodes.Status400BadRequest, "Invalid request", detail)
{
}
