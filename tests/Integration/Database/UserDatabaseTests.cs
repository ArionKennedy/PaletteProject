using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Palette.Api.Models;
using Palette.Tests.Infrastructure;
using Xunit;

namespace Palette.Tests.Integration.Database;

[Collection("Postgres Integration Tests")]
public class UserDatabaseTests
{
    private readonly PostgresFixture _fixture;

    public UserDatabaseTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CanPersistAndRetrieveAllUserFields()
    {
        await using var db = _fixture.CreateContext();
        await using var transaction =
            await db.Database.BeginTransactionAsync();

        var user = CreateUser();

        db.Users.Add(user);
        await db.SaveChangesAsync();

        db.ChangeTracker.Clear();

        var saved = await db.Users
            .AsNoTracking()
            .SingleAsync(item => item.UserId == user.UserId);

        Assert.Equal(user.UserId, saved.UserId);
        Assert.Equal(user.Auth0Id, saved.Auth0Id);
        Assert.Equal(user.Username, saved.Username);
        Assert.Equal(user.Email, saved.Email);
        Assert.Equal(user.DateJoined, saved.DateJoined);
        Assert.Equal(DateTimeKind.Utc, saved.DateJoined.Kind);
    }

    [Fact]
    public async Task CannotInsertDuplicateEmail()
    {
        await using var db = _fixture.CreateContext();
        await using var transaction =
            await db.Database.BeginTransactionAsync();

        var first = CreateUser();

        db.Users.Add(first);
        await db.SaveChangesAsync();

        var second = CreateUser();

        // Keep the identity and primary key different so only
        // the email uniqueness constraint is violated.
        second.Email = first.Email;

        db.Users.Add(second);

        var exception = await Assert.ThrowsAsync<DbUpdateException>(
            () => db.SaveChangesAsync());

        var postgres = Assert.IsType<PostgresException>(
            exception.InnerException);

        Assert.Equal(
            PostgresErrorCodes.UniqueViolation,
            postgres.SqlState);
        Assert.Equal(
            "IX_USERS_Email",
            postgres.ConstraintName);
    }

    [Fact]
    public async Task CannotInsertDuplicateAuth0Id()
    {
        await using var db = _fixture.CreateContext();
        await using var transaction =
            await db.Database.BeginTransactionAsync();

        var first = CreateUser();

        db.Users.Add(first);
        await db.SaveChangesAsync();

        var second = CreateUser();

        // Keep the email and primary key different so only
        // the external identity constraint is violated.
        second.Auth0Id = first.Auth0Id;

        db.Users.Add(second);

        var exception = await Assert.ThrowsAsync<DbUpdateException>(
            () => db.SaveChangesAsync());

        var postgres = Assert.IsType<PostgresException>(
            exception.InnerException);

        Assert.Equal(
            PostgresErrorCodes.UniqueViolation,
            postgres.SqlState);
        Assert.Equal(
            "IX_USERS_Auth0Id",
            postgres.ConstraintName);
    }

    [Theory]
    [InlineData("Email", "IX_USERS_Email")]
    [InlineData("Auth0Id", "IX_USERS_Auth0Id")]
    public async Task CannotUpdateUserToAnotherUsersUniqueValue(
        string property,
        string expectedConstraint)
    {
        await using var db = _fixture.CreateContext();
        await using var transaction =
            await db.Database.BeginTransactionAsync();

        var first = CreateUser();
        var second = CreateUser();

        db.Users.AddRange(first, second);
        await db.SaveChangesAsync();

        db.ChangeTracker.Clear();

        var savedSecond = await db.Users.SingleAsync(
            item => item.UserId == second.UserId);

        switch (property)
        {
            case "Email":
                savedSecond.Email = first.Email;
                break;

            case "Auth0Id":
                savedSecond.Auth0Id = first.Auth0Id;
                break;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(property),
                    property,
                    "Unknown test property.");
        }

        var exception = await Assert.ThrowsAsync<DbUpdateException>(
            () => db.SaveChangesAsync());

        var postgres = Assert.IsType<PostgresException>(
            exception.InnerException);

        Assert.Equal(
            PostgresErrorCodes.UniqueViolation,
            postgres.SqlState);
        Assert.Equal(
            expectedConstraint,
            postgres.ConstraintName);
    }

    [Theory]
    [InlineData("Email")]
    [InlineData("Username")]
    [InlineData("Auth0Id")]
    public async Task CannotInsertNullRequiredField(
        string property)
    {
        await using var db = _fixture.CreateContext();
        await using var transaction =
            await db.Database.BeginTransactionAsync();

        var user = CreateUser();

        // Deliberately bypass nullable-reference warnings to verify
        // PostgreSQL's actual NOT NULL constraint.
        switch (property)
        {
            case "Email":
                user.Email = null!;
                break;

            case "Username":
                user.Username = null!;
                break;

            case "Auth0Id":
                user.Auth0Id = null!;
                break;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(property),
                    property,
                    "Unknown test property.");
        }

        db.Users.Add(user);

        var exception = await Assert.ThrowsAsync<DbUpdateException>(
            () => db.SaveChangesAsync());

        var postgres = Assert.IsType<PostgresException>(
            exception.InnerException);

        Assert.Equal(
            PostgresErrorCodes.NotNullViolation,
            postgres.SqlState);
        Assert.Equal(
            "USERS",
            postgres.TableName);
        Assert.Equal(
            property,
            postgres.ColumnName);
    }

    [Fact]
    public async Task CanUpdateProfileWithoutChangingIdentityOrJoinDate()
    {
        await using var db = _fixture.CreateContext();
        await using var transaction =
            await db.Database.BeginTransactionAsync();

        var user = CreateUser();

        db.Users.Add(user);
        await db.SaveChangesAsync();

        var originalUserId = user.UserId;
        var originalAuth0Id = user.Auth0Id;
        var originalDateJoined = user.DateJoined;

        db.ChangeTracker.Clear();

        var saved = await db.Users.SingleAsync(
            item => item.UserId == originalUserId);

        var updatedEmail =
            $"updated-{Guid.NewGuid():N}@example.com";

        saved.Username = "Updated username";
        saved.Email = updatedEmail;

        await db.SaveChangesAsync();

        db.ChangeTracker.Clear();

        var updated = await db.Users
            .AsNoTracking()
            .SingleAsync(item => item.UserId == originalUserId);

        Assert.Equal("Updated username", updated.Username);
        Assert.Equal(updatedEmail, updated.Email);

        Assert.Equal(originalUserId, updated.UserId);
        Assert.Equal(originalAuth0Id, updated.Auth0Id);
        Assert.Equal(originalDateJoined, updated.DateJoined);
    }

    private static User CreateUser()
    {
        var id = Guid.NewGuid();

        return new User
        {
            UserId = id,
            Auth0Id = $"integration|{id:N}",
            Username = $"user_{id:N}",
            Email = $"user-{id:N}@example.com",

            // An exact UTC timestamp avoids PostgreSQL timestamp
            // precision differences in equality assertions.
            DateJoined = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc),
        };
    }
}
