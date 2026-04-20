using Microsoft.EntityFrameworkCore;
using VinhKhanhAudioGuide.Backend.Domain.Entities;
using VinhKhanhAudioGuide.Backend.Persistence;

namespace VinhKhanhAudioGuide.Backend.Application.Services;

public interface IShopAudioService
{
    // Audio Assets
    Task<ShopAudioAsset> UploadAudioAsync(Guid shopId, Guid poiId, string languageCode, string filePath, int durationSeconds, bool isTextToSpeech = false, string? ttsProvider = null, CancellationToken cancellationToken = default);
    Task<ShopAudioAsset?> GetAudioByIdAsync(Guid audioId, CancellationToken cancellationToken = default);
    Task<ShopAudioAsset?> GetAudioByPoiLanguageAsync(Guid shopId, Guid poiId, string languageCode, CancellationToken cancellationToken = default);
    Task<IEnumerable<ShopAudioAsset>> GetAudioByPoiAsync(Guid shopId, Guid poiId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ShopAudioAsset>> GetAudioByShopAsync(Guid shopId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ShopAudioAsset>> GetAudioByLanguageAsync(Guid shopId, string languageCode, CancellationToken cancellationToken = default);
    Task DeleteAudioAsync(Guid audioId, CancellationToken cancellationToken = default);
    
    // TTS Configuration
    Task<ShopTTSConfiguration> ConfigureTTSAsync(Guid shopId, string languageCode, string ttsProvider, string? voiceId = null, float speakingRate = 1.0f, CancellationToken cancellationToken = default);
    Task<ShopTTSConfiguration?> GetTTSConfigAsync(Guid shopId, string languageCode, CancellationToken cancellationToken = default);
    Task<IEnumerable<ShopTTSConfiguration>> GetAllTTSConfigAsync(Guid shopId, CancellationToken cancellationToken = default);
    Task EnableTTSAsync(Guid configId, CancellationToken cancellationToken = default);
    Task DisableTTSAsync(Guid configId, CancellationToken cancellationToken = default);
}

public sealed class ShopAudioService : IShopAudioService
{
    private readonly AudioGuideDbContext _dbContext;

    public ShopAudioService(AudioGuideDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ShopAudioAsset> UploadAudioAsync(Guid shopId, Guid poiId, string languageCode, string filePath, int durationSeconds, bool isTextToSpeech = false, string? ttsProvider = null, CancellationToken cancellationToken = default)
    {
        // Verify shop and POI exist
        var shop = await _dbContext.ShopProfiles.FindAsync(new object[] { shopId }, cancellationToken: cancellationToken);
        if (shop is null)
        {
            throw new KeyNotFoundException("Shop not found.");
        }

        var poi = await _dbContext.Pois.FindAsync(new object[] { poiId }, cancellationToken: cancellationToken);
        if (poi is null || poi.ShopId != shopId)
        {
            throw new InvalidOperationException("POI does not belong to this shop.");
        }

        // Remove existing audio for this POI and language
        var existing = await _dbContext.ShopAudioAssets
            .Where(a => a.PoiId == poiId && a.LanguageCode == languageCode)
            .ToListAsync(cancellationToken);
        _dbContext.ShopAudioAssets.RemoveRange(existing);

        var audio = new ShopAudioAsset
        {
            ShopId = shopId,
            PoiId = poiId,
            LanguageCode = languageCode,
            FilePath = filePath,
            DurationSeconds = durationSeconds,
            IsTextToSpeech = isTextToSpeech,
            SourceType = isTextToSpeech ? AudioSourceType.TextToSpeech : AudioSourceType.Uploaded,
            TTSProvider = ttsProvider
        };

        _dbContext.ShopAudioAssets.Add(audio);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return audio;
    }

    public async Task<ShopAudioAsset?> GetAudioByIdAsync(Guid audioId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ShopAudioAssets.FindAsync(new object[] { audioId }, cancellationToken: cancellationToken);
    }

    public async Task<ShopAudioAsset?> GetAudioByPoiLanguageAsync(Guid shopId, Guid poiId, string languageCode, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ShopAudioAssets
            .FirstOrDefaultAsync(a => a.ShopId == shopId && a.PoiId == poiId && a.LanguageCode == languageCode, cancellationToken);
    }

    public async Task<IEnumerable<ShopAudioAsset>> GetAudioByPoiAsync(Guid shopId, Guid poiId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ShopAudioAssets
            .Where(a => a.ShopId == shopId && a.PoiId == poiId)
            .OrderBy(a => a.LanguageCode)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ShopAudioAsset>> GetAudioByShopAsync(Guid shopId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ShopAudioAssets
            .Where(a => a.ShopId == shopId)
            .OrderBy(a => a.LanguageCode)
            .ThenBy(a => a.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ShopAudioAsset>> GetAudioByLanguageAsync(Guid shopId, string languageCode, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ShopAudioAssets
            .Where(a => a.ShopId == shopId && a.LanguageCode == languageCode)
            .ToListAsync(cancellationToken);
    }

    public async Task DeleteAudioAsync(Guid audioId, CancellationToken cancellationToken = default)
    {
        var audio = await _dbContext.ShopAudioAssets.FindAsync(new object[] { audioId }, cancellationToken: cancellationToken);
        if (audio is null)
        {
            throw new KeyNotFoundException("Audio not found.");
        }

        _dbContext.ShopAudioAssets.Remove(audio);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<ShopTTSConfiguration> ConfigureTTSAsync(Guid shopId, string languageCode, string ttsProvider, string? voiceId = null, float speakingRate = 1.0f, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.ShopTTSConfigurations
            .FirstOrDefaultAsync(t => t.ShopId == shopId && t.LanguageCode == languageCode, cancellationToken);

        if (existing is not null)
        {
            existing.TTSProvider = ttsProvider;
            existing.VoiceId = voiceId;
            existing.SpeakingRate = speakingRate;
            await _dbContext.SaveChangesAsync(cancellationToken);
            return existing;
        }

        var config = new ShopTTSConfiguration
        {
            ShopId = shopId,
            LanguageCode = languageCode,
            TTSProvider = ttsProvider,
            VoiceId = voiceId,
            SpeakingRate = speakingRate,
            IsEnabled = true
        };

        _dbContext.ShopTTSConfigurations.Add(config);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return config;
    }

    public async Task<ShopTTSConfiguration?> GetTTSConfigAsync(Guid shopId, string languageCode, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ShopTTSConfigurations
            .FirstOrDefaultAsync(t => t.ShopId == shopId && t.LanguageCode == languageCode, cancellationToken);
    }

    public async Task<IEnumerable<ShopTTSConfiguration>> GetAllTTSConfigAsync(Guid shopId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ShopTTSConfigurations
            .Where(t => t.ShopId == shopId)
            .OrderBy(t => t.LanguageCode)
            .ToListAsync(cancellationToken);
    }

    public async Task EnableTTSAsync(Guid configId, CancellationToken cancellationToken = default)
    {
        var config = await _dbContext.ShopTTSConfigurations.FindAsync(new object[] { configId }, cancellationToken: cancellationToken);
        if (config is null)
        {
            throw new KeyNotFoundException("TTS configuration not found.");
        }

        config.IsEnabled = true;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DisableTTSAsync(Guid configId, CancellationToken cancellationToken = default)
    {
        var config = await _dbContext.ShopTTSConfigurations.FindAsync(new object[] { configId }, cancellationToken: cancellationToken);
        if (config is null)
        {
            throw new KeyNotFoundException("TTS configuration not found.");
        }

        config.IsEnabled = false;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
