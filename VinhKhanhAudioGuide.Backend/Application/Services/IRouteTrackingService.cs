using VinhKhanhAudioGuide.Backend.Domain.Entities;

namespace VinhKhanhAudioGuide.Backend.Application.Services;

public interface IRouteTrackingService
{
    Task<RoutePoint> LogAnonymousRoutePointAsync(
        string anonymousRef,
        double latitude,
        double longitude,
        string source = "gps",
        CancellationToken cancellationToken = default);

    Task<IEnumerable<RoutePoint>> GetAnonymousRouteAsync(
        string anonymousRef,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default);
}
