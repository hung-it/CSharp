using VinhKhanhAudioGuide.Backend.Domain.Entities;
using VinhKhanhAudioGuide.Backend.Domain.Enums;

namespace VinhKhanhAudioGuide.Backend.Application.Services;

public interface IVisitTrackingService
{
    // POI Visit Sessions
    Task<VisitSession> StartVisitAsync(
        Guid userId,
        Guid poiId,
        VisitTriggerSource triggerSource = VisitTriggerSource.Map,
        PageSource pageSource = PageSource.Map,
        double? latitude = null,
        double? longitude = null,
        string? anonymousRef = null,
        CancellationToken cancellationToken = default);

    Task<VisitSession> EndVisitAsync(
        Guid visitId,
        CancellationToken cancellationToken = default);

    Task<VisitSession> UpdateVisitWithAudioDataAsync(
        Guid visitId,
        int listeningSessionCount,
        int totalListenDurationSeconds,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<VisitSession>> GetVisitsByUserAsync(
        Guid userId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<VisitSession>> GetVisitsByPoiAsync(
        Guid poiId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default);

    // Tour View Sessions
    Task<TourViewSession> StartTourViewAsync(
        Guid userId,
        Guid tourId,
        string? anonymousRef = null,
        CancellationToken cancellationToken = default);

    Task<TourViewSession> EndTourViewAsync(
        Guid tourViewId,
        int poiVisitedCount,
        int audioListenedCount,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<TourViewSession>> GetTourViewsByUserAsync(
        Guid userId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default);

    // Geofence Events
    Task<PoiGeofenceEvent> RecordGeofenceEventAsync(
        Guid userId,
        Guid poiId,
        PoiGeofenceEventType eventType,
        double latitude,
        double longitude,
        double distanceFromCenterMeters,
        string? anonymousRef = null,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<PoiGeofenceEvent>> GetGeofenceEventsByPoiAsync(
        Guid poiId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default);
}
