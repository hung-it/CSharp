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

        var audioPath = asset.FilePath ?? string.Empty;

        if (audioPath.StartsWith("http://") || audioPath.StartsWith("https://"))
        {
            var slashMedia = audioPath.IndexOf("/media/");
            audioPath = slashMedia >= 0
                ? audioPath[(slashMedia + 1)..]
                : audioPath;
        }

        if (audioPath.StartsWith("/media/"))
            audioPath = audioPath["/media/".Length..];
        else if (audioPath.StartsWith("/audio/"))
            audioPath = audioPath["/audio/".Length..];

        return new QrPlaybackContent(
            poi.Id,
            poi.Code,
            poi.Name,
            audioPath,
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

        // Handle "QR:" prefix (e.g., "QR:POI001")
        if (payload.StartsWith("QR:", StringComparison.OrdinalIgnoreCase))
        {
            return payload[3..].Trim();
        }

        // Handle "vk://poi/" prefix (e.g., "vk://poi/POI001")
        if (payload.StartsWith("vk://poi/", StringComparison.OrdinalIgnoreCase))
        {
            return payload["vk://poi/".Length..].Trim();
        }

        // Handle "vk://poi" prefix (e.g., "vk://poi/POI001")
        if (payload.StartsWith("vk://poi", StringComparison.OrdinalIgnoreCase))
        {
            var afterPrefix = payload["vk://poi".Length..];
            return afterPrefix.TrimStart('/').Trim();
        }

        // Return as-is if no known prefix
        return payload;
    }

    private static void EnsureLocalAudioFileIfApplicable(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new FileNotFoundException("Audio path is empty.");
        }

        // If it's a full URL (http/https), skip validation
        if (Uri.TryCreate(filePath, UriKind.Absolute, out var uri))
        {
            if (string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }

        // Convert path based on runtime environment
        string fullPath;
        if (filePath.StartsWith("/media/audio/", StringComparison.Ordinal) ||
            filePath.StartsWith("\\media\\audio\\", StringComparison.OrdinalIgnoreCase) ||
            filePath.StartsWith("/audio/", StringComparison.Ordinal) ||
            filePath.StartsWith("\\audio\\", StringComparison.OrdinalIgnoreCase))
        {
            var basePath = Path.Combine(AppContext.BaseDirectory, "Data", "uploads");
            var relativePath = filePath.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString());
            fullPath = Path.Combine(basePath, relativePath);
        }
        else if (filePath.StartsWith("/") || filePath.StartsWith("\\"))
        {
            // Unix-style absolute path on Windows - convert
            fullPath = filePath.TrimStart('/');
        }
        else if (filePath.Length >= 2 && filePath[1] == ':')
        {
            // Windows absolute path (C:\...)
            fullPath = filePath;
        }
        else
        {
            // Relative path
            fullPath = Path.GetFullPath(filePath);
        }

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Audio file '{filePath}' not found at '{fullPath}'.", fullPath);
        }
    }
}
