using Microsoft.EntityFrameworkCore;
using Palette.Api.Data;
using Palette.Api.Models;

namespace Palette.Tests;


public class TVShowTests {

    private readonly PaletteDbContext _context;

    public TVShowTests() {
        var options = new DbContextOptionsBuilder<PaletteDbContext>()
              .UseInMemoryDatabase(Guid.NewGuid().ToString())
              .Options;

        _context = new PaletteDbContext(options);
    }

    [Fact]
    public void CanAddTVShow() {
        Guid TestMediaId = Guid.NewGuid();
        _context.Media.Add(new Media{MediaId = TestMediaId, Title = "Adventures of Himbeault", MediaType = "TVShow"});
        _context.TvShows.Add(new TvShow{MediaId = TestMediaId, TmdbTvId = 0});

        _context.SaveChanges();

        Assert.Equal(1, _context.TvShows.Count());
        Assert.Equal(1, _context.Media.Count());
    }

    [Fact]
    public void CanGetTVShow() {
        Guid TestMediaId = Guid.NewGuid();
        _context.Media.Add(new Media{MediaId = TestMediaId, Title = "Adventures of Himbeault", MediaType = "TVShow"});
        _context.TvShows.Add(new TvShow{MediaId = TestMediaId, TmdbTvId = 0});

        _context.SaveChanges();

        var TVShow = _context.TvShows.Find(TestMediaId);

        Assert.NotNull(TVShow);
        Assert.Equal(TestMediaId, TVShow.MediaId);
    }
}