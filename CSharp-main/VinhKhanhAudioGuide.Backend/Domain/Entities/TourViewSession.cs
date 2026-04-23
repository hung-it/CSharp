using VinhKhanhAudioGuide.Backend.Domain.Enums;

namespace VinhKhanhAudioGuide.Backend.Domain.Entities;

public sealed class TourViewSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid TourId { get; set; }
    public DateTime ViewedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ClosedAtUtc { get; set; }
    public int DurationSeconds { get; set; } = 0;
    public int PoiVisitedCount { get; set; } = 0;
    public int AudioListenedCount { get; set; } = 0;
    public string? AnonymousRef { get; set; }

    public User? User { get; set; }
    public Tour? Tour { get; set; }
}
