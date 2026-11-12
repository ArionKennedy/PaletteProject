using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Palette.Api.Data;
using Palette.Api.Models;

namespace Backend.Features.Users;

public class UserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly PaletteDbContext _dbContext;

    public UserService(IHttpContextAccessor httpContextAccessor, PaletteDbContext dbContext)
    {
        _httpContextAccessor = httpContextAccessor;
        _dbContext = dbContext;
    }

    public string? GetCurrentClerkId()
    {
        return _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    public string? GetCurrentUsername()
    {
        return _httpContextAccessor.HttpContext?.User?.FindFirstValue("username");
    }

    public string? GetCurrentEmail()
    {
        return _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Email);
    }

    public Task<bool> UserExistsAsync(
        string clerkId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clerkId);

        return _dbContext.Users
            .AnyAsync(user => user.ClerkId == clerkId, cancellationToken);
    }

    public async Task<User> SaveCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        var clerkId = GetCurrentClerkId()
            ?? throw new InvalidOperationException("The current request has no Clerk user ID claim.");
        var username = GetCurrentUsername()
            ?? throw new InvalidOperationException("The current request has no username claim.");
        var email = GetCurrentEmail()
            ?? throw new InvalidOperationException("The current request has no email claim.");

        var user = await _dbContext.Users
            .SingleOrDefaultAsync(user => user.ClerkId == clerkId, cancellationToken);

        if (user is null)
        {
            // create new user
            user = new User
            {
                UserId = Guid.NewGuid(),
                ClerkId = clerkId,
                Username = username,
                Email = email,
                DateJoined = DateTime.UtcNow
            };
            _dbContext.Users.Add(user);
        }
        else
        {
            // update existing user
            user.Username = username;
            user.Email = email;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return user;
    }
}