using Microsoft.EntityFrameworkCore;
using VinhKhanhAudioGuide.Backend.Domain.Entities;
using VinhKhanhAudioGuide.Backend.Persistence;

namespace VinhKhanhAudioGuide.Backend.Application.Services;

public sealed class RouteTrackingService(AudioGuideDbContext dbContext) : IRouteTrackingService
{
    private readonly AudioGuideDbContext _dbContext = dbContext;

    public async Task<RoutePoint> LogAnonymousRoutePointAsync(
        string anonymousRef,
        double latitude,
        double longitude,
        string source = "gps",
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(anonymousRef))
        {
            throw new ArgumentException("anonymousRef is required.", nameof(anonymousRef));
        }

        var normalizedRef = $"ANON:{anonymousRef.Trim()}";
        var user = await _dbContext.Users.FirstOrDefaultAsync(x => x.ExternalRef == normalizedRef, cancellationToken);

        if (user is null)
        {
            user = new User
            {
                ExternalRef = normalizedRef,
                PreferredLanguage = "vi"
            };
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var routePoint = new RoutePoint
        {
            UserId = user.Id,
            Latitude = latitude,
            Longitude = longitude,
            Source = source,
            RecordedAtUtc = DateTime.UtcNow
        };

        _dbContext.RoutePoints.Add(routePoint);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return routePoint;
    }

    public async Task<IEnumerable<RoutePoint>> GetAnonymousRouteAsync(
        string anonymousRef,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedRef = $"ANON:{anonymousRef.Trim()}";
        var user = await _dbContext.Users.FirstOrDefaultAsync(x => x.ExternalRef == normalizedRef, cancellationToken);

        if (user is null)
        {
            return [];
        }

        var query = _dbContext.RoutePoints.Where(x => x.UserId == user.Id);

        if (startDate.HasValue)
        {
            query = query.Where(x => x.RecordedAtUtc >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(x => x.RecordedAtUtc <= endDate.Value);
        }

        return await query
            .OrderBy(x => x.RecordedAtUtc)
            .ToListAsync(cancellationToken);
    }
}
