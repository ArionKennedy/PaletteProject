namespace Backend.Shared.Errors;

/// <summary>The provider has no item with the requested id (HTTP 404).</summary>
public sealed class MediaNotFoundException(string mediaType, string externalId)
    : ApiException(
        StatusCodes.Status404NotFound,
        "Media not found",
        $"No {mediaType} found with id '{externalId}'.")
{
}
