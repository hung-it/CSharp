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
        await SeedUsersAsync(cancellationToken);
        await SeedSubscriptionsAsync(cancellationToken);
        await SeedPoisWithAudioAndTranslationsAsync(cancellationToken);
        await SeedToursWithStopsAsync(cancellationToken);
        await SeedQRCodesAsync(cancellationToken);
    }

    private async Task EnsureFeatureSegmentsAsync(CancellationToken cancellationToken)
    {
        if (await _dbContext.FeatureSegments.AnyAsync(cancellationToken))
            return;

        _dbContext.FeatureSegments.AddRange(
            new FeatureSegment { Code = "basic.poi", Name = "Basic POI" },
            new FeatureSegment { Code = "premium.segment.tour", Name = "Premium Tour Segment" },
            new FeatureSegment { Code = "premium.segment.audio", Name = "Premium Audio Segment" },
            new FeatureSegment { Code = "premium.segment.analytics", Name = "Premium Analytics Segment" }
        );
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedUsersAsync(CancellationToken cancellationToken)
    {
        // Admin with password "1"
        await EnsureUserAsync("admin", UserRole.Admin, "vi", "1", "ADMIN_USER", cancellationToken);

        // Shop Owners (Chủ cửa hàng) with password "1"
        // Mỗi owner quản lý 4 POIs (cửa hàng)
        await EnsureUserAsync("owner1", UserRole.ShopManager, "vi", "1", "SHOP_MANAGER_OWNER1", cancellationToken);
        await EnsureUserAsync("owner2", UserRole.ShopManager, "vi", "1", "SHOP_MANAGER_OWNER2", cancellationToken);
        await EnsureUserAsync("owner3", UserRole.ShopManager, "vi", "1", "SHOP_MANAGER_OWNER3", cancellationToken);
    }

    private async Task<User> EnsureUserAsync(string username, UserRole role, string language, string? password, string? externalRef, CancellationToken ct)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == username, ct);
        if (user is null)
        {
            user = new User
            {
                Username = username,
                ExternalRef = !string.IsNullOrEmpty(externalRef) ? externalRef : $"USER_{Guid.NewGuid():N}",
                PreferredLanguage = language,
                Role = role,
                PasswordHash = !string.IsNullOrEmpty(password) ? HashPassword(password) : null
            };
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync(ct);
        }
        return user;
    }

    private async Task SeedSubscriptionsAsync(CancellationToken cancellationToken)
    {
        if (await _dbContext.Subscriptions.AnyAsync(cancellationToken))
            return;

        var users = await _dbContext.Users.ToListAsync(cancellationToken);
        var basicSegment = await _dbContext.FeatureSegments.FirstAsync(s => s.Code == "basic.poi", cancellationToken);

        foreach (var user in users)
        {
            // Create Basic subscription for each user
            _dbContext.Subscriptions.Add(new Subscription
            {
                UserId = user.Id,
                PlanTier = PlanTier.Basic,
                AmountUsd = 0m,
                IsActive = true,
                ActivatedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = DateTime.UtcNow.AddYears(10)
            });

            // Grant basic.poi entitlement
            _dbContext.UserEntitlements.Add(new UserEntitlement
            {
                UserId = user.Id,
                FeatureSegmentId = basicSegment.Id,
                GrantedAtUtc = DateTime.UtcNow
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string HashPassword(string password)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(password + "VinhKhanhSalt2024");
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    private async Task SeedPoisWithAudioAndTranslationsAsync(CancellationToken cancellationToken)
    {
        var existingCodes = await _dbContext.Pois.Select(p => p.Code).ToListAsync(cancellationToken);
        var seededCodes = new HashSet<string>(existingCodes, StringComparer.OrdinalIgnoreCase);

        // Lấy user IDs
        var owner1 = await _dbContext.Users.FirstAsync(u => u.Username == "owner1", cancellationToken);
        var owner2 = await _dbContext.Users.FirstAsync(u => u.Username == "owner2", cancellationToken);
        var owner3 = await _dbContext.Users.FirstAsync(u => u.Username == "owner3", cancellationToken);

        // POI data: (Code, Name, Lat, Lon, Description, District, OwnerId)
        // owner1: 4 POIs (POI001-004)
        // owner2: 4 POIs (POI005-008)
        // owner3: 4 POIs (POI009-012)
        var pois = new[]
        {
            // Owner1's POIs (Cửa hàng 1-4)
            ("POI001", "Quán Bánh Mì Đặc Biệt", 10.7530, 106.6878, "Quán bánh mì nổi tiếng nhất vùng với công thức gia truyền", "Xóm Chiếu", owner1.Id),
            ("POI002", "Tiệm Cơm Gia Đình", 10.7670, 106.6950, "Tiệm cơm bình dân được yêu thích nhất khu vực", "Khánh Hội", owner1.Id),
            ("POI003", "Hẻm Ăn Vĩnh Hội", 10.7617, 106.6896, "Khu hẻm ăn uống địa phương nổi tiếng với nhiều món ngon", "Vĩnh Hội", owner1.Id),
            ("POI004", "Quán Cà Phê Sân Đình", 10.7548, 106.6875, "Quán cà phê sân đình view đẹp, nơi hội họp của người dân", "Xóm Chiếu", owner1.Id),

            // Owner2's POIs (Cửa hàng 5-8)
            ("POI005", "Chợ Xóm Chiếu", 10.7540, 106.6880, "Chợ truyền thống với nhiều đặc sản địa phương", "Xóm Chiếu", owner2.Id),
            ("POI006", "Nhà Thờ Vĩnh Hội", 10.7600, 106.6900, "Nhà thờ Công giáo với kiến trúc Pháp đẹp mắt", "Vĩnh Hội", owner2.Id),
            ("POI007", "Hồ Cá Cảnh", 10.7695, 106.6970, "Hồ cá tự nhiên với cảnh quan đẹp", "Khánh Hội", owner2.Id),
            ("POI008", "Cây Cổ Thụ", 10.7620, 106.6920, "Cây cổ thụ hơn 100 năm tuổi, biểu tượng của khu phố", "Vĩnh Hội", owner2.Id),

            // Owner3's POIs (Cửa hàng 9-12)
            ("POI009", "Đình Xóm Chiếu", 10.7550, 106.6882, "Đình thờ cổ có lịch sử hơn 100 năm", "Xóm Chiếu", owner3.Id),
            ("POI010", "Chùa An Lạc", 10.7680, 106.6960, "Chùa Phật giáo với kiến trúc đẹp và không gian yên bình", "Khánh Hội", owner3.Id),
            ("POI011", "Khách Sạn Vĩnh Hội", 10.7610, 106.6910, "Khách sạn lịch sử từ thời Pháp thuộc", "Vĩnh Hội", owner3.Id),
            ("POI012", "Chợ Đêm Khánh Hội", 10.7702, 106.6968, "Khu chợ đêm sôi động về đêm với nhiều món ăn đường phố", "Khánh Hội", owner3.Id),
        };

        foreach (var (code, name, lat, lon, desc, district, managerId) in pois)
        {
            if (seededCodes.Contains(code)) continue;

            // Tạo POI với ManagerUserId trực tiếp (không qua Shop)
            var poi = new Poi
            {
                Id = Guid.NewGuid(),
                Code = code,
                Name = name,
                Description = desc,
                Latitude = lat,
                Longitude = lon,
                District = district,
                Priority = 2,
                ImageUrl = $"https://picsum.photos/seed/{code}/640/360",
                MapLink = $"https://maps.google.com/?q={lat},{lon}",
                TriggerRadiusMeters = 30,
                ManagerUserId = managerId
            };

            _dbContext.Pois.Add(poi);
            await _dbContext.SaveChangesAsync(cancellationToken);

            // Audio files (2 languages: Vietnamese and English only)
            await _poiService.AssignAudioAsync(poi.Id, "vi", $"/media/audio/{code}-vi.mp3", 45, false, cancellationToken);
            await _poiService.AssignAudioAsync(poi.Id, "en", $"/media/audio/{code}-en.mp3", 50, false, cancellationToken);

            // Translations (Vietnamese and English only)
            await AddTranslationAsync(poi.Id, "vi", "name", name, cancellationToken);
            await AddTranslationAsync(poi.Id, "vi", "description", desc, cancellationToken);
            await AddTranslationAsync(poi.Id, "en", "name", $"English: {name}", cancellationToken);
            await AddTranslationAsync(poi.Id, "en", "description", $"English: {desc}", cancellationToken);

            seededCodes.Add(code);
        }
    }

    private async Task AddTranslationAsync(Guid poiId, string language, string field, string value, CancellationToken ct)
    {
        var key = $"poi.{poiId}.{field}";
        var existing = await _dbContext.ContentTranslations
            .AnyAsync(t => t.ContentKey == key && t.LanguageCode == language, ct);

        if (!existing)
        {
            _dbContext.ContentTranslations.Add(new ContentTranslation
            {
                ContentKey = key,
                LanguageCode = language,
                Value = value
            });
            await _dbContext.SaveChangesAsync(ct);
        }
    }

    private async Task SeedToursWithStopsAsync(CancellationToken cancellationToken)
    {
        if (await _dbContext.Tours.AnyAsync(cancellationToken))
            return;

        var pois = await _dbContext.Pois.AsNoTracking().ToListAsync(cancellationToken);
        if (pois.Count == 0) return;

        // Dictionary để tra cứu POI theo code
        var poiByCode = pois.ToDictionary(p => p.Code, p => p);

        // Tour 1: Khám Phá Phố Ăn Vĩnh Khách (tất cả 12 POIs)
        var tour1 = new Tour
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Code = "TOUR001",
            Name = "Khám Phá Phố Ăn Vĩnh Khách",
            Description = "Hành trình khám phá toàn bộ 12 điểm ẩm thực và di sản văn hóa đặc sắc tại Phố Cổ Vĩnh Khách. Phù hợp cho du khách muốn trải nghiệm trọn vẹn văn hóa ẩm thực Sài Gòn."
        };
        _dbContext.Tours.Add(tour1);

        // Tour 2: Tuyến Ẩm Thực Đường Phố (6 POIs đặc trưng)
        var tour2 = new Tour
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Code = "TOUR002",
            Name = "Tuyến Ăn Uống Đường Phố",
            Description = "Tập trung vào các điểm ăn uống đặc sản: bánh mì, cơm, hẻm ăn và chợ đêm. Phù hợp cho ai yêu thích ẩm thực đường phố Việt Nam."
        };
        _dbContext.Tours.Add(tour2);

        // Tour 3: Khám Phá Di Sản Văn Hóa (6 POIs di sản)
        var tour3 = new Tour
        {
            Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            Code = "TOUR003",
            Name = "Hành Trình Di Sản Văn Hóa",
            Description = "Khám phá các điểm di sản lịch sử: đình, chùa, nhà thờ, nhà cổ. Phù hợp cho du khách yêu thích lịch sử và kiến trúc."
        };
        _dbContext.Tours.Add(tour3);
        
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Thêm stops cho Tour 1 (tất cả 12 POIs, theo thứ tự địa lý)
        var tour1Stops = new[]
        {
            (Code: "POI001", Seq: 1, Hint: "Rẽ phải vào hẻm để tìm quán bánh mì"),
            (Code: "POI004", Seq: 2, Hint: "Đi bộ 200m về hướng đình"),
            (Code: "POI005", Seq: 3, Hint: "Qua ngã tư, chợ ngay trước mặt"),
            (Code: "POI009", Seq: 4, Hint: "Đi dọc theo đường chính"),
            (Code: "POI003", Seq: 5, Hint: "Hẻm nằm giữa khu dân cư"),
            (Code: "POI006", Seq: 6, Hint: "Nhìn tháp chuông từ xa"),
            (Code: "POI008", Seq: 7, Hint: "Cây cổ thụ ngay cạnh nhà thờ"),
            (Code: "POI011", Seq: 8, Hint: "Đi dọc đường Vĩnh Hội"),
            (Code: "POI002", Seq: 9, Hint: "Rẽ trái vào hẻm nhỏ"),
            (Code: "POI007", Seq: 10, Hint: "Đi tiếp 300m về phía nam"),
            (Code: "POI010", Seq: 11, Hint: "Nằm trên đường chính Khánh Hội"),
            (Code: "POI012", Seq: 12, Hint: "Chợ đêm mở từ 18h-22h"),
        };

        foreach (var (code, seq, hint) in tour1Stops)
        {
            if (poiByCode.TryGetValue(code, out var poi))
            {
                _dbContext.TourStops.Add(new TourStop
                {
                    TourId = tour1.Id,
                    PoiId = poi.Id,
                    Sequence = seq,
                    NextStopHint = hint
                });
            }
        }
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Thêm stops cho Tour 2 (tuyến ẩm thực - 6 POIs)
        var tour2Stops = new[]
        {
            (Code: "POI001", Seq: 1, Hint: "Bánh mì đặc biệt giòn rụm"),
            (Code: "POI003", Seq: 2, Hint: "Hẻm có nhiều quán ăn ngon"),
            (Code: "POI002", Seq: 3, Hint: "Tiệm cơm với món đặc sản Nam Bộ"),
            (Code: "POI007", Seq: 4, Hint: "Khu vực hồ cá yên tĩnh"),
            (Code: "POI010", Seq: 5, Hint: "Gần chùa, không gian thoáng mát"),
            (Code: "POI012", Seq: 6, Hint: "Chợ đêm bắt đầu từ 18h"),
        };

        foreach (var (code, seq, hint) in tour2Stops)
        {
            if (poiByCode.TryGetValue(code, out var poi))
            {
                _dbContext.TourStops.Add(new TourStop
                {
                    TourId = tour2.Id,
                    PoiId = poi.Id,
                    Sequence = seq,
                    NextStopHint = hint
                });
            }
        }
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Thêm stops cho Tour 3 (di sản văn hóa - 6 POIs)
        var tour3Stops = new[]
        {
            (Code: "POI009", Seq: 1, Hint: "Đình thờ cổ có từ thế kỷ 19"),
            (Code: "POI004", Seq: 2, Hint: "Quán cà phê sân đình view đẹp"),
            (Code: "POI006", Seq: 3, Hint: "Nhà thờ kiến trúc Pháp colonial"),
            (Code: "POI008", Seq: 4, Hint: "Cây cổ thụ hơn 100 năm tuổi"),
            (Code: "POI011", Seq: 5, Hint: "Khách sạn lịch sử từ thời Pháp"),
            (Code: "POI010", Seq: 6, Hint: "Chùa yên bình, thích hợp nghỉ ngơi"),
        };

        foreach (var (code, seq, hint) in tour3Stops)
        {
            if (poiByCode.TryGetValue(code, out var poi))
            {
                _dbContext.TourStops.Add(new TourStop
                {
                    TourId = tour3.Id,
                    PoiId = poi.Id,
                    Sequence = seq,
                    NextStopHint = hint
                });
            }
        }
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedQRCodesAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (await _dbContext.ShopQRCodes.AnyAsync(cancellationToken))
                return;

            var pois = await _dbContext.Pois
                .Where(p => p.ShopId.HasValue)
                .ToListAsync(cancellationToken);

            foreach (var poi in pois)
            {
                _dbContext.ShopQRCodes.Add(new ShopQRCode
                {
                    ShopId = poi.ShopId!.Value,
                    PoiId = poi.Id,
                    QRPayload = $"vk://poi/{poi.Code}",
                    QRImageUrl = $"https://api.qrserver.com/v1/create-qr-code/?size=200x200&data=vk://poi/{poi.Code}"
                });
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SeedQRCodesAsync error (non-fatal): {ex.Message}");
            // Continue without QR codes - not critical
        }
    }
}
