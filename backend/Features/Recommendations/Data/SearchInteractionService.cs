
using Microsoft.EntityFrameworkCore;
using Palette.Api.Data;
using Palette.Api.MachineLearning.ImplicitFeedback;
using Palette.Api.Models;

namespace Backend.Features.Recommendations.Data;

public sealed class SearchInteractionService
{
    private readonly PaletteDbContext _dbContext;

    // Initializes the search interaction service.
    public SearchInteractionService(PaletteDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    // Records a user's interest in a media item returned by a search.
    public Task RecordSearchAsync(
        Guid userId,
        string mediaType,
        string externalId,
        CancellationToken cancellationToken = default)
    {
        return RecordInteractionAsync(
            userId,
            mediaType,
            externalId,
            InteractionType.Search,
            cancellationToken);
    }

    // Records when a user selects a media item from search results.
    public Task RecordSelectionAsync(
        Guid userId,
        string mediaType,
        string externalId,
        CancellationToken cancellationToken = default)
    {
        return RecordInteractionAsync(
            userId,
            mediaType,
            externalId,
            InteractionType.SearchSelection,
            cancellationToken);
    }

    // Retrieves a user's recorded search interactions.
    public async Task<IReadOnlyList<SearchInteraction>>
        GetSearchInteractionsAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
    {
        return await _dbContext.SearchInteractions
            .AsNoTracking()
            .Where(interaction => interaction.UserId == userId)
            .OrderByDescending(interaction => interaction.OccurredAt)
            .ToListAsync(cancellationToken);
    }

    // Validates and persists a supported search interaction.
    private async Task RecordInteractionAsync(
        Guid userId,
        string mediaType,
        string externalId,
        InteractionType type,
        CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException(
                "A valid user ID is required.",
                nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(mediaType))
        {
            throw new ArgumentException(
                "A media type is required.",
                nameof(mediaType));
        }

        if (string.IsNullOrWhiteSpace(externalId))
        {
            throw new ArgumentException(
                "An external media ID is required.",
                nameof(externalId));
        }

        if (type != InteractionType.Search &&
            type != InteractionType.SearchSelection)
        {
            throw new ArgumentOutOfRangeException(nameof(type));
        }

        string normalizedMediaType = mediaType.Trim().ToLowerInvariant();
        string normalizedExternalId = externalId.Trim();

        if (normalizedMediaType is not
            ("movie" or "tv" or "book" or "game"))
        {
            throw new ArgumentException(
                "Unsupported media type.",
                nameof(mediaType));
        }

        var interaction = new SearchInteraction
        {
            SearchInteractionId = Guid.NewGuid(),
            UserId = userId,
            MediaType = normalizedMediaType,
            ExternalId = normalizedExternalId,
            Type = type,
            OccurredAt = DateTimeOffset.UtcNow
        };

        _dbContext.SearchInteractions.Add(interaction);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}