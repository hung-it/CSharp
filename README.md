# PhoAmThuc — Vinh Khanh Audio Guide

Hệ thống **audio guide ẩm thực tự động** dành cho khu vực Phố Cổ Vĩnh Khách (Vĩnh Hội, Khánh Hội, Xóm Chiếu). Hệ thống cho phép quản trị viên quản lý nội dung (POI, audio, tour), theo dõi hành vi người dùng qua session, và người dùng cuối trải nghiệm nghe audio qua ứng dụng di động hoặc quét QR.

---

## Mục lục

1. [Tổng quan kiến trúc](#1-tổng-quan-kiến-trúc)
2. [Cấu trúc repository](#2-cấu-trúc-repository)
3. [Công nghệ sử dụng](#3-công-nghệ-sử-dụng)
4. [Hướng dẫn cài đặt & chạy](#4-hướng-dẫn-cài-đặt--chạy)
5. [Kiến trúc backend chi tiết](#5-kiến-trúc-backend-chi-tiết)
6. [Admin Web chi tiết](#6-admin-web-chi-tiết)
7. [Mobile App (MAUI) chi tiết](#7-mobile-app-maui-chi-tiết)
8. [Luồng nghiệp vụ chính](#8-luồng-nghiệp-vụ-chính)
9. [Vai trò người dùng & phân quyền](#9-vai-trò-người-dùng--phân-quyền)
10. [Seed data mặc định](#10-seed-data-mặc-định)
11. [API reference nhanh](#11-api-reference-nhanh)
12. [Build & test](#12-build--test)

---

## 1. Tổng quan kiến trúc

Hệ thống được chia thành **3 thành phần độc lập**, cùng dùng chung Backend API:

```
┌──────────────────────────────────────────────────────────────┐
│                      Backend API                             │
│           ASP.NET Core Minimal API + EF Core + SQLite       │
│               VinhKhanhAudioGuide.Api                       │
│                  Port: 5140                                 │
└──────────────────┬─────────────────────────────────────────┘
                   │ HTTP JSON REST
        ┌──────────┴──────────┐
        ▼                     ▼
┌───────────────────┐  ┌─────────────────────────────────┐
│   Admin Web       │  │      Mobile App (MAUI)          │
│   React + Vite    │  │      .NET MAUI (Android/iOS/    │
│   PhoAmThuc.Admin │  │      Windows/macOS)             │
│   Port: 5173       │  │   VinhKhanhAudioGuide.App       │
│                    │  │                                 │
│ - CMS quản trị    │  │ - Nghe audio guide             │
│ - Analytics        │  │ - Bản đồ POI                  │
│ - Dashboard        │  │ - Quét QR                     │
│ - Quản lý nội dung│  │ - Tour & điều hướng          │
└───────────────────┘  └─────────────────────────────────┘
```

### Nguyên tắc thiết kế

- **Clean Architecture**: Backend tách rõ Domain / Application / Persistence / Infrastructure
- **Layered API**: API Host (Minimal API) gọi Application Services
- **Shared DbContext**: Backend library dùng chung cho cả API và tests
- **Stateless API**: Không có session server-side, mọi trạng thái người dùng nằm trong JWT token hoặc client-side

---

## 2. Cấu trúc repository

```
PhoAmThuc/
├── CSharp-main/
│   ├── VinhKhanhAudioGuide.Api/          # API Host (ASP.NET Core Minimal API)
│   ├── VinhKhanhAudioGuide.Backend/     # Business logic, Domain, Services, Persistence
│   │   ├── Application/
│   │   │   └── Services/                # ~18 service implementations
│   │   ├── Domain/
│   │   │   ├── Entities/                # User, POI, AudioAsset, Tour, TourStop,
│   │   │   │                            # Subscription, FeatureSegment, ListeningSession,
│   │   │   │                            # VisitSession, RoutePoint, etc.
│   │   │   ├── Enums/                  # PlanTier, UserRole, TriggerSource, etc.
│   │   │   └── Exceptions/              # Custom exceptions
│   │   ├── Infrastructure/              # DataSeeder, DatabaseInitializer
│   │   └── Persistence/                # AudioGuideDbContext (EF Core)
│   ├── PhoAmThuc.Admin/                  # Admin Web (React 19 + Vite + TailwindCSS)
│   │   └── src/
│   │       ├── pages/                   # 9 trang: Dashboard, Analytics, POI, Audio,
│   │       │                            # Translation, Tour, QR, UsageHistory, Subscription
│   │       ├── components/              # Layout, ProtectedRoute
│   │       ├── contexts/                # UserContext (auth state)
│   │       └── services/               # apiClient.js (axios-like fetch wrapper)
│   └── PhoAmThuc.Admin.Tests/           # (future)
│
├── CSharp-app/
│   └── VinhKhanhAudioGuide.App/         # Mobile App (.NET MAUI)
│       ├── Pages/                        # MainPage, MapPage, PoiListPage,
│       │                                 # PoiDetailPage, AudioPlayerPage, TourDetailPage,
│       │                                 # QrScanPage, SettingsPage, LoginPage, RegistrationPage
│       ├── Models/                       # PoiData, service models
│       ├── Services/                     # TrackingService, AppConfig
│       ├── Controls/                    # AudioPlayer custom control
│       ├── Resources/                   # Styles.xaml
│       ├── Platforms/                    # Android, iOS, Windows, macOS
│       └── Tests/                        # Integration & UI tests
│
├── docs/
│   └── HUONG_DAN_CHAY.md                 # Hướng dẫn chi tiết
├── scripts/
│   ├── run-dev.ps1                      # Chạy backend + frontend song song
│   └── run-backend-tests.ps1            # Chạy unit tests
└── PRD.html                             # Product Requirements Document (trình bày đẹp)
```

---

## 3. Công nghệ sử dụng

| Thành phần | Công nghệ | Phiên bản |
|-----------|-----------|-----------|
| **Backend API** | .NET 10, ASP.NET Core Minimal API | net10.0 |
| **ORM** | Entity Framework Core 10 | Code-first, SQLite |
| **Admin Web** | React 19, Vite 5, React Router 6 | SPA |
| **UI Components** | TailwindCSS, Lucide Icons | |
| **Bản đồ** | Leaflet + React-Leaflet (Admin) | |
| **QR Code** | qrcode.react (Admin), ZXing.Net.MAUI (App) | |
| **Mobile App** | .NET MAUI, CommunityToolkit.Mvvm | net10.0 |
| **Audio** | Plugin.Maui.Audio | |
| **Location** | Microsoft.Maui.Essentials (Geolocation) | |
| **Test** | xUnit + EF Core InMemory | |

---

## 4. Hướng dẫn cài đặt & chạy

### Yêu cầu môi trường

- .NET SDK 10+
- Node.js 20+
- PowerShell (để chạy script `.ps1`)
- Android SDK (để build Android app)

### Chạy nhanh (khuyến nghị)

```powershell
# Từ thư mục gốc repo
powershell -ExecutionPolicy Bypass -File .\scripts\run-dev.ps1
```

Script này mở **2 terminal riêng biệt**:
- Terminal 1: Backend API → `http://localhost:5140`
- Terminal 2: Admin Web → `http://localhost:5173`

### Chạy từng thành phần thủ công

```powershell
# Backend API
dotnet run --project .\CSharp-main\VinhKhanhAudioGuide.Api\VinhKhanhAudioGuide.Api.csproj --launch-profile http

# Admin Web
cd .\CSharp-main\PhoAmThuc.Admin
npm install
npm run dev
```

### Các endpoint kiểm tra nhanh

```
GET http://localhost:5140/api/v1/health          # Health check
GET http://localhost:5140/api/v1/pois             # Danh sách POI
GET http://localhost:5140/api/v1/tours            # Danh sách tour
GET http://localhost:5140/api/v1/feature-segments  # Feature segments
```

---

## 5. Kiến trúc backend chi tiết

### 5.1 Domain Entities

| Entity | Mô tả |
|--------|--------|
| **User** | Tài khoản người dùng (Admin, ShopManager, EndUser) |
| **Subscription** | Gói thuê bao (Basic/Premium) của user |
| **FeatureSegment** | Mã tính năng (basic.poi, premium.segment.*) |
| **UserEntitlement** | Quyền tính năng cụ thể cho user |
| **Poi** | Điểm tham quan ẩm thực (tọa độ, hình ảnh, QR) |
| **AudioAsset** | File audio theo POI và ngôn ngữ (vi/en) |
| **ContentTranslation** | Bản dịch nội dung theo ngôn ngữ |
| **Tour** | Tuyến tham quan gồm nhiều POI |
| **TourStop** | Điểm dừng trong tour (có thứ tự + hint) |
| **ListeningSession** | Session nghe audio của user |
| **VisitSession** | Phiên ghé thăm POI (kèm GPS, trigger source) |
| **RoutePoint** | Điểm GPS theo dõi hành trình anonymous |
| **PoiGeofenceEvent** | Sự kiện vào/vùng phủ của POI |

### 5.2 Application Services (~18 services)

| Service | Mô tả |
|---------|--------|
| `SubscriptionService` | Kiểm tra gói thuê bao & quyền segment |
| `PoiService` | CRUD POI + gán audio |
| `TourService` | CRUD tour + quản lý stops |
| `QrPlaybackService` | Xử lý QR payload, trả audio URL |
| `ListeningSessionService` | Start/end session nghe |
| `VisitTrackingService` | Ghi nhận visit + cập nhật audio stats |
| `RouteTrackingService` | Ghi điểm GPS cho anonymous user |
| `GeofenceService` | Đánh giá vùng phủ POI theo GPS |
| `AnalyticsService` | Top POIs, usage stats, heatmap |
| `ContentTranslationService` | Bản dịch nội dung đa ngôn ngữ |
| `ContentSyncService` | Sync snapshot nội dung |
| `NarrationQueueService` | Hàng đợi phát audio tự động |
| *(Shop management services)* | ShopProfile, ShopAudio, ShopQR, ShopAnalytics, etc. |

### 5.3 Database

- **Provider**: SQLite (file-based, `Data/vinh-khanh-guide.db`)
- **Auto-migration**: EF Core tự tạo schema khi chạy lần đầu
- **Seed data**: Idempotent — chạy lại không nhân bản dữ liệu

### 5.4 Security

- **Auth**: Dựa trên session token đơn giản (không phải JWT đầy đủ)
  - `POST /users/resolve` trả về `userId` (GUID)
  - Client lưu `userId` trong SecureStorage (MAUI) / localStorage (Web)
  - Header `X-User-Id` xác thực người gọi ở mỗi request
- **Password**: SHA-256 hash với salt cố định
- **Role-based access**: `Admin` (toàn quyền), `ShopManager` (quản POI của mình), `EndUser` (nghe audio)

### 5.5 Feature Gating

| Plan | Giá | Quyền |
|------|------|--------|
| **Basic** | 1 USD | Nghe audio tiếng Việt POI cơ bản |
| **Premium** | 10 USD | Nghe audio đa ngôn ngữ, tour nâng cao, analytics |

- `SubscriptionService.HasAccessToSegmentAsync()` kiểm tra quyền trước khi phát audio
- Endpoint `/qr/start` trả 400 nếu user không có quyền

---

## 6. Admin Web chi tiết

### 6.1 Cấu trúc trang

| Trang | Route | Vai trò | Mô tả |
|-------|-------|---------|--------|
| Dashboard | `/` | Admin, ShopManager | Bản đồ Leaflet + top POIs + live sessions |
| Analytics | `/analytics` | Admin, ShopManager | Biểu đồ chi tiết: visits, listens, daily, geofence |
| POI List | `/pois` | Admin, ShopManager | CRUD POI (Admin thấy tất cả, ShopManager chỉ POI của mình) |
| Audio Manager | `/audio` | Admin, ShopManager | Upload + gán audio (mp3) theo ngôn ngữ |
| Translation Manager | `/translations` | Admin, ShopManager | CRUD bản dịch nội dung |
| Tour Manager | `/tours` | Admin | CRUD tour + reorder stops |
| QR Manager | `/qr-manager` | Admin, ShopManager | Tạo + tải QR PNG + in tem QR + export CSV |
| Usage History | `/usage-history` | Admin | Bảng session nghe theo user |
| Subscription Manager | `/subscriptions` | Admin | CRUD subscription + check segment access |

### 6.2 Authentication (Admin)

- Login form gọi `POST /users/resolve` với username + password
- User info lưu trong `localStorage` key `currentUser`
- ProtectedRoute kiểm tra role trước khi render trang
- Tài khoản mặc định: `admin/1`, `owner1/1`, `owner2/1`, `owner3/1`

### 6.3 API Client (`apiClient.js`)

Wrapper fetch đơn giản:
- Auto-inject `X-User-Id` header từ localStorage
- Auto-parse JSON response
- Support `apiGet`, `apiPost`, `apiPatch`, `apiDelete`, `apiPut`

---

## 7. Mobile App (MAUI) chi tiết

### 7.1 Trang chính

| Trang | Mô tả |
|-------|--------|
| **MainPage** | Trang chủ: stats, POIs gần, POIs phổ biến, tài khoản |
| **MapPage** | Bản đồ Leaflet với markers POI + heatmap |
| **PoiListPage** | Danh sách POI + tìm kiếm + filter theo district |
| **PoiDetailPage** | Chi tiết POI: hình ảnh, mô tả, nút nghe audio, nút bản đồ |
| **AudioPlayerPage** | Trình phát audio với play/pause, progress, đổi ngôn ngữ |
| **TourManagerPage** | Danh sách tour + stats cá nhân |
| **TourDetailPage** | Chi tiết tour: danh sách stops + điều hướng |
| **QrScanPage** | Quét mã QR với ZXing |
| **LoginPage** | Đăng nhập / skip (demo) |
| **RegistrationPage** | Đăng ký tài khoản mới |
| **SettingsPage** | Cài đặt: GPS, auto-play, ngôn ngữ, tài khoản |

### 7.2 Audio Playback Flow

```
1. Người dùng bấm nút nghe (POI detail) hoặc nút xanh (list/map)
   ↓
2. Gọi AppConfig.ResolveDefaultUserIdAsync()
   ├── Đã login → lấy userId từ SecureStorage
   ├── Chưa login + có stored username → gọi /users/resolve + X-User-Id header
   └── Chưa login + không có stored → gọi /users/resolve với "demo/1"
   ↓
3. Navigate sang AudioPlayerPage với query params: ?qr={poiCode}&userId={userId}
   ↓
4. AudioPlayerPage gọi POST /qr/start với userId + qrPayload
   ↓
5. Backend: kiểm tra subscription → tạo ListeningSession → trả audio URL
   ↓
6. App tải audio về cache → phát → ghi duration khi kết thúc
   ↓
7. OnDisappearing: POST /sessions/{id}/end với duration
```

### 7.3 Feature Gate (App-side)

- `FeatureGate.SetPlan(plan)` đọc từ SecureStorage khi restore session
- Kiểm tra `FeatureGate.IsPremium` trước khi cho phép nghe audio tiếng Anh
- Ngôn ngữ fallback: ưu tiên `en` nếu Premium, luôn có `vi`

### 7.4 Tracking

- **Visit**: Ghi nhận khi vào PoiDetailPage (với GPS nếu có)
- **Listening Session**: Start khi bắt đầu phát, End khi rời trang
- **Route**: Ghi điểm GPS anonymous khi di chuyển
- **Geofence**: Tự động phát khi vào vùng phủ POI (nếu bật auto-play)

---

## 8. Luồng nghiệp vụ chính

### 8.1 Người dùng nghe audio (có đăng nhập)

```
App ──POST /users/resolve──→ Backend (xác thực user)
App ──POST /qr/start────────→ Backend (kiểm tra quyền, tạo session)
Backend ──{audioPath, sessionId}──→ App
App ──GET /media/audio/*───→ Backend (tải file audio)
App ──POST /sessions/{id}/end──→ Backend (ghi duration)
```

### 8.2 Người dùng nghe audio (không đăng nhập / demo)

```
App gọi /users/resolve với username="demo", password="1"
→ Backend tự động tạo user "demo" nếu chưa có
→ Session được ghi nhận dưới userId của "demo"
```

### 8.3 Admin quản lý nội dung

```
Admin đăng nhập (admin/1)
→ Dashboard hiển thị tổng quan + bản đồ POI
→ Upload audio mp3 → Backend lưu vào Data/uploads/audio/
→ Gán audio cho POI theo ngôn ngữ
→ Tạo/update translation
→ Tạo tour và sắp xếp thứ tự stops
→ Tạo QR codes cho POI → tải PNG/in tem
```

### 8.4 Shop Manager quản lý POI riêng

```
ShopManager đăng nhập (owner1/1)
→ Chỉ thấy POIs có ManagerUserId = owner1.Id
→ Upload audio cho POIs của mình
→ Tạo QR codes cho POIs của mình
→ Xem analytics của POIs của mình
```

---

## 9. Vai trò người dùng & phân quyền

| Role | Mô tả | Quyền |
|------|--------|--------|
| **Admin** | Quản trị viên hệ thống | Toàn quyền: tất cả POIs, tất cả trang admin |
| **ShopManager** | Chủ cửa hàng | Chỉ POIs của mình, không thấy Tour Manager |
| **EndUser** | Người dùng cuối | Nghe audio qua app (không vào admin) |

### Cơ chế phân quyền trong API

- Backend endpoint không kiểm tra role cho GET dữ liệu chung (POI list, tours)
- ShopManager filter data ở **service layer** (`WHERE ManagerUserId = currentUserId`)
- Subscription check trước mỗi thao tác nghe audio

---

## 10. Seed data mặc định

### Người dùng

| Username | Password | Role | Mô tả |
|---------|----------|------|--------|
| admin | 1 | Admin | Quản trị viên |
| owner1 | 1 | ShopManager | Chủ POIs 1-4 |
| owner2 | 1 | ShopManager | Chủ POIs 5-8 |
| owner3 | 1 | ShopManager | Chủ POIs 9-12 |

### POIs (12 điểm)

```
District Xóm Chiếu:  Quán Bánh Mì Đặc Biệt, Quán Cà Phê Sân Đình, Chợ Xóm Chiếu, Đình Xóm Chiếu
District Vĩnh Hội:   Hẻm Ăn Vĩnh Hội, Nhà Thờ Vĩnh Hội, Cây Cổ Thụ, Khách Sạn Vĩnh Hội
District Khánh Hội:  Tiệm Cơm Gia Đình, Hồ Cá Cảnh, Chùa An Lạc, Chợ Đêm Khánh Hội
```

- Mỗi POI có audio tiếng Việt + tiếng Anh (đã seed sẵn file `.mp3`)
- Mỗi POI có translation cho name + description (vi + en)
- 3 Tour seed sẵn:
  - **TOUR001**: Khám Phá Phố Ăn Vĩnh Khách (12 POIs)
  - **TOUR002**: Tuyến Ăn Uống Đường Phố (6 POIs)
  - **TOUR003**: Hành Trình Di Sản Văn Hóa (6 POIs)
- QR payload format: `vk://poi/{POI_CODE}` (ví dụ: `vk://poi/POI001`)

### Feature Segments

| Code | Mô tả |
|------|--------|
| `basic.poi` | Truy cập POI cơ bản |
| `premium.segment.tour` | Truy cập tour nâng cao |
| `premium.segment.audio` | Truy cập audio đa ngôn ngữ |
| `premium.segment.analytics` | Truy cập analytics nâng cao |

---

## 11. API reference nhanh

Base URL: `http://localhost:5140/api/v1`

### Nhóm API chính

| Nhóm | Endpoints | Mô tả |
|------|-----------|--------|
| **Health** | `GET /health` | Smoke test |
| **Users** | `POST /users/register`, `POST /users/resolve`, `GET /users/{id}` | Đăng ký, đăng nhập |
| **Subscriptions** | `POST /subscriptions`, `GET /subscriptions`, `PATCH /subscriptions/{id}` | Quản lý gói thuê bao |
| **POIs** | `GET /pois`, `POST /pois`, `PATCH /pois/{id}`, `DELETE /pois/{id}` | CRUD điểm tham quan |
| **Audio** | `POST /uploads/audio`, `GET /pois/{id}/audios` | Upload + gán audio |
| **Tours** | `GET /tours`, `POST /tours`, `POST /tours/{id}/stops`, `POST /tours/{id}/reorder` | CRUD tour + stops |
| **Sessions** | `POST /sessions/start`, `POST /sessions/{id}/end`, `GET /sessions` | Tracking session nghe |
| **QR Playback** | `POST /qr/start` | Quét QR → trả audio + tạo session |
| **Analytics** | `GET /analytics/top`, `GET /analytics/usage`, `GET /analytics/heatmap` | Dashboard analytics |
| **Geofence** | `POST /geofence/evaluate` | Kiểm tra vùng phủ theo GPS |
| **Route** | `POST /routes/anonymous/{ref}/points`, `GET /routes/anonymous/{ref}` | Tracking anonymous |
| **Translation** | `PUT /translations`, `GET /translations/{key}` | Bản dịch đa ngôn ngữ |
| **Admin** | `POST /admin/cleanup-anon-users` | Dọn dẹp user ảo |

Chi tiết đầy đủ: `CSharp-main/VinhKhanhAudioGuide.Api/API_CONTRACT.md`

---

## 12. Build & test

```powershell
# Build toàn bộ solution
dotnet build .\PhoAmThuc.sln -v minimal

# Chạy unit tests
dotnet test .\PhoAmThuc.sln -v minimal

# Build admin web production
cd .\CSharp-main\PhoAmThuc.Admin
npm install
npm run build

# Chạy backend test bằng script
powershell -ExecutionPolicy Bypass -File .\scripts\run-backend-tests.ps1
```

---

## Trạng thái dự án

Hệ thống đã hoàn thiện các tính năng cốt lõi và sẵn sàng cho demo/bảo vệ đồ án:

- Backend API ổn định với đầy đủ CRUD, session tracking, analytics
- Admin Web hoàn chỉnh với 9 trang quản trị
- Mobile App (MAUI) đầy đủ chức năng: bản đồ, POI, audio, QR, tour, đăng nhập
- Seed data 12 POIs + 3 tours + audio files mẫu

**Roadmap sau đồ án:**
- JWT authentication đầy đủ với refresh token
- Object storage (S3/Azure Blob) cho media files thay vì local filesystem
- Migration strategy versioned thay vì runtime patch
- Observability: tracing, metrics, centralized logging
- Tích hợp payment gateway thật cho subscription
