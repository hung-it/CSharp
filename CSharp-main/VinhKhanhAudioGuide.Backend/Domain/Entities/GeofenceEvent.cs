using VinhKhanhAudioGuide.Backend.Domain.Enums;

namespace VinhKhanhAudioGuide.Backend.Domain.Entities;

public sealed class PoiGeofenceEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid PoiId { get; set; }
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
    public PoiGeofenceEventType EventType { get; set; } = PoiGeofenceEventType.Enter;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double DistanceFromCenterMeters { get; set; } = 0;
    public string? AnonymousRef { get; set; }

    public User? User { get; set; }
    public Poi? Poi { get; set; }
}
