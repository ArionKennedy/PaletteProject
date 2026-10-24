namespace Backend.Shared.Errors;

/// <summary>An upstream provider failed, timed out or sent garbage (HTTP 502).</summary>
public sealed class ProviderUnavailableException(string providerName, Exception innerException)
    : ApiException(
        StatusCodes.Status502BadGateway,
        "Provider unavailable",
        "The media provider is currently unavailable. Please try again later.",
        innerException)
{
    /// <summary>Which provider failed. It is logged but never sent to the client.</summary>
    public string ProviderName { get; } = providerName;
}
