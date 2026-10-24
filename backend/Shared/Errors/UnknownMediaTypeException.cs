namespace Backend.Shared.Errors;

/// <summary>No provider is registered for the requested media type (HTTP 400).</summary>
public sealed class UnknownMediaTypeException(string mediaType, IEnumerable<string> supportedTypes)
    : ApiException(
        StatusCodes.Status400BadRequest,
        "Unknown media type",
        $"Media type '{mediaType}' is not supported. " +
        $"Supported: {string.Join(", ", supportedTypes)}.")
{
}
