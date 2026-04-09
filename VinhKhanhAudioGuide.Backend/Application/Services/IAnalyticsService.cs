namespace VinhKhanhAudioGuide.Backend.Application.Services;

public interface IAnalyticsService
{
    Task<IEnumerable<PoiListeningStat>> GetTopPoisByListeningCountAsync(
        int limit = 5,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<PoiListeningStat>> GetPoiListeningStatsAsync(
        CancellationToken cancellationToken = default);

    Task<IEnumerable<UserRoutePoint>> GetUserRouteAsync(
        Guid userId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<HeatmapCell>> GetHeatmapDataAsync(
        DateTime startDate,
        DateTime endDate,
        int precision = 3,
        CancellationToken cancellationToken = default);
}

public sealed record PoiListeningStat(
    Guid PoiId,
    string PoiCode,
    string PoiName,
    int ListeningCount,
    int TotalDurationSeconds,
    double AverageDurationSeconds);

public sealed record UserRoutePoint(
    DateTime RecordedAtUtc,
    double Latitude,
    double Longitude,
    string Source);

public sealed record HeatmapCell(
    double CellLatitude,
    double CellLongitude,
    int PointCount);
