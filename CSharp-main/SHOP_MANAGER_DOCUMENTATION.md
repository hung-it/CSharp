# Tính Năng Shop Manager - Hướng Dẫn Chi Tiết

## 📋 Tổng Quan

Shop Manager là một hệ thống quản lý toàn diện cho chủ cửa hàng quản lý:
1. Hồ sơ quán
2. POI (Điểm) của quán
3. Nội dung thuyết minh
4. Bản dịch đa ngôn ngữ
5. Audio và TTS
6. QR code
7. Tour
8. Thống kê & Analytics
9. Luồng duyệt nội dung với Admin

---

## 🗄️ Database Architecture

### Core Entities

#### 1. **ShopProfile** - Hồ sơ quán
```csharp
public sealed class ShopProfile
{
    public Guid Id { get; set; }
    public Guid ManagerUserId { get; set; }
    public string Name { get; set; }                    // Tên quán
    public string? Description { get; set; }            // Mô tả
    public string? Address { get; set; }                // Địa chỉ
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? MapLink { get; set; }                // Link Google Maps
    public string? OpeningHours { get; set; }           // Giờ mở cửa
    public string? AvatarUrl { get; set; }              // Ảnh đại diện
    public string? CoverImageUrl { get; set; }          // Ảnh cover
    public ShopVerificationStatus VerificationStatus { get; set; }  // Trạng thái duyệt
    public DateTime ApprovedAtUtc { get; set; }         // Ngày duyệt
}

public enum ShopVerificationStatus
{
    Pending = 1,      // Chờ duyệt
    Approved = 2,     // Đã duyệt
    Rejected = 3,     // Bị từ chối
    Suspended = 4     // Bị tạm dừng
}
```

#### 2. **ShopContent** - Nội dung thuyết minh
```csharp
public sealed class ShopContent
{
    public Guid Id { get; set; }
    public Guid ShopId { get; set; }
    public Guid PoiId { get; set; }
    public string? TextScript { get; set; }             // Script text gốc
    public ContentApprovalStatus ApprovalStatus { get; set; }
    public DateTime SubmittedAtUtc { get; set; }        // Ngày gửi duyệt
    public DateTime? ApprovedAtUtc { get; set; }        // Ngày được duyệt
    public string? RejectionReason { get; set; }        // Lý do từ chối
}

public enum ContentApprovalStatus
{
    Draft = 1,                  // Nháp
    PendingApproval = 2,        // Chờ duyệt
    Approved = 3,               // Đã duyệt
    Rejected = 4,               // Bị từ chối
    Published = 5               // Đã công bố
}
```

#### 3. **ShopContentTranslation** - Bản dịch
```csharp
public sealed class ShopContentTranslation
{
    public Guid Id { get; set; }
    public Guid ContentId { get; set; }
    public string LanguageCode { get; set; }            // "vi", "en", "fr", ...
    public string? TranslatedText { get; set; }         // Nội dung dịch
    public bool IsAutoTranslated { get; set; }          // Có phải dịch tự động?
}
```

#### 4. **ShopAudioAsset** - Audio upload
```csharp
public sealed class ShopAudioAsset
{
    public Guid Id { get; set; }
    public Guid ShopId { get; set; }
    public Guid PoiId { get; set; }
    public string LanguageCode { get; set; }
    public string FilePath { get; set; }                // Đường dẫn file audio
    public int DurationSeconds { get; set; }
    public AudioSourceType SourceType { get; set; }     // Uploaded | TTS | External
    public bool IsTextToSpeech { get; set; }
    public string? TTSProvider { get; set; }            // "Google", "Azure", "AWS"
}
```

#### 5. **ShopQRCode** - QR code management
```csharp
public sealed class ShopQRCode
{
    public Guid Id { get; set; }
    public Guid ShopId { get; set; }
    public Guid PoiId { get; set; }
    public string QRPayload { get; set; }               // Ví dụ: "QR:POI001"
    public string? QRImageUrl { get; set; }             // PNG image URL
    public int ScanCount { get; set; }                  // Lượt quét
    public DateTime? LastScannedAtUtc { get; set; }
}
```

#### 6. **ShopAnalyticsSnapshot** - Thống kê
```csharp
public sealed class ShopAnalyticsSnapshot
{
    public Guid ShopId { get; set; }
    public Guid PoiId { get; set; }
    public DateTime DateUtc { get; set; }               // Ngày thống kê
    public int ListenCount { get; set; }                // Số lần nghe
    public int QRScanCount { get; set; }                // Số lần quét QR
    public int AverageListeningDurationSeconds { get; set; }
    public int UniqueListenersCount { get; set; }
}
```

#### 7. **ShopLanguageReadiness** - Trạng thái sẵn sàng ngôn ngữ
```csharp
public sealed class ShopLanguageReadiness
{
    public Guid ShopId { get; set; }
    public string LanguageCode { get; set; }
    public int TotalPois { get; set; }
    public int PoiWithTranslation { get; set; }
    public int PoiWithAudio { get; set; }
    public double ReadinessPercentage { get; set; }     // % sẵn sàng
}
```

---

## 🔧 Backend Services

### 1. IShopManagementService
Quản lý hồ sơ quán và nội dung thuyết minh

```csharp
// Shop Profile
Task<ShopProfile> CreateShopAsync(Guid managerUserId, string name, ...);
Task<ShopProfile> UpdateShopAsync(Guid shopId, string? name, ...);
Task<ShopProfile?> GetShopByIdAsync(Guid shopId, CancellationToken cancellationToken);
Task<ShopProfile?> GetShopByManagerAsync(Guid managerUserId, CancellationToken cancellationToken);

// Content
Task<ShopContent> CreateContentAsync(Guid shopId, Guid poiId, string? textScript);
Task<ShopContent> UpdateContentAsync(Guid contentId, string? textScript);
Task SubmitContentForApprovalAsync(Guid contentId, CancellationToken cancellationToken);

// Approval Workflow
Task ApproveContentAsync(Guid contentId, Guid adminUserId, string? notes);
Task RejectContentAsync(Guid contentId, Guid adminUserId, string rejectionReason);

// Translation
Task<ShopContentTranslation> UpsertTranslationAsync(Guid contentId, string languageCode, string translatedText);
```

### 2. IShopAnalyticsService
Thống kê và phân tích

```csharp
// Analytics
Task<ShopAnalyticsSnapshot?> GetDailyAnalyticsAsync(Guid shopId, Guid poiId, DateTime dateUtc);
Task<IEnumerable<ShopAnalyticsSnapshot>> GetAnalyticsByPeriodAsync(Guid shopId, DateTime startDate, DateTime endDate);
Task<IEnumerable<ShopAnalyticsSnapshot>> GetTopPoiAsync(Guid shopId, int limit);
Task RecordListenEventAsync(Guid shopId, Guid poiId, int durationSeconds);
Task RecordQRScanAsync(Guid shopId, Guid poiId);

// Language Readiness
Task<ShopLanguageReadiness?> GetLanguageReadinessAsync(Guid shopId, string languageCode);
Task UpdateLanguageReadinessAsync(Guid shopId);
```

### 3. IShopQRCodeService
Quản lý QR code

```csharp
Task<ShopQRCode> CreateOrGetQRCodeAsync(Guid shopId, Guid poiId, string? poiCode = null);
Task<ShopQRCode?> GetQRCodeByPoiAsync(Guid shopId, Guid poiId);
Task<IEnumerable<ShopQRCode>> GetQRCodesByShopAsync(Guid shopId);
Task<IEnumerable<ShopQRCode>> GetQRCodesByShopExportAsync(Guid shopId);  // For CSV export
Task UpdateQRCodeImageAsync(Guid qrCodeId, string qrImageUrl);
Task DeactivateQRCodeAsync(Guid qrCodeId);
Task ActivateQRCodeAsync(Guid qrCodeId);
```

### 4. IShopAudioService
Quản lý audio và TTS

```csharp
// Audio Assets
Task<ShopAudioAsset> UploadAudioAsync(Guid shopId, Guid poiId, string languageCode, string filePath, int durationSeconds);
Task<ShopAudioAsset?> GetAudioByPoiLanguageAsync(Guid shopId, Guid poiId, string languageCode);
Task<IEnumerable<ShopAudioAsset>> GetAudioByPoiAsync(Guid shopId, Guid poiId);
Task<IEnumerable<ShopAudioAsset>> GetAudioByShopAsync(Guid shopId);
Task DeleteAudioAsync(Guid audioId);

// TTS Configuration
Task<ShopTTSConfiguration> ConfigureTTSAsync(Guid shopId, string languageCode, string ttsProvider, ...);
Task<ShopTTSConfiguration?> GetTTSConfigAsync(Guid shopId, string languageCode);
Task<IEnumerable<ShopTTSConfiguration>> GetAllTTSConfigAsync(Guid shopId);
Task EnableTTSAsync(Guid configId);
Task DisableTTSAsync(Guid configId);
```

### 5. IShopAuthorizationService
Phân quyền truy cập

```csharp
Task<bool> IsShopOwnerAsync(Guid userId, Guid shopId);
Task<bool> CanAccessShopAsync(Guid userId, Guid shopId);
Task<bool> CanManageShopContentAsync(Guid userId, Guid shopId);
Task<Guid?> GetUserShopIdAsync(Guid userId);
```

---

## 📊 API Endpoints (Sắp tới)

### Shop Profile Management
```
POST   /api/v1/shops               - Tạo hồ sơ quán
GET    /api/v1/shops/{shopId}      - Lấy hồ sơ quán
PATCH  /api/v1/shops/{shopId}      - Cập nhật hồ sơ quán
GET    /api/v1/shops/me             - Lấy hồ sơ quán của user hiện tại
```

### Shop Content Management
```
POST   /api/v1/shops/{shopId}/contents           - Tạo nội dung
GET    /api/v1/shops/{shopId}/contents           - Lấy danh sách nội dung
GET    /api/v1/shops/{shopId}/contents/{id}      - Chi tiết nội dung
PATCH  /api/v1/shops/{shopId}/contents/{id}      - Cập nhật nội dung
DELETE /api/v1/shops/{shopId}/contents/{id}      - Xóa nội dung
POST   /api/v1/shops/{shopId}/contents/{id}/submit   - Gửi duyệt
```

### Translation
```
POST   /api/v1/shops/{shopId}/contents/{id}/translations         - Thêm/cập nhật bản dịch
GET    /api/v1/shops/{shopId}/contents/{id}/translations         - Lấy bản dịch
GET    /api/v1/shops/{shopId}/language-readiness                 - Trạng thái sẵn sàng ngôn ngữ
PUT    /api/v1/shops/{shopId}/language-readiness/update          - Cập nhật trạng thái
```

### Audio Management
```
POST   /api/v1/shops/{shopId}/audios               - Upload audio
GET    /api/v1/shops/{shopId}/audios               - Lấy danh sách audio
GET    /api/v1/shops/{shopId}/audios/{audioId}     - Chi tiết audio
DELETE /api/v1/shops/{shopId}/audios/{audioId}     - Xóa audio
```

### TTS Configuration
```
POST   /api/v1/shops/{shopId}/tts-config           - Cấu hình TTS
GET    /api/v1/shops/{shopId}/tts-config           - Lấy cấu hình TTS
POST   /api/v1/shops/{shopId}/tts-config/{id}/enable   - Bật TTS
POST   /api/v1/shops/{shopId}/tts-config/{id}/disable  - Tắt TTS
```

### QR Code Management
```
POST   /api/v1/shops/{shopId}/qr-codes             - Tạo/lấy QR code
GET    /api/v1/shops/{shopId}/qr-codes             - Lấy danh sách QR
GET    /api/v1/shops/{shopId}/qr-codes/export      - Export CSV
PATCH  /api/v1/shops/{shopId}/qr-codes/{id}        - Cập nhật QR
DELETE /api/v1/shops/{shopId}/qr-codes/{id}        - Deactivate QR
```

### Admin Approval
```
POST   /api/v1/admin/contents/{id}/approve         - Duyệt nội dung
POST   /api/v1/admin/contents/{id}/reject          - Từ chối nội dung
GET    /api/v1/admin/shops                         - Danh sách quán (Admin)
POST   /api/v1/admin/shops/{id}/approve            - Duyệt quán (Admin)
POST   /api/v1/admin/shops/{id}/reject             - Từ chối quán (Admin)
```

### Analytics
```
GET    /api/v1/shops/{shopId}/analytics/daily      - Thống kê ngày
GET    /api/v1/shops/{shopId}/analytics/period     - Thống kê khoảng thời gian
GET    /api/v1/shops/{shopId}/analytics/top-poi    - Top POI được nghe
GET    /api/v1/shops/{shopId}/analytics/summary    - Tóm tắt
```

---

## 🔐 Authorization Rules

| Operation | Admin | ShopManager(Owner) | ShopManager(Other) | EndUser |
|-----------|-------|-------------------|-------------------|---------|
| View shop profile | ✓ | ✓ | ✗ | ✗ |
| Edit shop profile | ✓ | ✓ (own) | ✗ | ✗ |
| Approve shop | ✓ | ✗ | ✗ | ✗ |
| Create content | ✓ | ✓ (own) | ✗ | ✗ |
| Edit content | ✓ | ✓ (own) | ✗ | ✗ |
| Submit for approval | ✗ | ✓ (own) | ✗ | ✗ |
| Approve content | ✓ | ✗ | ✗ | ✗ |
| Manage QR | ✓ | ✓ (own) | ✗ | ✗ |
| View analytics | ✓ | ✓ (own) | ✗ | ✗ |

---

## 🚀 Workflow Example

### 1. Shop Manager quy trình tạo POI với nội dung

```
1. Tạo hồ sơ quán
   POST /api/v1/shops
   {
     "name": "Quán Bánh Mì Xóm Chiếu",
     "address": "Xóm Chiếu, Q.1, TPHCM",
     "openingHours": "06:00-22:00"
   }

2. Tạo POI cho quán
   POST /api/v1/pois
   X-User-Id: <shop-manager-id>
   {
     "code": "BANH_MI_001",
     "name": "Quán Bánh Mì Nổi Tiếng",
     "latitude": 10.7530,
     "longitude": 106.6878,
     "shopId": <shop-id>
   }

3. Tạo nội dung thuyết minh
   POST /api/v1/shops/<shopId>/contents
   {
     "poiId": <poi-id>,
     "textScript": "Quán bánh mì này được thành lập từ 1985..."
   }

4. Thêm bản dịch
   POST /api/v1/shops/<shopId>/contents/<id>/translations
   {
     "languageCode": "en",
     "translatedText": "This banh mi shop was established in 1985..."
   }

5. Upload audio
   POST /api/v1/shops/<shopId>/audios
   {
     "poiId": <poi-id>,
     "languageCode": "vi",
     "filePath": "/media/audio/banh-mi-001-vi.mp3",
     "durationSeconds": 45
   }

6. Gửi duyệt
   POST /api/v1/shops/<shopId>/contents/<id>/submit

7. Admin duyệt
   POST /api/v1/admin/contents/<id>/approve
```

### 2. QR Code Workflow

```
1. Tạo QR code
   POST /api/v1/shops/<shopId>/qr-codes
   {
     "poiId": <poi-id>
   }
   → Response: QR:BANH_MI_001

2. Lấy ảnh QR
   GET response chứa QRImageUrl

3. Export danh sách QR
   GET /api/v1/shops/<shopId>/qr-codes/export
   → CSV file với payload, image URL cho triển khai thực địa
```

---

## 📦 Database Changes

Migration sẽ tạo bảng mới:
- `ShopProfiles`
- `ShopContents`
- `ShopContentTranslations`
- `ContentApprovalLogs`
- `ShopAudioAssets`
- `ShopTTSConfigurations`
- `ShopQRCodes`
- `ShopAnalyticsSnapshots`
- `ShopLanguageReadiness`

Sửa đổi bảng hiện có:
- `Pois` - Thêm `ShopId` (nullable)
- `Tours` - Thêm `ShopId` (nullable), `IsActive`, `CreatedAtUtc`

---

## ✅ Kiểm Thử

### Unit Tests

```csharp
[Fact]
public async Task CreateShop_ShouldCreateValidShop()
{
    var service = new ShopManagementService(_dbContext);
    var shopId = await service.CreateShopAsync(userId, "Quán Bánh Mì");
    Assert.NotNull(shopId);
}

[Fact]
public async Task SubmitContent_OnlyDraftCanBeSubmitted()
{
    var content = CreateTestContent(ContentApprovalStatus.Approved);
    await Assert.ThrowsAsync<InvalidOperationException>(
        () => service.SubmitContentForApprovalAsync(content.Id));
}

[Fact]
public async Task Authorization_ShopManagerCanOnlyAccessOwnShop()
{
    var result = await authService.CanAccessShopAsync(managerId, otherShopId);
    Assert.False(result);
}
```

### Integration Tests

```bash
# API Test
curl -X POST http://localhost:5140/api/v1/shops \
  -H "X-User-Id: <manager-id>" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Test Shop",
    "address": "123 Test St"
  }'

# Export QR
curl -X GET http://localhost:5140/api/v1/shops/<shopId>/qr-codes/export \
  -H "X-User-Id: <manager-id>" \
  > qr_codes.csv
```

---

## 📝 Files Created

### Entities
- `Domain/Entities/ShopProfile.cs`
- `Domain/Entities/ShopContent.cs`
- `Domain/Entities/ShopAudioAsset.cs`
- `Domain/Entities/ShopQRCode.cs`
- `Domain/Entities/ShopAnalyticsSnapshot.cs`

### Services
- `Application/Services/IShopManagementService.cs`
- `Application/Services/IShopAnalyticsService.cs`
- `Application/Services/IShopQRCodeService.cs`
- `Application/Services/IShopAudioService.cs`
- `Application/Services/IShopAuthorizationService.cs`

### Updates
- `Persistence/AudioGuideDbContext.cs` - Added DbSets
- `Domain/Entities/Poi.cs` - Added ShopId
- `Domain/Entities/Tour.cs` - Added ShopId, IsActive
- `Application/ApplicationServiceCollectionExtensions.cs` - Registered services

---

## 🎯 Next Steps

1. Create Entity Framework migrations
2. Create API endpoints in Program.cs
3. Add admin approval endpoints
4. Implement QR code generation (QRCoder library)
5. Add TTS integration (Google Cloud, Azure)
6. Create frontend shop manager UI
7. Add email notifications for approvals
8. Implement image upload/optimization

