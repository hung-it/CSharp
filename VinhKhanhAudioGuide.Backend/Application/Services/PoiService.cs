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

    public async Task<Poi> CreatePoiAsync(string code, string name, double latitude, double longitude, double triggerRadiusMeters = 30, string? description = null, string? district = null, CancellationToken cancellationToken = default)
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
            District = district
        };

        _dbContext.Pois.Add(poi);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return poi;
    }

    public async Task<Poi> UpdatePoiAsync(Guid poiId, string? name = null, string? description = null, double? triggerRadiusMeters = null, CancellationToken cancellationToken = default)
    {
        var poi = await _dbContext.Pois.FindAsync(new object[] { poiId }, cancellationToken: cancellationToken);
        if (poi is null)
        {
            throw new KeyNotFoundException($"POI with ID {poiId} not found.");
        }

        if (name is not null)
            poi.Name = name;
        if (description is not null)
            poi.Description = description;
        if (triggerRadiusMeters.HasValue)
            poi.TriggerRadiusMeters = triggerRadiusMeters.Value;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return poi;
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
}
