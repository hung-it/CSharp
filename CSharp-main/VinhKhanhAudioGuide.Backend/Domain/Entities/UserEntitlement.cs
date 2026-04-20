namespace VinhKhanhAudioGuide.Backend.Domain.Entities;

public sealed class UserEntitlement
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid FeatureSegmentId { get; set; }
    public DateTime GrantedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? RevokedAtUtc { get; set; }

    public User? User { get; set; }
    public FeatureSegment? FeatureSegment { get; set; }
}
