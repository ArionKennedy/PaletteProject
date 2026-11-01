namespace Palette.Api.Contracts;

/// <summary>Public profile information for the authenticated user.</summary>
public sealed record UserResponse(
    Guid UserId,
    string Username,
    string Email,
    DateTimeOffset DateJoined);
