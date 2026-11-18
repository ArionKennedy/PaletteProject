using System.Security.Claims;
using Backend.Features.Users;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Palette.Api.Data;

namespace Palette.Tests.Unit.Users;

public class UserProvisioningTests
{
    [Fact]
    public async Task MiddlewareContinuesWithoutDatabaseAccessWhenPrincipalHasNoIdentity()
    {
        await using var db = new PaletteDbContext(new DbContextOptionsBuilder<PaletteDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=unused;Username=unused;Timeout=1").Options);
        var context = new DefaultHttpContext { User = new ClaimsPrincipal() };
        Assert.Null(context.User.Identity);
        var nextCalls = 0;
        var middleware = new EnsureUserExistsMiddleware(_ => { nextCalls++; return Task.CompletedTask; });

        await middleware.InvokeAsync(context, new UserService(new HttpContextAccessor { HttpContext = context }, db));

        Assert.Equal(1, nextCalls);
        Assert.Empty(db.ChangeTracker.Entries());
    }

    [Fact]
    public async Task MissingPrincipalReturnsNoClaimsAndCannotProvisionUser()
    {
        await using var db = new PaletteDbContext(new DbContextOptionsBuilder<PaletteDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var accessor = new HttpContextAccessor { HttpContext = new MissingPrincipalHttpContext() };
        var service = new UserService(accessor, db);

        Assert.Null(service.GetCurrentClerkId());
        Assert.Null(service.GetCurrentUsername());
        Assert.Null(service.GetCurrentEmail());
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => service.SaveCurrentUserAsync());
        Assert.Equal("The current request has no Clerk user ID claim.", error.Message);
        Assert.Empty(await db.Users.ToListAsync());
    }

    [Theory]
    [InlineData(false, "anonymous-id")]
    [InlineData(true, null)]
    [InlineData(true, "")]
    [InlineData(true, " ")]
    public async Task MiddlewareSkipsProvisioningWithoutAuthenticatedClerkIdentity(bool authenticated, string? id)
    {
        // An unusable database makes an accidental provisioning query fail.
        await using var db = new PaletteDbContext(new DbContextOptionsBuilder<PaletteDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=unused;Username=unused;Timeout=1").Options);
        var identity = new ClaimsIdentity(authenticated ? "test" : null);
        if (id is not null) identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, id));
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        var nextCalls = 0;
        var middleware = new EnsureUserExistsMiddleware(_ => { nextCalls++; return Task.CompletedTask; });
        await middleware.InvokeAsync(context, new UserService(new HttpContextAccessor { HttpContext = context }, db));
        Assert.Equal(1, nextCalls);
        Assert.Empty(db.ChangeTracker.Entries());
    }

    [Theory]
    [InlineData(ClaimTypes.NameIdentifier, "Clerk user ID")]
    [InlineData("username", "username")]
    [InlineData(ClaimTypes.Email, "email")]
    public async Task SaveRejectsMissingRequiredClaimBeforeWriting(string omitted, string expectedMessage)
    {
        await using var db = new PaletteDbContext(new DbContextOptionsBuilder<PaletteDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        Claim[] claims = [new(ClaimTypes.NameIdentifier, "user-id"), new("username", "name"), new(ClaimTypes.Email, "user@example.test")];
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims.Where(c => c.Type != omitted), "test")) };
        var service = new UserService(new HttpContextAccessor { HttpContext = context }, db);
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => service.SaveCurrentUserAsync());
        Assert.Contains(expectedMessage, error.Message);
        Assert.Empty(await db.Users.ToListAsync());
    }

    [Theory]
    [InlineData("username")]
    [InlineData(ClaimTypes.Email)]
    public async Task MiddlewareDoesNotContinueWhenProvisioningFails(string omitted)
    {
        await using var db = new PaletteDbContext(new DbContextOptionsBuilder<PaletteDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        Claim[] claims = [new(ClaimTypes.NameIdentifier, "new-user"), new("username", "name"), new(ClaimTypes.Email, "user@example.test")];
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims.Where(c => c.Type != omitted), "test")) };
        var nextCalled = false;
        var middleware = new EnsureUserExistsMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });
        var service = new UserService(new HttpContextAccessor { HttpContext = context }, db);
        await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.InvokeAsync(context, service));
        Assert.False(nextCalled);
        Assert.Empty(await db.Users.ToListAsync());
    }

    [Fact]
    public async Task MissingRequestReturnsNoClaimsAndInvalidExistenceArgumentsAreRejected()
    {
        await using var db = new PaletteDbContext(new DbContextOptionsBuilder<PaletteDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var service = new UserService(new HttpContextAccessor(), db);
        Assert.Null(service.GetCurrentClerkId());
        Assert.Null(service.GetCurrentUsername());
        Assert.Null(service.GetCurrentEmail());
        foreach (var id in new[] { null, "", " " })
            await Assert.ThrowsAnyAsync<ArgumentException>(() => service.UserExistsAsync(id!));
    }

    // DefaultHttpContext replaces a null User with an empty principal. A custom
    // context is needed to exercise UserService's explicit null-principal guards.
    private sealed class MissingPrincipalHttpContext : HttpContext
    {
        private readonly DefaultHttpContext _inner = new();
        public override ClaimsPrincipal User { get => null!; set => throw new NotSupportedException(); }
        public override Microsoft.AspNetCore.Http.Features.IFeatureCollection Features => _inner.Features;
        public override HttpRequest Request => _inner.Request;
        public override HttpResponse Response => _inner.Response;
        public override ConnectionInfo Connection => _inner.Connection;
        public override WebSocketManager WebSockets => _inner.WebSockets;
        public override IDictionary<object, object?> Items { get => _inner.Items; set => _inner.Items = value; }
        public override IServiceProvider RequestServices { get => _inner.RequestServices; set => _inner.RequestServices = value; }
        public override CancellationToken RequestAborted { get => _inner.RequestAborted; set => _inner.RequestAborted = value; }
        public override string TraceIdentifier { get => _inner.TraceIdentifier; set => _inner.TraceIdentifier = value; }
        public override ISession Session { get => _inner.Session; set => _inner.Session = value; }
        public override void Abort() => _inner.Abort();
    }
}
