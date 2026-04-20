namespace VinhKhanhAudioGuide.Backend.Domain.Entities;

public sealed class Tour
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public Guid? ShopId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ShopProfile? Shop { get; set; }
    public ICollection<TourStop> Stops { get; set; } = new List<TourStop>();
}
