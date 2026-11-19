
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Palette.Api.MachineLearning.ImplicitFeedback;

namespace Palette.Api.Models;

public class SearchInteraction
{
    [Key]
    public Guid SearchInteractionId { get; set; }

    [ForeignKey(nameof(User))]
    public Guid UserId { get; set; }

    [Required]
    public string ExternalId { get; set; } = string.Empty;

    [Required]
    public string MediaType { get; set; } = string.Empty;

    public InteractionType Type { get; set; }

    public DateTimeOffset OccurredAt { get; set; }

    public User User { get; set; } = null!;
}
