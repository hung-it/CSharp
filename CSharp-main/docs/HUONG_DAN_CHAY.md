# Huong Dan Chay Du An (Backend + Frontend Admin)

Luu y pham vi:
- Web chi dung cho admin quan ly noi dung/van hanh.
- User cuoi se dung app mobile (MAUI), khong dung web.

## 1. Yeu cau moi truong
- .NET SDK 10
- Node.js 20+

## 2. Kiem tra nhanh truoc khi chay
Mo terminal tai thu muc goc repo va chay:

dotnet build PhoAmThuc.sln -v minimal
dotnet test PhoAmThuc.sln -v minimal

## 3. Chay Backend API
1. Mo terminal tai thu muc goc repo.
2. Chay lenh:

dotnet run --project VinhKhanhAudioGuide.Api/VinhKhanhAudioGuide.Api.csproj --launch-profile http

3. API se lang nghe tai:

http://localhost:5140

4. Smoke test backend:
- Health:
   http://localhost:5140/api/v1/health
- Danh sach POI:
   http://localhost:5140/api/v1/pois

## 4. Chay Frontend Admin
1. Mo terminal moi tai thu muc PhoAmThuc.Admin.
2. Cai dependency:

npm install

3. Tao file moi truong (lan dau):

copy .env.example .env

4. Chay frontend:

npm run dev

5. Truy cap:
- http://localhost:5173
- Neu 5173 dang ban, Vite se doi cong (vi du 5174).

## 5. Cac man admin can test
Sau khi vao web admin, test cac man:
- Dashboard Analytics
- Quan ly POI
- Quan ly Audio (CRUD: them/sua/xoa audio theo tung POI)
- Quan ly Ban Dich
- Quan ly Tour
- Lich su su dung
- Subscription (CRUD + check access theo segment)

## 6. Kiem tra ket noi Frontend-Backend
- Frontend dang dung base URL mac dinh:
   http://localhost:5140/api/v1
- Co the doi bang bien moi truong trong .env:
   VITE_API_BASE_URL=http://localhost:5140/api/v1

## 7. Loi thuong gap
- Frontend bao loi ket noi API:
   - Kiem tra backend co dang chay khong.
   - Kiem tra dung cong 5140.
- Port 5140 da bi chiem:
   - Dung process cu dang chiem cong, hoac doi applicationUrl trong launchSettings.
- Frontend khong chay duoc vite:
   - Chua npm install.
   - Thu xoa node_modules + npm install lai.
