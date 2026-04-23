using Microsoft.EntityFrameworkCore;
using VinhKhanhAudioGuide.Backend.Domain.Entities;
using VinhKhanhAudioGuide.Backend.Domain.Enums;
using VinhKhanhAudioGuide.Backend.Persistence;

namespace VinhKhanhAudioGuide.Backend.Application.Services;

public sealed class VisitTrackingService(AudioGuideDbContext dbContext) : IVisitTrackingService
{
    private readonly AudioGuideDbContext _dbContext = dbContext;

    #region POI Visit Sessions

    public async Task<VisitSession> StartVisitAsync(
        Guid userId,
        Guid poiId,
        VisitTriggerSource triggerSource = VisitTriggerSource.Map,
        PageSource pageSource = PageSource.Map,
        double? latitude = null,
        double? longitude = null,
        string? anonymousRef = null,
        CancellationToken cancellationToken = default)
    {
        var visit = new VisitSession
        {
            UserId = userId,
            PoiId = poiId,
            TriggerSource = triggerSource,
            PageSource = pageSource,
            Latitude = latitude,
            Longitude = longitude,
            AnonymousRef = anonymousRef,
            VisitedAtUtc = DateTime.UtcNow
        };

        _dbContext.VisitSessions.Add(visit);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return visit;
    }

    public async Task<VisitSession> EndVisitAsync(
        Guid visitId,
        CancellationToken cancellationToken = default)
    {
        var visit = await _dbContext.VisitSessions.FindAsync(new object[] { visitId }, cancellationToken: cancellationToken);
        if (visit is null)
        {
            throw new KeyNotFoundException($"Visit session {visitId} not found.");
        }

        visit.LeftAtUtc = DateTime.UtcNow;
        visit.DurationSeconds = (int)(visit.LeftAtUtc.Value - visit.VisitedAtUtc).TotalSeconds;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return visit;
    }

    public async Task<VisitSession> UpdateVisitWithAudioDataAsync(
        Guid visitId,
        int listeningSessionCount,
        int totalListenDurationSeconds,
        CancellationToken cancellationToken = default)
    {
        var visit = await _dbContext.VisitSessions.FindAsync(new object[] { visitId }, cancellationToken: cancellationToken);
        if (visit is null)
        {
            throw new KeyNotFoundException($"Visit session {visitId} not found.");
        }

        visit.ListenedToAudio = listeningSessionCount > 0;
        visit.ListeningSessionCount = listeningSessionCount;
        visit.TotalListenDurationSeconds = totalListenDurationSeconds;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return visit;
    }

    public async Task<IEnumerable<VisitSession>> GetVisitsByUserAsync(
        Guid userId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.VisitSessions
            .Include(v => v.Poi)
            .Where(v => v.UserId == userId);

        if (startDate.HasValue)
        {
            query = query.Where(v => v.VisitedAtUtc >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(v => v.VisitedAtUtc <= endDate.Value);
        }

        return await query
            .OrderByDescending(v => v.VisitedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<VisitSession>> GetVisitsByPoiAsync(
        Guid poiId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.VisitSessions
            .Include(v => v.User)
            .Where(v => v.PoiId == poiId);

        if (startDate.HasValue)
        {
            query = query.Where(v => v.VisitedAtUtc >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(v => v.VisitedAtUtc <= endDate.Value);
        }

        return await query
            .OrderByDescending(v => v.VisitedAtUtc)
            .ToListAsync(cancellationToken);
    }

    #endregion

    #region Tour View Sessions

    public async Task<TourViewSession> StartTourViewAsync(
        Guid userId,
        Guid tourId,
        string? anonymousRef = null,
        CancellationToken cancellationToken = default)
    {
        var tourView = new TourViewSession
        {
            UserId = userId,
            TourId = tourId,
            AnonymousRef = anonymousRef,
            ViewedAtUtc = DateTime.UtcNow
        };

        _dbContext.TourViewSessions.Add(tourView);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return tourView;
    }

    public async Task<TourViewSession> EndTourViewAsync(
        Guid tourViewId,
        int poiVisitedCount,
        int audioListenedCount,
        CancellationToken cancellationToken = default)
    {
        var tourView = await _dbContext.TourViewSessions.FindAsync(new object[] { tourViewId }, cancellationToken: cancellationToken);
        if (tourView is null)
        {
            throw new KeyNotFoundException($"Tour view session {tourViewId} not found.");
        }

        tourView.ClosedAtUtc = DateTime.UtcNow;
        tourView.DurationSeconds = (int)(tourView.ClosedAtUtc.Value - tourView.ViewedAtUtc).TotalSeconds;
        tourView.PoiVisitedCount = poiVisitedCount;
        tourView.AudioListenedCount = audioListenedCount;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return tourView;
    }

    public async Task<IEnumerable<TourViewSession>> GetTourViewsByUserAsync(
        Guid userId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.TourViewSessions
            .Include(t => t.Tour)
            .Where(t => t.UserId == userId);

        if (startDate.HasValue)
        {
            query = query.Where(t => t.ViewedAtUtc >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(t => t.ViewedAtUtc <= endDate.Value);
        }

        return await query
            .OrderByDescending(t => t.ViewedAtUtc)
            .ToListAsync(cancellationToken);
    }

    #endregion

    #region Geofence Events

    public async Task<PoiGeofenceEvent> RecordGeofenceEventAsync(
        Guid userId,
        Guid poiId,
        PoiGeofenceEventType eventType,
        double latitude,
        double longitude,
        double distanceFromCenterMeters,
        string? anonymousRef = null,
        CancellationToken cancellationToken = default)
    {
        var geofenceEvent = new PoiGeofenceEvent
        {
            UserId = userId,
            PoiId = poiId,
            EventType = eventType,
            Latitude = latitude,
            Longitude = longitude,
            DistanceFromCenterMeters = distanceFromCenterMeters,
            AnonymousRef = anonymousRef,
            OccurredAtUtc = DateTime.UtcNow
        };

        _dbContext.PoiGeofenceEvents.Add(geofenceEvent);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return geofenceEvent;
    }

    public async Task<IEnumerable<PoiGeofenceEvent>> GetGeofenceEventsByPoiAsync(
        Guid poiId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.PoiGeofenceEvents
            .Include(g => g.User)
            .Where(g => g.PoiId == poiId);

        if (startDate.HasValue)
        {
            query = query.Where(g => g.OccurredAtUtc >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(g => g.OccurredAtUtc <= endDate.Value);
        }

        return await query
            .OrderByDescending(g => g.OccurredAtUtc)
            .ToListAsync(cancellationToken);
    }

    #endregion
}
