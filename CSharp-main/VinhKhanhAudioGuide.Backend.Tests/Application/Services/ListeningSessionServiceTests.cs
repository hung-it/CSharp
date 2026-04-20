using Microsoft.EntityFrameworkCore;
using VinhKhanhAudioGuide.Backend.Application.Services;
using VinhKhanhAudioGuide.Backend.Domain.Entities;
using VinhKhanhAudioGuide.Backend.Domain.Enums;
using VinhKhanhAudioGuide.Backend.Persistence;

namespace VinhKhanhAudioGuide.Backend.Tests.Application.Services;

public sealed class ListeningSessionServiceTests
{
    private AudioGuideDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AudioGuideDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AudioGuideDbContext(options);
    }

    [Fact]
    public async Task StartSessionAsync_CreatesSession()
    {
        var dbContext = CreateDbContext();
        var user = new User { ExternalRef = "USER001" };
        var poi = new Poi { Code = "POI001", Name = "Địa điểm", Latitude = 10.0, Longitude = 20.0 };

        dbContext.Users.Add(user);
        dbContext.Pois.Add(poi);
        await dbContext.SaveChangesAsync();

        var service = new ListeningSessionService(dbContext);
        var session = await service.StartSessionAsync(user.Id, poi.Id, TriggerSource.QrCode);

        Assert.NotNull(session);
        Assert.Equal(user.Id, session.UserId);
        Assert.Equal(poi.Id, session.PoiId);
        Assert.Equal(TriggerSource.QrCode, session.TriggerSource);
        Assert.Null(session.EndedAtUtc);
    }

    [Fact]
    public async Task StartSessionAsync_ThrowsWhenUserNotFound()
    {
        var dbContext = CreateDbContext();
        var poi = new Poi { Code = "POI001", Name = "Địa điểm", Latitude = 10.0, Longitude = 20.0 };
        dbContext.Pois.Add(poi);
        await dbContext.SaveChangesAsync();

        var service = new ListeningSessionService(dbContext);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.StartSessionAsync(Guid.NewGuid(), poi.Id));
    }

    [Fact]
    public async Task StartSessionAsync_ThrowsWhenPoiNotFound()
    {
        var dbContext = CreateDbContext();
        var user = new User { ExternalRef = "USER001" };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var service = new ListeningSessionService(dbContext);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.StartSessionAsync(user.Id, Guid.NewGuid()));
    }

    [Fact]
    public async Task EndSessionAsync_EndsSession()
    {
        var dbContext = CreateDbContext();
        var user = new User { ExternalRef = "USER001" };
        var poi = new Poi { Code = "POI001", Name = "Địa điểm", Latitude = 10.0, Longitude = 20.0 };

        dbContext.Users.Add(user);
        dbContext.Pois.Add(poi);
        await dbContext.SaveChangesAsync();

        var service = new ListeningSessionService(dbContext);
        var session = await service.StartSessionAsync(user.Id, poi.Id);
        var ended = await service.EndSessionAsync(session.Id, 120);

        Assert.NotNull(ended.EndedAtUtc);
        Assert.Equal(120, ended.DurationSeconds);
    }

    [Fact]
    public async Task EndSessionAsync_ThrowsWhenSessionNotFound()
    {
        var dbContext = CreateDbContext();
        var service = new ListeningSessionService(dbContext);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.EndSessionAsync(Guid.NewGuid(), 100));
    }

    [Fact]
    public async Task GetSessionsByUserAsync_ReturnsSessions()
    {
        var dbContext = CreateDbContext();
        var user = new User { ExternalRef = "USER001" };
        var poi1 = new Poi { Code = "POI001", Name = "Địa điểm 1", Latitude = 10.0, Longitude = 20.0 };
        var poi2 = new Poi { Code = "POI002", Name = "Địa điểm 2", Latitude = 10.1, Longitude = 20.1 };

        dbContext.Users.Add(user);
        dbContext.Pois.AddRange(poi1, poi2);
        await dbContext.SaveChangesAsync();

        var service = new ListeningSessionService(dbContext);
        var session1 = await service.StartSessionAsync(user.Id, poi1.Id);
        var session2 = await service.StartSessionAsync(user.Id, poi2.Id);

        var sessions = await service.GetSessionsByUserAsync(user.Id);

        Assert.Equal(2, sessions.Count());
    }

    [Fact]
    public async Task GetSessionsByUserAsync_FiltersByDateRange()
    {
        var dbContext = CreateDbContext();
        var user = new User { ExternalRef = "USER001" };
        var poi = new Poi { Code = "POI001", Name = "Địa điểm", Latitude = 10.0, Longitude = 20.0 };

        dbContext.Users.Add(user);
        dbContext.Pois.Add(poi);
        await dbContext.SaveChangesAsync();

        var service = new ListeningSessionService(dbContext);
        var session1 = await service.StartSessionAsync(user.Id, poi.Id);

        await Task.Delay(100);
        var midDate = DateTime.UtcNow;
        await Task.Delay(100);

        var session2 = await service.StartSessionAsync(user.Id, poi.Id);

        var filtered = await service.GetSessionsByUserAsync(user.Id, startDate: midDate);

        Assert.Single(filtered);
        Assert.Equal(session2.Id, filtered.First().Id);
    }

    [Fact]
    public async Task GetSessionsByPoiAsync_ReturnsSessionsForPoi()
    {
        var dbContext = CreateDbContext();
        var user = new User { ExternalRef = "USER001" };
        var poi = new Poi { Code = "POI001", Name = "Địa điểm", Latitude = 10.0, Longitude = 20.0 };

        dbContext.Users.Add(user);
        dbContext.Pois.Add(poi);
        await dbContext.SaveChangesAsync();

        var service = new ListeningSessionService(dbContext);
        var session1 = await service.StartSessionAsync(user.Id, poi.Id);
        await service.EndSessionAsync(session1.Id, 120);

        var session2 = await service.StartSessionAsync(user.Id, poi.Id);
        await service.EndSessionAsync(session2.Id, 100);

        var sessions = await service.GetSessionsByPoiAsync(poi.Id);

        Assert.Equal(2, sessions.Count());
    }

    [Fact]
    public async Task GetActiveSessionsAsync_ReturnsOnlyActiveSessions()
    {
        var dbContext = CreateDbContext();
        var user = new User { ExternalRef = "USER001" };
        var poi = new Poi { Code = "POI001", Name = "Địa điểm", Latitude = 10.0, Longitude = 20.0 };

        dbContext.Users.Add(user);
        dbContext.Pois.Add(poi);
        await dbContext.SaveChangesAsync();

        var service = new ListeningSessionService(dbContext);
        var session1 = await service.StartSessionAsync(user.Id, poi.Id);
        await service.EndSessionAsync(session1.Id, 120);

        var session2 = await service.StartSessionAsync(user.Id, poi.Id);

        var active = await service.GetActiveSessionsAsync(user.Id);

        Assert.Single(active);
        Assert.Equal(session2.Id, active.First().Id);
    }

    [Fact]
    public async Task CancelSessionAsync_RemovesActiveSession()
    {
        var dbContext = CreateDbContext();
        var user = new User { ExternalRef = "USER001" };
        var poi = new Poi { Code = "POI001", Name = "Địa điểm", Latitude = 10.0, Longitude = 20.0 };

        dbContext.Users.Add(user);
        dbContext.Pois.Add(poi);
        await dbContext.SaveChangesAsync();

        var service = new ListeningSessionService(dbContext);
        var session = await service.StartSessionAsync(user.Id, poi.Id);

        await service.CancelSessionAsync(session.Id);

        var active = await service.GetActiveSessionsAsync(user.Id);
        Assert.Empty(active);
    }

    [Fact]
    public async Task GetTotalListeningDurationForPoiAsync_SumsAllDurations()
    {
        var dbContext = CreateDbContext();
        var user = new User { ExternalRef = "USER001" };
        var poi = new Poi { Code = "POI001", Name = "Địa điểm", Latitude = 10.0, Longitude = 20.0 };

        dbContext.Users.Add(user);
        dbContext.Pois.Add(poi);
        await dbContext.SaveChangesAsync();

        var service = new ListeningSessionService(dbContext);
        var session1 = await service.StartSessionAsync(user.Id, poi.Id);
        await service.EndSessionAsync(session1.Id, 100);

        var session2 = await service.StartSessionAsync(user.Id, poi.Id);
        await service.EndSessionAsync(session2.Id, 150);

        var total = await service.GetTotalListeningDurationForPoiAsync(poi.Id);

        Assert.Equal(250, total);
    }

    [Fact]
    public async Task GetAverageListeningDurationForPoiAsync_CalculatesAverage()
    {
        var dbContext = CreateDbContext();
        var user = new User { ExternalRef = "USER001" };
        var poi = new Poi { Code = "POI001", Name = "Địa điểm", Latitude = 10.0, Longitude = 20.0 };

        dbContext.Users.Add(user);
        dbContext.Pois.Add(poi);
        await dbContext.SaveChangesAsync();

        var service = new ListeningSessionService(dbContext);
        var session1 = await service.StartSessionAsync(user.Id, poi.Id);
        await service.EndSessionAsync(session1.Id, 100);

        var session2 = await service.StartSessionAsync(user.Id, poi.Id);
        await service.EndSessionAsync(session2.Id, 140);

        var avg = await service.GetAverageListeningDurationForPoiAsync(poi.Id);

        Assert.Equal(120.0, avg);
    }

    [Fact]
    public async Task GetAverageListeningDurationForPoiAsync_ReturnsZeroWhenNoSessions()
    {
        var dbContext = CreateDbContext();
        var poi = new Poi { Code = "POI001", Name = "Địa điểm", Latitude = 10.0, Longitude = 20.0 };
        dbContext.Pois.Add(poi);
        await dbContext.SaveChangesAsync();

        var service = new ListeningSessionService(dbContext);
        var avg = await service.GetAverageListeningDurationForPoiAsync(poi.Id);

        Assert.Equal(0, avg);
    }

    [Fact]
    public async Task GetListeningCountForPoiAsync_CountsSessions()
    {
        var dbContext = CreateDbContext();
        var user = new User { ExternalRef = "USER001" };
        var poi = new Poi { Code = "POI001", Name = "Địa điểm", Latitude = 10.0, Longitude = 20.0 };

        dbContext.Users.Add(user);
        dbContext.Pois.Add(poi);
        await dbContext.SaveChangesAsync();

        var service = new ListeningSessionService(dbContext);
        var session1 = await service.StartSessionAsync(user.Id, poi.Id);
        await service.EndSessionAsync(session1.Id, 100);

        var session2 = await service.StartSessionAsync(user.Id, poi.Id);
        await service.EndSessionAsync(session2.Id, 150);

        var count = await service.GetListeningCountForPoiAsync(poi.Id);

        Assert.Equal(2, count);
    }

    [Fact]
    public async Task GetListeningCountForPoiAsync_ExcludesActiveSessions()
    {
        var dbContext = CreateDbContext();
        var user = new User { ExternalRef = "USER001" };
        var poi = new Poi { Code = "POI001", Name = "Địa điểm", Latitude = 10.0, Longitude = 20.0 };

        dbContext.Users.Add(user);
        dbContext.Pois.Add(poi);
        await dbContext.SaveChangesAsync();

        var service = new ListeningSessionService(dbContext);
        var session1 = await service.StartSessionAsync(user.Id, poi.Id);
        await service.EndSessionAsync(session1.Id, 100);

        var session2 = await service.StartSessionAsync(user.Id, poi.Id);

        var count = await service.GetListeningCountForPoiAsync(poi.Id);

        Assert.Single(new[] { count });
        Assert.Equal(1, count);
    }
}
