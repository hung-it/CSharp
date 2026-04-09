using VinhKhanhAudioGuide.Backend.Domain.Entities;

namespace VinhKhanhAudioGuide.Backend.Application.Services;

public interface IPoiService
{
    /// <summary>
    /// Get all POIs (có pagination nếu cần large datasets).
    /// </summary>
    Task<IEnumerable<Poi>> GetAllPoiAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get POI by ID.
    /// </summary>
    Task<Poi?> GetPoiByIdAsync(Guid poiId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get POI by code (unique identifier for POI).
    /// </summary>
    Task<Poi?> GetPoiByCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get POIs in a geographic area (by district or bounding box).
    /// </summary>
    Task<IEnumerable<Poi>> GetPoisByDistrictAsync(string district, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new POI.
    /// </summary>
    Task<Poi> CreatePoiAsync(string code, string name, double latitude, double longitude, double triggerRadiusMeters = 30, string? description = null, string? district = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update POI metadata.
    /// </summary>
    Task<Poi> UpdatePoiAsync(Guid poiId, string? name = null, string? description = null, double? triggerRadiusMeters = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Assign audio asset to POI.
    /// </summary>
    Task AssignAudioAsync(Guid poiId, string languageCode, string filePath, int durationSeconds, bool isTextToSpeech = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get audio assets for a POI.
    /// </summary>
    Task<IEnumerable<AudioAsset>> GetAudiosByPoiAsync(Guid poiId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get audio in specific language for a POI.
    /// </summary>
    Task<AudioAsset?> GetAudioByLanguageAsync(Guid poiId, string languageCode, CancellationToken cancellationToken = default);
}
