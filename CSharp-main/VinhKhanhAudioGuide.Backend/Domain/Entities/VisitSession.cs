using VinhKhanhAudioGuide.Backend.Domain.Enums;

namespace VinhKhanhAudioGuide.Backend.Domain.Entities;

public sealed class VisitSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid PoiId { get; set; }
    public DateTime VisitedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LeftAtUtc { get; set; }
    public int DurationSeconds { get; set; } = 0;
    public VisitTriggerSource TriggerSource { get; set; } = VisitTriggerSource.Map;
    public PageSource PageSource { get; set; } = PageSource.Map;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public bool ListenedToAudio { get; set; } = false;
    public int? ListeningSessionCount { get; set; } = 0;
    public int? TotalListenDurationSeconds { get; set; } = 0;
    public string? AnonymousRef { get; set; }

    public User? User { get; set; }
    public Poi? Poi { get; set; }
}
