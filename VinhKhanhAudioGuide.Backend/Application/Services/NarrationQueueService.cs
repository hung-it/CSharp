namespace VinhKhanhAudioGuide.Backend.Application.Services;

public sealed class NarrationQueueService : INarrationQueueService
{
    private readonly Dictionary<Guid, UserNarrationState> _states = [];
    private readonly object _stateLock = new();
    private static readonly TimeSpan StateTtl = TimeSpan.FromHours(6);

    public Task<bool> EnqueueAsync(
        Guid userId,
        Guid poiId,
        string audioPath,
        int priority = 0,
        CancellationToken cancellationToken = default)
    {
        TrimExpiredStates();

        if (string.IsNullOrWhiteSpace(audioPath))
        {
            throw new ArgumentException("audioPath is required.", nameof(audioPath));
        }

        var state = GetOrCreateState(userId);
        lock (state.SyncRoot)
        {
            state.LastSeenUtc = DateTime.UtcNow;

            if (state.Active is not null && state.Active.PoiId == poiId)
            {
                return Task.FromResult(false);
            }

            if (state.Pending.Any(x => x.PoiId == poiId))
            {
                return Task.FromResult(false);
            }

            state.Pending.Add(new NarrationRequest(
                Guid.NewGuid(),
                poiId,
                audioPath,
                priority,
                DateTime.UtcNow));

            state.Pending = state.Pending
                .OrderByDescending(x => x.Priority)
                .ThenBy(x => x.EnqueuedAtUtc)
                .ToList();

            return Task.FromResult(true);
        }
    }

    public Task<NarrationRequest?> TryStartNextAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        TrimExpiredStates();

        var state = GetOrCreateState(userId);
        lock (state.SyncRoot)
        {
            state.LastSeenUtc = DateTime.UtcNow;

            if (state.Active is not null)
            {
                return Task.FromResult<NarrationRequest?>(state.Active);
            }

            if (state.Pending.Count == 0)
            {
                return Task.FromResult<NarrationRequest?>(null);
            }

            state.Active = state.Pending[0];
            state.Pending.RemoveAt(0);
            return Task.FromResult<NarrationRequest?>(state.Active);
        }
    }

    public Task CompleteCurrentAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        TrimExpiredStates();

        var state = GetOrCreateState(userId);
        lock (state.SyncRoot)
        {
            state.LastSeenUtc = DateTime.UtcNow;
            state.Active = null;
        }

        return Task.CompletedTask;
    }

    public Task CancelAsync(Guid userId, Guid narrationId, CancellationToken cancellationToken = default)
    {
        TrimExpiredStates();

        var state = GetOrCreateState(userId);
        lock (state.SyncRoot)
        {
            state.LastSeenUtc = DateTime.UtcNow;

            if (state.Active is not null && state.Active.Id == narrationId)
            {
                state.Active = null;
                return Task.CompletedTask;
            }

            state.Pending.RemoveAll(x => x.Id == narrationId);
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<NarrationRequest>> GetPendingQueueAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        TrimExpiredStates();

        var state = GetOrCreateState(userId);
        lock (state.SyncRoot)
        {
            state.LastSeenUtc = DateTime.UtcNow;
            return Task.FromResult<IReadOnlyList<NarrationRequest>>(state.Pending.ToList());
        }
    }

    public Task<NarrationRequest?> GetCurrentAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        TrimExpiredStates();

        var state = GetOrCreateState(userId);
        lock (state.SyncRoot)
        {
            state.LastSeenUtc = DateTime.UtcNow;
            return Task.FromResult<NarrationRequest?>(state.Active);
        }
    }

    private UserNarrationState GetOrCreateState(Guid userId)
    {
        lock (_stateLock)
        {
            if (_states.TryGetValue(userId, out var state))
            {
                state.LastSeenUtc = DateTime.UtcNow;
                return state;
            }

            state = new UserNarrationState
            {
                LastSeenUtc = DateTime.UtcNow
            };
            _states[userId] = state;
            return state;
        }
    }

    private void TrimExpiredStates()
    {
        var threshold = DateTime.UtcNow - StateTtl;

        lock (_stateLock)
        {
            var expiredUserIds = _states
                .Where(x => x.Value.LastSeenUtc < threshold)
                .Select(x => x.Key)
                .ToList();

            foreach (var userId in expiredUserIds)
            {
                _states.Remove(userId);
            }
        }
    }

    private sealed class UserNarrationState
    {
        public DateTime LastSeenUtc { get; set; }
        public List<NarrationRequest> Pending { get; set; } = [];
        public NarrationRequest? Active { get; set; }
        public object SyncRoot { get; } = new();
    }
}
