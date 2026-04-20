using Microsoft.EntityFrameworkCore;
using VinhKhanhAudioGuide.Backend.Application.Services;
using VinhKhanhAudioGuide.Backend.Persistence;

namespace VinhKhanhAudioGuide.Backend.Tests.Application.Services;

public sealed class TourServiceTests
{
    private AudioGuideDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AudioGuideDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AudioGuideDbContext(options);
    }

    private async Task<(PoiService, TourService)> SetupServicesAsync(AudioGuideDbContext dbContext)
    {
        var poiService = new PoiService(dbContext);
        var tourService = new TourService(dbContext);
        return (poiService, tourService);
    }

    [Fact]
    public async Task CreateTourAsync_CreatesNewTour()
    {
        var dbContext = CreateDbContext();
        var (_, tourService) = await SetupServicesAsync(dbContext);

        var tour = await tourService.CreateTourAsync("TOUR001", "Tour 1", "Lịch trình 1");

        Assert.NotNull(tour);
        Assert.Equal("TOUR001", tour.Code);
        Assert.Equal("Tour 1", tour.Name);
    }

    [Fact]
    public async Task CreateTourAsync_ThrowsWhenDuplicate()
    {
        var dbContext = CreateDbContext();
        var (_, tourService) = await SetupServicesAsync(dbContext);

        await tourService.CreateTourAsync("TOUR001", "Tour 1");

        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            tourService.CreateTourAsync("TOUR001", "Tour khác"));
    }

    [Fact]
    public async Task GetTourByIdAsync_ReturnsTourWithStops()
    {
        var dbContext = CreateDbContext();
        var (poiService, tourService) = await SetupServicesAsync(dbContext);

        var poi1 = await poiService.CreatePoiAsync("POI001", "Địa điểm 1", 10.0, 20.0);
        var poi2 = await poiService.CreatePoiAsync("POI002", "Địa điểm 2", 10.1, 20.1);

        var tour = await tourService.CreateTourAsync("TOUR001", "Tour 1");
        await tourService.AddStopAsync(tour.Id, poi1.Id, 1);
        await tourService.AddStopAsync(tour.Id, poi2.Id, 2);

        var retrieved = await tourService.GetTourByIdAsync(tour.Id);

        Assert.NotNull(retrieved);
        Assert.Equal(2, retrieved.Stops.Count);
    }

    [Fact]
    public async Task GetAllToursAsync_ReturnsAllTours()
    {
        var dbContext = CreateDbContext();
        var (_, tourService) = await SetupServicesAsync(dbContext);

        await tourService.CreateTourAsync("TOUR001", "Tour 1");
        await tourService.CreateTourAsync("TOUR002", "Tour 2");

        var tours = await tourService.GetAllToursAsync();

        Assert.Equal(2, tours.Count());
    }

    [Fact]
    public async Task AddStopAsync_AddsPoiToTour()
    {
        var dbContext = CreateDbContext();
        var (poiService, tourService) = await SetupServicesAsync(dbContext);

        var poi = await poiService.CreatePoiAsync("POI001", "Địa điểm 1", 10.0, 20.0);
        var tour = await tourService.CreateTourAsync("TOUR001", "Tour 1");

        var stop = await tourService.AddStopAsync(tour.Id, poi.Id, 1, "Tiếp tục sang phải");

        Assert.NotNull(stop);
        Assert.Equal(1, stop.Sequence);
        Assert.Equal(poi.Id, stop.PoiId);
        Assert.Equal("Tiếp tục sang phải", stop.NextStopHint);
    }

    [Fact]
    public async Task AddStopAsync_ThrowsWhenSequenceDuplicate()
    {
        var dbContext = CreateDbContext();
        var (poiService, tourService) = await SetupServicesAsync(dbContext);

        var poi1 = await poiService.CreatePoiAsync("POI001", "Địa điểm 1", 10.0, 20.0);
        var poi2 = await poiService.CreatePoiAsync("POI002", "Địa điểm 2", 10.1, 20.1);
        var tour = await tourService.CreateTourAsync("TOUR001", "Tour 1");

        await tourService.AddStopAsync(tour.Id, poi1.Id, 1);

        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            tourService.AddStopAsync(tour.Id, poi2.Id, 1));
    }

    [Fact]
    public async Task GetTourStopsAsync_ReturnsStopsOrdered()
    {
        var dbContext = CreateDbContext();
        var (poiService, tourService) = await SetupServicesAsync(dbContext);

        var poi1 = await poiService.CreatePoiAsync("POI001", "Địa điểm 1", 10.0, 20.0);
        var poi2 = await poiService.CreatePoiAsync("POI002", "Địa điểm 2", 10.1, 20.1);
        var poi3 = await poiService.CreatePoiAsync("POI003", "Địa điểm 3", 10.2, 20.2);

        var tour = await tourService.CreateTourAsync("TOUR001", "Tour 1");
        await tourService.AddStopAsync(tour.Id, poi3.Id, 3);
        await tourService.AddStopAsync(tour.Id, poi1.Id, 1);
        await tourService.AddStopAsync(tour.Id, poi2.Id, 2);

        var stops = await tourService.GetTourStopsAsync(tour.Id);

        Assert.Equal(3, stops.Count());
        Assert.Equal(1, stops.ElementAt(0).Sequence);
        Assert.Equal(2, stops.ElementAt(1).Sequence);
        Assert.Equal(3, stops.ElementAt(2).Sequence);
    }

    [Fact]
    public async Task RemoveStopAsync_RemovesStop()
    {
        var dbContext = CreateDbContext();
        var (poiService, tourService) = await SetupServicesAsync(dbContext);

        var poi = await poiService.CreatePoiAsync("POI001", "Địa điểm 1", 10.0, 20.0);
        var tour = await tourService.CreateTourAsync("TOUR001", "Tour 1");
        var stop = await tourService.AddStopAsync(tour.Id, poi.Id, 1);

        await tourService.RemoveStopAsync(stop.Id);

        var stops = await tourService.GetTourStopsAsync(tour.Id);
        Assert.Empty(stops);
    }

    [Fact]
    public async Task GetNextStopAsync_ReturnsNextStop()
    {
        var dbContext = CreateDbContext();
        var (poiService, tourService) = await SetupServicesAsync(dbContext);

        var poi1 = await poiService.CreatePoiAsync("POI001", "Địa điểm 1", 10.0, 20.0);
        var poi2 = await poiService.CreatePoiAsync("POI002", "Địa điểm 2", 10.1, 20.1);
        var poi3 = await poiService.CreatePoiAsync("POI003", "Địa điểm 3", 10.2, 20.2);

        var tour = await tourService.CreateTourAsync("TOUR001", "Tour 1");
        await tourService.AddStopAsync(tour.Id, poi1.Id, 1);
        await tourService.AddStopAsync(tour.Id, poi2.Id, 2);
        await tourService.AddStopAsync(tour.Id, poi3.Id, 3);

        var nextStop = await tourService.GetNextStopAsync(tour.Id, 1);

        Assert.NotNull(nextStop);
        Assert.Equal(2, nextStop.Sequence);
        Assert.Equal(poi2.Id, nextStop.PoiId);
    }

    [Fact]
    public async Task GetPreviousStopAsync_ReturnsPreviousStop()
    {
        var dbContext = CreateDbContext();
        var (poiService, tourService) = await SetupServicesAsync(dbContext);

        var poi1 = await poiService.CreatePoiAsync("POI001", "Địa điểm 1", 10.0, 20.0);
        var poi2 = await poiService.CreatePoiAsync("POI002", "Địa điểm 2", 10.1, 20.1);
        var poi3 = await poiService.CreatePoiAsync("POI003", "Địa điểm 3", 10.2, 20.2);

        var tour = await tourService.CreateTourAsync("TOUR001", "Tour 1");
        await tourService.AddStopAsync(tour.Id, poi1.Id, 1);
        await tourService.AddStopAsync(tour.Id, poi2.Id, 2);
        await tourService.AddStopAsync(tour.Id, poi3.Id, 3);

        var prevStop = await tourService.GetPreviousStopAsync(tour.Id, 3);

        Assert.NotNull(prevStop);
        Assert.Equal(2, prevStop.Sequence);
        Assert.Equal(poi2.Id, prevStop.PoiId);
    }

    [Fact]
    public async Task ReorderStopsAsync_ReordersSequences()
    {
        var dbContext = CreateDbContext();
        var (poiService, tourService) = await SetupServicesAsync(dbContext);

        var poi1 = await poiService.CreatePoiAsync("POI001", "Địa điểm 1", 10.0, 20.0);
        var poi2 = await poiService.CreatePoiAsync("POI002", "Địa điểm 2", 10.1, 20.1);
        var poi3 = await poiService.CreatePoiAsync("POI003", "Địa điểm 3", 10.2, 20.2);

        var tour = await tourService.CreateTourAsync("TOUR001", "Tour 1");
        var stop1 = await tourService.AddStopAsync(tour.Id, poi1.Id, 1);
        var stop2 = await tourService.AddStopAsync(tour.Id, poi2.Id, 2);
        var stop3 = await tourService.AddStopAsync(tour.Id, poi3.Id, 3);

        // Reorder: 3 -> 1 -> 2
        await tourService.ReorderStopsAsync(tour.Id, new List<Guid> { stop3.Id, stop1.Id, stop2.Id });

        var stops = await tourService.GetTourStopsAsync(tour.Id);
        Assert.Equal(stop3.Id, stops.ElementAt(0).Id);
        Assert.Equal(1, stops.ElementAt(0).Sequence);
        Assert.Equal(stop1.Id, stops.ElementAt(1).Id);
        Assert.Equal(2, stops.ElementAt(1).Sequence);
        Assert.Equal(stop2.Id, stops.ElementAt(2).Id);
        Assert.Equal(3, stops.ElementAt(2).Sequence);
    }
}
