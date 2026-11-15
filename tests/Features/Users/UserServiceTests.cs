using System.Security.Claims;
using Backend.Features.Users;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Palette.Api.Data;
using Palette.Api.Models;

namespace Palette.Tests;

public class UserServiceTests
{
    [Fact]
    public async Task InvokeAsync_WhenInsertLosesClerkIdRace_ReusesExistingUserAndContinuesRequest()
    {
        var databaseName = Guid.NewGuid().ToString();
        var databaseRoot = new InMemoryDatabaseRoot();
        var interceptor = new DuplicateClerkIdSaveInterceptor(databaseName, databaseRoot);
        var options = new DbContextOptionsBuilder<PaletteDbContext>()
            .UseInMemoryDatabase(databaseName, databaseRoot)
            .AddInterceptors(interceptor)
            .Options;

        await using var context = new PaletteDbContext(options);
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, "user_concurrent"),
                new Claim(ClaimTypes.Email, "concurrent@example.com"),
                new Claim("username", "concurrent-user")
            ], "test"))
        };
        var userService = new UserService(new HttpContextAccessor { HttpContext = httpContext }, context);
        var endpointReached = false;
        var middleware = new EnsureUserExistsMiddleware(_ =>
        {
            endpointReached = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(httpContext, userService);

        Assert.True(interceptor.DuplicateConflictSimulated);
        Assert.True(endpointReached);
        Assert.Equal(1, await context.Users.CountAsync());
    }

    private sealed class DuplicateClerkIdSaveInterceptor(
        string databaseName,
        InMemoryDatabaseRoot databaseRoot) : SaveChangesInterceptor
    {
        public bool DuplicateConflictSimulated { get; private set; }

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            var options = new DbContextOptionsBuilder<PaletteDbContext>()
                .UseInMemoryDatabase(databaseName, databaseRoot)
                .Options;
            await using var competingContext = new PaletteDbContext(options);
            competingContext.Users.Add(new User
            {
                UserId = Guid.NewGuid(),
                ClerkId = "user_concurrent",
                Username = "concurrent-user",
                Email = "concurrent@example.com",
                DateJoined = DateTime.UtcNow
            });
            await competingContext.SaveChangesAsync(cancellationToken);

            DuplicateConflictSimulated = true;
            throw new DbUpdateException(
                "A user with this ClerkId already exists.",
                new InvalidOperationException("Unique ClerkId constraint violation."));
        }
    }
}
