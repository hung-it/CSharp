using VinhKhanhAudioGuide.Backend.Domain.Enums;

namespace VinhKhanhAudioGuide.Backend.Domain.Entities;

public sealed class ListeningSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid PoiId { get; set; }
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAtUtc { get; set; }
    public int? DurationSeconds { get; set; }
    public TriggerSource TriggerSource { get; set; } = TriggerSource.QrCode;

    public User? User { get; set; }
    public Poi? Poi { get; set; }
}
