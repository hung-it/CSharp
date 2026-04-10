# PhoAmThuc Admin (Frontend)

## Muc tieu
Web admin quan tri noi dung POI cho he thong Audio Guide Vinh Khanh.

## Yeu cau
- Node.js 20+
- Backend API dang chay o http://localhost:5140

## Cai dat
1. Cai dependency:
	npm install
2. Tao file env tu mau:
	copy .env.example .env
3. Chay local:
	npm run dev

## Bien moi truong
- VITE_API_BASE_URL
  - Mac dinh: http://localhost:5140/api/v1

## Chuc nang da ket noi backend
- Trang POI (duong dan /pois) da goi API that:
  - GET /api/v1/pois

## Build production
- npm run build

## Ghi chu
- Neu backend dung cong khac, cap nhat VITE_API_BASE_URL trong file .env.
- Neu Vite canh bao do duong dan co ky tu dac biet (nhu #), van co the chay binh thuong.
	Neu gap loi resolve module, nen doi workspace sang duong dan don gian (vi du D:/Projects/).
