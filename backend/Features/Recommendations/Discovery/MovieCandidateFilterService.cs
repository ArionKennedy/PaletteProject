
using Backend.Providers.Tmdb;
using Microsoft.EntityFrameworkCore;
using Palette.Api.Data;

namespace Backend.Features.Recommendations.Discovery;

public sealed class MovieCandidateFilterService
{
    private readonly PaletteDbContext _dbContext;

    public MovieCandidateFilterService(PaletteDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    // Excludes movies the user has already completed.
    public async Task<IReadOnlyList<TmdbMovieSummary>> FilterCompletedAsync(
        Guid userId,
        IEnumerable<TmdbMovieSummary> candidates,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(candidates);

        var candidateList = candidates
            .Where(movie => movie.Id > 0)
            .DistinctBy(movie => movie.Id)
            .ToArray();

        if (candidateList.Length == 0)
        {
            return [];
        }

        var candidateIds = candidateList
            .Select(movie => movie.Id)
            .ToArray();

        // Find completed movies belonging to this user.
        // Match local movie records to external TMDB IDs.
        var completedIds = await (
            from log in _dbContext.UserMediaLogs.AsNoTracking()
            join movie in _dbContext.Movies.AsNoTracking()
                on log.MediaId equals movie.MediaId
            where log.UserId == userId
                && log.Status != null
                && log.Status.Trim().ToLower() == "completed"
                && candidateIds.Contains(movie.TmdbMovieId)
            select movie.TmdbMovieId
        ).Distinct().ToListAsync(cancellationToken);

        var excludedIds = completedIds.ToHashSet();

        return candidateList
            .Where(movie => !excludedIds.Contains(movie.Id))
            .ToArray();
    }

    public async Task<IReadOnlyList<TmdbMovieSummary>> FilterInteractedAsync(
    Guid userId,
    IEnumerable<TmdbMovieSummary> candidates,
    CancellationToken cancellationToken = default)
    {
        var candidateList = candidates.ToArray();

        if (candidateList.Length == 0)
            return candidateList;

        var candidateIds = candidateList
            .Select(movie => movie.Id)
            .Distinct()
            .ToArray();

        var excludedIds = await _dbContext.UserMediaLogs
            .AsNoTracking()
            .Where(log => log.UserId == userId)
            .Where(log => log.Media.Movie != null)
            .Where(log => candidateIds.Contains(
                log.Media.Movie!.TmdbMovieId))
            .Select(log => log.Media.Movie!.TmdbMovieId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var excluded = excludedIds.ToHashSet();

        return candidateList
            .Where(movie => !excluded.Contains(movie.Id))
            .ToArray();
    }
}