using Microsoft.EntityFrameworkCore;
using VinhKhanhAudioGuide.Backend.Domain.Entities;
using VinhKhanhAudioGuide.Backend.Persistence;

namespace VinhKhanhAudioGuide.Backend.Application.Services;

public sealed class ContentSyncService(AudioGuideDbContext dbContext) : IContentSyncService
{
    private readonly AudioGuideDbContext _dbContext = dbContext;

    public async Task<ContentSyncResult> SyncFromSnapshotAsync(
        ContentSyncSnapshot snapshot,
        CancellationToken cancellationToken = default)
    {
        var inserted = 0;
        var updated = 0;
        var skipped = 0;

        foreach (var item in snapshot.Pois)
        {
            var poi = await _dbContext.Pois.FirstOrDefaultAsync(x => x.Code == item.Code, cancellationToken);
            if (poi is null)
            {
                _dbContext.Pois.Add(new Poi
                {
                    Code = item.Code,
                    Name = item.Name,
                    Latitude = item.Latitude,
                    Longitude = item.Longitude,
                    TriggerRadiusMeters = item.TriggerRadiusMeters,
                    Description = item.Description,
                    District = item.District
                });
                inserted++;
            }
            else
            {
                poi.Name = item.Name;
                poi.Latitude = item.Latitude;
                poi.Longitude = item.Longitude;
                poi.TriggerRadiusMeters = item.TriggerRadiusMeters;
                poi.Description = item.Description;
                poi.District = item.District;
                updated++;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var poiMap = await _dbContext.Pois.ToDictionaryAsync(x => x.Code, cancellationToken);

        foreach (var item in snapshot.Audios)
        {
            if (!poiMap.TryGetValue(item.PoiCode, out var poi))
            {
                skipped++;
                continue;
            }

            var audio = await _dbContext.AudioAssets.FirstOrDefaultAsync(
                x => x.PoiId == poi.Id && x.LanguageCode == item.LanguageCode,
                cancellationToken);

            if (audio is null)
            {
                _dbContext.AudioAssets.Add(new AudioAsset
                {
                    PoiId = poi.Id,
                    LanguageCode = item.LanguageCode,
                    FilePath = item.FilePath,
                    DurationSeconds = item.DurationSeconds,
                    IsTextToSpeech = item.IsTextToSpeech
                });
                inserted++;
            }
            else
            {
                audio.FilePath = item.FilePath;
                audio.DurationSeconds = item.DurationSeconds;
                audio.IsTextToSpeech = item.IsTextToSpeech;
                updated++;
            }
        }

        foreach (var item in snapshot.Translations)
        {
            var translation = await _dbContext.ContentTranslations.FirstOrDefaultAsync(
                x => x.ContentKey == item.ContentKey && x.LanguageCode == item.LanguageCode,
                cancellationToken);

            if (translation is null)
            {
                _dbContext.ContentTranslations.Add(new ContentTranslation
                {
                    ContentKey = item.ContentKey,
                    LanguageCode = item.LanguageCode,
                    Value = item.Value
                });
                inserted++;
            }
            else
            {
                translation.Value = item.Value;
                updated++;
            }
        }

        foreach (var item in snapshot.Tours)
        {
            var tour = await _dbContext.Tours.FirstOrDefaultAsync(x => x.Code == item.Code, cancellationToken);
            if (tour is null)
            {
                _dbContext.Tours.Add(new Tour
                {
                    Code = item.Code,
                    Name = item.Name,
                    Description = item.Description
                });
                inserted++;
            }
            else
            {
                tour.Name = item.Name;
                tour.Description = item.Description;
                updated++;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var tourMap = await _dbContext.Tours.ToDictionaryAsync(x => x.Code, cancellationToken);

        foreach (var item in snapshot.TourStops)
        {
            if (!tourMap.TryGetValue(item.TourCode, out var tour) || !poiMap.TryGetValue(item.PoiCode, out var poi))
            {
                skipped++;
                continue;
            }

            var stop = await _dbContext.TourStops.FirstOrDefaultAsync(
                x => x.TourId == tour.Id && x.Sequence == item.Sequence,
                cancellationToken);

            if (stop is null)
            {
                _dbContext.TourStops.Add(new TourStop
                {
                    TourId = tour.Id,
                    PoiId = poi.Id,
                    Sequence = item.Sequence,
                    NextStopHint = item.NextStopHint
                });
                inserted++;
            }
            else
            {
                stop.PoiId = poi.Id;
                stop.NextStopHint = item.NextStopHint;
                updated++;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return new ContentSyncResult(inserted, updated, skipped);
    }
}
