using Microsoft.EntityFrameworkCore;
using VinhKhanhAudioGuide.Backend.Application.Services;
using VinhKhanhAudioGuide.Backend.Infrastructure;
using VinhKhanhAudioGuide.Backend.Persistence;

namespace VinhKhanhAudioGuide.Backend.Tests.Infrastructure;

public sealed class DataSeederTests
{
    private AudioGuideDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AudioGuideDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AudioGuideDbContext(options);
    }

    [Fact]
    public async Task SeedAsync_CreatesPoisWithAudio()
    {
        var dbContext = CreateDbContext();
        var poiService = new PoiService(dbContext);
        var tourService = new TourService(dbContext);
        var subscriptionService = new SubscriptionService(dbContext);
        var seeder = new DataSeeder(dbContext, poiService, tourService, subscriptionService);

        await seeder.SeedAsync();

        var pois = await dbContext.Pois.ToListAsync();
        Assert.Equal(14, pois.Count);

        var audioCount = await dbContext.AudioAssets.CountAsync();
        Assert.Equal(28, audioCount);
    }

    [Fact]
    public async Task SeedAsync_CreatesToursWithStops()
    {
        var dbContext = CreateDbContext();
        var poiService = new PoiService(dbContext);
        var tourService = new TourService(dbContext);
        var subscriptionService = new SubscriptionService(dbContext);
        var seeder = new DataSeeder(dbContext, poiService, tourService, subscriptionService);

        await seeder.SeedAsync();

        var tours = await dbContext.Tours.ToListAsync();
        Assert.Equal(2, tours.Count);

        var stopCount = await dbContext.TourStops.CountAsync();
        Assert.Equal(15, stopCount);
    }

    [Fact]
    public async Task SeedAsync_CreatesUsersWithSubscriptions()
    {
        var dbContext = CreateDbContext();
        var poiService = new PoiService(dbContext);
        var tourService = new TourService(dbContext);
        var subscriptionService = new SubscriptionService(dbContext);
        var seeder = new DataSeeder(dbContext, poiService, tourService, subscriptionService);

        await seeder.SeedAsync();

        var users = await dbContext.Users.ToListAsync();
        Assert.Equal(6, users.Count);

        var subscriptions = await dbContext.Subscriptions
            .Where(s => s.IsActive)
            .ToListAsync();
        Assert.Equal(2, subscriptions.Count);
    }

    [Fact]
    public async Task SeedAsync_SkipsIfAlreadySeeded()
    {
        var dbContext = CreateDbContext();
        var poiService = new PoiService(dbContext);
        var tourService = new TourService(dbContext);
        var subscriptionService = new SubscriptionService(dbContext);
        var seeder = new DataSeeder(dbContext, poiService, tourService, subscriptionService);

        await seeder.SeedAsync();
        var firstSeedCount = await dbContext.Pois.CountAsync();

        await seeder.SeedAsync();
        var secondSeedCount = await dbContext.Pois.CountAsync();

        Assert.Equal(firstSeedCount, secondSeedCount);
    }

    [Fact]
    public async Task SeedAsync_CreatesPoisByDistrict()
    {
        var dbContext = CreateDbContext();
        var poiService = new PoiService(dbContext);
        var tourService = new TourService(dbContext);
        var subscriptionService = new SubscriptionService(dbContext);
        var seeder = new DataSeeder(dbContext, poiService, tourService, subscriptionService);

        await seeder.SeedAsync();

        var xomChieuPois = await dbContext.Pois
            .Where(p => p.District == "Xóm Chiếu")
            .ToListAsync();
        var vinhHoiPois = await dbContext.Pois
            .Where(p => p.District == "Vĩnh Hội")
            .ToListAsync();
        var khanhHoiPois = await dbContext.Pois
            .Where(p => p.District == "Khánh Hội")
            .ToListAsync();

        Assert.Equal(4, xomChieuPois.Count);
        Assert.Equal(5, vinhHoiPois.Count);
        Assert.Equal(5, khanhHoiPois.Count);
    }

    [Fact]
    public async Task SeedAsync_PremiumUserHasAllSegmentEntitlements()
    {
        var dbContext = CreateDbContext();
        var poiService = new PoiService(dbContext);
        var tourService = new TourService(dbContext);
        var subscriptionService = new SubscriptionService(dbContext);
        var seeder = new DataSeeder(dbContext, poiService, tourService, subscriptionService);

        await seeder.SeedAsync();

        var premiumUser = await dbContext.Users
            .FirstOrDefaultAsync(u => u.ExternalRef == "USER_PREMIUM");

        Assert.NotNull(premiumUser);

        var entitlements = await dbContext.UserEntitlements
            .Where(e => e.UserId == premiumUser.Id && e.RevokedAtUtc == null)
            .ToListAsync();

        Assert.NotEmpty(entitlements);
    }
}
