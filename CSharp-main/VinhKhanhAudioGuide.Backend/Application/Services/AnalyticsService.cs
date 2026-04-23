using Microsoft.EntityFrameworkCore;
using VinhKhanhAudioGuide.Backend.Domain.Enums;
using VinhKhanhAudioGuide.Backend.Persistence;

namespace VinhKhanhAudioGuide.Backend.Application.Services;

public sealed class AnalyticsService(AudioGuideDbContext dbContext) : IAnalyticsService
{
    private readonly AudioGuideDbContext _dbContext = dbContext;

    #region Listening Stats

    public async Task<IEnumerable<PoiListeningStat>> GetTopPoisByListeningCountAsync(
        int limit = 5,
        Guid? managerId = null,
        CancellationToken cancellationToken = default)
    {
        if (limit <= 0) return [];

        var stats = (await GetPoiListeningStatsAsync(managerId, cancellationToken))
            .OrderByDescending(x => x.ListeningCount)
            .ThenBy(x => x.PoiName)
            .Take(limit)
            .ToList();

        return stats;
    }

    public async Task<IEnumerable<PoiListeningStat>> GetPoiListeningStatsAsync(
        Guid? managerId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.ListeningSessions
            .AsNoTracking()
            .Where(x => x.DurationSeconds.HasValue);

        if (managerId.HasValue)
        {
            query = query.Where(x => x.Poi != null && x.Poi.ManagerUserId == managerId.Value);
        }

        var sessionStats = await query
            .GroupBy(x => x.PoiId)
            .Select(x => new
            {
                PoiId = x.Key,
                ListeningCount = x.Count(),
                TotalDurationSeconds = x.Sum(y => y.DurationSeconds!.Value),
                AverageDurationSeconds = x.Average(y => y.DurationSeconds!.Value)
            })
            .ToListAsync(cancellationToken);

        var poiQuery = _dbContext.Pois.AsNoTracking();
        if (managerId.HasValue)
        {
            poiQuery = poiQuery.Where(p => p.ManagerUserId == managerId.Value);
        }

        var pois = await poiQuery
            .Select(x => new { x.Id, x.Code, x.Name, x.ManagerUserId, ManagerUsername = x.ManagerUser != null ? x.ManagerUser.Username : null })
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        return sessionStats
            .Where(x => pois.ContainsKey(x.PoiId))
            .Select(x =>
            {
                var poi = pois[x.PoiId];
                return new PoiListeningStat(
                    x.PoiId,
                    poi.Code,
                    poi.Name,
                    x.ListeningCount,
                    x.TotalDurationSeconds,
                    x.AverageDurationSeconds,
                    poi.ManagerUserId,
                    poi.ManagerUsername);
            })
            .OrderByDescending(x => x.ListeningCount)
            .ThenBy(x => x.PoiName)
            .ToList();
    }

    #endregion

    #region Visit Stats

    public async Task<IEnumerable<PoiVisitStat>> GetTopPoisByVisitCountAsync(
        int limit = 5,
        Guid? managerId = null,
        CancellationToken cancellationToken = default)
    {
        if (limit <= 0) return [];

        var stats = (await GetPoiVisitStatsAsync(managerId, cancellationToken))
            .OrderByDescending(x => x.VisitCount)
            .ThenBy(x => x.PoiName)
            .Take(limit)
            .ToList();

        return stats;
    }

    public async Task<IEnumerable<PoiVisitStat>> GetPoiVisitStatsAsync(
        Guid? managerId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.VisitSessions.AsNoTracking();

        if (managerId.HasValue)
        {
            query = query.Where(v => v.Poi != null && v.Poi.ManagerUserId == managerId.Value);
        }

        var visitStats = await query
            .GroupBy(x => x.PoiId)
            .Select(x => new
            {
                PoiId = x.Key,
                VisitCount = x.Count(),
                UniqueVisitors = x.Select(v => v.UserId).Distinct().Count(),
                TotalDurationSeconds = x.Sum(v => v.DurationSeconds),
                AverageDurationSeconds = x.Average(v => v.DurationSeconds),
                AudioListenedCount = x.Sum(v => v.ListeningSessionCount ?? 0),
                TotalListenDurationSeconds = x.Sum(v => v.TotalListenDurationSeconds ?? 0)
            })
            .ToListAsync(cancellationToken);

        var poiQuery = _dbContext.Pois.AsNoTracking();
        if (managerId.HasValue)
        {
            poiQuery = poiQuery.Where(p => p.ManagerUserId == managerId.Value);
        }

        var pois = await poiQuery
            .Select(x => new { x.Id, x.Code, x.Name, x.ManagerUserId, ManagerUsername = x.ManagerUser != null ? x.ManagerUser.Username : null })
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        return visitStats
            .Where(x => pois.ContainsKey(x.PoiId))
            .Select(x =>
            {
                var poi = pois[x.PoiId];
                return new PoiVisitStat(
                    x.PoiId,
                    poi.Code,
                    poi.Name,
                    x.VisitCount,
                    x.UniqueVisitors,
                    x.TotalDurationSeconds,
                    x.AverageDurationSeconds,
                    x.AudioListenedCount,
                    x.TotalListenDurationSeconds,
                    poi.ManagerUserId,
                    poi.ManagerUsername);
            })
            .OrderByDescending(x => x.VisitCount)
            .ThenBy(x => x.PoiName)
            .ToList();
    }

    public async Task<PoiVisitStat?> GetPoiVisitStatAsync(
        Guid poiId,
        CancellationToken cancellationToken = default)
    {
        var visitStats = await _dbContext.VisitSessions
            .AsNoTracking()
            .Where(v => v.PoiId == poiId)
            .GroupBy(x => x.PoiId)
            .Select(x => new
            {
                PoiId = x.Key,
                VisitCount = x.Count(),
                UniqueVisitors = x.Select(v => v.UserId).Distinct().Count(),
                TotalDurationSeconds = x.Sum(v => v.DurationSeconds),
                AverageDurationSeconds = x.Average(v => v.DurationSeconds),
                AudioListenedCount = x.Sum(v => v.ListeningSessionCount ?? 0),
                TotalListenDurationSeconds = x.Sum(v => v.TotalListenDurationSeconds ?? 0)
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (visitStats == null) return null;

        var poi = await _dbContext.Pois
            .AsNoTracking()
            .Where(p => p.Id == poiId)
            .Select(p => new { p.Id, p.Code, p.Name, p.ManagerUserId, ManagerUsername = p.ManagerUser != null ? p.ManagerUser.Username : null })
            .FirstOrDefaultAsync(cancellationToken);

        if (poi == null) return null;

        return new PoiVisitStat(
            visitStats.PoiId,
            poi.Code,
            poi.Name,
            visitStats.VisitCount,
            visitStats.UniqueVisitors,
            visitStats.TotalDurationSeconds,
            visitStats.AverageDurationSeconds,
            visitStats.AudioListenedCount,
            visitStats.TotalListenDurationSeconds,
            poi.ManagerUserId,
            poi.ManagerUsername);
    }

    #endregion

    #region Daily Stats

    public async Task<IEnumerable<DailyStat>> GetDailyStatsAsync(
        int days = 7,
        Guid? managerId = null,
        CancellationToken cancellationToken = default)
    {
        if (days <= 0) days = 7;

        var startDate = DateTime.UtcNow.Date.AddDays(-days);
        var endDate = DateTime.UtcNow.Date.AddDays(1);

        // Get all users created in period for new visitors
        var newUserIds = await _dbContext.Users
            .AsNoTracking()
            .Where(u => u.CreatedAtUtc >= startDate && u.CreatedAtUtc < endDate)
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        // Get visits
        var visitQuery = _dbContext.VisitSessions.AsNoTracking()
            .Where(v => v.VisitedAtUtc >= startDate && v.VisitedAtUtc < endDate);
        if (managerId.HasValue)
        {
            visitQuery = visitQuery.Where(v => v.Poi != null && v.Poi.ManagerUserId == managerId.Value);
        }

        var visits = await visitQuery.ToListAsync(cancellationToken);

        // Get listenings
        var listeningQuery = _dbContext.ListeningSessions.AsNoTracking()
            .Where(l => l.StartedAtUtc >= startDate && l.StartedAtUtc < endDate && l.DurationSeconds.HasValue);
        if (managerId.HasValue)
        {
            listeningQuery = listeningQuery.Where(l => l.Poi != null && l.Poi.ManagerUserId == managerId.Value);
        }

        var listenings = await listeningQuery.ToListAsync(cancellationToken);

        // Get tour views
        var tourViewQuery = _dbContext.TourViewSessions.AsNoTracking()
            .Where(t => t.ViewedAtUtc >= startDate && t.ViewedAtUtc < endDate);
        if (managerId.HasValue)
        {
            tourViewQuery = tourViewQuery.Where(t => t.Tour != null && t.Tour.ShopId.HasValue && 
                _dbContext.Pois.Any(p => p.ShopId == t.Tour.ShopId && p.ManagerUserId == managerId.Value));
        }

        var tourViews = await tourViewQuery.ToListAsync(cancellationToken);

        // Get geofence events
        var geofenceQuery = _dbContext.PoiGeofenceEvents.AsNoTracking()
            .Where(g => g.OccurredAtUtc >= startDate && g.OccurredAtUtc < endDate);
        if (managerId.HasValue)
        {
            geofenceQuery = geofenceQuery.Where(g => g.Poi != null && g.Poi.ManagerUserId == managerId.Value);
        }

        var geofenceEvents = await geofenceQuery.ToListAsync(cancellationToken);

        var result = new List<DailyStat>();
        for (var date = startDate; date < endDate; date = date.AddDays(1))
        {
            var dayEnd = date.AddDays(1);

            var dayVisits = visits.Where(v => v.VisitedAtUtc >= date && v.VisitedAtUtc < dayEnd).ToList();
            var dayListenings = listenings.Where(l => l.StartedAtUtc >= date && l.StartedAtUtc < dayEnd).ToList();
            var dayTourViews = tourViews.Where(t => t.ViewedAtUtc >= date && t.ViewedAtUtc < dayEnd).ToList();
            var dayGeofence = geofenceEvents.Where(g => g.OccurredAtUtc >= date && g.OccurredAtUtc < dayEnd).ToList();
            var dayNewUsers = newUserIds.Where(id => 
                _dbContext.Users.Any(u => u.Id == id && u.CreatedAtUtc >= date && u.CreatedAtUtc < dayEnd)).ToList();

            result.Add(new DailyStat(
                date,
                dayVisits.Count,
                dayListenings.Count,
                dayListenings.Sum(l => l.DurationSeconds ?? 0),
                dayVisits.Select(v => v.UserId).Distinct().Count(),
                dayNewUsers.Count,
                dayTourViews.Count,
                dayGeofence.Count));
        }

        return result;
    }

    #endregion

    #region Tour View Stats

    public async Task<IEnumerable<TourViewStat>> GetTourViewStatsAsync(
        Guid? managerId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.TourViewSessions
            .AsNoTracking()
            .Include(t => t.Tour)
            .AsQueryable();

        if (managerId.HasValue)
        {
            query = query.Where(t => t.Tour != null &&
                _dbContext.Pois.Any(p => p.ShopId == t.Tour.ShopId && p.ManagerUserId == managerId.Value));
        }

        var tourStats = await query
            .GroupBy(x => x.TourId)
            .Select(x => new
            {
                TourId = x.Key,
                ViewCount = x.Count(),
                UniqueViewers = x.Select(t => t.UserId).Distinct().Count(),
                AverageDurationSeconds = x.Average(t => t.DurationSeconds),
                TotalPoiVisited = x.Sum(t => t.PoiVisitedCount),
                TotalAudioListened = x.Sum(t => t.AudioListenedCount)
            })
            .ToListAsync(cancellationToken);

        var tourMap = await _dbContext.Tours
            .AsNoTracking()
            .ToDictionaryAsync(t => t.Id, cancellationToken);

        return tourStats
            .Where(x => tourMap.ContainsKey(x.TourId))
            .Select(x =>
            {
                var tour = tourMap[x.TourId];
                return new TourViewStat(
                    x.TourId,
                    tour.Code,
                    tour.Name,
                    x.ViewCount,
                    x.UniqueViewers,
                    (int)x.AverageDurationSeconds,
                    x.TotalPoiVisited,
                    x.TotalAudioListened);
            })
            .OrderByDescending(x => x.ViewCount)
            .ToList();
    }

    #endregion

    #region Geofence Stats

    public async Task<IEnumerable<GeofenceStat>> GetGeofenceStatsAsync(
        Guid? poiId = null,
        Guid? managerId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.PoiGeofenceEvents.AsNoTracking().AsQueryable();

        if (poiId.HasValue)
        {
            query = query.Where(g => g.PoiId == poiId.Value);
        }

        if (managerId.HasValue)
        {
            query = query.Where(g => g.Poi != null && g.Poi.ManagerUserId == managerId.Value);
        }

        var geofenceStats = await query
            .GroupBy(x => x.PoiId)
            .Select(x => new
            {
                PoiId = x.Key,
                EnterCount = x.Count(e => e.EventType == PoiGeofenceEventType.Enter),
                ExitCount = x.Count(e => e.EventType == PoiGeofenceEventType.Exit),
                DwellCount = x.Count(e => e.EventType == PoiGeofenceEventType.Dwell)
            })
            .ToListAsync(cancellationToken);

        var poiMap = await _dbContext.Pois
            .AsNoTracking()
            .ToDictionaryAsync(p => p.Id, p => p.Name, cancellationToken);

        return geofenceStats
            .Where(x => poiMap.ContainsKey(x.PoiId))
            .Select(x => new GeofenceStat(
                x.PoiId,
                poiMap[x.PoiId],
                x.EnterCount,
                x.ExitCount,
                x.DwellCount))
            .OrderByDescending(x => x.EnterCount)
            .ToList();
    }

    #endregion

    #region Usage Summary

    public async Task<UsageSummary> GetUsageSummaryAsync(
        int days = 7,
        Guid? managerId = null,
        CancellationToken cancellationToken = default)
    {
        if (days <= 0) days = 7;

        var startDate = DateTime.UtcNow.Date.AddDays(-days);
        var endDate = DateTime.UtcNow;

        // Get visits
        var visitQuery = _dbContext.VisitSessions.AsNoTracking()
            .Where(v => v.VisitedAtUtc >= startDate);
        if (managerId.HasValue)
        {
            visitQuery = visitQuery.Where(v => v.Poi != null && v.Poi.ManagerUserId == managerId.Value);
        }
        var visits = await visitQuery.ToListAsync(cancellationToken);

        // Get listenings
        var listeningQuery = _dbContext.ListeningSessions.AsNoTracking()
            .Where(l => l.StartedAtUtc >= startDate && l.DurationSeconds.HasValue);
        if (managerId.HasValue)
        {
            listeningQuery = listeningQuery.Where(l => l.Poi != null && l.Poi.ManagerUserId == managerId.Value);
        }
        var listenings = await listeningQuery.ToListAsync(cancellationToken);

        // Get tour views
        var tourViewQuery = _dbContext.TourViewSessions.AsNoTracking()
            .Where(t => t.ViewedAtUtc >= startDate);
        if (managerId.HasValue)
        {
            tourViewQuery = tourViewQuery.Where(t => t.Tour != null &&
                _dbContext.Pois.Any(p => p.ShopId == t.Tour.ShopId && p.ManagerUserId == managerId.Value));
        }
        var tourViews = await tourViewQuery.ToListAsync(cancellationToken);

        // Get geofence events
        var geofenceQuery = _dbContext.PoiGeofenceEvents.AsNoTracking()
            .Where(g => g.OccurredAtUtc >= startDate);
        if (managerId.HasValue)
        {
            geofenceQuery = geofenceQuery.Where(g => g.Poi != null && g.Poi.ManagerUserId == managerId.Value);
        }
        var geofenceEvents = await geofenceQuery.ToListAsync(cancellationToken);

        // Get new users in period
        var newUserCount = await _dbContext.Users
            .CountAsync(u => u.CreatedAtUtc >= startDate, cancellationToken);

        // Get active POI count
        var activePoiQuery = _dbContext.Pois.AsNoTracking().AsQueryable();
        if (managerId.HasValue)
        {
            activePoiQuery = activePoiQuery.Where(p => p.ManagerUserId == managerId.Value);
        }
        var activePoiCount = await activePoiQuery.CountAsync(cancellationToken);

        // Get top POIs
        var topPoisByVisit = (await GetTopPoisByVisitCountAsync(5, managerId, cancellationToken)).ToList();
        var topPoisByListening = (await GetTopPoisByListeningCountAsync(5, managerId, cancellationToken)).ToList();
        var topTours = (await GetTourViewStatsAsync(managerId, cancellationToken)).Take(5).ToList();

        // Get daily breakdown
        var dailyStats = (await GetDailyStatsAsync(days, managerId, cancellationToken)).ToList();

        return new UsageSummary(
            visits.Count,
            listenings.Count,
            listenings.Sum(l => l.DurationSeconds ?? 0),
            visits.Select(v => v.UserId).Distinct().Count(),
            newUserCount,
            activePoiCount,
            tourViews.Count,
            geofenceEvents.Count,
            dailyStats,
            topPoisByVisit,
            topPoisByListening,
            topTours);
    }

    #endregion

    #region Route & Heatmap

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
        Guid? managerId = null,
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

        var query = _dbContext.RoutePoints
            .AsNoTracking()
            .Where(x => x.RecordedAtUtc >= startDate && x.RecordedAtUtc <= endDate);

        var points = await query
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

    #endregion
}
