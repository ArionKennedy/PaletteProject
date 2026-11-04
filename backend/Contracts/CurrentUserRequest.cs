using System.ComponentModel.DataAnnotations;

namespace Palette.Api.Contracts;

/// <summary>Profile details supplied by the authenticated Clerk user.</summary>
public sealed record CurrentUserRequest
{
    [Required, MaxLength(100)]
    public required string Username { get; init; }

    [Required, EmailAddress, MaxLength(320)]
    public required string Email { get; init; }
}
