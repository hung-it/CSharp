import React, { useEffect, useState, useRef } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { ArrowLeft, MapPin, Play, ChevronRight, Navigation } from 'lucide-react'
import { api, endpoints } from '../config/api.js'
import { useAuth } from '../context/AuthContext.jsx'

export default function WebTourDetail() {
  const { id } = useParams()
  const navigate = useNavigate()
  const { userId } = useAuth()

  const [tour, setTour] = useState(null)
  const [stops, setStops] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)
  const [currentStopIndex, setCurrentStopIndex] = useState(0)
  const [userLocation, setUserLocation] = useState(null)

  const tourViewIdRef = useRef(null)
  const poiVisitedRef = useRef(new Set())
  const audioListenedRef = useRef(0)

  useEffect(() => {
    if (!id) return
    Promise.all([
      api.get(endpoints.tourDetail(id)),
      api.get(endpoints.tourStops(id)),
    ])
      .then(([tourData, stopsData]) => {
        setTour(tourData)
        const stopList = Array.isArray(stopsData) ? stopsData : (stopsData.stops || [])
        setStops(stopList)
        setLoading(false)
      })
      .catch(() => {
        setError('Không tìm thấy tour.')
        setLoading(false)
      })
  }, [id])

  // Start tour view tracking
  useEffect(() => {
    if (!userId || !tour) return
    api.post(endpoints.tourViewStart(tour.id), {
      userId,
      anonymousRef: null,
    })
      .then((data) => {
        tourViewIdRef.current = data.id
      })
      .catch(() => {})

    return () => {
      if (tourViewIdRef.current) {
        api.post(endpoints.tourViewEnd(tour.id, tourViewIdRef.current), {
          poiVisitedCount: poiVisitedRef.current.size,
          audioListenedCount: audioListenedRef.current,
        }).catch(() => {})
      }
    }
  }, [userId, tour])

  // Track location for distance
  useEffect(() => {
    if (!navigator.geolocation) return
    const watchId = navigator.geolocation.watchPosition(
      (pos) => setUserLocation({ lat: pos.coords.latitude, lng: pos.coords.longitude }),
      () => {},
      { enableHighAccuracy: true }
    )
    return () => navigator.geolocation.clearWatch(watchId)
  }, [])

  const handleStopClick = (stop, index) => {
    poiVisitedRef.current.add(stop.poiId)
    setCurrentStopIndex(index)
    navigate(`/poi/${stop.poiId}`)
  }

  if (loading) {
    return (
      <div className="h-screen bg-gray-50 flex items-center justify-center">
        <div className="w-16 h-16 border-4 border-orange-400 border-t-transparent rounded-full animate-spin" />
      </div>
    )
  }

  if (error || !tour) {
    return (
      <div className="h-screen bg-gray-50 flex items-center justify-center">
        <div className="text-center p-4">
          <p className="text-red-500">{error || 'Tour không tìm thấy'}</p>
          <button onClick={() => navigate('/tours')} className="mt-4 px-6 py-2 bg-orange-500 text-white rounded-xl">
            Quay về
          </button>
        </div>
      </div>
    )
  }

  const haversineDistance = (lat1, lng1, lat2, lng2) => {
    const R = 6371000
    const dLat = ((lat2 - lat1) * Math.PI) / 180
    const dLng = ((lng2 - lng1) * Math.PI) / 180
    const a =
      Math.sin(dLat / 2) ** 2 +
      Math.cos((lat1 * Math.PI) / 180) *
        Math.cos((lat2 * Math.PI) / 180) *
        Math.sin(dLng / 2) ** 2
    const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a))
    return R * c
  }

  const formatDistance = (meters) => {
    if (!meters && meters !== 0) return ''
    if (meters < 1000) return `${Math.round(meters)}m`
    return `${(meters / 1000).toFixed(1)}km`
  }

  return (
    <div className="min-h-screen bg-gray-50">
      <header className="bg-gradient-to-r from-orange-500 to-amber-500 text-white shadow-lg sticky top-0 z-50">
        <div className="flex items-center gap-3 px-4 py-3">
          <button
            onClick={() => navigate('/tours')}
            className="p-2 bg-white/20 rounded-xl hover:bg-white/30 transition"
          >
            <ArrowLeft size={20} />
          </button>
          <div className="flex-1 min-w-0">
            <h1 className="font-bold text-lg truncate">{tour.name}</h1>
            <p className="text-orange-100 text-xs">{stops.length} điểm dừng</p>
          </div>
          <button
            onClick={() => navigate('/map')}
            className="p-2 bg-white/20 rounded-xl hover:bg-white/30 transition"
          >
            <Navigation size={20} />
          </button>
        </div>
      </header>

      <main className="max-w-2xl mx-auto px-4 py-6">
        {tour.description && (
          <p className="text-gray-600 mb-6 leading-relaxed">{tour.description}</p>
        )}

        {/* Stop list */}
        <div className="space-y-3">
          {stops.map((stop, index) => {
            const dist = userLocation && stop.poi
              ? haversineDistance(
                  userLocation.lat, userLocation.lng,
                  stop.poi.latitude, stop.poi.longitude
                )
              : null

            return (
              <div
                key={stop.id}
                className="bg-white rounded-xl shadow-sm overflow-hidden"
              >
                {/* Stop header */}
                <div
                  onClick={() => handleStopClick(stop, index)}
                  className="p-4 cursor-pointer hover:bg-orange-50 transition"
                >
                  <div className="flex items-center gap-3">
                    {/* Sequence number */}
                    <div className="w-10 h-10 bg-orange-500 text-white rounded-full flex items-center justify-center font-bold text-lg flex-shrink-0">
                      {index + 1}
                    </div>

                    <div className="flex-1 min-w-0">
                      <h3 className="font-bold text-gray-800 line-clamp-1">
                        {stop.poi?.name || 'Điểm dừng'}
                      </h3>
                      {stop.poi?.district && (
                        <p className="text-sm text-gray-500">{stop.poi.district}</p>
                      )}
                      {dist !== null && (
                        <p className="text-xs text-orange-600 font-medium">
                          {formatDistance(dist)} từ vị trí của bạn
                        </p>
                      )}
                      {stop.nextStopHint && (
                        <p className="text-xs text-gray-400 mt-1 italic">
                          → {stop.nextStopHint}
                        </p>
                      )}
                    </div>

                    <div className="flex items-center gap-2">
                      <button className="w-10 h-10 bg-orange-500 rounded-full flex items-center justify-center text-white hover:bg-orange-600 transition">
                        <Play size={18} fill="white" />
                      </button>
                      <ChevronRight size={20} className="text-gray-400" />
                    </div>
                  </div>
                </div>
              </div>
            )
          })}
        </div>
      </main>
    </div>
  )
}
