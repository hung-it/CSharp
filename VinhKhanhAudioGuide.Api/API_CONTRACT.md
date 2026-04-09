# VinhKhanhAudioGuide API Contract (v1)

Base URL (local dev):
- http://localhost:5099 (hoac port do dotnet run cap)

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
```

## 2. Users

### Resolve/Create user by externalRef
- POST /api/v1/users/resolve
- Request:
```json
{
  "externalRef": "USER_WEB_001",
  "preferredLanguage": "vi"
}
```
- Response 200:
```json
{
  "id": "guid",
  "externalRef": "USER_WEB_001",
  "preferredLanguage": "vi",
  "createdAtUtc": "2026-04-02T10:00:00Z"
}
```

### Get user by id
- GET /api/v1/users/{userId}

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
  "district": "Khanh Hoi"
}
```

### Update POI
- PATCH /api/v1/pois/{poiId}

### Audio
- GET /api/v1/pois/{poiId}/audios
- POST /api/v1/pois/{poiId}/audios

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
