namespace VinhKhanhAudioGuide.Backend.Domain.Entities;

public sealed class Poi
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double TriggerRadiusMeters { get; set; } = 30;
    public int Priority { get; set; } = 0;
    public string? District { get; set; }
    public string? ImageUrl { get; set; }
    public string? MapLink { get; set; }
    public Guid? ShopId { get; set; }
    public Guid? ManagerUserId { get; set; }

    public ShopProfile? Shop { get; set; }
    public User? ManagerUser { get; set; }
    public ICollection<AudioAsset> AudioAssets { get; set; } = new List<AudioAsset>();
    public ICollection<TourStop> TourStops { get; set; } = new List<TourStop>();
    public ICollection<ListeningSession> ListeningSessions { get; set; } = new List<ListeningSession>();
    public ICollection<VisitSession> VisitSessions { get; set; } = new List<VisitSession>();
    public ICollection<PoiGeofenceEvent> GeofenceEvents { get; set; } = new List<PoiGeofenceEvent>();
}
