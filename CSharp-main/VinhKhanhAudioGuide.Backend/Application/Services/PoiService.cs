using Microsoft.EntityFrameworkCore;
using VinhKhanhAudioGuide.Backend.Domain.Entities;
using VinhKhanhAudioGuide.Backend.Persistence;

namespace VinhKhanhAudioGuide.Backend.Application.Services;

public sealed class PoiService(AudioGuideDbContext dbContext) : IPoiService
{
    private readonly AudioGuideDbContext _dbContext = dbContext;

    public async Task<IEnumerable<Poi>> GetAllPoiAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Pois
            .Include(p => p.AudioAssets)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Poi?> GetPoiByIdAsync(Guid poiId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Pois
            .Include(p => p.AudioAssets)
            .FirstOrDefaultAsync(p => p.Id == poiId, cancellationToken);
    }

    public async Task<Poi?> GetPoiByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Pois
            .Include(p => p.AudioAssets)
            .FirstOrDefaultAsync(p => p.Code == code, cancellationToken);
    }

    public async Task<IEnumerable<Poi>> GetPoisByDistrictAsync(string district, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Pois
            .Include(p => p.AudioAssets)
            .Where(p => p.District == district)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Poi> CreatePoiAsync(
        string code,
        string name,
        double latitude,
        double longitude,
        double triggerRadiusMeters = 30,
        string? description = null,
        string? district = null,
        int priority = 0,
        string? imageUrl = null,
        string? mapLink = null,
        CancellationToken cancellationToken = default)
    {
        var existingPoi = await GetPoiByCodeAsync(code, cancellationToken);
        if (existingPoi is not null)
        {
            throw new InvalidOperationException($"POI with code '{code}' already exists.");
        }

        var poi = new Poi
        {
            Code = code,
            Name = name,
            Description = description,
            Latitude = latitude,
            Longitude = longitude,
            TriggerRadiusMeters = triggerRadiusMeters,
            District = district,
            Priority = priority,
            ImageUrl = imageUrl,
            MapLink = mapLink
        };

        _dbContext.Pois.Add(poi);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return poi;
    }

    public async Task<Poi> UpdatePoiAsync(
        Guid poiId,
        string? code = null,
        string? name = null,
        string? description = null,
        double? latitude = null,
        double? longitude = null,
        double? triggerRadiusMeters = null,
        string? district = null,
        int? priority = null,
        string? imageUrl = null,
        string? mapLink = null,
        CancellationToken cancellationToken = default)
    {
        var poi = await _dbContext.Pois.FindAsync(new object[] { poiId }, cancellationToken: cancellationToken);
        if (poi is null)
        {
            throw new KeyNotFoundException($"POI with ID {poiId} not found.");
        }

        if (!string.IsNullOrWhiteSpace(code) && !string.Equals(poi.Code, code, StringComparison.OrdinalIgnoreCase))
        {
            var codeExists = await _dbContext.Pois.AnyAsync(x => x.Code == code && x.Id != poiId, cancellationToken);
            if (codeExists)
            {
                throw new InvalidOperationException($"POI with code '{code}' already exists.");
            }

            poi.Code = code;
        }

        if (name is not null)
            poi.Name = name;
        if (description is not null)
            poi.Description = description;
        if (latitude.HasValue)
            poi.Latitude = latitude.Value;
        if (longitude.HasValue)
            poi.Longitude = longitude.Value;
        if (triggerRadiusMeters.HasValue)
            poi.TriggerRadiusMeters = triggerRadiusMeters.Value;
        if (district is not null)
            poi.District = district;
        if (priority.HasValue)
            poi.Priority = priority.Value;
        if (imageUrl is not null)
            poi.ImageUrl = imageUrl;
        if (mapLink is not null)
            poi.MapLink = mapLink;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return poi;
    }

    public async Task DeletePoiAsync(Guid poiId, CancellationToken cancellationToken = default)
    {
        var poi = await _dbContext.Pois.FindAsync(new object[] { poiId }, cancellationToken: cancellationToken);
        if (poi is null)
        {
            return;
        }

        var hasSessions = await _dbContext.ListeningSessions
            .AnyAsync(x => x.PoiId == poiId, cancellationToken);

        if (hasSessions)
        {
            throw new InvalidOperationException("Không thể xóa POI đã phát sinh lịch sử nghe.");
        }

        var hasTourStops = await _dbContext.TourStops
            .AnyAsync(x => x.PoiId == poiId, cancellationToken);

        if (hasTourStops)
        {
            throw new InvalidOperationException("Không thể xóa POI đang được dùng trong tour.");
        }

        var audios = await _dbContext.AudioAssets
            .Where(x => x.PoiId == poiId)
            .ToListAsync(cancellationToken);

        _dbContext.AudioAssets.RemoveRange(audios);
        _dbContext.Pois.Remove(poi);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AssignAudioAsync(Guid poiId, string languageCode, string filePath, int durationSeconds, bool isTextToSpeech = false, CancellationToken cancellationToken = default)
    {
        var poi = await _dbContext.Pois.FindAsync(new object[] { poiId }, cancellationToken: cancellationToken);
        if (poi is null)
        {
            throw new KeyNotFoundException($"POI with ID {poiId} not found.");
        }

        var existingAudio = await _dbContext.AudioAssets
            .FirstOrDefaultAsync(a => a.PoiId == poiId && a.LanguageCode == languageCode, cancellationToken);

        if (existingAudio is not null)
        {
            existingAudio.FilePath = filePath;
            existingAudio.DurationSeconds = durationSeconds;
            existingAudio.IsTextToSpeech = isTextToSpeech;
        }
        else
        {
            var audio = new AudioAsset
            {
                PoiId = poiId,
                LanguageCode = languageCode,
                FilePath = filePath,
                DurationSeconds = durationSeconds,
                IsTextToSpeech = isTextToSpeech
            };
            _dbContext.AudioAssets.Add(audio);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IEnumerable<AudioAsset>> GetAudiosByPoiAsync(Guid poiId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.AudioAssets
            .Where(a => a.PoiId == poiId)
            .ToListAsync(cancellationToken);
    }

    public async Task<AudioAsset?> GetAudioByLanguageAsync(Guid poiId, string languageCode, CancellationToken cancellationToken = default)
    {
        return await _dbContext.AudioAssets
            .FirstOrDefaultAsync(a => a.PoiId == poiId && a.LanguageCode == languageCode, cancellationToken);
    }

    public async Task<AudioAsset> UpdateAudioAsync(
        Guid poiId,
        Guid audioId,
        string languageCode,
        string filePath,
        int durationSeconds,
        bool isTextToSpeech = false,
        CancellationToken cancellationToken = default)
    {
        var poiExists = await _dbContext.Pois.AnyAsync(x => x.Id == poiId, cancellationToken);
        if (!poiExists)
        {
            throw new KeyNotFoundException($"POI with ID {poiId} not found.");
        }

        var audio = await _dbContext.AudioAssets
            .FirstOrDefaultAsync(x => x.Id == audioId && x.PoiId == poiId, cancellationToken);

        if (audio is null)
        {
            throw new KeyNotFoundException($"Audio asset with ID {audioId} not found in POI {poiId}.");
        }

        var languageConflict = await _dbContext.AudioAssets.AnyAsync(
            x => x.PoiId == poiId && x.LanguageCode == languageCode && x.Id != audioId,
            cancellationToken);

        if (languageConflict)
        {
            throw new InvalidOperationException($"POI {poiId} already has audio for language '{languageCode}'.");
        }

        audio.LanguageCode = languageCode;
        audio.FilePath = filePath;
        audio.DurationSeconds = durationSeconds;
        audio.IsTextToSpeech = isTextToSpeech;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return audio;
    }

    public async Task DeleteAudioAsync(Guid poiId, Guid audioId, CancellationToken cancellationToken = default)
    {
        var audio = await _dbContext.AudioAssets
            .FirstOrDefaultAsync(x => x.Id == audioId && x.PoiId == poiId, cancellationToken);

        if (audio is null)
        {
            return;
        }

        _dbContext.AudioAssets.Remove(audio);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
