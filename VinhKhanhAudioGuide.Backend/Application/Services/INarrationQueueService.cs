namespace VinhKhanhAudioGuide.Backend.Application.Services;

public interface INarrationQueueService
{
    Task<bool> EnqueueAsync(
        Guid userId,
        Guid poiId,
        string audioPath,
        int priority = 0,
        CancellationToken cancellationToken = default);

    Task<NarrationRequest?> TryStartNextAsync(Guid userId, CancellationToken cancellationToken = default);

    Task CompleteCurrentAsync(Guid userId, CancellationToken cancellationToken = default);

    Task CancelAsync(Guid userId, Guid narrationId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NarrationRequest>> GetPendingQueueAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<NarrationRequest?> GetCurrentAsync(Guid userId, CancellationToken cancellationToken = default);
}

public sealed record NarrationRequest(
    Guid Id,
    Guid PoiId,
    string AudioPath,
    int Priority,
    DateTime EnqueuedAtUtc);
