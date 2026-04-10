using Microsoft.EntityFrameworkCore;
using VinhKhanhAudioGuide.Backend.Domain.Enums;
using VinhKhanhAudioGuide.Backend.Persistence;

namespace VinhKhanhAudioGuide.Backend.Application.Services;

public sealed class QrPlaybackService(
    AudioGuideDbContext dbContext,
    IListeningSessionService listeningSessionService,
    ISubscriptionService subscriptionService) : IQrPlaybackService
{
    private readonly AudioGuideDbContext _dbContext = dbContext;
    private readonly IListeningSessionService _listeningSessionService = listeningSessionService;
    private readonly ISubscriptionService _subscriptionService = subscriptionService;

    public async Task<QrPlaybackContent> ResolvePlaybackContentAsync(
        string qrPayload,
        string languageCode = "vi",
        CancellationToken cancellationToken = default)
    {
        var poiCode = ParsePoiCode(qrPayload);

        var poi = await _dbContext.Pois
            .FirstOrDefaultAsync(x => x.Code == poiCode, cancellationToken);

        if (poi is null)
        {
            throw new KeyNotFoundException($"POI with code '{poiCode}' not found for QR payload.");
        }

        var asset = await _dbContext.AudioAssets
            .Where(x => x.PoiId == poi.Id && x.LanguageCode == languageCode)
            .FirstOrDefaultAsync(cancellationToken)
            ?? await _dbContext.AudioAssets
                .Where(x => x.PoiId == poi.Id && x.LanguageCode == "vi")
                .FirstOrDefaultAsync(cancellationToken)
            ?? await _dbContext.AudioAssets
                .Where(x => x.PoiId == poi.Id)
                .OrderBy(x => x.LanguageCode)
                .FirstOrDefaultAsync(cancellationToken);

        if (asset is null)
        {
            throw new KeyNotFoundException($"No audio asset found for POI '{poi.Code}'.");
        }

        EnsureLocalAudioFileIfApplicable(asset.FilePath);

        return new QrPlaybackContent(
            poi.Id,
            poi.Code,
            poi.Name,
            asset.FilePath,
            asset.IsTextToSpeech);
    }

    public async Task<QrPlaybackSessionResult> StartSessionByQrAsync(
        Guid userId,
        string qrPayload,
        string languageCode = "vi",
        CancellationToken cancellationToken = default)
    {
        var hasAccess = await _subscriptionService.HasAccessToSegmentAsync(userId, "basic.poi", cancellationToken);
        if (!hasAccess)
        {
            throw new UnauthorizedAccessException("User does not have access to basic POI content.");
        }

        var content = await ResolvePlaybackContentAsync(qrPayload, languageCode, cancellationToken);

        var session = await _listeningSessionService.StartSessionAsync(
            userId,
            content.PoiId,
            TriggerSource.QrCode,
            cancellationToken);

        return new QrPlaybackSessionResult(session, content);
    }

    private static string ParsePoiCode(string qrPayload)
    {
        if (string.IsNullOrWhiteSpace(qrPayload))
        {
            throw new ArgumentException("QR payload is required.", nameof(qrPayload));
        }

        var payload = qrPayload.Trim();

        if (payload.StartsWith("QR:", StringComparison.OrdinalIgnoreCase))
        {
            return payload[3..].Trim();
        }

        return payload;
    }

    private static void EnsureLocalAudioFileIfApplicable(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new FileNotFoundException("Audio path is empty.");
        }

        if (Uri.TryCreate(filePath, UriKind.Absolute, out var uri))
        {
            if (string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (uri.IsFile)
            {
                var localPath = uri.LocalPath;
                if (!File.Exists(localPath))
                {
                    throw new FileNotFoundException($"Audio file '{localPath}' not found.", localPath);
                }

                return;
            }
        }

        if (Path.IsPathRooted(filePath))
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Audio file '{filePath}' not found.", filePath);
            }

            return;
        }

        // Relative paths can represent app-packaged or deferred-download assets.
        // Validation is deferred to the mobile playback layer after resolution.
    }
}
