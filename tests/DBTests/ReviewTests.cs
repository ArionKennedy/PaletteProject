using Microsoft.EntityFrameworkCore;
using Palette.Api.Data;
using Palette.Api.Models;

namespace Palette.Tests;


public class ReviewTests {


    private readonly PaletteDbContext _context;

    public ReviewTests() {
        var options = new DbContextOptionsBuilder<PaletteDbContext>()
              .UseInMemoryDatabase(Guid.NewGuid().ToString())
              .Options;

        _context = new PaletteDbContext(options);
    }
    
    [Fact]
    public void CanAddReview() {
        Guid TestUserId = Guid.NewGuid();
        Guid TestMediaId = Guid.NewGuid();
        _context.Users.Add(new User { UserId = TestUserId, Username = "test", Email = "test@test.com", Auth0Id = "auth0|123" });
        _context.Media.Add(new Media { MediaId = TestMediaId, Title = "Revenge of Guderian", MediaType = "Movie" });
        _context.Reviews.Add(new Review { UserId = TestUserId, MediaId = TestMediaId, Rating = 5, Body = "Great movie" });

        _context.SaveChanges();

        Assert.Equal(1, _context.Reviews.Count());
    }

    [Fact]
    public void CanGetReview() {
        Guid TestUserId = Guid.NewGuid();
        Guid TestMediaId = Guid.NewGuid();
        Guid TestReviewId = Guid.NewGuid();
        _context.Users.Add(new User { UserId = TestUserId, Username = "test", Email = "test@test.com", Auth0Id = "auth0|123" });
        _context.Media.Add(new Media { MediaId = TestMediaId, Title = "Revenge of Guderian", MediaType = "Movie" });
        _context.Reviews.Add(new Review { ReviewId = TestReviewId, UserId = TestUserId, MediaId = TestMediaId, Rating = 5, Body = "Great movie" });

        _context.SaveChanges();

        var review = _context.Reviews.Find(TestReviewId);

        Assert.NotNull(review);
        Assert.Equal(TestReviewId, review.ReviewId);
    }
}