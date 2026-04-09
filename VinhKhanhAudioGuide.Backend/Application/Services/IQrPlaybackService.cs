using VinhKhanhAudioGuide.Backend.Domain.Entities;

namespace VinhKhanhAudioGuide.Backend.Application.Services;

public interface IQrPlaybackService
{
    Task<QrPlaybackContent> ResolvePlaybackContentAsync(
        string qrPayload,
        string languageCode = "vi",
        CancellationToken cancellationToken = default);

    Task<QrPlaybackSessionResult> StartSessionByQrAsync(
        Guid userId,
        string qrPayload,
        string languageCode = "vi",
        CancellationToken cancellationToken = default);
}

public sealed record QrPlaybackContent(
    Guid PoiId,
    string PoiCode,
    string PoiName,
    string AudioPath,
    bool IsTextToSpeech);

public sealed record QrPlaybackSessionResult(
    ListeningSession Session,
    QrPlaybackContent Content);
