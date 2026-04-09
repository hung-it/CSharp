namespace VinhKhanhAudioGuide.Backend.Domain.Entities;

public sealed class TourStop
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TourId { get; set; }
    public Guid PoiId { get; set; }
    public int Sequence { get; set; }
    public string? NextStopHint { get; set; }

    public Tour? Tour { get; set; }
    public Poi? Poi { get; set; }
}
