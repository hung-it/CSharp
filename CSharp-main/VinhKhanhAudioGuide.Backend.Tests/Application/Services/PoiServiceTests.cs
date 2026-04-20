using Microsoft.EntityFrameworkCore;
using VinhKhanhAudioGuide.Backend.Application.Services;
using VinhKhanhAudioGuide.Backend.Persistence;

namespace VinhKhanhAudioGuide.Backend.Tests.Application.Services;

public sealed class PoiServiceTests
{
    private AudioGuideDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AudioGuideDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AudioGuideDbContext(options);
    }

    [Fact]
    public async Task CreatePoiAsync_CreatesNewPoi()
    {
        var dbContext = CreateDbContext();
        var service = new PoiService(dbContext);

        var poi = await service.CreatePoiAsync("POI001", "Địa điểm 1", 10.123, 20.456, 50, "Mô tả", "Xóm Chiếu");

        Assert.NotNull(poi);
        Assert.Equal("POI001", poi.Code);
        Assert.Equal("Địa điểm 1", poi.Name);
        Assert.Equal(10.123, poi.Latitude);
        Assert.Equal(20.456, poi.Longitude);
    }

    [Fact]
    public async Task CreatePoiAsync_ThrowsWhenDuplicate()
    {
        var dbContext = CreateDbContext();
        var service = new PoiService(dbContext);

        await service.CreatePoiAsync("POI001", "Địa điểm 1", 10.0, 20.0);

        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            service.CreatePoiAsync("POI001", "Địa điểm khác", 10.0, 20.0));
    }

    [Fact]
    public async Task GetPoiByIdAsync_ReturnsPoiWhenExists()
    {
        var dbContext = CreateDbContext();
        var service = new PoiService(dbContext);

        var poi = await service.CreatePoiAsync("POI001", "Địa điểm 1", 10.0, 20.0);
        var retrieved = await service.GetPoiByIdAsync(poi.Id);

        Assert.NotNull(retrieved);
        Assert.Equal(poi.Id, retrieved.Id);
        Assert.Equal("POI001", retrieved.Code);
    }

    [Fact]
    public async Task GetPoiByIdAsync_ReturnsNullWhenNotExists()
    {
        var dbContext = CreateDbContext();
        var service = new PoiService(dbContext);

        var retrieved = await service.GetPoiByIdAsync(Guid.NewGuid());

        Assert.Null(retrieved);
    }

    [Fact]
    public async Task GetPoiByCodeAsync_ReturnsPoi()
    {
        var dbContext = CreateDbContext();
        var service = new PoiService(dbContext);

        await service.CreatePoiAsync("POI001", "Địa điểm 1", 10.0, 20.0);
        var retrieved = await service.GetPoiByCodeAsync("POI001");

        Assert.NotNull(retrieved);
        Assert.Equal("POI001", retrieved.Code);
    }

    [Fact]
    public async Task GetAllPoiAsync_ReturnAllPois()
    {
        var dbContext = CreateDbContext();
        var service = new PoiService(dbContext);

        await service.CreatePoiAsync("POI001", "Địa điểm 1", 10.0, 20.0);
        await service.CreatePoiAsync("POI002", "Địa điểm 2", 10.1, 20.1);
        await service.CreatePoiAsync("POI003", "Địa điểm 3", 10.2, 20.2);

        var pois = await service.GetAllPoiAsync();

        Assert.Equal(3, pois.Count());
    }

    [Fact]
    public async Task GetPoisByDistrictAsync_ReturnsPoiInDistrict()
    {
        var dbContext = CreateDbContext();
        var service = new PoiService(dbContext);

        await service.CreatePoiAsync("POI001", "Địa điểm 1", 10.0, 20.0, 30, null, "Xóm Chiếu");
        await service.CreatePoiAsync("POI002", "Địa điểm 2", 10.1, 20.1, 30, null, "Xóm Chiếu");
        await service.CreatePoiAsync("POI003", "Địa điểm 3", 10.2, 20.2, 30, null, "Vĩnh Hội");

        var pois = await service.GetPoisByDistrictAsync("Xóm Chiếu");

        Assert.Equal(2, pois.Count());
        Assert.All(pois, p => Assert.Equal("Xóm Chiếu", p.District));
    }

    [Fact]
    public async Task UpdatePoiAsync_UpdatesPoiMetadata()
    {
        var dbContext = CreateDbContext();
        var service = new PoiService(dbContext);

        var poi = await service.CreatePoiAsync("POI001", "Địa điểm 1", 10.0, 20.0, 30);
        var updated = await service.UpdatePoiAsync(
            poi.Id,
            name: "Địa điểm mới",
            description: "Mô tả mới",
            triggerRadiusMeters: 50);

        Assert.Equal("Địa điểm mới", updated.Name);
        Assert.Equal("Mô tả mới", updated.Description);
        Assert.Equal(50, updated.TriggerRadiusMeters);
    }

    [Fact]
    public async Task UpdatePoiAsync_ThrowsWhenNotFound()
    {
        var dbContext = CreateDbContext();
        var service = new PoiService(dbContext);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => 
            service.UpdatePoiAsync(Guid.NewGuid(), name: "Mới"));
    }

    [Fact]
    public async Task AssignAudioAsync_CreatesNewAudio()
    {
        var dbContext = CreateDbContext();
        var service = new PoiService(dbContext);

        var poi = await service.CreatePoiAsync("POI001", "Địa điểm 1", 10.0, 20.0);
        await service.AssignAudioAsync(poi.Id, "vi", "/audio/poi001-vi.mp3", 120);

        var audios = await service.GetAudiosByPoiAsync(poi.Id);

        Assert.Single(audios);
        Assert.Equal("vi", audios.First().LanguageCode);
        Assert.Equal("/audio/poi001-vi.mp3", audios.First().FilePath);
    }

    [Fact]
    public async Task AssignAudioAsync_UpdatesExistingAudio()
    {
        var dbContext = CreateDbContext();
        var service = new PoiService(dbContext);

        var poi = await service.CreatePoiAsync("POI001", "Địa điểm 1", 10.0, 20.0);
        await service.AssignAudioAsync(poi.Id, "vi", "/audio/old.mp3", 100);
        await service.AssignAudioAsync(poi.Id, "vi", "/audio/new.mp3", 150);

        var audios = await service.GetAudiosByPoiAsync(poi.Id);

        Assert.Single(audios);
        Assert.Equal("/audio/new.mp3", audios.First().FilePath);
        Assert.Equal(150, audios.First().DurationSeconds);
    }

    [Fact]
    public async Task GetAudioByLanguageAsync_ReturnsAudioForLanguage()
    {
        var dbContext = CreateDbContext();
        var service = new PoiService(dbContext);

        var poi = await service.CreatePoiAsync("POI001", "Địa điểm 1", 10.0, 20.0);
        await service.AssignAudioAsync(poi.Id, "vi", "/audio/poi001-vi.mp3", 120);
        await service.AssignAudioAsync(poi.Id, "en", "/audio/poi001-en.mp3", 130);

        var viAudio = await service.GetAudioByLanguageAsync(poi.Id, "vi");
        var enAudio = await service.GetAudioByLanguageAsync(poi.Id, "en");

        Assert.NotNull(viAudio);
        Assert.Equal("vi", viAudio.LanguageCode);
        Assert.NotNull(enAudio);
        Assert.Equal("en", enAudio.LanguageCode);
    }
}
