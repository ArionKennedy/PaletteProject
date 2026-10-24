using Microsoft.EntityFrameworkCore;
using Palette.Api.Data;
using Palette.Api.Models;

namespace Palette.Tests;


public class UserMediaLogTests {


    private readonly PaletteDbContext _context;

    public UserMediaLogTests() {
        var options = new DbContextOptionsBuilder<PaletteDbContext>()
              .UseInMemoryDatabase(Guid.NewGuid().ToString())
              .Options;

        _context = new PaletteDbContext(options);
    }

    [Fact]
    public void CanAddUserMediaLog() {
        Guid TestUserId = Guid.NewGuid();
        Guid TestMediaId = Guid.NewGuid();
        _context.Users.Add(new User { UserId = TestUserId, Username = "test", Email = "test@test.com", Auth0Id = "auth0|123" });
        _context.Media.Add(new Media { MediaId = TestMediaId, Title = "Revenge of Guderian", MediaType = "Movie" });
        _context.UserMediaLogs.Add(new UserMediaLog { UserId = TestUserId, MediaId = TestMediaId, Status = "Watched", LoggedDate = DateOnly.FromDateTime(DateTime.Now) });

        _context.SaveChanges();

        Assert.Equal(1, _context.UserMediaLogs.Count());
    }

    [Fact]
    public void CanGetUserMediaLog() {
        Guid TestUserId = Guid.NewGuid();
        Guid TestMediaId = Guid.NewGuid();
        Guid TestLogId = Guid.NewGuid();
        _context.Users.Add(new User { UserId = TestUserId, Username = "test", Email = "test@test.com", Auth0Id = "auth0|123" });
        _context.Media.Add(new Media { MediaId = TestMediaId, Title = "Revenge of Guderian", MediaType = "Movie" });
        _context.UserMediaLogs.Add(new UserMediaLog { LogId = TestLogId, UserId = TestUserId, MediaId = TestMediaId, Status = "Watched", LoggedDate =
        DateOnly.FromDateTime(DateTime.Now) });

        _context.SaveChanges();

        var log = _context.UserMediaLogs.Find(TestLogId);

        Assert.NotNull(log);
        Assert.Equal(TestLogId, log.LogId);
    }
    
}