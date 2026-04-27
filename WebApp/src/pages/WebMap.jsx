import React, { useEffect, useState, useRef } from 'react'
import { useNavigate } from 'react-router-dom'
import { MapContainer, TileLayer, Marker, Popup, useMap, useMapEvents } from 'react-leaflet'
import L from 'leaflet'
import { ArrowLeft, Navigation, ZoomIn, ZoomOut, MapPin, AudioLines } from 'lucide-react'
import { api, endpoints } from '../config/api.js'
import { useAuth } from '../context/AuthContext.jsx'
import { useGeolocation } from '../context/GeolocationContext.jsx'
import { api as apiService } from '../config/api.js'

const userIcon = new L.DivIcon({
  className: 'user-marker',
  html: `<div style="width:20px;height:20px;background:#3b82f6;border:3px solid white;border-radius:50%;box-shadow:0 2px 8px rgba(0,0,0,0.3)"></div>`,
  iconSize: [20, 20],
  iconAnchor: [10, 10],
})

const poiIcon = new L.DivIcon({
  className: 'poi-marker',
  html: `<div style="width:36px;height:36px;background:#f97316;border:3px solid white;border-radius:50%;display:flex;align-items:center;justify-content:center;box-shadow:0 2px 8px rgba(0,0,0,0.3)">
    <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="white" stroke="white" stroke-width="2"><path d="M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"/><polyline points="9 22 9 12 15 12 15 22"/></svg>
  </div>`,
  iconSize: [36, 36],
  iconAnchor: [18, 36],
  popupAnchor: [0, -36],
})

const nearbyIcon = new L.DivIcon({
  className: 'poi-marker-nearby',
  html: `<div style="width:44px;height:44px;background:#ea580c;border:3px solid #fde68a;border-radius:50%;display:flex;align-items:center;justify-content:center;box-shadow:0 2px 12px rgba(234,88,12,0.5);animation:pulse 2s infinite">
    <svg xmlns="http://www.w3.org/2000/svg" width="22" height="22" viewBox="0 0 24 24" fill="white" stroke="white" stroke-width="2"><path d="M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"/><polyline points="9 22 9 12 15 12 15 22"/></svg>
  </div>`,
  iconSize: [44, 44],
  iconAnchor: [22, 44],
  popupAnchor: [0, -44],
})

function LocationWatcher({ onLocationUpdate }) {
  useMapEvents({
    locationfound(e) {
      onLocationUpdate(e.latlng)
    },
  })
  return null
}

function MapCenterUpdater({ center }) {
  const map = useMap()
  useEffect(() => {
    if (center) {
      map.setView([center.lat, center.lng], map.getZoom())
    }
  }, [center, map])
  return null
}

export default function WebMap() {
  const navigate = useNavigate()
  const { userId } = useAuth()
  const { location, startWatching, error: geoError } = useGeolocation()
  const [pois, setPois] = useState([])
  const [loading, setLoading] = useState(true)
  const [nearbyPoi, setNearbyPoi] = useState(null)
  const [userPos, setUserPos] = useState(null)
  const [showNearAlert, setShowNearAlert] = useState(false)
  const [geofenceTriggered, setGeofenceTriggered] = useState(new Set())
  const visitIdRef = useRef(null)
  const visitAudioStats = useRef({ count: 0, duration: 0 })

  useEffect(() => {
    apiService.get(endpoints.pois).then((data) => {
      const list = Array.isArray(data) ? data : (data.pois || [])
      setPois(list)
      setLoading(false)
    }).catch(() => setLoading(false))
  }, [])

  useEffect(() => {
    if (!navigator.geolocation) return
    const id = navigator.geolocation.getCurrentPosition(
      (pos) => setUserPos({ lat: pos.coords.latitude, lng: pos.coords.longitude }),
      () => {},
      { enableHighAccuracy: true }
    )
    return () => navigator.geolocation.clearWatch(id)
  }, [])

  useEffect(() => {
    if (!userPos || pois.length === 0 || !userId) return

    const R = 6371000

    // Calculate distance to all POIs
    const poiDistances = pois.map((poi) => {
      const dLat = ((poi.latitude - userPos.lat) * Math.PI) / 180
      const dLng = ((poi.longitude - userPos.lng) * Math.PI) / 180
      const a =
        Math.sin(dLat / 2) ** 2 +
        Math.cos((userPos.lat * Math.PI) / 180) *
          Math.cos((poi.latitude * Math.PI) / 180) *
          Math.sin(dLng / 2) ** 2
      const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a))
      const dist = R * c
      return { poi, dist }
    })

    // Sort by: 1) distance ASC, 2) priority DESC (higher priority first), 3) ID ASC (deterministic tiebreaker)
    poiDistances.sort((a, b) => {
      if (Math.abs(a.dist - b.dist) > 0.5) return a.dist - b.dist // >0.5m diff
      if (b.poi.priority !== a.poi.priority) return b.poi.priority - a.poi.priority
      return a.poi.id.localeCompare(b.poi.id)
    })

    const triggerRadius = 30
    const closestInRange = poiDistances.find(({ poi, dist }) => {
      const poiTriggerRadius = poi.triggerRadiusMeters || triggerRadius
      return dist < poiTriggerRadius
    })

    // Clear old geofence states for POIs no longer in range
    const inRangeIds = new Set(poiDistances
      .filter(({ poi, dist }) => dist < (poi.triggerRadiusMeters || triggerRadius))
      .map(({ poi }) => poi.id))

    setGeofenceTriggered((prev) => {
      const newSet = new Set()
      prev.forEach((id) => {
        if (inRangeIds.has(id)) newSet.add(id)
      })
      return newSet
    })

    // If there's a closest POI in range
    if (closestInRange) {
      const { poi: nearestPoi, dist: nearestDist } = closestInRange

      if (!geofenceTriggered.has(nearestPoi.id)) {
        // New POI entered - trigger geofence
        setGeofenceTriggered((prev) => new Set([...prev, nearestPoi.id]))
        setNearbyPoi(nearestPoi)
        setShowNearAlert(true)

        apiService.post(endpoints.geofenceEvent, {
          userId,
          poiId: nearestPoi.id,
          eventType: 'Enter',
          latitude: userPos.lat,
          longitude: userPos.lng,
          distanceFromCenterMeters: nearestDist,
        }).catch(() => {})

        if (!visitIdRef.current) {
          apiService.post(endpoints.visitStart, {
            userId,
            poiId: nearestPoi.id,
            triggerSource: 'Geofence',
            pageSource: 'Web',
            latitude: userPos.lat,
            longitude: userPos.lng,
          }).then((data) => {
            visitIdRef.current = data.id
          }).catch(() => {})
        }
      } else if (nearbyPoi?.id !== nearestPoi.id) {
        // User moved to different POI - update display
        setNearbyPoi(nearestPoi)
        setShowNearAlert(true)
      }
    } else {
      // No POI in range - clear alert
      if (nearbyPoi) {
        setShowNearAlert(false)
        setNearbyPoi(null)
        if (visitIdRef.current) {
          apiService.post(endpoints.visitEnd(visitIdRef.current), {}).catch(() => {})
          visitIdRef.current = null
        }
      }
    }
  }, [userPos, pois, userId])

  const center = userPos || { lat: 10.7769, lng: 106.7009 }
  const zoom = userPos ? 16 : 15

  return (
    <div className="h-screen flex flex-col bg-gray-100">
      {/* Header */}
      <header className="bg-gradient-to-r from-orange-500 to-amber-500 text-white shadow-lg z-[1000] relative flex-shrink-0">
        <div className="flex items-center justify-between px-4 py-3">
          <div className="flex items-center gap-3">
            <button
              onClick={() => navigate('/')}
              className="p-2 bg-white/20 rounded-xl hover:bg-white/30 transition"
            >
              <ArrowLeft size={20} />
            </button>
            <div>
              <h1 className="font-bold text-lg">Bản đồ</h1>
              <p className="text-orange-100 text-xs">
                {geoError ? 'Định vị không khả dụng' : userPos ? 'Đã xác định vị trí' : 'Đang xác định vị trí...'}
              </p>
            </div>
          </div>
          <button
            onClick={startWatching}
            className="p-2 bg-white/20 rounded-xl hover:bg-white/30 transition"
            title="Cập nhật vị trí"
          >
            <Navigation size={20} />
          </button>
        </div>
      </header>

      {/* Nearby POI alert */}
      {showNearAlert && nearbyPoi && (
        <div className="bg-orange-500 text-white px-4 py-3 flex items-center justify-between z-[1000] relative shadow-lg flex-shrink-0">
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 bg-white/20 rounded-full flex items-center justify-center animate-pulse-custom">
              <AudioLines size={20} />
            </div>
            <div>
              <p className="text-sm font-bold">Bạn đang ở gần <strong>{nearbyPoi.name}</strong></p>
              <p className="text-xs text-orange-100">Auto-trigger geofence</p>
            </div>
          </div>
          <div className="flex items-center gap-2">
            <button
              onClick={() => { setShowNearAlert(false) }}
              className="px-3 py-1.5 bg-white/20 rounded-lg text-sm hover:bg-white/30 transition"
            >
              Bỏ qua
            </button>
            <button
              onClick={() => navigate(`/poi/${nearbyPoi.id}`)}
              className="px-3 py-1.5 bg-white text-orange-600 rounded-lg text-sm font-bold hover:bg-orange-50 transition"
            >
              Nghe ngay
            </button>
          </div>
        </div>
      )}

      {/* Map */}
      <div className="flex-1 relative">
        <MapContainer
          center={[center.lat, center.lng]}
          zoom={zoom}
          style={{ height: '100%', width: '100%' }}
          zoomControl={false}
        >
          <TileLayer
            attribution='&copy; <a href="https://www.openstreetmap.org/">OpenStreetMap</a>'
            url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
          />
          <LocationWatcher onLocationUpdate={setUserPos} />
          {userPos && (
            <Marker position={[userPos.lat, userPos.lng]} icon={userIcon}>
              <Popup>Vị trí của bạn</Popup>
            </Marker>
          )}
          {pois.map((poi) => {
            const isNear = nearbyPoi?.id === poi.id
            return (
              <Marker
                key={poi.id}
                position={[poi.latitude, poi.longitude]}
                icon={isNear ? nearbyIcon : poiIcon}
                eventHandlers={{
                  click: () => navigate(`/poi/${poi.id}`),
                }}
              >
                <Popup>
                  <div className="text-center">
                    <strong>{poi.name}</strong>
                    <p className="text-sm text-gray-600">{poi.district}</p>
                    <button
                      onClick={() => navigate(`/poi/${poi.id}`)}
                      className="mt-2 px-3 py-1 bg-orange-500 text-white rounded-lg text-sm"
                    >
                      Nghe audio
                    </button>
                  </div>
                </Popup>
              </Marker>
            )
          })}
        </MapContainer>

        {/* Zoom controls */}
        <div className="absolute bottom-24 right-4 z-[1000] flex flex-col gap-2">
          <button
            onClick={() => {
              const map = document.querySelector('.leaflet-container')._leaflet_map
              if (map) map.zoomIn()
            }}
            className="w-10 h-10 bg-white rounded-xl shadow-lg flex items-center justify-center hover:bg-gray-50"
          >
            <ZoomIn size={20} />
          </button>
          <button
            onClick={() => {
              const map = document.querySelector('.leaflet-container')._leaflet_map
              if (map) map.zoomOut()
            }}
            className="w-10 h-10 bg-white rounded-xl shadow-lg flex items-center justify-center hover:bg-gray-50"
          >
            <ZoomOut size={20} />
          </button>
        </div>

        {/* POI count badge */}
        <div className="absolute top-4 right-4 z-[1000] bg-white/90 backdrop-blur rounded-xl px-3 py-2 shadow-lg">
          <div className="flex items-center gap-2">
            <MapPin size={16} className="text-orange-500" />
            <span className="text-sm font-bold text-gray-700">{pois.length} địa điểm</span>
          </div>
        </div>
      </div>
    </div>
  )
}
