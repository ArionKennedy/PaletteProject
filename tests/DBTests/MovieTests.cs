using Microsoft.EntityFrameworkCore;
using Palette.Api.Data;
using Palette.Api.Models;

namespace Palette.Tests;

public class MovieTests {

    private readonly PaletteDbContext _context;

    public MovieTests() {
        var options = new DbContextOptionsBuilder<PaletteDbContext>()
              .UseInMemoryDatabase(Guid.NewGuid().ToString())
              .Options;


        _context = new PaletteDbContext(options);
    }

    [Fact]
    public void CanAddMovie() {
        Guid TestMediaId = Guid.NewGuid();
        _context.Media.Add(new Media{MediaId = TestMediaId, Title = "Revenge of Guderian", MediaType = "Movie"});
        _context.Movies.Add(new Movie{MediaId = TestMediaId, TmdbMovieId = 0});

        _context.SaveChanges();


        Assert.Equal(1, _context.Movies.Count());
        Assert.Equal(1, _context.Media.Count());
    }

    [Fact]
    public void CanGetMovie() {
        Guid TestMediaId = Guid.NewGuid();
        _context.Media.Add(new Media{MediaId = TestMediaId, Title = "Revenge of Guderian", MediaType = "Movie"});
        _context.Movies.Add(new Movie{MediaId = TestMediaId, TmdbMovieId = 0});

        _context.SaveChanges();

        var movie = _context.Movies.Find(TestMediaId);

        Assert.NotNull(movie);
        Assert.Equal(TestMediaId, movie.MediaId);
    }

}