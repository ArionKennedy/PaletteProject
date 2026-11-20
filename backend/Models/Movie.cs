using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Palette.Api.Models;

public class Movie
{
    [Key]
    [ForeignKey(nameof(Media))]
    public Guid MediaId { get; set; }
    [Required]
    public int TmdbMovieId { get; set; }
    public int? RuntimeMinutes { get; set; }
    public string[]? Genres { get; set; }
    
    public Media Media { get; set; } = null!;
}
