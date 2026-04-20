namespace VinhKhanhAudioGuide.Backend.Application.Services;

public interface IGeofenceService
{
    Task<IReadOnlyList<GeofenceEvent>> EvaluateLocationAsync(
        Guid userId,
        double latitude,
        double longitude,
        double nearFactor = 1.5,
        CancellationToken cancellationToken = default);
}

public enum GeofenceEventType
{
    Entered,
    Exited,
    Nearby
}

public sealed record GeofenceEvent(
    Guid PoiId,
    string PoiCode,
    string PoiName,
    GeofenceEventType EventType,
    double DistanceMeters,
    double TriggerRadiusMeters,
    bool ShouldStartNarration);
