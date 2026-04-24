# VinhKhanhAudioGuide API Contract (v1)

Base URL (local dev):
- http://localhost:5140 (theo launchSettings hien tai)

OpenAPI:
- GET /openapi/v1.json

## 1. Health

- GET /api/v1/health
- Response 200:
```json
{
  "status": "ok",
  "service": "VinhKhanhAudioGuide.Api",
  "utc": "2026-04-02T10:00:00Z"
}
```::

## 2. Users

### Register new account
- POST /api/v1/users/register
- Request:
```json
{
  "username": "myusername",
  "password": "mypassword",
  "preferredLanguage": "vi"
}
```
- Validation: username >= 3 chars, password >= 4 chars, no duplicate username
- Response 201:
```json
{
  "success": true,
  "id": "guid",
  "username": "myusername",
  "role": "EndUser",
  "preferredLanguage": "vi",
  "createdAtUtc": "2026-04-24T00:00:00Z",
  "plan": "Basic",
  "message": "Đăng ký thành công! Vui lòng liên hệ quản trị viên để kích hoạt gói Premium."
}
```
- Response 400:
```json
{ "success": false, "message": "Tên đăng nhập đã được sử dụng." }
```

### Login / Resolve user
- POST /api/v1/users/resolve
- Request:
```json
{
  "username": "myusername",
  "preferredLanguage": "vi",
  "password": "mypassword"
}
```
- Response 200:
```json
{
  "success": true,
  "id": "guid",
  "username": "myusername",
  "role": "EndUser",
  "preferredLanguage": "vi",
  "plan": "Basic"
}
```

### Get user by id
- GET /api/v1/users/{userId}

### Search/List users (admin)
- GET /api/v1/users?search=USER_WEB&limit=20

## 2.1 Feature Segments

- GET /api/v1/feature-segments

## 3. Subscription

### Activate subscription
- POST /api/v1/subscriptions/activate
- Request:
```json
{
  "userId": "guid",
  "planTier": "Basic",
  "amountUsd": 1
}
```
- planTier cho phep: Basic, PremiumSegmented, 1, 10

### Get active subscription
- GET /api/v1/subscriptions/users/{userId}/active

### Check segment access
- GET /api/v1/subscriptions/users/{userId}/access/{segmentCode}
- Example segmentCode: basic.poi, premium.segment.analytics

### List subscriptions (admin)
- GET /api/v1/subscriptions?isActive=true&limit=50

### Create subscription (admin CRUD)
- POST /api/v1/subscriptions
- Request:
```json
{
  "userId": "guid",
  "planTier": "10",
  "amountUsd": 10,
  "isActive": true,
  "expiresAtUtc": null
}
```

### Get subscription by id
- GET /api/v1/subscriptions/{subscriptionId}

### Update subscription (admin CRUD)
- PATCH /api/v1/subscriptions/{subscriptionId}

### Delete subscription (admin CRUD)
- DELETE /api/v1/subscriptions/{subscriptionId}

## 4. POI & Audio

### Get POIs
- GET /api/v1/pois
- GET /api/v1/pois?district=Khanh%20Hoi

### Get POI by id
- GET /api/v1/pois/{poiId}

### Create POI
- POST /api/v1/pois
- Request:
```json
{
  "code": "POI900",
  "name": "Mon moi",
  "latitude": 10.7,
  "longitude": 106.6,
  "triggerRadiusMeters": 30,
  "description": "Mo ta",
  "district": "Khanh Hoi",
  "priority": 3,
  "imageUrl": "https://...",
  "mapLink": "https://maps.google.com/?q=10.7,106.6"
}
```

### Update POI
- PATCH /api/v1/pois/{poiId}
- Co the cap nhat: code, name, description, latitude, longitude, triggerRadiusMeters, district, priority, imageUrl, mapLink.

### Audio
- GET /api/v1/pois/{poiId}/audios
- POST /api/v1/pois/{poiId}/audios
- PATCH /api/v1/pois/{poiId}/audios/{audioId}
- DELETE /api/v1/pois/{poiId}/audios/{audioId}

### Upload audio file
- POST /api/v1/uploads/audio
- Content-Type: multipart/form-data
- Form field: `file`
- Response 200:
```json
{
  "fileName": "20260410123000-abc123.mp3",
  "filePath": "http://localhost:5140/media/audio/20260410123000-abc123.mp3",
  "relativePath": "/media/audio/20260410123000-abc123.mp3",
  "size": 123456,
  "contentType": "audio/mpeg"
}
```

## 5. Tours

- GET /api/v1/tours
- GET /api/v1/tours/{tourId}
- GET /api/v1/tours/{tourId}/stops
- POST /api/v1/tours
- POST /api/v1/tours/{tourId}/stops
- POST /api/v1/tours/{tourId}/reorder

## 6. Sessions

### Start session
- POST /api/v1/sessions/start
- Request:
```json
{
  "userId": "guid",
  "poiId": "guid",
  "triggerSource": "Manual"
}
```
- triggerSource: QrCode, Gps, Manual, AutoPlay

### End session
- POST /api/v1/sessions/{sessionId}/end
- Request:
```json
{
  "durationSeconds": 65
}
```

### Get user sessions
- GET /api/v1/sessions/users/{userId}?startDate=...&endDate=...

### Query sessions (admin)
- GET /api/v1/sessions?userId={guid}&poiId={guid}&startDate=...&endDate=...&limit=200

## 7. QR Playback

### Start by QR payload
- POST /api/v1/qr/start
- Request:
```json
{
  "userId": "guid",
  "qrPayload": "QR:POI001",
  "languageCode": "vi"
}
```

## 8. Analytics

- GET /api/v1/analytics/top?limit=5
- GET /api/v1/analytics/pois
- GET /api/v1/analytics/usage?days=7
- GET /api/v1/analytics/heatmap?startDate=2026-04-01T00:00:00Z&endDate=2026-04-02T00:00:00Z&precision=3

## 9. Geofence

### Evaluate current location
- POST /api/v1/geofence/evaluate
- Request:
```json
{
  "userId": "guid",
  "latitude": 10.753,
  "longitude": 106.688,
  "nearFactor": 1.5
}
```

## 10. Narration Queue

- POST /api/v1/narration/enqueue
- POST /api/v1/narration/next
- POST /api/v1/narration/complete

## 11. Content Sync

- POST /api/v1/sync/snapshot
- Request body la ContentSyncSnapshot gom Pois/Audios/Translations/Tours/TourStops.

## 12. Translation

- PUT /api/v1/translations
- GET /api/v1/translations/{contentKey}?languageCode=en&fallbackLanguageCode=vi
- GET /api/v1/translations/{contentKey}/all
- DELETE /api/v1/translations/{contentKey}/{languageCode}

## 13. Anonymous Route Tracking

- POST /api/v1/routes/anonymous/{anonymousRef}/points
- GET /api/v1/routes/anonymous/{anonymousRef}

## Frontend integration flow (recommended)

1. Call POST /users/resolve to get userId.
2. Call POST /subscriptions/activate (or check active/access endpoints).
3. Load POIs and Tours.
4. When playing content: /sessions/start and /sessions/{id}/end.
5. QR scan: /qr/start.
6. Dashboard: /analytics/top and /analytics/pois.
