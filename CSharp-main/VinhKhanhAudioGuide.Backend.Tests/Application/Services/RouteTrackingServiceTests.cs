using Microsoft.EntityFrameworkCore;
using VinhKhanhAudioGuide.Backend.Application.Services;
using VinhKhanhAudioGuide.Backend.Persistence;

namespace VinhKhanhAudioGuide.Backend.Tests.Application.Services;

public sealed class RouteTrackingServiceTests
{
    private AudioGuideDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AudioGuideDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AudioGuideDbContext(options);
    }

    [Fact]
    public async Task LogAnonymousRoutePointAsync_CreatesAnonymousUserAndPoint()
    {
        var db = CreateDbContext();
        var service = new RouteTrackingService(db);

        var point = await service.LogAnonymousRoutePointAsync("device-001", 10.75, 106.68);

        Assert.Equal(10.75, point.Latitude);
        Assert.Equal(106.68, point.Longitude);
        Assert.True(await db.Users.AnyAsync(x => x.ExternalRef == "ANON:device-001"));
    }

    [Fact]
    public async Task GetAnonymousRouteAsync_ReturnsOrderedPoints()
    {
        var db = CreateDbContext();
        var service = new RouteTrackingService(db);

        await service.LogAnonymousRoutePointAsync("device-xyz", 10.1, 106.1);
        await Task.Delay(10);
        await service.LogAnonymousRoutePointAsync("device-xyz", 10.2, 106.2);

        var route = (await service.GetAnonymousRouteAsync("device-xyz")).ToList();

        Assert.Equal(2, route.Count);
        Assert.True(route[0].RecordedAtUtc <= route[1].RecordedAtUtc);
    }

    [Fact]
    public async Task GetAnonymousRouteAsync_ReturnsEmptyWhenUnknownRef()
    {
        var db = CreateDbContext();
        var service = new RouteTrackingService(db);

        var route = await service.GetAnonymousRouteAsync("not-exist");

        Assert.Empty(route);
    }
}
