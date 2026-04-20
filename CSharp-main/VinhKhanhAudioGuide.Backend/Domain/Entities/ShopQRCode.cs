namespace VinhKhanhAudioGuide.Backend.Domain.Entities;

public sealed class ShopQRCode
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ShopId { get; set; }
    public Guid PoiId { get; set; }
    public required string QRPayload { get; set; }
    public string? QRImageUrl { get; set; }
    public int ScanCount { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LastScannedAtUtc { get; set; }

    public ShopProfile? Shop { get; set; }
    public Poi? Poi { get; set; }
}
