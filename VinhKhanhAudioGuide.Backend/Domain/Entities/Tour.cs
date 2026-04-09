namespace VinhKhanhAudioGuide.Backend.Domain.Entities;

public sealed class Tour
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }

    public ICollection<TourStop> Stops { get; set; } = new List<TourStop>();
}
