using Microsoft.EntityFrameworkCore;
using VinhKhanhAudioGuide.Backend.Application.Services;
using VinhKhanhAudioGuide.Backend.Persistence;

namespace VinhKhanhAudioGuide.Backend.Tests.Application.Services;

public sealed class ContentSyncServiceTests
{
    private AudioGuideDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AudioGuideDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AudioGuideDbContext(options);
    }

    [Fact]
    public async Task SyncFromSnapshotAsync_CreatesPoiAudioTranslationTourAndStop()
    {
        var db = CreateDbContext();
        var service = new ContentSyncService(db);

        var snapshot = new ContentSyncSnapshot
        {
            Pois =
            [
                new PoiSyncItem("POI100", "Pho Moi", 10.11, 106.11, 35, "desc", "Khanh Hoi")
            ],
            Audios =
            [
                new AudioSyncItem("POI100", "vi", "audio/poi100-vi.mp3", 60, false)
            ],
            Translations =
            [
                new TranslationSyncItem("poi.POI100.name", "en", "New Pho")
            ],
            Tours =
            [
                new TourSyncItem("TOUR100", "Food Tour", "desc")
            ],
            TourStops =
            [
                new TourStopSyncItem("TOUR100", "POI100", 1, "next")
            ]
        };

        var result = await service.SyncFromSnapshotAsync(snapshot);

        Assert.True(result.Inserted >= 5);
        Assert.Single(db.Pois);
        Assert.Single(db.AudioAssets);
        Assert.Single(db.ContentTranslations);
        Assert.Single(db.Tours);
        Assert.Single(db.TourStops);
    }

    [Fact]
    public async Task SyncFromSnapshotAsync_UpdatesExistingPoi()
    {
        var db = CreateDbContext();
        var service = new ContentSyncService(db);

        await service.SyncFromSnapshotAsync(new ContentSyncSnapshot
        {
            Pois = [new PoiSyncItem("POI200", "Old Name", 10.0, 106.0, 30, null, null)]
        });

        var result = await service.SyncFromSnapshotAsync(new ContentSyncSnapshot
        {
            Pois = [new PoiSyncItem("POI200", "New Name", 11.0, 107.0, 50, "updated", "Vinh Hoi")]
        });

        var poi = await db.Pois.FirstAsync(x => x.Code == "POI200");
        Assert.True(result.Updated >= 1);
        Assert.Equal("New Name", poi.Name);
        Assert.Equal(50, poi.TriggerRadiusMeters);
    }

    [Fact]
    public async Task SyncFromSnapshotAsync_SkipsTourStopWhenReferencesNotFound()
    {
        var db = CreateDbContext();
        var service = new ContentSyncService(db);

        var result = await service.SyncFromSnapshotAsync(new ContentSyncSnapshot
        {
            TourStops = [new TourStopSyncItem("TOUR_MISSING", "POI_MISSING", 1, null)]
        });

        Assert.True(result.Skipped >= 1);
        Assert.Empty(db.TourStops);
    }
}
