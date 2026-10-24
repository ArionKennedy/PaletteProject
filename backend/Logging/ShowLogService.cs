using Microsoft.EntityFrameworkCore;
using Palette.Api.Data;
using Palette.Api.Models;

namespace Palette.Api.Logging;

public sealed record LogShowRequest(int TmdbTvId, string? Title);
public sealed record ShowLogItem(Guid LogId, Guid MediaId, int TmdbTvId,
    string Title, string? Status, DateOnly LoggedDate);
public sealed record ShowLogPage(IReadOnlyList<ShowLogItem> Items, int Page, bool HasMore);

public sealed class ShowLogService(PaletteDbContext db)
{
    private const int PageSize = 50;
    private const int TvLogLockNamespace = 4350;

    public async Task<ShowLogPage> GetPage(Guid userId, int page, CancellationToken cancellationToken)
    {
        var items = await db.UserMediaLogs.AsNoTracking().Where(log => log.UserId == userId && log.Media.TvShow != null)
            .OrderByDescending(log => log.LoggedDate).ThenByDescending(log => log.LogId).Skip((page - 1) * PageSize)
            .Take(PageSize + 1).Select(log => new ShowLogItem(log.LogId, log.MediaId,
                log.Media.TvShow!.TmdbTvId, log.Media.Title, log.Status, log.LoggedDate))
            .ToListAsync(cancellationToken);
        return new ShowLogPage(items.Take(PageSize).ToList(), page, items.Count > PageSize);
    }

    public async Task<ShowLogItem> SaveWatched(Guid userId, LogShowRequest request,
        CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        // Serialize writes for this external TV ID across API instances. This makes concurrent
        // taps idempotent and prevents two users from creating the same media record at once.
        // The lock is released automatically when the transaction commits or rolls back.
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock({TvLogLockNamespace}, {request.TmdbTvId})",
            cancellationToken);

        var show = await db.TvShows.Include(item => item.Media)
            .SingleOrDefaultAsync(item => item.TmdbTvId == request.TmdbTvId, cancellationToken);
        if (show is null)
        {
            var media = new Media
            {
                MediaId = Guid.NewGuid(), Title = request.Title!.Trim(), MediaType = "tv",
            };
            show = new TvShow { MediaId = media.MediaId, TmdbTvId = request.TmdbTvId, Media = media };
            db.TvShows.Add(show);
        }

        var log = await db.UserMediaLogs
            .Where(item => item.UserId == userId && item.MediaId == show.MediaId)
            .OrderByDescending(item => item.LoggedDate).ThenByDescending(item => item.LogId)
            .FirstOrDefaultAsync(cancellationToken);
        if (log is null)
        {
            log = new UserMediaLog
            {
                LogId = Guid.NewGuid(), UserId = userId, MediaId = show.MediaId,
                Media = show.Media, Status = "watched",
                LoggedDate = DateOnly.FromDateTime(DateTime.UtcNow),
            };
            db.UserMediaLogs.Add(log);
        }
        else if (log.Status != "watched")
        {
            log.Status = "watched";
            log.LoggedDate = DateOnly.FromDateTime(DateTime.UtcNow);
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new ShowLogItem(log.LogId, log.MediaId, show.TmdbTvId,
            show.Media.Title, log.Status, log.LoggedDate);
    }
}
