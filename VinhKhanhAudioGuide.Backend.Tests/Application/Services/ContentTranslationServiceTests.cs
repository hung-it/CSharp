using Microsoft.EntityFrameworkCore;
using VinhKhanhAudioGuide.Backend.Application.Services;
using VinhKhanhAudioGuide.Backend.Persistence;

namespace VinhKhanhAudioGuide.Backend.Tests.Application.Services;

public sealed class ContentTranslationServiceTests
{
    private AudioGuideDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AudioGuideDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AudioGuideDbContext(options);
    }

    [Fact]
    public async Task UpsertTranslationAsync_CreatesAndUpdates()
    {
        var db = CreateDbContext();
        var service = new ContentTranslationService(db);

        var created = await service.UpsertTranslationAsync("poi.POI001.name", "vi", "Bánh mì Xóm Chiếu");
        var updated = await service.UpsertTranslationAsync("poi.POI001.name", "vi", "Banh mi Xom Chieu");

        Assert.Equal(created.Id, updated.Id);
        Assert.Equal("Banh mi Xom Chieu", updated.Value);
    }

    [Fact]
    public async Task GetTranslationAsync_FallsBackToDefaultLanguage()
    {
        var db = CreateDbContext();
        var service = new ContentTranslationService(db);

        await service.UpsertTranslationAsync("poi.POI001.desc", "vi", "Mo ta tieng Viet");

        var value = await service.GetTranslationAsync("poi.POI001.desc", "en");

        Assert.Equal("Mo ta tieng Viet", value);
    }

    [Fact]
    public async Task DeleteTranslationAsync_RemovesTranslation()
    {
        var db = CreateDbContext();
        var service = new ContentTranslationService(db);

        await service.UpsertTranslationAsync("poi.POI002.name", "en", "Pho spot");
        await service.DeleteTranslationAsync("poi.POI002.name", "en");

        var value = await service.GetTranslationAsync("poi.POI002.name", "en", "en");
        Assert.Null(value);
    }
}
