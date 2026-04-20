namespace VinhKhanhAudioGuide.Backend.Domain.Entities;

public sealed class FeatureSegment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }

    public ICollection<UserEntitlement> Entitlements { get; set; } = new List<UserEntitlement>();
}
