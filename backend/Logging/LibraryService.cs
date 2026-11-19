using System.Globalization;
using Backend.Constants;
using Backend.Features.Lookup;
using Backend.Shared.Errors;
using Backend.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Palette.Api.Data;
using Palette.Api.Enums;
using Palette.Api.Models;

namespace Palette.Api.Logging;

public sealed record LibraryPage(IReadOnlyList<MediaSearchResult> Items, int Page, bool HasMore);
public sealed record LibraryCounts(int Movie, int Tv, int Book);
public sealed record LibraryMembership(bool Saved);
public sealed record LibraryPreviews(
    IReadOnlyList<MediaSearchResult> Movie,
    IReadOnlyList<MediaSearchResult> Tv,
    IReadOnlyList<MediaSearchResult> Book);

public sealed class LibraryService(PaletteDbContext db, LookupService lookup)
{
    private const int PageSize = 50;
    private const int PreviewSize = 12;

    public async Task<LibraryCounts> GetCounts(Guid userId, CancellationToken cancellationToken)
    {
        var counts = await db.UserMediaLogs.Where(log => log.UserId == userId)
            .Where(log => (log.Media.MediaType == MediaType.Movie && log.Media.Movie != null)
                || (log.Media.MediaType == MediaType.TVShow && log.Media.TvShow != null)
                || (log.Media.MediaType == MediaType.Book && log.Media.Book != null))
            .GroupBy(log => log.Media.MediaType)
            .Select(group => new { Type = group.Key, Count = group.Select(log => log.MediaId).Distinct().Count() })
            .ToDictionaryAsync(group => group.Type, group => group.Count, cancellationToken);

        return new LibraryCounts(counts.GetValueOrDefault(MediaType.Movie),
            counts.GetValueOrDefault(MediaType.TVShow), counts.GetValueOrDefault(MediaType.Book));
    }

    public async Task<LibraryPage> GetPage(Guid userId, string type, int page, CancellationToken cancellationToken)
    {
        if (page is < 1 or > 10000)
            throw new InvalidRequestException("Page must be between 1 and 10000.");

        var items = await GetItems(userId, type, (page - 1) * PageSize, PageSize + 1, cancellationToken);
        return new LibraryPage(items.Take(PageSize).ToList(), page, items.Count > PageSize);
    }

    public async Task<LibraryPreviews> GetPreviews(Guid userId, CancellationToken cancellationToken)
    {
        return new LibraryPreviews(
            await GetItems(userId, MediaConstants.MEDIA_TYPE_MOVIE, 0, PreviewSize, cancellationToken),
            await GetItems(userId, MediaConstants.MEDIA_TYPE_TV, 0, PreviewSize, cancellationToken),
            await GetItems(userId, MediaConstants.MEDIA_TYPE_BOOK, 0, PreviewSize, cancellationToken));
    }

    private Task<List<MediaSearchResult>> GetItems(
        Guid userId, string type, int offset, int limit, CancellationToken cancellationToken)
    {
        var latestLogs = db.UserMediaLogs.Where(log => log.UserId == userId)
            .GroupBy(log => log.MediaId)
            .Select(group => new { MediaId = group.Key, Date = group.Max(log => log.LoggedDate) });
        return (
            from media in MediaForType(type)
            join log in latestLogs on media.MediaId equals log.MediaId
            orderby log.Date descending, media.MediaId
            select new MediaSearchResult(media.ExternalId, type, media.Title, media.ReleaseDate, media.PosterUrl))
            .Skip(offset)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<LibraryMembership> GetMembership(
        Guid userId, string type, string externalId, CancellationToken cancellationToken)
    {
        externalId = NormalizeId(type, externalId);
        return new LibraryMembership(await LogsForItem(type, externalId)
            .AnyAsync(log => log.UserId == userId, cancellationToken));
    }

    public Task<bool> IsSavedBy(string clerkId, string type, string externalId, CancellationToken cancellationToken)
    {
        return LogsForItem(type, NormalizeId(type, externalId))
            .AnyAsync(log => log.User.ClerkId == clerkId, cancellationToken);
    }

    public async Task<LibraryMembership> SetMembership(
        Guid userId, string type, string externalId, bool saved, CancellationToken cancellationToken)
    {
        externalId = NormalizeId(type, externalId);
        var matchingMedia = MediaForType(type).Where(media => media.ExternalId == externalId);
        MediaLookupResult? details = null;
        if (saved)
        {
            var existing = await matchingMedia.SingleOrDefaultAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(existing?.PosterUrl))
            {
                try
                {
                    details = await lookup.LookupAsync(type, externalId, cancellationToken);
                }
                catch (ApiException) when (existing is not null)
                {
                    // An unavailable cover should not prevent saving media already in the catalog.
                }
            }
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        // Serialize writes for this provider ID, including concurrent first saves by different users.
        var lockKey = $"library:{type}:{externalId}";
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({lockKey}, 0))", cancellationToken);

        var logs = LogsForItem(type, externalId).Where(log => log.UserId == userId);
        if (!saved)
        {
            await logs.ExecuteDeleteAsync(cancellationToken);
        }
        else
        {
            var mediaId = await matchingMedia.Select(media => (Guid?)media.MediaId)
                .SingleOrDefaultAsync(cancellationToken);
            if (mediaId is null)
            {
                var media = CreateMedia(details ?? throw new InvalidOperationException("Media details are missing."), externalId);
                db.Media.Add(media);
                mediaId = media.MediaId;
            }
            else if (details?.PosterUrl is { } posterUrl && !string.IsNullOrWhiteSpace(posterUrl))
            {
                await db.Media
                    .Where(media => media.MediaId == mediaId && string.IsNullOrWhiteSpace(media.PosterUrl))
                    .ExecuteUpdateAsync(update => update.SetProperty(media => media.PosterUrl, posterUrl), cancellationToken);
            }

            if (!await logs.AnyAsync(cancellationToken))
            {
                db.UserMediaLogs.Add(new UserMediaLog
                {
                    LogId = Guid.NewGuid(),
                    UserId = userId,
                    MediaId = mediaId.Value,
                    LoggedDate = DateOnly.FromDateTime(DateTime.UtcNow)
                });
                await db.SaveChangesAsync(cancellationToken);
            }
        }

        await transaction.CommitAsync(cancellationToken);
        return new LibraryMembership(saved);
    }

    private IQueryable<UserMediaLog> LogsForItem(string type, string externalId)
    {
        var mediaIds = MediaForType(type).Where(media => media.ExternalId == externalId)
            .Select(media => media.MediaId);
        return db.UserMediaLogs.Where(log => mediaIds.Contains(log.MediaId));
    }

    private IQueryable<LibraryMedia> MediaForType(string type) => type switch
    {
        MediaConstants.MEDIA_TYPE_MOVIE => db.Movies.Where(movie => movie.Media.MediaType == MediaType.Movie)
            .Select(movie => new LibraryMedia
            {
                MediaId = movie.MediaId,
                ExternalId = movie.TmdbMovieId.ToString(),
                Title = movie.Media.Title,
                ReleaseDate = movie.Media.ReleaseDate,
                PosterUrl = movie.Media.PosterUrl
            }),
        MediaConstants.MEDIA_TYPE_TV => db.TvShows.Where(tv => tv.Media.MediaType == MediaType.TVShow)
            .Select(tv => new LibraryMedia
            {
                MediaId = tv.MediaId,
                ExternalId = tv.TmdbTvId.ToString(),
                Title = tv.Media.Title,
                ReleaseDate = tv.Media.ReleaseDate,
                PosterUrl = tv.Media.PosterUrl
            }),
        MediaConstants.MEDIA_TYPE_BOOK => db.Books.Where(book => book.Media.MediaType == MediaType.Book)
            .Select(book => new LibraryMedia
            {
                MediaId = book.MediaId,
                ExternalId = book.GoogleBooksId,
                Title = book.Media.Title,
                ReleaseDate = book.Media.ReleaseDate,
                PosterUrl = book.Media.PosterUrl
            }),
        _ => throw new InvalidRequestException("Choose movie, tv, or book.")
    };

    private static string NormalizeId(string type, string externalId)
    {
        var id = externalId.Trim();
        return type switch
        {
            MediaConstants.MEDIA_TYPE_BOOK when id.Length > 0 => id,
            MediaConstants.MEDIA_TYPE_MOVIE or MediaConstants.MEDIA_TYPE_TV
                when int.TryParse(id, NumberStyles.None, CultureInfo.InvariantCulture, out var number) && number > 0
                => number.ToString(CultureInfo.InvariantCulture),
            _ => throw new InvalidRequestException("Provide a valid movie, tv, or book ID.")
        };
    }

    private static Media CreateMedia(MediaLookupResult details, string externalId)
    {
        var media = new Media
        {
            MediaId = Guid.NewGuid(),
            Title = details.Title,
            ReleaseDate = details.ReleaseDate,
            Description = details.Description,
            PosterUrl = details.PosterUrl
        };

        switch (details)
        {
            case MovieLookupResult movie:
                media.MediaType = MediaType.Movie;
                media.Movie = new Movie
                {
                    TmdbMovieId = int.Parse(externalId, CultureInfo.InvariantCulture),
                    RuntimeMinutes = movie.Runtime
                };
                break;
            case TvLookupResult tv:
                media.MediaType = MediaType.TVShow;
                media.TvShow = new TvShow
                {
                    TmdbTvId = int.Parse(externalId, CultureInfo.InvariantCulture),
                    NumberOfSeasons = tv.NumberOfSeasons
                };
                break;
            case BookLookupResult book:
                media.MediaType = MediaType.Book;
                media.Book = new Book { GoogleBooksId = externalId, ISBN = book.ISBN };
                break;
            default:
                throw new InvalidOperationException("Unsupported media details.");
        }

        return media;
    }

    private sealed class LibraryMedia
    {
        public Guid MediaId { get; init; }
        public required string ExternalId { get; init; }
        public required string Title { get; init; }
        public DateOnly? ReleaseDate { get; init; }
        public string? PosterUrl { get; init; }
    }
}
