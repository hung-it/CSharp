using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VinhKhanhAudioGuide.Backend.Persistence;

namespace VinhKhanhAudioGuide.Backend.Application.Services;

public sealed class GeofenceService(IServiceScopeFactory scopeFactory) : IGeofenceService
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly Dictionary<Guid, UserGeofenceState> _states = [];
    private readonly object _stateLock = new();
    private static readonly TimeSpan StateTtl = TimeSpan.FromHours(6);

    public async Task<IReadOnlyList<GeofenceEvent>> EvaluateLocationAsync(
        Guid userId,
        double latitude,
        double longitude,
        double nearFactor = 1.5,
        CancellationToken cancellationToken = default)
    {
        TrimExpiredStates();

        if (nearFactor < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(nearFactor), "nearFactor must be >= 1.");
        }

        List<PoiSnapshot> pois;
        using (var scope = _scopeFactory.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AudioGuideDbContext>();
            pois = await dbContext.Pois
                .AsNoTracking()
                .Select(x => new PoiSnapshot(x.Id, x.Code, x.Name, x.Latitude, x.Longitude, x.TriggerRadiusMeters))
                .ToListAsync(cancellationToken);
        }

        var state = GetOrCreateUserState(userId);
        var events = new List<GeofenceEvent>();

        lock (state.SyncRoot)
        {
            state.LastSeenUtc = DateTime.UtcNow;

            foreach (var poi in pois)
            {
                var distanceMeters = DistanceInMeters(latitude, longitude, poi.Latitude, poi.Longitude);
                var isInsideNow = distanceMeters <= poi.TriggerRadiusMeters;
                var isNearNow = !isInsideNow && distanceMeters <= poi.TriggerRadiusMeters * nearFactor;

                var wasInside = state.InsidePoiIds.Contains(poi.Id);
                var wasNear = state.NearbyPoiIds.Contains(poi.Id);

                if (isInsideNow && !wasInside)
                {
                    events.Add(new GeofenceEvent(
                        poi.Id,
                        poi.Code,
                        poi.Name,
                        GeofenceEventType.Entered,
                        distanceMeters,
                        poi.TriggerRadiusMeters,
                        true));
                }
                else if (!isInsideNow && wasInside)
                {
                    events.Add(new GeofenceEvent(
                        poi.Id,
                        poi.Code,
                        poi.Name,
                        GeofenceEventType.Exited,
                        distanceMeters,
                        poi.TriggerRadiusMeters,
                        false));
                }
                else if (isNearNow && !wasNear)
                {
                    events.Add(new GeofenceEvent(
                        poi.Id,
                        poi.Code,
                        poi.Name,
                        GeofenceEventType.Nearby,
                        distanceMeters,
                        poi.TriggerRadiusMeters,
                        false));
                }

                if (isInsideNow)
                {
                    state.InsidePoiIds.Add(poi.Id);
                    state.NearbyPoiIds.Remove(poi.Id);
                }
                else
                {
                    state.InsidePoiIds.Remove(poi.Id);
                    if (isNearNow)
                    {
                        state.NearbyPoiIds.Add(poi.Id);
                    }
                    else
                    {
                        state.NearbyPoiIds.Remove(poi.Id);
                    }
                }
            }
        }

        return events
            .OrderBy(x => x.DistanceMeters)
            .ToList();
    }

    private UserGeofenceState GetOrCreateUserState(Guid userId)
    {
        lock (_stateLock)
        {
            if (_states.TryGetValue(userId, out var state))
            {
                state.LastSeenUtc = DateTime.UtcNow;
                return state;
            }

            state = new UserGeofenceState
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

    private static double DistanceInMeters(double lat1, double lon1, double lat2, double lon2)
    {
        const double earthRadiusMeters = 6371000;
        var dLat = DegreesToRadians(lat2 - lat1);
        var dLon = DegreesToRadians(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return earthRadiusMeters * c;
    }

    private static double DegreesToRadians(double value)
    {
        return value * (Math.PI / 180.0);
    }

    private sealed class UserGeofenceState
    {
        public DateTime LastSeenUtc { get; set; }
        public HashSet<Guid> InsidePoiIds { get; } = [];
        public HashSet<Guid> NearbyPoiIds { get; } = [];
        public object SyncRoot { get; } = new();
    }

    private sealed record PoiSnapshot(
        Guid Id,
        string Code,
        string Name,
        double Latitude,
        double Longitude,
        double TriggerRadiusMeters);
}
