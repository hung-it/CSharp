namespace VinhKhanhAudioGuide.Backend.Domain.Entities;

public sealed class ShopAudioAsset
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ShopId { get; set; }
    public Guid PoiId { get; set; }
    public required string LanguageCode { get; set; }
    public required string FilePath { get; set; }
    public int DurationSeconds { get; set; }
    public AudioSourceType SourceType { get; set; } = AudioSourceType.Uploaded;
    public bool IsTextToSpeech { get; set; }
    public string? TTSProvider { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ShopProfile? Shop { get; set; }
    public Poi? Poi { get; set; }
}

public enum AudioSourceType
{
    Uploaded = 1,
    TextToSpeech = 2,
    External = 3
}

public sealed class ShopTTSConfiguration
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ShopId { get; set; }
    public required string LanguageCode { get; set; }
    public required string TTSProvider { get; set; }
    public string? VoiceId { get; set; }
    public float SpeakingRate { get; set; } = 1.0f;
    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ShopProfile? Shop { get; set; }
}
