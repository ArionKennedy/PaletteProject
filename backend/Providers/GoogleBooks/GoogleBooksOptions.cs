using Backend.Constants;
using System.ComponentModel.DataAnnotations;

namespace Backend.Providers.GoogleBooks;

public sealed class GoogleBooksOptions
{
    [Required, Url, RegularExpression("^.*/$", ErrorMessage = "BaseUrl must end with '/'.")]
    public string BaseUrl { get; init; } = string.Empty;

    public string? ApiKey { get; init; }

    public int TimeoutSeconds { get; init; } = GoogleBooksConstants.DEFAULT_TIMEOUT_SECONDS;

}