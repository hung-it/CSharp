namespace VinhKhanhAudioGuide.Backend.Application.Services;

public interface IAnalyticsService
{
    // Listening stats
    Task<IEnumerable<PoiListeningStat>> GetTopPoisByListeningCountAsync(
        int limit = 5,
        Guid? managerId = null,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<PoiListeningStat>> GetPoiListeningStatsAsync(
        Guid? managerId = null,
        CancellationToken cancellationToken = default);

    // Visit stats
    Task<IEnumerable<PoiVisitStat>> GetTopPoisByVisitCountAsync(
        int limit = 5,
        Guid? managerId = null,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<PoiVisitStat>> GetPoiVisitStatsAsync(
        Guid? managerId = null,
        CancellationToken cancellationToken = default);

    Task<PoiVisitStat?> GetPoiVisitStatAsync(
        Guid poiId,
        CancellationToken cancellationToken = default);

    // Daily stats
    Task<IEnumerable<DailyStat>> GetDailyStatsAsync(
        int days = 7,
        Guid? managerId = null,
        CancellationToken cancellationToken = default);

    // Tour stats
    Task<IEnumerable<TourViewStat>> GetTourViewStatsAsync(
        Guid? managerId = null,
        CancellationToken cancellationToken = default);

    // Geofence stats
    Task<IEnumerable<GeofenceStat>> GetGeofenceStatsAsync(
        Guid? poiId = null,
        Guid? managerId = null,
        CancellationToken cancellationToken = default);

    // Combined usage summary
    Task<UsageSummary> GetUsageSummaryAsync(
        int days = 7,
        Guid? managerId = null,
        CancellationToken cancellationToken = default);

    // Route & heatmap
    Task<IEnumerable<UserRoutePoint>> GetUserRouteAsync(
        Guid userId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<HeatmapCell>> GetHeatmapDataAsync(
        DateTime startDate,
        DateTime endDate,
        int precision = 3,
        Guid? managerId = null,
        CancellationToken cancellationToken = default);
}

// Listening stats
public sealed record PoiListeningStat(
    Guid PoiId,
    string PoiCode,
    string PoiName,
    int ListeningCount,
    int TotalDurationSeconds,
    double AverageDurationSeconds,
    Guid? ManagerUserId = null,
    string? ManagerUsername = null);

// Visit stats
public sealed record PoiVisitStat(
    Guid PoiId,
    string PoiCode,
    string PoiName,
    int VisitCount,
    int UniqueVisitors,
    int TotalDurationSeconds,
    double AverageDurationSeconds,
    int AudioListenedCount,
    int TotalListenDurationSeconds,
    Guid? ManagerUserId = null,
    string? ManagerUsername = null);

// Tour view stats
public sealed record TourViewStat(
    Guid TourId,
    string TourCode,
    string TourName,
    int ViewCount,
    int UniqueViewers,
    int AverageDurationSeconds,
    int TotalPoiVisited,
    int TotalAudioListened);

// Geofence stats
public sealed record GeofenceStat(
    Guid PoiId,
    string PoiName,
    int EnterCount,
    int ExitCount,
    int DwellCount);

// Daily aggregated stats
public sealed record DailyStat(
    DateTime Date,
    int TotalVisits,
    int TotalListenings,
    int TotalListenDurationSeconds,
    int UniqueVisitors,
    int NewVisitors,
    int TourViews,
    int GeofenceEvents);

// Usage summary for dashboard
public sealed record UsageSummary(
    int TotalVisits,
    int TotalListenings,
    int TotalListenDurationSeconds,
    int UniqueVisitors,
    int NewVisitors,
    int ActivePois,
    int TourViews,
    int GeofenceEvents,
    IEnumerable<DailyStat> DailyBreakdown,
    IEnumerable<PoiVisitStat> TopPoisByVisit,
    IEnumerable<PoiListeningStat> TopPoisByListening,
    IEnumerable<TourViewStat> TopTours);

// Route & heatmap
public sealed record UserRoutePoint(
    DateTime RecordedAtUtc,
    double Latitude,
    double Longitude,
    string Source);

public sealed record HeatmapCell(
    double CellLatitude,
    double CellLongitude,
    int PointCount);
