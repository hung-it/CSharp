using Microsoft.EntityFrameworkCore;
using VinhKhanhAudioGuide.Backend.Domain.Entities;
using VinhKhanhAudioGuide.Backend.Persistence;

namespace VinhKhanhAudioGuide.Backend.Application.Services;

public interface IShopAnalyticsService
{
    // Analytics
    Task<ShopAnalyticsSnapshot?> GetDailyAnalyticsAsync(Guid shopId, Guid poiId, DateTime dateUtc, CancellationToken cancellationToken = default);
    Task<IEnumerable<ShopAnalyticsSnapshot>> GetAnalyticsByPeriodAsync(Guid shopId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<IEnumerable<ShopAnalyticsSnapshot>> GetTopPoiAsync(Guid shopId, int limit, CancellationToken cancellationToken = default);
    Task RecordListenEventAsync(Guid shopId, Guid poiId, int durationSeconds, CancellationToken cancellationToken = default);
    Task RecordQRScanAsync(Guid shopId, Guid poiId, CancellationToken cancellationToken = default);
    
    // Language Readiness
    Task<ShopLanguageReadiness?> GetLanguageReadinessAsync(Guid shopId, string languageCode, CancellationToken cancellationToken = default);
    Task<IEnumerable<ShopLanguageReadiness>> GetAllLanguageReadinessAsync(Guid shopId, CancellationToken cancellationToken = default);
    Task UpdateLanguageReadinessAsync(Guid shopId, CancellationToken cancellationToken = default);
}

public sealed class ShopAnalyticsService : IShopAnalyticsService
{
    private readonly AudioGuideDbContext _dbContext;

    public ShopAnalyticsService(AudioGuideDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ShopAnalyticsSnapshot?> GetDailyAnalyticsAsync(Guid shopId, Guid poiId, DateTime dateUtc, CancellationToken cancellationToken = default)
    {
        var dateOnly = dateUtc.Date;
        return await _dbContext.ShopAnalyticsSnapshots
            .FirstOrDefaultAsync(a => a.ShopId == shopId && a.PoiId == poiId && a.DateUtc.Date == dateOnly, cancellationToken);
    }

    public async Task<IEnumerable<ShopAnalyticsSnapshot>> GetAnalyticsByPeriodAsync(Guid shopId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ShopAnalyticsSnapshots
            .Where(a => a.ShopId == shopId && a.DateUtc >= startDate && a.DateUtc <= endDate)
            .OrderByDescending(a => a.DateUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ShopAnalyticsSnapshot>> GetTopPoiAsync(Guid shopId, int limit, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ShopAnalyticsSnapshots
            .Where(a => a.ShopId == shopId)
            .GroupBy(a => a.PoiId)
            .Select(g => new ShopAnalyticsSnapshot
            {
                ShopId = shopId,
                PoiId = g.Key,
                ListenCount = g.Sum(a => a.ListenCount),
                QRScanCount = g.Sum(a => a.QRScanCount),
                AverageListeningDurationSeconds = (int)g.Average(a => a.AverageListeningDurationSeconds),
                UniqueListenersCount = g.Sum(a => a.UniqueListenersCount)
            })
            .OrderByDescending(a => a.ListenCount)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task RecordListenEventAsync(Guid shopId, Guid poiId, int durationSeconds, CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        var snapshot = await _dbContext.ShopAnalyticsSnapshots
            .FirstOrDefaultAsync(a => a.ShopId == shopId && a.PoiId == poiId && a.DateUtc.Date == today, cancellationToken);

        if (snapshot is null)
        {
            snapshot = new ShopAnalyticsSnapshot
            {
                ShopId = shopId,
                PoiId = poiId,
                DateUtc = today,
                ListenCount = 1,
                AverageListeningDurationSeconds = durationSeconds,
                UniqueListenersCount = 1
            };
            _dbContext.ShopAnalyticsSnapshots.Add(snapshot);
        }
        else
        {
            snapshot.ListenCount++;
            snapshot.AverageListeningDurationSeconds = (snapshot.AverageListeningDurationSeconds + durationSeconds) / 2;
            snapshot.UniqueListenersCount++;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RecordQRScanAsync(Guid shopId, Guid poiId, CancellationToken cancellationToken = default)
    {
        var qrCode = await _dbContext.ShopQRCodes
            .FirstOrDefaultAsync(q => q.ShopId == shopId && q.PoiId == poiId, cancellationToken);

        if (qrCode is not null)
        {
            qrCode.ScanCount++;
            qrCode.LastScannedAtUtc = DateTime.UtcNow;
        }

        var today = DateTime.UtcNow.Date;
        var snapshot = await _dbContext.ShopAnalyticsSnapshots
            .FirstOrDefaultAsync(a => a.ShopId == shopId && a.PoiId == poiId && a.DateUtc.Date == today, cancellationToken);

        if (snapshot is null)
        {
            snapshot = new ShopAnalyticsSnapshot
            {
                ShopId = shopId,
                PoiId = poiId,
                DateUtc = today,
                QRScanCount = 1
            };
            _dbContext.ShopAnalyticsSnapshots.Add(snapshot);
        }
        else
        {
            snapshot.QRScanCount++;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<ShopLanguageReadiness?> GetLanguageReadinessAsync(Guid shopId, string languageCode, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ShopLanguageReadiness
            .FirstOrDefaultAsync(r => r.ShopId == shopId && r.LanguageCode == languageCode, cancellationToken);
    }

    public async Task<IEnumerable<ShopLanguageReadiness>> GetAllLanguageReadinessAsync(Guid shopId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ShopLanguageReadiness
            .Where(r => r.ShopId == shopId)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateLanguageReadinessAsync(Guid shopId, CancellationToken cancellationToken = default)
    {
        var pois = await _dbContext.Pois
            .Where(p => p.ShopId == shopId)
            .ToListAsync(cancellationToken);

        if (pois.Count == 0)
        {
            return;
        }

        var languages = await _dbContext.ShopContentTranslations
            .Where(t => t.Content!.ShopId == shopId)
            .Select(t => t.LanguageCode)
            .Distinct()
            .ToListAsync(cancellationToken);

        foreach (var lang in languages)
        {
            var existingReadiness = await _dbContext.ShopLanguageReadiness
                .FirstOrDefaultAsync(r => r.ShopId == shopId && r.LanguageCode == lang, cancellationToken);

            var poiWithTranslation = await _dbContext.ShopContentTranslations
                .Where(t => t.LanguageCode == lang && t.Content!.ShopId == shopId)
                .Select(t => t.Content!.PoiId)
                .Distinct()
                .CountAsync(cancellationToken);

            var poiWithAudio = await _dbContext.ShopAudioAssets
                .Where(a => a.LanguageCode == lang && a.ShopId == shopId)
                .Select(a => a.PoiId)
                .Distinct()
                .CountAsync(cancellationToken);

            var readinessPercentage = pois.Count > 0 
                ? Math.Min(poiWithTranslation, poiWithAudio) * 100.0 / pois.Count 
                : 0;

            if (existingReadiness is null)
            {
                var readiness = new ShopLanguageReadiness
                {
                    ShopId = shopId,
                    LanguageCode = lang,
                    TotalPois = pois.Count,
                    PoiWithTranslation = poiWithTranslation,
                    PoiWithAudio = poiWithAudio,
                    ReadinessPercentage = readinessPercentage
                };
                _dbContext.ShopLanguageReadiness.Add(readiness);
            }
            else
            {
                existingReadiness.TotalPois = pois.Count;
                existingReadiness.PoiWithTranslation = poiWithTranslation;
                existingReadiness.PoiWithAudio = poiWithAudio;
                existingReadiness.ReadinessPercentage = readinessPercentage;
                existingReadiness.LastUpdatedAtUtc = DateTime.UtcNow;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
