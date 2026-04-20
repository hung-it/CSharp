using Microsoft.EntityFrameworkCore;
using VinhKhanhAudioGuide.Backend.Application.Services;
using VinhKhanhAudioGuide.Backend.Domain.Entities;
using VinhKhanhAudioGuide.Backend.Persistence;

namespace VinhKhanhAudioGuide.Backend.Tests.Application.Services;

public sealed class AnalyticsServiceTests
{
    private AudioGuideDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AudioGuideDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AudioGuideDbContext(options);
    }

    [Fact]
    public async Task GetTopPoisByListeningCountAsync_ReturnsTopPoisOrderedByCount()
    {
        var dbContext = CreateDbContext();
        var service = new AnalyticsService(dbContext);

        var user = new User { ExternalRef = "USER001" };
        var poi1 = new Poi { Code = "POI001", Name = "Bánh mì", Latitude = 10.0, Longitude = 20.0 };
        var poi2 = new Poi { Code = "POI002", Name = "Phá lấu", Latitude = 10.1, Longitude = 20.1 };

        dbContext.Users.Add(user);
        dbContext.Pois.AddRange(poi1, poi2);
        dbContext.ListeningSessions.AddRange(
            new ListeningSession { UserId = user.Id, PoiId = poi1.Id, DurationSeconds = 60, EndedAtUtc = DateTime.UtcNow },
            new ListeningSession { UserId = user.Id, PoiId = poi1.Id, DurationSeconds = 90, EndedAtUtc = DateTime.UtcNow },
            new ListeningSession { UserId = user.Id, PoiId = poi2.Id, DurationSeconds = 120, EndedAtUtc = DateTime.UtcNow });

        await dbContext.SaveChangesAsync();

        var result = (await service.GetTopPoisByListeningCountAsync(limit: 1)).ToList();

        Assert.Single(result);
        Assert.Equal(poi1.Id, result[0].PoiId);
        Assert.Equal(2, result[0].ListeningCount);
    }

    [Fact]
    public async Task GetTopPoisByListeningCountAsync_ReturnsEmptyWhenLimitInvalid()
    {
        var dbContext = CreateDbContext();
        var service = new AnalyticsService(dbContext);

        var result = await service.GetTopPoisByListeningCountAsync(limit: 0);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetPoiListeningStatsAsync_ComputesCountTotalAndAverage()
    {
        var dbContext = CreateDbContext();
        var service = new AnalyticsService(dbContext);

        var user = new User { ExternalRef = "USER001" };
        var poi = new Poi { Code = "POI001", Name = "Bánh mì", Latitude = 10.0, Longitude = 20.0 };

        dbContext.Users.Add(user);
        dbContext.Pois.Add(poi);
        dbContext.ListeningSessions.AddRange(
            new ListeningSession { UserId = user.Id, PoiId = poi.Id, DurationSeconds = 40, EndedAtUtc = DateTime.UtcNow },
            new ListeningSession { UserId = user.Id, PoiId = poi.Id, DurationSeconds = 80, EndedAtUtc = DateTime.UtcNow },
            new ListeningSession { UserId = user.Id, PoiId = poi.Id });

        await dbContext.SaveChangesAsync();

        var stats = (await service.GetPoiListeningStatsAsync()).Single();

        Assert.Equal(2, stats.ListeningCount);
        Assert.Equal(120, stats.TotalDurationSeconds);
        Assert.Equal(60, stats.AverageDurationSeconds);
    }

    [Fact]
    public async Task GetUserRouteAsync_ReturnsOnlyRequestedUserAndOrderedByTime()
    {
        var dbContext = CreateDbContext();
        var service = new AnalyticsService(dbContext);

        var user1 = new User { ExternalRef = "USER001" };
        var user2 = new User { ExternalRef = "USER002" };

        dbContext.Users.AddRange(user1, user2);

        var t1 = DateTime.UtcNow.AddMinutes(-2);
        var t2 = DateTime.UtcNow.AddMinutes(-1);

        dbContext.RoutePoints.AddRange(
            new RoutePoint { UserId = user1.Id, RecordedAtUtc = t2, Latitude = 10.2, Longitude = 106.2, Source = "gps" },
            new RoutePoint { UserId = user1.Id, RecordedAtUtc = t1, Latitude = 10.1, Longitude = 106.1, Source = "gps" },
            new RoutePoint { UserId = user2.Id, RecordedAtUtc = t1, Latitude = 9.0, Longitude = 105.0, Source = "gps" });

        await dbContext.SaveChangesAsync();

        var route = (await service.GetUserRouteAsync(user1.Id)).ToList();

        Assert.Equal(2, route.Count);
        Assert.True(route[0].RecordedAtUtc <= route[1].RecordedAtUtc);
        Assert.Equal(10.1, route[0].Latitude);
    }

    [Fact]
    public async Task GetUserRouteAsync_AppliesDateRange()
    {
        var dbContext = CreateDbContext();
        var service = new AnalyticsService(dbContext);

        var user = new User { ExternalRef = "USER001" };
        dbContext.Users.Add(user);

        var oldPoint = DateTime.UtcNow.AddDays(-2);
        var newPoint = DateTime.UtcNow.AddHours(-1);

        dbContext.RoutePoints.AddRange(
            new RoutePoint { UserId = user.Id, RecordedAtUtc = oldPoint, Latitude = 10.0, Longitude = 106.0, Source = "gps" },
            new RoutePoint { UserId = user.Id, RecordedAtUtc = newPoint, Latitude = 10.1, Longitude = 106.1, Source = "gps" });

        await dbContext.SaveChangesAsync();

        var route = (await service.GetUserRouteAsync(user.Id, startDate: DateTime.UtcNow.AddDays(-1))).ToList();

        Assert.Single(route);
        Assert.Equal(10.1, route[0].Latitude);
    }

    [Fact]
    public async Task GetHeatmapDataAsync_GroupsByRoundedCells()
    {
        var dbContext = CreateDbContext();
        var service = new AnalyticsService(dbContext);

        var user = new User { ExternalRef = "USER001" };
        dbContext.Users.Add(user);

        var now = DateTime.UtcNow;

        dbContext.RoutePoints.AddRange(
            new RoutePoint { UserId = user.Id, RecordedAtUtc = now, Latitude = 10.1234, Longitude = 106.1234, Source = "gps" },
            new RoutePoint { UserId = user.Id, RecordedAtUtc = now, Latitude = 10.12341, Longitude = 106.12341, Source = "gps" },
            new RoutePoint { UserId = user.Id, RecordedAtUtc = now, Latitude = 10.9876, Longitude = 106.9876, Source = "gps" });

        await dbContext.SaveChangesAsync();

        var heatmap = (await service.GetHeatmapDataAsync(now.AddMinutes(-1), now.AddMinutes(1), precision: 3)).ToList();

        Assert.Equal(2, heatmap.Count);
        Assert.Equal(2, heatmap[0].PointCount);
    }

    [Fact]
    public async Task GetHeatmapDataAsync_ThrowsWhenDateRangeInvalid()
    {
        var dbContext = CreateDbContext();
        var service = new AnalyticsService(dbContext);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.GetHeatmapDataAsync(DateTime.UtcNow, DateTime.UtcNow.AddMinutes(-1)));
    }

    [Fact]
    public async Task GetHeatmapDataAsync_ThrowsWhenPrecisionOutOfRange()
    {
        var dbContext = CreateDbContext();
        var service = new AnalyticsService(dbContext);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            service.GetHeatmapDataAsync(DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow, precision: 7));
    }
}
