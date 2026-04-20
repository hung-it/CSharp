using Microsoft.EntityFrameworkCore;
using VinhKhanhAudioGuide.Backend.Domain.Entities;
using VinhKhanhAudioGuide.Backend.Domain.Enums;
using VinhKhanhAudioGuide.Backend.Persistence;

namespace VinhKhanhAudioGuide.Backend.Application.Services;

public sealed class ListeningSessionService(AudioGuideDbContext dbContext) : IListeningSessionService
{
    private readonly AudioGuideDbContext _dbContext = dbContext;

    public async Task<ListeningSession> StartSessionAsync(
        Guid userId,
        Guid poiId,
        TriggerSource triggerSource = TriggerSource.QrCode,
        CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users.FindAsync(new object[] { userId }, cancellationToken: cancellationToken);
        if (user is null)
        {
            throw new KeyNotFoundException($"User {userId} not found.");
        }

        var poi = await _dbContext.Pois.FindAsync(new object[] { poiId }, cancellationToken: cancellationToken);
        if (poi is null)
        {
            throw new KeyNotFoundException($"POI {poiId} not found.");
        }

        var session = new ListeningSession
        {
            UserId = userId,
            PoiId = poiId,
            TriggerSource = triggerSource,
            StartedAtUtc = DateTime.UtcNow
        };

        _dbContext.ListeningSessions.Add(session);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return session;
    }

    public async Task<ListeningSession> EndSessionAsync(
        Guid sessionId,
        int durationSeconds,
        CancellationToken cancellationToken = default)
    {
        var session = await _dbContext.ListeningSessions.FindAsync(
            new object[] { sessionId },
            cancellationToken: cancellationToken);

        if (session is null)
        {
            throw new KeyNotFoundException($"Session {sessionId} not found.");
        }

        session.EndedAtUtc = DateTime.UtcNow;
        session.DurationSeconds = durationSeconds;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return session;
    }

    public async Task<IEnumerable<ListeningSession>> GetSessionsByUserAsync(
        Guid userId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.ListeningSessions
            .Where(s => s.UserId == userId);

        if (startDate.HasValue)
        {
            query = query.Where(s => s.StartedAtUtc >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(s => s.StartedAtUtc <= endDate.Value);
        }

        return await query
            .OrderByDescending(s => s.StartedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ListeningSession>> GetSessionsByPoiAsync(
        Guid poiId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.ListeningSessions
            .Where(s => s.PoiId == poiId && s.DurationSeconds.HasValue)
            .OrderByDescending(s => s.StartedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ListeningSession>> GetActiveSessionsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.ListeningSessions
            .Where(s => s.UserId == userId && s.EndedAtUtc == null)
            .OrderByDescending(s => s.StartedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task CancelSessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var session = await _dbContext.ListeningSessions.FindAsync(
            new object[] { sessionId },
            cancellationToken: cancellationToken);

        if (session is not null && session.EndedAtUtc is null)
        {
            _dbContext.ListeningSessions.Remove(session);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<int> GetTotalListeningDurationForPoiAsync(
        Guid poiId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.ListeningSessions
            .Where(s => s.PoiId == poiId && s.DurationSeconds.HasValue)
            .SumAsync(s => s.DurationSeconds!.Value, cancellationToken);
    }

    public async Task<double> GetAverageListeningDurationForPoiAsync(
        Guid poiId,
        CancellationToken cancellationToken = default)
    {
        var sessions = await _dbContext.ListeningSessions
            .Where(s => s.PoiId == poiId && s.DurationSeconds.HasValue)
            .Select(s => s.DurationSeconds!.Value)
            .ToListAsync(cancellationToken);

        if (sessions.Count == 0)
        {
            return 0;
        }

        return sessions.Average();
    }

    public async Task<int> GetListeningCountForPoiAsync(
        Guid poiId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.ListeningSessions
            .Where(s => s.PoiId == poiId && s.DurationSeconds.HasValue)
            .CountAsync(cancellationToken);
    }
}
