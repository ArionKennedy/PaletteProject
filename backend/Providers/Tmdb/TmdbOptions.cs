using System.ComponentModel.DataAnnotations;
using Backend.Constants;

namespace Backend.Providers.Tmdb;

public sealed class TmdbOptions
{
    [Required, Url, RegularExpression("^.*/$", ErrorMessage = "BaseUrl must end with '/'.")]
    public string BaseUrl { get; init; } = string.Empty;

    [Required, Url]
    public string ImageBaseUrl { get; init; } = string.Empty;


    /// Comes from user-secrets (or an environment variable), never from a committed file.
    [Required]
    public string ReadAccessToken { get; init; } = string.Empty;

    [Range(1, 120)]
    public int TimeoutSeconds { get; init; } = TmdbConstants.DEFAULT_TIMEOUT_SECONDS;
}