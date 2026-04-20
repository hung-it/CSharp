namespace VinhKhanhAudioGuide.Backend.Domain.Entities;

public sealed class ShopAnalyticsSnapshot
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ShopId { get; set; }
    public Guid PoiId { get; set; }
    public DateTime DateUtc { get; set; }
    public int ListenCount { get; set; } = 0;
    public int QRScanCount { get; set; } = 0;
    public int AverageListeningDurationSeconds { get; set; } = 0;
    public int UniqueListenersCount { get; set; } = 0;

    public ShopProfile? Shop { get; set; }
    public Poi? Poi { get; set; }
}

public sealed class ShopLanguageReadiness
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ShopId { get; set; }
    public required string LanguageCode { get; set; }
    public int TotalPois { get; set; } = 0;
    public int PoiWithTranslation { get; set; } = 0;
    public int PoiWithAudio { get; set; } = 0;
    public double ReadinessPercentage { get; set; } = 0;
    public DateTime LastUpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public ShopProfile? Shop { get; set; }
}
