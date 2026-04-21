# Huong Dan Chay Du An (Backend + Frontend Admin + MAUI)

Luu y pham vi:
- Web chi dung cho admin quan ly noi dung/van hanh.
- User cuoi su dung app mobile MAUI.

## 1. Yeu cau moi truong
- .NET SDK 10
- Node.js 20+
- Visual Studio 2022 / workload .NET MAUI (neu chay app MAUI tren Windows)

## 2. Kiem tra nhanh truoc khi chay
Mo terminal tai thu muc goc repo D:\PhoAmThuc va chay:

powershell -ExecutionPolicy Bypass -File .\scripts\run-backend-tests.ps1

Kiem tra frontend build:

Set-Location .\CSharp-main\PhoAmThuc.Admin
npm run build

## 3. Chay Backend API
Lenh chay backend tu root:

dotnet run --project .\CSharp-main\VinhKhanhAudioGuide.Api\VinhKhanhAudioGuide.Api.csproj --urls http://localhost:5140

Hoac chay nhanh API + frontend bang script:

powershell -ExecutionPolicy Bypass -File .\scripts\run-dev.ps1

Sau khi API chay:
- Health: http://localhost:5140/api/v1/health
- POI list: http://localhost:5140/api/v1/pois

Smoke test backend:

powershell -ExecutionPolicy Bypass -File .\scripts\run-api-smoke.ps1 -BaseUrl "http://localhost:5140"

## 4. Chay Frontend Admin
1. Mo terminal tai .\CSharp-main\PhoAmThuc.Admin
2. Lan dau: npm install
3. Chay: npm run dev
4. Truy cap: http://localhost:5173 (hoac cong tiep theo neu 5173 ban)

## 5. Chay MAUI tren Windows
Dung script helper tu root de tranh loi dotnet run sai thu muc:

Build MAUI:

powershell -ExecutionPolicy Bypass -File .\scripts\run-maui.ps1 -BuildOnly

Run MAUI:

powershell -ExecutionPolicy Bypass -File .\scripts\run-maui.ps1

Run MAUI va tu mo backend API:

powershell -ExecutionPolicy Bypass -File .\scripts\run-maui.ps1 -StartBackend

## 6. Checklist runtime MAUI de test nhanh
1. App mo duoc va vao trang tong quan khong crash.
2. Danh sach POI load du lieu backend thanh cong.
3. QR scan page mo duoc camera va xu ly ket qua scan.
4. Audio player bat/tam dung/chuyen track binh thuong.
5. Nut mo Admin web hoac dieu huong lien quan hoat dong.
6. Khong co loi ket noi API khi backend dang chay o http://localhost:5140.

## 7. Loi thuong gap
- dotnet run tai root bao "Couldn't find a project to run":
  Dung script .\scripts\run-maui.ps1 hoac truyen --project day du.
- Frontend bao loi ket noi API:
  Kiem tra backend co dang chay va cong 5140 dung.
- Smoke test fail o health:
  Co the truyen BaseUrl dang root hoac /api/v1, script da ho tro ca hai.
