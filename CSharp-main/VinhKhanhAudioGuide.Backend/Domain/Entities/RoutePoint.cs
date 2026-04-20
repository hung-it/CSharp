namespace VinhKhanhAudioGuide.Backend.Domain.Entities;

public sealed class RoutePoint
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public DateTime RecordedAtUtc { get; set; } = DateTime.UtcNow;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string Source { get; set; } = "gps";

    public User? User { get; set; }
}
