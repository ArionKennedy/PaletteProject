
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Palette.Api.Data;
using Palette.Api.Models;
using Palette.Api.MachineLearning.ContentBased;

namespace Backend.Features.Recommendations.Data;

public sealed class MediaMetadataService
{
    private readonly PaletteDbContext _dbContext;

    // Initializes the media metadata service.
    public MediaMetadataService(PaletteDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    // Retrieves recommendation features for a single media item.
    public async Task<MediaFeatures?> GetMediaFeaturesAsync(
        Guid mediaId,
        CancellationToken cancellationToken = default)
    {
        var media = await CreateMediaQuery()
            .FirstOrDefaultAsync(
                item => item.MediaId == mediaId,
                cancellationToken);

        return media == null
            ? null
            : MapStoredFeatures(media);
    }

    // Retrieves recommendation features for multiple media items.
    public async Task<IReadOnlyList<MediaFeatures>> GetMediaFeaturesAsync(
        IEnumerable<Guid> mediaIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(mediaIds);

        var ids = mediaIds.Distinct().ToArray();

        if (ids.Length == 0)
        {
            return [];
        }

        var media = await CreateMediaQuery()
            .Where(item => ids.Contains(item.MediaId))
            .ToListAsync(cancellationToken);

        return media
            .Select(MapStoredFeatures)
            .ToArray();
    }

    // Retrieves recommendation features for all stored media.
    public async Task<IReadOnlyList<MediaFeatures>> GetAllMediaFeaturesAsync(
        CancellationToken cancellationToken = default)
    {
        var media = await CreateMediaQuery()
            .ToListAsync(cancellationToken);

        return media
            .Select(MapStoredFeatures)
            .ToArray();
    }

    // Maps database metadata into recommendation features.
    // Movie genres are read from PostgreSQL rather than TMDB.
    private static MediaFeatures MapStoredFeatures(Media media)
    {
        return MediaFeatureMapper.Map(
            media,
            genres: media.Movie?.Genres);
    }

    // Creates a read-only query containing media-specific metadata.
    private IQueryable<Media> CreateMediaQuery()
    {
        return _dbContext.Media
            .AsNoTracking()
            .Include(media => media.Movie)
            .Include(media => media.TvShow)
            .Include(media => media.Book)
            .Include(media => media.Game);
    }
}
