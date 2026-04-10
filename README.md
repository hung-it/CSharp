# PhoAmThuc Project

Monorepo gom backend API va frontend admin cho do an Pho am thuc Vinh Khanh.

## Cau truc thu muc
- PhoAmThuc.sln: solution chinh cho backend/.NET
- VinhKhanhAudioGuide.Backend: business/domain/persistence
- VinhKhanhAudioGuide.Api: API host cho app/web
- VinhKhanhAudioGuide.Backend.Tests: unit tests backend
- PhoAmThuc.Admin: web admin React + Vite
- docs: tai lieu huong dan
- scripts: script ho tro dev/test

## Chay nhanh
1. Chay backend:
   dotnet run --project VinhKhanhAudioGuide.Api/VinhKhanhAudioGuide.Api.csproj --launch-profile http
2. Chay frontend:
   cd PhoAmThuc.Admin
   npm install
   copy .env.example .env
   npm run dev

Huong dan day du: docs/HUONG_DAN_CHAY.md

## Luu y duong dan
Thu muc chua ky tu dac biet (vi du #) co the gay canh bao voi mot so JS tool.
Neu co loi build/dev bat thuong, nen dat repo o duong dan don gian (vi du D:/Projects/PhoAmThuc).
