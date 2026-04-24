using VinhKhanhAudioGuide.Backend.Domain.Entities;
using VinhKhanhAudioGuide.Backend.Domain.Enums;

namespace VinhKhanhAudioGuide.Backend.Application.Services;

public interface IListeningSessionService
{
    /// <summary>
    /// Start a new listening session when user taps play on a POI.
    /// </summary>
    Task<ListeningSession> StartSessionAsync(
        Guid userId,
        Guid poiId,
        TriggerSource triggerSource = TriggerSource.QrCode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// End a listening session with duration.
    /// </summary>
    Task<ListeningSession> EndSessionAsync(
        Guid sessionId,
        int durationSeconds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all sessions for a user (optionally filtered by date range).
    /// </summary>
    Task<IEnumerable<ListeningSession>> GetSessionsByUserAsync(
        Guid userId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all sessions for a specific POI.
    /// </summary>
    Task<IEnumerable<ListeningSession>> GetSessionsByPoiAsync(
        Guid poiId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get active (not ended) sessions.
    /// </summary>
    Task<IEnumerable<ListeningSession>> GetActiveSessionsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancel a session without recording duration (when user navigates away).
    /// </summary>
    Task CancelSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get total listening duration for a POI (sum of all sessions).
    /// </summary>
    Task<int> GetTotalListeningDurationForPoiAsync(
        Guid poiId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get average listening duration for a POI.
    /// </summary>
    Task<double> GetAverageListeningDurationForPoiAsync(
        Guid poiId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get count of times a POI was listened to.
    /// </summary>
    Task<int> GetListeningCountForPoiAsync(
        Guid poiId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get count of active (ongoing, not ended) sessions per POI.
    /// </summary>
    Task<Dictionary<Guid, int>> GetActiveSessionCountsByPoiAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get total active session count across all POIs.
    /// </summary>
    Task<int> GetTotalActiveSessionCountAsync(
        CancellationToken cancellationToken = default);
}
