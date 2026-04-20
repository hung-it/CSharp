namespace VinhKhanhAudioGuide.Backend.Application.Services;

public interface IContentSyncService
{
    Task<ContentSyncResult> SyncFromSnapshotAsync(
        ContentSyncSnapshot snapshot,
        CancellationToken cancellationToken = default);
}

public sealed class ContentSyncSnapshot
{
    public List<PoiSyncItem> Pois { get; set; } = [];
    public List<AudioSyncItem> Audios { get; set; } = [];
    public List<TranslationSyncItem> Translations { get; set; } = [];
    public List<TourSyncItem> Tours { get; set; } = [];
    public List<TourStopSyncItem> TourStops { get; set; } = [];
}

public sealed record PoiSyncItem(
    string Code,
    string Name,
    double Latitude,
    double Longitude,
    double TriggerRadiusMeters,
    string? Description,
    string? District);

public sealed record AudioSyncItem(
    string PoiCode,
    string LanguageCode,
    string FilePath,
    int DurationSeconds,
    bool IsTextToSpeech);

public sealed record TranslationSyncItem(
    string ContentKey,
    string LanguageCode,
    string Value);

public sealed record TourSyncItem(
    string Code,
    string Name,
    string? Description);

public sealed record TourStopSyncItem(
    string TourCode,
    string PoiCode,
    int Sequence,
    string? NextStopHint);

public sealed record ContentSyncResult(
    int Inserted,
    int Updated,
    int Skipped);
