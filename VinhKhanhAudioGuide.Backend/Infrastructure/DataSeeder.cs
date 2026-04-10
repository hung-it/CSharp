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
        var existingCodes = await _dbContext.Pois
            .Select(p => p.Code)
            .ToListAsync(cancellationToken);

        var seededCodeSet = new HashSet<string>(existingCodes, StringComparer.OrdinalIgnoreCase);

        var pois = new[]
        {
            // Xóm Chiếu
            ("POI001", "Quán Bánh Mì Xóm Chiếu", 10.7530, 106.6878, "Quán bánh mì nổi tiếng", "Xóm Chiếu", 3),
            ("POI002", "Chợ Xóm Chiếu", 10.7540, 106.6880, "Chợ truyền thống", "Xóm Chiếu", 2),
            ("POI003", "Đình Xóm Chiếu", 10.7550, 106.6882, "Đình thờ cổ", "Xóm Chiếu", 1),
            ("POI010", "Nhà Cổ Xóm Chiếu", 10.7524, 106.6869, "Nhà cổ mang kiến trúc Nam Bộ", "Xóm Chiếu", 2),

            // Vĩnh Hội
            ("POI004", "Nhà Thờ Vĩnh Hội", 10.7600, 106.6900, "Nhà thờ Công giáo", "Vĩnh Hội", 2),
            ("POI005", "Khách Sạn Vĩnh Hội", 10.7610, 106.6910, "Khách sạn lịch sử", "Vĩnh Hội", 1),
            ("POI006", "Cây Cổ Thụ Vĩnh Hội", 10.7620, 106.6920, "Cây cổ thụ hơn 100 năm", "Vĩnh Hội", 3),
            ("POI011", "Hẻm Ẩm Thực Vĩnh Hội", 10.7617, 106.6896, "Khu hẻm ăn uống địa phương", "Vĩnh Hội", 3),
            ("POI012", "Công Viên Bờ Kênh", 10.7632, 106.6931, "Không gian xanh ven kênh", "Vĩnh Hội", 2),

            // Khánh Hội
            ("POI007", "Tiệm Cơm Khánh Hội", 10.7670, 106.6950, "Tiệm cơm nổi tiếng", "Khánh Hội", 3),
            ("POI008", "Chùa Khánh Hội", 10.7680, 106.6960, "Chùa Phật giáo", "Khánh Hội", 1),
            ("POI009", "Hồ Cá Khánh Hội", 10.7690, 106.6970, "Hồ cá tự nhiên", "Khánh Hội", 2),
            ("POI013", "Bến Tàu Khánh Hội", 10.7661, 106.6984, "Bến tàu cũ gắn với lịch sử khu vực", "Khánh Hội", 1),
            ("POI014", "Chợ Đêm Khánh Hội", 10.7702, 106.6968, "Khu chợ đêm sôi động", "Khánh Hội", 3),
        };

        foreach (var (code, name, lat, lon, desc, district, priority) in pois)
        {
            if (seededCodeSet.Contains(code))
            {
                continue;
            }

            var poi = await _poiService.CreatePoiAsync(
                code,
                name,
                lat,
                lon,
                30,
                desc,
                district,
                priority,
                $"https://picsum.photos/seed/{code}/640/360",
                $"https://maps.google.com/?q={lat},{lon}",
                cancellationToken);

            await _poiService.AssignAudioAsync(poi.Id, "vi", $"/audio/{code}-vi.mp3", 60, false, cancellationToken);
            await _poiService.AssignAudioAsync(poi.Id, "en", $"/audio/{code}-en.mp3", 65, false, cancellationToken);
            seededCodeSet.Add(code);
        }
    }

    private async Task SeedToursAsync(CancellationToken cancellationToken)
    {
        if (await _dbContext.Tours.AnyAsync(cancellationToken))
        {
            return;
        }

        var tour1 = await _tourService.CreateTourAsync(
            "TOUR_CITY", "Vòng Quanh Phố",
            "Tuyến du lịch quanh 3 quận Xóm Chiếu, Vĩnh Hội, Khánh Hội",
            cancellationToken);

        var tour2 = await _tourService.CreateTourAsync(
            "TOUR_FOOD", "Tuyến Ẩm Thực",
            "Khám phá những quán ăn nổi tiếng",
            cancellationToken);

        var pois = await _poiService.GetAllPoiAsync(cancellationToken);
        var poiMap = pois.ToDictionary(p => p.Code, StringComparer.OrdinalIgnoreCase);

        var cityStops = new (string Code, string Note)[]
        {
            ("POI002", "Qua chợ"),
            ("POI003", "Tìm đình"),
            ("POI010", "Ghé nhà cổ"),
            ("POI004", "Sang Vĩnh Hội"),
            ("POI006", "Lên cây cổ thụ"),
            ("POI012", "Dạo công viên"),
            ("POI007", "Sang Khánh Hội"),
            ("POI008", "Viếng chùa"),
            ("POI013", "Thăm bến tàu"),
            ("POI009", "Kết thúc"),
        };

        var cityOrder = 1;
        foreach (var (code, note) in cityStops)
        {
            if (!poiMap.TryGetValue(code, out var poi))
            {
                continue;
            }

            await _tourService.AddStopAsync(tour1.Id, poi.Id, cityOrder, note, cancellationToken);
            cityOrder++;
        }

        var foodStops = new (string Code, string Note)[]
        {
            ("POI001", "Bắt đầu với bánh mì"),
            ("POI011", "Ghé hẻm ẩm thực"),
            ("POI007", "Sang quán cơm"),
            ("POI014", "Dạo chợ đêm"),
            ("POI009", "Kết thúc"),
        };

        var foodOrder = 1;
        foreach (var (code, note) in foodStops)
        {
            if (!poiMap.TryGetValue(code, out var poi))
            {
                continue;
            }

            await _tourService.AddStopAsync(tour2.Id, poi.Id, foodOrder, note, cancellationToken);
            foodOrder++;
        }
    }

    private async Task SeedUsersAndSubscriptionsAsync(CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.ExternalRef == "USER_DEMO", cancellationToken);

        if (user is null)
        {
            user = new User
            {
                ExternalRef = "USER_DEMO",
                PreferredLanguage = "vi"
            };
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var hasBasicSub = await _dbContext.Subscriptions
            .AnyAsync(s => s.UserId == user.Id, cancellationToken);
        if (!hasBasicSub)
        {
            await _subscriptionService.ActivateSubscriptionAsync(
                user.Id, PlanTier.Basic, 1m, cancellationToken);
        }

        var premiumUser = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.ExternalRef == "USER_PREMIUM", cancellationToken);

        if (premiumUser is null)
        {
            premiumUser = new User
            {
                ExternalRef = "USER_PREMIUM",
                PreferredLanguage = "vi"
            };
            _dbContext.Users.Add(premiumUser);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var hasPremiumSub = await _dbContext.Subscriptions
            .AnyAsync(s => s.UserId == premiumUser.Id, cancellationToken);
        if (!hasPremiumSub)
        {
            await _subscriptionService.ActivateSubscriptionAsync(
                premiumUser.Id, PlanTier.PremiumSegmented, 10m, cancellationToken);
        }
    }
}
