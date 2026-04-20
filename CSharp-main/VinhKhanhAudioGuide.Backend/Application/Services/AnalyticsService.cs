using Microsoft.EntityFrameworkCore;
using VinhKhanhAudioGuide.Backend.Persistence;

namespace VinhKhanhAudioGuide.Backend.Application.Services;

public sealed class AnalyticsService(AudioGuideDbContext dbContext) : IAnalyticsService
{
    private readonly AudioGuideDbContext _dbContext = dbContext;

    public async Task<IEnumerable<PoiListeningStat>> GetTopPoisByListeningCountAsync(
        int limit = 5,
        CancellationToken cancellationToken = default)
    {
        if (limit <= 0)
        {
            return [];
        }

        var stats = (await GetPoiListeningStatsAsync(cancellationToken))
            .OrderByDescending(x => x.ListeningCount)
            .ThenBy(x => x.PoiName)
            .Take(limit)
            .ToList();

        return stats;
    }

    public async Task<IEnumerable<PoiListeningStat>> GetPoiListeningStatsAsync(
        CancellationToken cancellationToken = default)
    {
        var sessionStats = await _dbContext.ListeningSessions
            .AsNoTracking()
            .Where(x => x.DurationSeconds.HasValue)
            .GroupBy(x => x.PoiId)
            .Select(x => new
            {
                PoiId = x.Key,
                ListeningCount = x.Count(),
                TotalDurationSeconds = x.Sum(y => y.DurationSeconds!.Value),
                AverageDurationSeconds = x.Average(y => y.DurationSeconds!.Value)
            })
            .ToListAsync(cancellationToken);

        var poiMap = await _dbContext.Pois
            .AsNoTracking()
            .Select(x => new { x.Id, x.Code, x.Name })
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        return sessionStats
            .Where(x => poiMap.ContainsKey(x.PoiId))
            .Select(x =>
            {
                var poi = poiMap[x.PoiId];
                return new PoiListeningStat(
                    x.PoiId,
                    poi.Code,
                    poi.Name,
                    x.ListeningCount,
                    x.TotalDurationSeconds,
                    x.AverageDurationSeconds);
            })
            .OrderByDescending(x => x.ListeningCount)
            .ThenBy(x => x.PoiName)
            .ToList();
    }

    public async Task<IEnumerable<UserRoutePoint>> GetUserRouteAsync(
        Guid userId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.RoutePoints
            .AsNoTracking()
            .Where(x => x.UserId == userId);

        if (startDate.HasValue)
        {
            query = query.Where(x => x.RecordedAtUtc >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(x => x.RecordedAtUtc <= endDate.Value);
        }

        return await query
            .OrderBy(x => x.RecordedAtUtc)
            .Select(x => new UserRoutePoint(
                x.RecordedAtUtc,
                x.Latitude,
                x.Longitude,
                x.Source))
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<HeatmapCell>> GetHeatmapDataAsync(
        DateTime startDate,
        DateTime endDate,
        int precision = 3,
        CancellationToken cancellationToken = default)
    {
        if (endDate < startDate)
        {
            throw new ArgumentException("endDate must be greater than or equal to startDate.");
        }

        if (precision < 0 || precision > 6)
        {
            throw new ArgumentOutOfRangeException(nameof(precision), "precision must be between 0 and 6.");
        }

        var points = await _dbContext.RoutePoints
            .AsNoTracking()
            .Where(x => x.RecordedAtUtc >= startDate && x.RecordedAtUtc <= endDate)
            .Select(x => new { x.Latitude, x.Longitude })
            .ToListAsync(cancellationToken);

        return points
            .Select(x => new
            {
                LatCell = Math.Round(x.Latitude, precision),
                LngCell = Math.Round(x.Longitude, precision)
            })
            .GroupBy(x => new { x.LatCell, x.LngCell })
            .Select(x => new HeatmapCell(
                x.Key.LatCell,
                x.Key.LngCell,
                x.Count()))
            .OrderByDescending(x => x.PointCount)
            .ToList();
    }
}
