using Microsoft.EntityFrameworkCore;
using VinhKhanhAudioGuide.Backend.Application.Services;
using VinhKhanhAudioGuide.Backend.Domain.Entities;
using VinhKhanhAudioGuide.Backend.Domain.Enums;
using VinhKhanhAudioGuide.Backend.Persistence;

namespace VinhKhanhAudioGuide.Backend.Tests.Application.Services;

public sealed class QrPlaybackServiceTests
{
    private AudioGuideDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AudioGuideDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AudioGuideDbContext(options);
    }

    [Fact]
    public async Task ResolvePlaybackContentAsync_ParsesQrPrefixAndReturnsAudio()
    {
        var db = CreateDbContext();

        var user = new User { ExternalRef = "USER001" };
        var poi = new Poi { Code = "POI001", Name = "Quan banh mi", Latitude = 10.0, Longitude = 106.0 };

        db.Users.Add(user);
        db.Pois.Add(poi);
        db.AudioAssets.Add(new AudioAsset
        {
            PoiId = poi.Id,
            LanguageCode = "vi",
            FilePath = "audio/POI001-vi.mp3",
            DurationSeconds = 60
        });

        await db.SaveChangesAsync();

        var listeningService = new ListeningSessionService(db);
        var subscriptionService = new SubscriptionService(db);
        var qrService = new QrPlaybackService(db, listeningService, subscriptionService);

        var content = await qrService.ResolvePlaybackContentAsync("QR:POI001");

        Assert.Equal(poi.Id, content.PoiId);
        Assert.Equal("audio/POI001-vi.mp3", content.AudioPath);
    }

    [Fact]
    public async Task StartSessionByQrAsync_StartsQrSessionWhenUserHasAccess()
    {
        var db = CreateDbContext();

        var user = new User { ExternalRef = "USER001" };
        var poi = new Poi { Code = "POI002", Name = "Cho Xom Chieu", Latitude = 10.1, Longitude = 106.1 };

        db.Users.Add(user);
        db.Pois.Add(poi);
        db.FeatureSegments.Add(new FeatureSegment { Code = "basic.poi", Name = "Basic POI" });
        db.AudioAssets.Add(new AudioAsset
        {
            PoiId = poi.Id,
            LanguageCode = "vi",
            FilePath = "audio/POI002-vi.mp3",
            DurationSeconds = 65
        });

        await db.SaveChangesAsync();

        var subscriptionService = new SubscriptionService(db);
        await subscriptionService.ActivateSubscriptionAsync(user.Id, PlanTier.Basic, 1m);

        var listeningService = new ListeningSessionService(db);
        var qrService = new QrPlaybackService(db, listeningService, subscriptionService);

        var result = await qrService.StartSessionByQrAsync(user.Id, "POI002");

        Assert.Equal(TriggerSource.QrCode, result.Session.TriggerSource);
        Assert.Equal(poi.Id, result.Session.PoiId);
        Assert.Equal("POI002", result.Content.PoiCode);
    }

    [Fact]
    public async Task StartSessionByQrAsync_ThrowsWhenUserHasNoBasicAccess()
    {
        var db = CreateDbContext();

        var user = new User { ExternalRef = "USER001" };
        var poi = new Poi { Code = "POI003", Name = "Dinh Xom Chieu", Latitude = 10.2, Longitude = 106.2 };

        db.Users.Add(user);
        db.Pois.Add(poi);
        db.AudioAssets.Add(new AudioAsset
        {
            PoiId = poi.Id,
            LanguageCode = "vi",
            FilePath = "audio/POI003-vi.mp3",
            DurationSeconds = 70
        });

        await db.SaveChangesAsync();

        var subscriptionService = new SubscriptionService(db);
        var listeningService = new ListeningSessionService(db);
        var qrService = new QrPlaybackService(db, listeningService, subscriptionService);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            qrService.StartSessionByQrAsync(user.Id, "POI003"));
    }
}
