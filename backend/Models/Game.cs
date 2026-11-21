using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Palette.Api.Models;

public class Game
{
    [Key]
    [ForeignKey(nameof(Media))]
    public Guid MediaId { get; set; }
    [Required]
    public int IgdbGameId { get; set; }

    public Media Media { get; set; } = null!;
}
