using Microsoft.EntityFrameworkCore;
using Palette.Api.Contracts;
using Palette.Api.Data;
using Palette.Api.Services;

namespace Palette.Tests;

public class CurrentUserServiceTests
{
    [Fact]
    public async Task SyncAsync_CreatesAndReturnsCurrentUser()
    {
        await using var context = CreateContext();
        var service = new CurrentUserService(context);

        var result = await service.SyncAsync(
            "user_123",
            new CurrentUserRequest { Username = "palette-user", Email = "user@example.com" },
            CancellationToken.None);

        Assert.True(result.Created);
        Assert.False(result.EmailInUse);
        Assert.NotNull(result.User);
        Assert.Equal("user@example.com", result.User.Email);

        var storedUser = await service.GetAsync("user_123", CancellationToken.None);
        Assert.Equal(result.User, storedUser);
    }

    [Fact]
    public async Task SyncAsync_UpdatesExistingCurrentUser()
    {
        await using var context = CreateContext();
        var service = new CurrentUserService(context);
        var original = await service.SyncAsync(
            "user_123",
            new CurrentUserRequest { Username = "old-name", Email = "user@example.com" },
            CancellationToken.None);

        var updated = await service.SyncAsync(
            "user_123",
            new CurrentUserRequest { Username = "new-name", Email = "user@example.com" },
            CancellationToken.None);

        Assert.False(updated.Created);
        Assert.Equal(original.User?.UserId, updated.User?.UserId);
        Assert.Equal("new-name", updated.User?.Username);
    }

    [Fact]
    public async Task SyncAsync_RejectsEmailAlreadyLinkedToAnotherUser()
    {
        await using var context = CreateContext();
        var service = new CurrentUserService(context);
        await service.SyncAsync(
            "user_123",
            new CurrentUserRequest { Username = "first-user", Email = "user@example.com" },
            CancellationToken.None);

        var result = await service.SyncAsync(
            "user_456",
            new CurrentUserRequest { Username = "second-user", Email = "user@example.com" },
            CancellationToken.None);

        Assert.True(result.EmailInUse);
        Assert.Null(result.User);
        Assert.Null(await service.GetAsync("user_456", CancellationToken.None));
    }

    private static PaletteDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<PaletteDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new PaletteDbContext(options);
    }
}
