using VinhKhanhAudioGuide.Backend.Domain.Entities;

namespace VinhKhanhAudioGuide.Backend.Application.Services;

public interface ITourService
{
    /// <summary>
    /// Get all tours.
    /// </summary>
    Task<IEnumerable<Tour>> GetAllToursAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get tour by ID with all stops.
    /// </summary>
    Task<Tour?> GetTourByIdAsync(Guid tourId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get tour by code.
    /// </summary>
    Task<Tour?> GetTourByCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new tour.
    /// </summary>
    Task<Tour> CreateTourAsync(string code, string name, string? description = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update tour metadata.
    /// </summary>
    Task<Tour> UpdateTourAsync(Guid tourId, string? name = null, string? description = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get tour stops ordered by sequence.
    /// </summary>
    Task<IEnumerable<TourStop>> GetTourStopsAsync(Guid tourId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add a POI to tour at specific sequence.
    /// </summary>
    Task<TourStop> AddStopAsync(Guid tourId, Guid poiId, int sequence, string? nextStopHint = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove a stop from tour.
    /// </summary>
    Task RemoveStopAsync(Guid tourStopId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reorder stops (shift sequences).
    /// </summary>
    Task ReorderStopsAsync(Guid tourId, List<Guid> tourStopIdsInOrder, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get next stop in tour after current POI.
    /// </summary>
    Task<TourStop?> GetNextStopAsync(Guid tourId, int currentSequence, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get previous stop in tour.
    /// </summary>
    Task<TourStop?> GetPreviousStopAsync(Guid tourId, int currentSequence, CancellationToken cancellationToken = default);
}
