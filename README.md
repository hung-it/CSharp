# PhoAmThuc - Vinh Khanh Audio Guide

Monorepo cho đồ án hệ thống audio guide ẩm thực, gồm:
- Backend API (ASP.NET Core Minimal API + EF Core + SQLite)
- Frontend Admin CMS (React + Vite)

Mục tiêu của dự án:
- Quản trị POI, audio, tour, translation và subscription.
- Ghi nhận session nghe và analytics vận hành.
- Hỗ trợ các luồng thực tế như QR playback, geofence, route tracking.

## 1) Kiến trúc tổng quan

Hệ thống được tách thành 3 lớp rõ ràng:
- API Host: `VinhKhanhAudioGuide.Api`
   - Nhận request, map endpoint, trả response JSON.
- Business/Core: `VinhKhanhAudioGuide.Backend`
   - Chứa Domain, Application Services, Persistence, Infrastructure.
- Admin UI: `PhoAmThuc.Admin`
   - Dashboard và các màn quản trị nội dung.

Lợi ích:
- Dễ test nghiệp vụ ở service layer.
- Dễ mở rộng endpoint và tính năng.
- Giữ coupling thấp giữa UI và backend.

## 2) Tính năng chính

- Quản lý POI đầy đủ (thêm/sửa/xóa + district + priority + imageUrl + mapLink).
- Quản lý audio theo POI và upload file audio qua API.
- Quản lý tour và thứ tự điểm dừng (reorder stop).
- Quản lý translation theo ngôn ngữ.
- Quản lý subscription, entitlement và check access theo segment.
- Session tracking + analytics (top POI, usage trend, heatmap).
- QR playback và anonymous route tracking.
- Dashboard bản đồ (POI marker + heatmap + route polyline).

## 3) Cấu trúc repository

```
PhoAmThuc.sln
docs/
   HUONG_DAN_CHAY.md
scripts/
   run-dev.ps1
   run-backend-tests.ps1
PhoAmThuc.Admin/
VinhKhanhAudioGuide.Api/
VinhKhanhAudioGuide.Backend/
VinhKhanhAudioGuide.Backend.Tests/
```

Mô tả nhanh:
- `PhoAmThuc.sln`: solution tổng cho backend + test.
- `VinhKhanhAudioGuide.Api`: host ASP.NET Core Minimal API.
- `VinhKhanhAudioGuide.Backend`: nghiệp vụ và dữ liệu.
- `VinhKhanhAudioGuide.Backend.Tests`: unit tests xUnit.
- `PhoAmThuc.Admin`: frontend admin React + Vite.
- `docs`: tài liệu hướng dẫn chạy.
- `scripts`: script hỗ trợ chạy nhanh và test nhanh.

## 4) Công nghệ sử dụng

Backend:
- .NET 10 (`net10.0`)
- ASP.NET Core Minimal API
- Entity Framework Core 10
- SQLite

Frontend:
- React 19
- Vite 5
- React Router
- Leaflet + React Leaflet
- qrcode.react

Test:
- xUnit
- EF Core InMemory/SQLite cho test scenario

## 5) Yêu cầu môi trường

- .NET SDK 10+
- Node.js 20+
- PowerShell (để chạy script trong `scripts/`)

## 6) Chạy nhanh (khuyến nghị)

Từ thư mục gốc repo, chạy 1 lệnh để mở backend và frontend trong 2 terminal riêng:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\run-dev.ps1
```

Tùy chọn hữu ích:

```powershell
# Bước npm install trước khi chạy frontend
powershell -ExecutionPolicy Bypass -File .\scripts\run-dev.ps1 -InstallDependencies

# Xem lệnh sẽ chạy mà không mở terminal mới
powershell -ExecutionPolicy Bypass -File .\scripts\run-dev.ps1 -DryRun
```

Sau khi start:
- Backend: `http://localhost:5140`
- Frontend: `http://localhost:5173` (hoặc cổng khác nếu 5173 đang bận)

## 7) Chạy thủ công (manual)

### 7.1 Backend API

```powershell
dotnet run --project .\VinhKhanhAudioGuide.Api\VinhKhanhAudioGuide.Api.csproj --launch-profile http
```

Kiểm tra nhanh:
- Health: `http://localhost:5140/api/v1/health`
- POI list: `http://localhost:5140/api/v1/pois`

### 7.2 Frontend Admin

```powershell
Set-Location .\PhoAmThuc.Admin
npm install
Copy-Item .env.example .env -Force
npm run dev
```

Biến môi trường quan trọng:

```env
VITE_API_BASE_URL=http://localhost:5140/api/v1
```

## 8) Build và test

Build + test solution:

```powershell
dotnet build .\PhoAmThuc.sln -v minimal
dotnet test .\PhoAmThuc.sln -v minimal
```

Hoặc dùng script backend test:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\run-backend-tests.ps1
```

Build frontend production:

```powershell
Set-Location .\PhoAmThuc.Admin
npm run build
```

## 9) API reference

- OpenAPI JSON: `GET /openapi/v1.json`
- API contract chi tiết: `VinhKhanhAudioGuide.Api/API_CONTRACT.md`

## 10) Lưu ý vận hành

- Repository đã ignore `.env` và các biến thể (`.env.*`), nhưng vẫn giữ `.env.example` để onboard nhanh.
- Database SQLite được tạo tự động theo cấu hình backend khi chạy lần đầu.
- Thư mục chứa ký tự đặc biệt có thể gây lỗi với một số JS tool; nên đặt repo ở đường dẫn gọn, dễ đọc.

## 11) Tài liệu bổ sung

- Hướng dẫn chạy chi tiết: `docs/HUONG_DAN_CHAY.md`
- Hợp đồng API: `VinhKhanhAudioGuide.Api/API_CONTRACT.md`

## 12) Trạng thái dự án

Dự án đang ở giai đoạn hoàn thiện cho đồ án học phần:
- Ưu tiên tính ổn định local, dễ demo và dễ bảo vệ.
- Kiến trúc sẵn sàng mở rộng lên hệ thống production (auth, object storage, migration strategy, observability).
