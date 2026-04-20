namespace VinhKhanhAudioGuide.Backend.Domain.Entities;

public sealed class ShopProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ManagerUserId { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string? Address { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? MapLink { get; set; }
    public string? OpeningHours { get; set; }
    public string? AvatarUrl { get; set; }
    public string? CoverImageUrl { get; set; }
    public ShopVerificationStatus VerificationStatus { get; set; } = ShopVerificationStatus.Pending;
    public string? VerificationReason { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ApprovedAtUtc { get; set; }

    public User? ManagerUser { get; set; }
    public ICollection<Poi> Pois { get; set; } = new List<Poi>();
    public ICollection<Tour> Tours { get; set; } = new List<Tour>();
    public ICollection<ShopContent> Contents { get; set; } = new List<ShopContent>();
}

public enum ShopVerificationStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3,
    Suspended = 4
}
