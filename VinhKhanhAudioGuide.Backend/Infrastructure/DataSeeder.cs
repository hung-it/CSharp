using Microsoft.EntityFrameworkCore;
using VinhKhanhAudioGuide.Backend.Application.Services;
using VinhKhanhAudioGuide.Backend.Domain.Entities;
using VinhKhanhAudioGuide.Backend.Domain.Enums;
using VinhKhanhAudioGuide.Backend.Persistence;

namespace VinhKhanhAudioGuide.Backend.Infrastructure;

public interface IDataSeeder
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}

public sealed class DataSeeder(
    AudioGuideDbContext dbContext,
    IPoiService poiService,
    ITourService tourService,
    ISubscriptionService subscriptionService) : IDataSeeder
{
    private readonly AudioGuideDbContext _dbContext = dbContext;
    private readonly IPoiService _poiService = poiService;
    private readonly ITourService _tourService = tourService;
    private readonly ISubscriptionService _subscriptionService = subscriptionService;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await _dbContext.Pois.AnyAsync(cancellationToken))
        {
            return;
        }

        await EnsureFeatureSegmentsAsync(cancellationToken);
        await SeedPoisAsync(cancellationToken);
        await SeedToursAsync(cancellationToken);
        await SeedUsersAndSubscriptionsAsync(cancellationToken);
    }

    private async Task EnsureFeatureSegmentsAsync(CancellationToken cancellationToken)
    {
        var existingSegments = await _dbContext.FeatureSegments.CountAsync(cancellationToken);
        if (existingSegments > 0)
        {
            return;
        }

        _dbContext.FeatureSegments.AddRange(
            new FeatureSegment { Code = "basic.poi", Name = "Basic POI" },
            new FeatureSegment { Code = "premium.segment.tour", Name = "Premium Tour Segment" },
            new FeatureSegment { Code = "premium.segment.audio", Name = "Premium Audio Segment" },
            new FeatureSegment { Code = "premium.segment.analytics", Name = "Premium Analytics Segment" });

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedPoisAsync(CancellationToken cancellationToken)
    {
        var pois = new[]
        {
            // Xóm Chiếu
            ("POI001", "Quán Bánh Mì Xóm Chiếu", 10.7530, 106.6878, "Quán bánh mì nổi tiếng", "Xóm Chiếu"),
            ("POI002", "Chợ Xóm Chiếu", 10.7540, 106.6880, "Chợ truyền thống", "Xóm Chiếu"),
            ("POI003", "Đình Xóm Chiếu", 10.7550, 106.6882, "Đình thờ cổ", "Xóm Chiếu"),

            // Vĩnh Hội
            ("POI004", "Nhà Thờ Vĩnh Hội", 10.7600, 106.6900, "Nhà thờ Công giáo", "Vĩnh Hội"),
            ("POI005", "Khách Sạn Vĩnh Hội", 10.7610, 106.6910, "Khách sạn lịch sử", "Vĩnh Hội"),
            ("POI006", "Cây Cổ Thụ Vĩnh Hội", 10.7620, 106.6920, "Cây cổ thụ hơn 100 năm", "Vĩnh Hội"),

            // Khánh Hội
            ("POI007", "Tiệm Cơm Khánh Hội", 10.7670, 106.6950, "Tiệm cơm nổi tiếng", "Khánh Hội"),
            ("POI008", "Chùa Khánh Hội", 10.7680, 106.6960, "Chùa Phật giáo", "Khánh Hội"),
            ("POI009", "Hồ Cá Khánh Hội", 10.7690, 106.6970, "Hồ cá tự nhiên", "Khánh Hội"),
        };

        foreach (var (code, name, lat, lon, desc, district) in pois)
        {
            var poi = await _poiService.CreatePoiAsync(code, name, lat, lon, 30, desc, district, cancellationToken);

            await _poiService.AssignAudioAsync(poi.Id, "vi", $"/audio/{code}-vi.mp3", 60, false, cancellationToken);
            await _poiService.AssignAudioAsync(poi.Id, "en", $"/audio/{code}-en.mp3", 65, false, cancellationToken);
        }
    }

    private async Task SeedToursAsync(CancellationToken cancellationToken)
    {
        var tour1 = await _tourService.CreateTourAsync(
            "TOUR_CITY", "Vòng Quanh Phố",
            "Tuyến du lịch quanh 3 quận Xóm Chiếu, Vĩnh Hội, Khánh Hội",
            cancellationToken);

        var tour2 = await _tourService.CreateTourAsync(
            "TOUR_FOOD", "Tuyến Ẩm Thực",
            "Khám phá những quán ăn nổi tiếng",
            cancellationToken);

        var pois = await _poiService.GetAllPoiAsync(cancellationToken);
        var poiList = pois.ToList();

        await _tourService.AddStopAsync(tour1.Id, poiList[0].Id, 1, "Qua chợ", cancellationToken);
        await _tourService.AddStopAsync(tour1.Id, poiList[1].Id, 2, "Tìm đình", cancellationToken);
        await _tourService.AddStopAsync(tour1.Id, poiList[2].Id, 3, "Sang Vĩnh Hội", cancellationToken);
        await _tourService.AddStopAsync(tour1.Id, poiList[3].Id, 4, "Ghé nhà thờ", cancellationToken);
        await _tourService.AddStopAsync(tour1.Id, poiList[4].Id, 5, "Lên cây cổ thụ", cancellationToken);
        await _tourService.AddStopAsync(tour1.Id, poiList[5].Id, 6, "Sang Khánh Hội", cancellationToken);
        await _tourService.AddStopAsync(tour1.Id, poiList[6].Id, 7, "Ăn cơm", cancellationToken);
        await _tourService.AddStopAsync(tour1.Id, poiList[7].Id, 8, "Viếng chùa", cancellationToken);
        await _tourService.AddStopAsync(tour1.Id, poiList[8].Id, 9, "Kết thúc", cancellationToken);

        await _tourService.AddStopAsync(tour2.Id, poiList[0].Id, 1, "Sang quán cơm", cancellationToken);
        await _tourService.AddStopAsync(tour2.Id, poiList[6].Id, 2, "Ghé hồ cá", cancellationToken);
        await _tourService.AddStopAsync(tour2.Id, poiList[8].Id, 3, "Kết thúc", cancellationToken);
    }

    private async Task SeedUsersAndSubscriptionsAsync(CancellationToken cancellationToken)
    {
        var user = new User
        {
            ExternalRef = "USER_DEMO",
            PreferredLanguage = "vi"
        };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var basicSub = await _subscriptionService.ActivateSubscriptionAsync(
            user.Id, PlanTier.Basic, 1m, cancellationToken);

        var premiumUser = new User
        {
            ExternalRef = "USER_PREMIUM",
            PreferredLanguage = "vi"
        };
        _dbContext.Users.Add(premiumUser);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var premiumSub = await _subscriptionService.ActivateSubscriptionAsync(
            premiumUser.Id, PlanTier.PremiumSegmented, 10m, cancellationToken);
    }
}
