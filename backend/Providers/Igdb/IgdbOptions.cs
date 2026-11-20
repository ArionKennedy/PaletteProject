using Backend.Constants;
using System.ComponentModel.DataAnnotations;

namespace Backend.Providers.Igdb;

public sealed class IgdbOptions
{
    [Required, Url, RegularExpression("^.*/$", ErrorMessage = "BaseUrl must end with '/'.")]
    public string BaseUrl { get; init; } = string.Empty;

    [Required, Url, RegularExpression("^.*/$", ErrorMessage = "ImageBaseUrl must end with '/'.")]
    public string ImageBaseUrl { get; init; } = string.Empty;

    [Required]
    public string ClientId { get; init; } = string.Empty;


    // Fixed Twitch app token. It expires about 60 days after it's issued and isn't refreshed,
    // so IGDB calls start failing with 401 after that. Regenerate it and update the secret. Should be done manually.
    [Required]
    public string AccessToken { get; init; } = string.Empty;

    [Range(1, 120)]
    public int TimeoutSeconds { get; init; } = IgdbConstants.DEFAULT_TIMEOUT_SECONDS;

}