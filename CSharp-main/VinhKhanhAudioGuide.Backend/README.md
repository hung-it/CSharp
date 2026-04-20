# VinhKhanhAudioGuide.Backend

README nay tap trung 100% vao phan backend theo yeu cau do an Vinh Khanh.

## 1. Muc tieu backend

Backend cung cap cac nang luc sau:
1. Quan tri noi dung POI, Audio, Ban dich, Tour, Lich su su dung.
2. Quan ly goi nap 1 USD va 10 USD theo phan khuc tinh nang.
3. Phan tich du lieu nghe audio va du lieu di chuyen.
4. Ho tro luong QR code: quet ma la nghe, khong can GPS.
5. Luu du lieu offline bang SQLite (EF Core).

## 2. Checklist doi chieu yeu cau de bai

### 2.1 Yeu cau chinh
1. Da dat: Da ngon ngu.
Bang chung: `ContentTranslation`, `IContentTranslationService`.

2. Da dat: Nap 1 USD chi mo tinh nang co ban.
Bang chung: `SubscriptionService.HasAccessToSegmentAsync`, segment `basic.poi`.

3. Da dat: Nap 10 USD mo tinh nang theo phan khuc.
Bang chung: `PlanTier.PremiumSegmented`, `UserEntitlement`, `FeatureSegment`.

### 2.2 He thong quan tri noi dung
1. Da dat: POI.
Bang chung: `IPoiService`, `PoiService`, entity `Poi`.

2. Da dat: Audio.
Bang chung: entity `AudioAsset`, `AssignAudioAsync`, `GetAudioByLanguageAsync`.

3. Da dat: Ban dich.
Bang chung: `IContentTranslationService`, `ContentTranslationService`, entity `ContentTranslation`.

4. Da dat: Lich su su dung.
Bang chung: `IListeningSessionService`, entity `ListeningSession`.

5. Dat muc MVP: Quan ly tour theo luoc do tuyen tinh co thu tu stop.
Bang chung: `Tour`, `TourStop`, `ITourService` (AddStop, ReorderStops, Next/Previous).
Ghi chu: Neu giang vien yeu cau graph phuc tap (co nhanh, edge co trong so), can mo rong them bang canh noi.

### 2.3 Phan tich du lieu
1. Da dat: Luu tuyen di chuyen an danh.
Bang chung: `IRouteTrackingService`, `RouteTrackingService`, entity `RoutePoint`.

2. Da dat: Top dia diem nghe nhieu nhat.
Bang chung: `IAnalyticsService.GetTopPoisByListeningCountAsync`.

3. Da dat: Thoi gian trung binh nghe POI.
Bang chung: `IAnalyticsService.GetPoiListeningStatsAsync`, `IListeningSessionService.GetAverageListeningDurationForPoiAsync`.

4. Da dat: Heatmap vi tri nguoi dung.
Bang chung: `IAnalyticsService.GetHeatmapDataAsync` (gom cell theo do chinh xac).

### 2.4 QR code theo noi dung (khong can GPS)
1. Da dat: Quet ma la lay noi dung POI + audio.
Bang chung: `IQrPlaybackService.ResolvePlaybackContentAsync`.

2. Da dat: Quet ma la tao listening session trigger QR.
Bang chung: `IQrPlaybackService.StartSessionByQrAsync` (TriggerSource.QrCode).

### 2.5 Kien truc goi y trong de
1. Dat backend core: Content Layer offline SQLite.
Bang chung: `AudioGuideDbContext`, `AddAudioGuideBackend` su dung SQLite.

2. Da dat: Geofence Engine va trigger theo khoang cach Haversine.
Bang chung: `IGeofenceService`, `GeofenceService` (`EvaluateLocationAsync`, event Entered/Exited/Nearby).

3. Da dat: Narration Engine o muc backend queue/dedup/chong overlap.
Bang chung: `INarrationQueueService`, `NarrationQueueService`.

4. Da dat: Dong bo content tu server snapshot vao offline DB.
Bang chung: `IContentSyncService`, `ContentSyncService`.

Ghi chu: Cac muc tren thuong nam o app service/runtime, khong phai data backend thuan.

## 3. Kien truc backend hien tai

Project duoc tach thanh:
1. `Domain`: Entity, Enum, Exception.
2. `Application`: Service interface + implementation (business logic).
3. `Persistence`: `AudioGuideDbContext`, model constraints/indexes.
4. `Infrastructure`: DI registration, database initializer, seed data.

## 4. Cac service backend chinh

1. Subscription va phan quyen segment:
- `ISubscriptionService`, `SubscriptionService`.

2. Quan tri noi dung:
- `IPoiService`, `PoiService`.
- `ITourService`, `TourService`.
- `IContentTranslationService`, `ContentTranslationService`.

3. Lich su nghe:
- `IListeningSessionService`, `ListeningSessionService`.

4. Phan tich du lieu:
- `IAnalyticsService`, `AnalyticsService`.

5. Tuyen di chuyen an danh:
- `IRouteTrackingService`, `RouteTrackingService`.

6. QR playback backend flow:
- `IQrPlaybackService`, `QrPlaybackService`.

7. Geofence trigger engine:
- `IGeofenceService`, `GeofenceService`.

8. Narration queue engine:
- `INarrationQueueService`, `NarrationQueueService`.

9. Content sync engine:
- `IContentSyncService`, `ContentSyncService`.

## 5. Database va du lieu mau

`DataSeeder` tao du lieu mau cho 3 phuong:
1. Xom Chieu.
2. Vinh Hoi.
3. Khanh Hoi.

Seed gom:
1. POI + audio vi/en.
2. 2 tour mau.
3. 2 user (basic va premium).
4. Feature segments co ban va premium.

## 6. Chat luong va kiem thu

Unit tests hien tai:
1. Tong so test: 84.
2. Pass: 84.
3. Fail: 0.

Nhom test gom:
1. Bootstrap database.
2. Subscription service.
3. POI service.
4. Tour service.
5. Listening session service.
6. Analytics service.
7. Data seeder.
8. Content translation service.
9. Route tracking service.
10. QR playback service.
11. Geofence service.
12. Narration queue service.
13. Content sync service.

Lenh chay test:

```bash
dotnet test VinhKhanhAudioGuide.Backend.Tests --logger "console;verbosity=minimal"
```

## 7. Cach dung backend trong MAUI app

Dang ky backend:

```csharp
builder.Services.AddAudioGuideBackend(options => options.DatabasePath = databasePath);
```

Dang ky toan bo services application:

```csharp
services.AddApplicationServices();
```

Khoi tao database luc app startup:

```csharp
var databaseInitializer = scope.ServiceProvider.GetRequiredService<IDatabaseInitializer>();
await databaseInitializer.InitializeAsync();
```

## 8. Danh gia tong ket

Theo scope backend, he thong da dat yeu cau de bai o muc hoan chinh cho do an.

Phan can mo rong neu muon day len muc production hoac diem cao hon:
1. Tour graph nang cao (khong chi sequence).
2. Geofence trigger engine + haversine runtime.
3. Dong bo offline-online voi conflict handling.
4. Policy va audit log chi tiet hon cho entitlement.
