using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VinhKhanhAudioGuide.Backend.Application.Services;
using VinhKhanhAudioGuide.Backend.Domain.Entities;
using VinhKhanhAudioGuide.Backend.Persistence;

namespace VinhKhanhAudioGuide.Backend.Tests.Application.Services;

public sealed class GeofenceServiceTests
{
    private static ServiceProvider CreateProvider(string dbName)
    {
        var services = new ServiceCollection();
        services.AddDbContext<AudioGuideDbContext>(options => options.UseInMemoryDatabase(dbName));
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task EvaluateLocationAsync_RaisesEnteredEventWhenCrossingIntoRadius()
    {
        var provider = CreateProvider(Guid.NewGuid().ToString());
        await using (var seedScope = provider.CreateAsyncScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<AudioGuideDbContext>();
            db.Pois.Add(new Poi
            {
                Code = "POI001",
                Name = "Bus Stop",
                Latitude = 10.0,
                Longitude = 106.0,
                TriggerRadiusMeters = 100
            });
            await db.SaveChangesAsync();
        }

        var service = new GeofenceService(provider.GetRequiredService<IServiceScopeFactory>());
        var userId = Guid.NewGuid();

        _ = await service.EvaluateLocationAsync(userId, 10.002, 106.002);
        var events = await service.EvaluateLocationAsync(userId, 10.0, 106.0);

        Assert.Contains(events, x => x.EventType == GeofenceEventType.Entered && x.ShouldStartNarration);
    }

    [Fact]
    public async Task EvaluateLocationAsync_RaisesExitedEventWhenLeavingRadius()
    {
        var provider = CreateProvider(Guid.NewGuid().ToString());
        await using (var seedScope = provider.CreateAsyncScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<AudioGuideDbContext>();
            db.Pois.Add(new Poi
            {
                Code = "POI001",
                Name = "Bus Stop",
                Latitude = 10.0,
                Longitude = 106.0,
                TriggerRadiusMeters = 100
            });
            await db.SaveChangesAsync();
        }

        var service = new GeofenceService(provider.GetRequiredService<IServiceScopeFactory>());
        var userId = Guid.NewGuid();

        _ = await service.EvaluateLocationAsync(userId, 10.0, 106.0);
        var events = await service.EvaluateLocationAsync(userId, 10.002, 106.002);

        Assert.Contains(events, x => x.EventType == GeofenceEventType.Exited);
    }

    [Fact]
    public async Task EvaluateLocationAsync_RaisesNearbyEventWhenNearButNotInside()
    {
        var provider = CreateProvider(Guid.NewGuid().ToString());
        await using (var seedScope = provider.CreateAsyncScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<AudioGuideDbContext>();
            db.Pois.Add(new Poi
            {
                Code = "POI001",
                Name = "Bus Stop",
                Latitude = 10.0,
                Longitude = 106.0,
                TriggerRadiusMeters = 30
            });
            await db.SaveChangesAsync();
        }

        var service = new GeofenceService(provider.GetRequiredService<IServiceScopeFactory>());
        var userId = Guid.NewGuid();

        var events = await service.EvaluateLocationAsync(userId, 10.0003, 106.0003, nearFactor: 2);

        Assert.Contains(events, x => x.EventType == GeofenceEventType.Nearby);
    }

}
