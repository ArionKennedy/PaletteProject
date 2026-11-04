using Palette.Api.Contracts;

namespace Palette.Api.Services;

public interface ICurrentUserService
{
    Task<SyncCurrentUserResult> SyncAsync(
        string clerkUserId,
        CurrentUserRequest request,
        CancellationToken cancellationToken);

    Task<UserResponse?> GetAsync(
        string clerkUserId,
        CancellationToken cancellationToken);
}

public sealed record SyncCurrentUserResult(
    UserResponse? User,
    bool Created,
    bool EmailInUse);
