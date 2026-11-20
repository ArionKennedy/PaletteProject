using System.Security.Claims;
using Backend.Features.Users;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;
using Palette.Api.Data;
using Palette.Api.Models;
using Palette.Tests.Infrastructure;

namespace Palette.Tests.Integration.Database;

[Collection("Postgres Integration Tests")]
public class UserProvisioningDatabaseTests(PostgresFixture fixture)
{
    private static UserService Service(PaletteDbContext db, string id, string username = "test-user", string? email = null)
    {
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
            [new(ClaimTypes.NameIdentifier, id), new("username", username), new(ClaimTypes.Email, email ?? $"{id}@example.test")], "test"))
        };
        return new UserService(new HttpContextAccessor { HttpContext = context }, db);
    }

    [Fact]
    public async Task SaveCreatesThenUpdatesProfileWithoutChangingIdentityOrJoinDate()
    {
        await using var db = fixture.CreateContext();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var id = $"service-{Guid.NewGuid():N}";
        var service = Service(db, id);
        Assert.False(await service.UserExistsAsync(id));
        var before = DateTime.UtcNow.AddSeconds(-1);
        var created = await service.SaveCurrentUserAsync();
        Assert.NotEqual(Guid.Empty, created.UserId);
        Assert.Equal(id, created.ClerkId);
        Assert.Equal("test-user", created.Username);
        Assert.Equal($"{id}@example.test", created.Email);
        Assert.InRange(created.DateJoined, before, DateTime.UtcNow);
        Assert.True(await service.UserExistsAsync(id));
        var userId = created.UserId;
        db.ChangeTracker.Clear();
        var joined = (await db.Users.SingleAsync(u => u.ClerkId == id)).DateJoined;
        var updated = await Service(db, id, "updated", $"updated-{id}@example.test").SaveCurrentUserAsync();
        Assert.Equal(userId, updated.UserId);
        db.ChangeTracker.Clear();
        var saved = await db.Users.SingleAsync(u => u.ClerkId == id);
        Assert.Equal("updated", saved.Username);
        Assert.Equal($"updated-{id}@example.test", saved.Email);
        Assert.Equal(joined, saved.DateJoined);
        Assert.Equal(1, await db.Users.CountAsync(u => u.ClerkId == id));
    }

    [Fact]
    public async Task CancelledOperationsDoNotProvisionUser()
    {
        await using var db = fixture.CreateContext();
        var id = $"cancelled-{Guid.NewGuid():N}";
        var service = Service(db, id);
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.UserExistsAsync(id, cancelled.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.SaveCurrentUserAsync(cancelled.Token));
        Assert.False(await db.Users.AnyAsync(u => u.ClerkId == id));
    }

    [Fact]
    public async Task SaveDoesNotSwallowUnrelatedUniqueEmailViolation()
    {
        var firstId = $"email-owner-{Guid.NewGuid():N}";
        var secondId = $"email-conflict-{Guid.NewGuid():N}";
        await using var db = fixture.CreateContext();
        try
        {
            var first = await Service(db, firstId).SaveCurrentUserAsync();
            var error = await Assert.ThrowsAsync<DbUpdateException>(() => Service(db, secondId, email: first.Email).SaveCurrentUserAsync());
            var postgres = Assert.IsType<PostgresException>(error.InnerException);
            Assert.Equal(PostgresErrorCodes.UniqueViolation, postgres.SqlState);
            Assert.Equal("IX_USERS_Email", postgres.ConstraintName);
            Assert.DoesNotContain(db.ChangeTracker.Entries<User>(), e => e.Entity.ClerkId == secondId);
            await using var verify = fixture.CreateContext();
            Assert.False(await verify.Users.AnyAsync(u => u.ClerkId == secondId));
            Assert.True(await verify.Users.AnyAsync(u => u.ClerkId == firstId));
        }
        finally
        {
            await db.Users.Where(u => u.ClerkId == firstId || u.ClerkId == secondId).ExecuteDeleteAsync();
        }
    }

    [Fact]
    public async Task ConcurrentFirstRequestsUseSeparateContextsAndRecoverFromRealUniqueViolation()
    {
        var id = $"race-{Guid.NewGuid():N}";
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var gate = new ConcurrentInsertGate();
        var options = new DbContextOptionsBuilder<PaletteDbContext>()
            .UseNpgsql(fixture.ConnectionString).AddInterceptors(gate).Options;
        await using var first = new PaletteDbContext(options);
        await using var second = new PaletteDbContext(options);
        try
        {
            // Neither context has a transaction: the winner must commit before
            // the loser's unique constraint check and recovery SELECT can finish.
            async Task<User> Provision(PaletteDbContext db, string email)
            {
                var service = Service(db, id, email: email);
                Assert.False(await service.UserExistsAsync(id, timeout.Token));
                return await service.SaveCurrentUserAsync(timeout.Token);
            }

            var users = await Task.WhenAll(Provision(first, $"first-{id}@example.test"), Provision(second, $"second-{id}@example.test")).WaitAsync(timeout.Token);
            Assert.Equal(2, gate.Arrivals);
            var conflict = Assert.Single(gate.Failures);
            var postgres = Assert.IsType<PostgresException>(Assert.IsType<DbUpdateException>(conflict).InnerException);
            Assert.Equal(PostgresErrorCodes.UniqueViolation, postgres.SqlState);
            Assert.Equal("IX_USERS_ClerkId", postgres.ConstraintName);
            Assert.Equal(users[0].UserId, users[1].UserId);
            Assert.DoesNotContain(first.ChangeTracker.Entries<User>(), e => e.State == EntityState.Added);
            Assert.DoesNotContain(second.ChangeTracker.Entries<User>(), e => e.State == EntityState.Added);
            await using var verify = fixture.CreateContext();
            var saved = Assert.Single(await verify.Users.Where(u => u.ClerkId == id).ToListAsync());
            Assert.Equal(users[0].UserId, saved.UserId);
            Assert.Equal(users[0].Email, saved.Email);
            Assert.Equal(users[1].Email, saved.Email);
        }
        finally
        {
            await using var cleanup = fixture.CreateContext();
            await cleanup.Users.Where(u => u.ClerkId == id).ExecuteDeleteAsync();
        }
    }

    // Both real SELECTs have returned absent before either INSERT may execute.
    // No sleeps, simulated exceptions, or shared DbContext instances.
    private sealed class ConcurrentInsertGate : SaveChangesInterceptor
    {
        private int _arrivals;
        private readonly TaskCompletionSource _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int Arrivals => _arrivals;
        public System.Collections.Concurrent.ConcurrentBag<Exception> Failures { get; } = [];

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (Interlocked.Increment(ref _arrivals) == 2) _ready.TrySetResult();
            await _ready.Task.WaitAsync(cancellationToken);
            return result;
        }

        public override Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
        {
            Failures.Add(eventData.Exception);
            return Task.CompletedTask;
        }
    }
}
