using VinhKhanhAudioGuide.Backend.Domain.Enums;

namespace VinhKhanhAudioGuide.Backend.Domain.Entities;

public sealed class Subscription
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public PlanTier PlanTier { get; set; }
    public decimal AmountUsd { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime ActivatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAtUtc { get; set; }

    public User? User { get; set; }
}
