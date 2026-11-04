using Microsoft.EntityFrameworkCore;
using Palette.Api.Contracts;
using Palette.Api.Data;
using Palette.Api.Models;

namespace Palette.Api.Services;

public sealed class CurrentUserService(PaletteDbContext dbContext) : ICurrentUserService
{
    public async Task<SyncCurrentUserResult> SyncAsync(
        string clerkUserId,
        CurrentUserRequest request,
        CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.SingleOrDefaultAsync(
            candidate => candidate.Auth0Id == clerkUserId,
            cancellationToken);

        var emailOwner = await dbContext.Users
            .Where(candidate => candidate.Email == request.Email)
            .Select(candidate => candidate.Auth0Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (emailOwner is not null && emailOwner != clerkUserId)
        {
            return new SyncCurrentUserResult(null, false, true);
        }

        var created = user is null;
        if (user is null)
        {
            user = new User
            {
                Auth0Id = clerkUserId,
                DateJoined = DateTime.UtcNow,
            };
            dbContext.Users.Add(user);
        }

        user.Username = request.Username;
        user.Email = request.Email;
        await dbContext.SaveChangesAsync(cancellationToken);

        return new SyncCurrentUserResult(ToResponse(user), created, false);
    }

    public async Task<UserResponse?> GetAsync(
        string clerkUserId,
        CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.AsNoTracking().SingleOrDefaultAsync(
            candidate => candidate.Auth0Id == clerkUserId,
            cancellationToken);

        return user is null ? null : ToResponse(user);
    }

    private static UserResponse ToResponse(User user) =>
        new(
            user.UserId,
            user.Username,
            user.Email,
            new DateTimeOffset(DateTime.SpecifyKind(user.DateJoined, DateTimeKind.Utc)));
}
