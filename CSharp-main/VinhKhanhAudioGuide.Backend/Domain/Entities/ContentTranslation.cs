namespace VinhKhanhAudioGuide.Backend.Domain.Entities;

public sealed class ContentTranslation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string ContentKey { get; set; }
    public required string LanguageCode { get; set; }
    public required string Value { get; set; }
}
