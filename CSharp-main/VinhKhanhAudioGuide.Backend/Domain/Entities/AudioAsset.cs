namespace VinhKhanhAudioGuide.Backend.Domain.Entities;

public sealed class AudioAsset
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PoiId { get; set; }
    public required string LanguageCode { get; set; }
    public required string FilePath { get; set; }
    public int DurationSeconds { get; set; }
    public bool IsTextToSpeech { get; set; }

    public Poi? Poi { get; set; }
}
