import { useState, useEffect, useRef } from 'react'
import { useNavigate } from 'react-router-dom'
import { MapPin, Navigation, Headphones, Menu, X, Globe, Play, BookOpen, User, LogOut, Crown } from 'lucide-react'
import { api, endpoints } from '../config/api.js'
import { useAuth } from '../context/AuthContext.jsx'

const DISTRICT_LABELS = {
  'Xóm Chiếu': 'Xóm Chiếu',
  'Vĩnh Hội': 'Vĩnh Hội',
  'Khánh Hội': 'Khánh Hội',
}

function haversineDistance(lat1, lng1, lat2, lng2) {
  const R = 6371000
  const dLat = ((lat2 - lat1) * Math.PI) / 180
  const dLng = ((lng2 - lng1) * Math.PI) / 180
  const a =
    Math.sin(dLat / 2) * Math.sin(dLat / 2) +
    Math.cos((lat1 * Math.PI) / 180) *
      Math.cos((lat2 * Math.PI) / 180) *
      Math.sin(dLng / 2) *
      Math.sin(dLng / 2)
  const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a))
  return R * c
}

export default function WebHome() {
  const { loading: authLoading, error: authError, user, isPremium, username, logout } = useAuth()
  const navigate = useNavigate()
  const [pois, setPois] = useState([])
  const [tours, setTours] = useState([])
  const [activePOI, setActivePOI] = useState(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)
  const [menuOpen, setMenuOpen] = useState(false)
  const [selectedDistrict, setSelectedDistrict] = useState('all')
  const [userLocation, setUserLocation] = useState(null)
  const [districts, setDistricts] = useState([])
  const [poiDistances, setPoiDistances] = useState({})
  const [userMenuOpen, setUserMenuOpen] = useState(false)
  const watchIdRef = useRef(null)
  const userMenuRef = useRef(null)

  useEffect(() => {
    if (!navigator.geolocation) return
    watchIdRef.current = navigator.geolocation.watchPosition(
      (pos) => {
        setUserLocation({ lat: pos.coords.latitude, lng: pos.coords.longitude })
      },
      () => {},
      { enableHighAccuracy: true, timeout: 10000, maximumAge: 30000 }
    )
    return () => {
      if (watchIdRef.current !== null) navigator.geolocation.clearWatch(watchIdRef.current)
    }
  }, [])

  // Close user menu on outside click
  useEffect(() => {
    const handler = (e) => {
      if (userMenuRef.current && !userMenuRef.current.contains(e.target)) {
        setUserMenuOpen(false)
      }
    }
    if (userMenuOpen) document.addEventListener('mousedown', handler)
    return () => document.removeEventListener('mousedown', handler)
  }, [userMenuOpen])

  const fetchData = async () => {
    try {
      setLoading(true)
      const [poisData, toursData] = await Promise.all([
        api.get(endpoints.pois),
        api.get(endpoints.tours),
      ])
      const poiList = Array.isArray(poisData) ? poisData : (poisData.pois || [])
      setPois(poiList)
      setTours(Array.isArray(toursData) ? toursData : [])
      const dists = {}
      poiList.forEach((p) => {
        if (userLocation) {
          dists[p.id] = haversineDistance(
            userLocation.lat, userLocation.lng,
            p.latitude, p.longitude
          )
        }
      })
      setPoiDistances(dists)
      const distSet = [...new Set(poiList.map((p) => p.district).filter(Boolean))]
      setDistricts(distSet)
      setError(null)
    } catch (err) {
      console.error('Fetch error:', err)
      setError('Không thể tải dữ liệu. Vui lòng kiểm tra server.')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    if (!authLoading) fetchData()
  }, [authLoading])

  useEffect(() => {
    const dists = {}
    pois.forEach((p) => {
      if (userLocation) {
        dists[p.id] = haversineDistance(
          userLocation.lat, userLocation.lng,
          p.latitude, p.longitude
        )
      }
    })
    setPoiDistances(dists)
  }, [userLocation, pois])

  const filteredPois = selectedDistrict === 'all'
    ? pois
    : pois.filter((p) => p.district === selectedDistrict)

  const formatDistance = (meters) => {
    if (!meters && meters !== 0) return ''
    if (meters < 1000) return `${Math.round(meters)}m`
    return `${(meters / 1000).toFixed(1)}km`
  }

  if (authLoading || loading) {
    return (
      <div className="h-screen bg-gradient-to-br from-orange-50 to-amber-50 flex items-center justify-center">
        <div className="text-center">
          <div className="w-16 h-16 border-4 border-orange-400 border-t-transparent rounded-full animate-spin mx-auto" />
          <p className="mt-4 text-gray-600 font-medium">Đang tải...</p>
        </div>
      </div>
    )
  }

  if (authError || error) {
    return (
      <div className="h-screen bg-gradient-to-br from-orange-50 to-amber-50 flex items-center justify-center p-4">
        <div className="bg-white rounded-2xl shadow-xl p-8 text-center max-w-sm w-full">
          <div className="text-red-500 text-5xl mb-4">!</div>
          <h2 className="text-xl font-bold text-gray-800 mb-2">Không thể kết nối</h2>
          <p className="text-gray-600 mb-4">{authError || error}</p>
          <p className="text-sm text-gray-500">Vui lòng kiểm tra server đang chạy ở port 5140</p>
          <button
            onClick={fetchData}
            className="mt-4 px-6 py-2 bg-orange-500 text-white rounded-xl font-medium hover:bg-orange-600 transition"
          >
            Thử lại
          </button>
        </div>
      </div>
    )
  }

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Header */}
      <header className="bg-gradient-to-r from-orange-500 to-amber-500 text-white shadow-lg sticky top-0 z-50">
        <div className="max-w-2xl mx-auto px-4 py-4">
          <div className="flex items-center justify-between">
            <div>
              <h1 className="text-xl font-bold">Phố Ăm Thực</h1>
              <p className="text-orange-100 text-xs">Audio Guide</p>
            </div>
            <div className="flex items-center gap-2">
              <button
                onClick={() => navigate('/map')}
                className="p-2 bg-white/20 rounded-xl hover:bg-white/30 transition"
                title="Bản đồ"
              >
                <Navigation size={20} />
              </button>

              {/* User menu */}
              <div className="relative" ref={userMenuRef}>
                <button
                  onClick={() => setUserMenuOpen(!userMenuOpen)}
                  className={`p-2 rounded-xl transition ${username ? 'bg-white/20 hover:bg-white/30' : 'bg-white/20 hover:bg-white/30'}`}
                  title={username || 'Tài khoản'}
                >
                  {isPremium ? <Crown size={20} className="text-amber-200" /> : <User size={20} />}
                </button>

                {userMenuOpen && (
                  <div className="absolute right-0 top-full mt-2 w-56 bg-white rounded-2xl shadow-xl border border-gray-100 overflow-hidden z-50">
                    {username ? (
                      <>
                        <div className="px-4 py-3 border-b border-gray-100">
                          <p className="font-bold text-gray-800 text-sm">{username}</p>
                          <p className={`text-xs font-semibold ${isPremium ? 'text-amber-600' : 'text-gray-400'}`}>
                            {isPremium ? 'Gói Premium' : 'Gói Basic'}
                          </p>
                        </div>
                        <button
                          onClick={() => { setUserMenuOpen(false); navigate('/profile') }}
                          className="w-full flex items-center gap-3 px-4 py-3 text-gray-700 hover:bg-gray-50 transition text-sm"
                        >
                          <User size={16} />
                          Hồ sơ
                        </button>
                        <button
                          onClick={() => { setUserMenuOpen(false); navigate('/login') }}
                          className="w-full flex items-center gap-3 px-4 py-3 text-gray-700 hover:bg-gray-50 transition text-sm"
                        >
                          <Navigation size={16} />
                          Đổi tài khoản
                        </button>
                        <button
                          onClick={async () => { setUserMenuOpen(false); await logout(); navigate('/') }}
                          className="w-full flex items-center gap-3 px-4 py-3 text-red-600 hover:bg-red-50 transition text-sm border-t border-gray-100"
                        >
                          <LogOut size={16} />
                          Đăng xuất
                        </button>
                      </>
                    ) : (
                      <>
                        <div className="px-4 py-3 border-b border-gray-100">
                          <p className="font-bold text-gray-800 text-sm">Khách</p>
                          <p className="text-xs text-gray-400">Chưa đăng nhập</p>
                        </div>
                        <button
                          onClick={() => { setUserMenuOpen(false); navigate('/login') }}
                          className="w-full flex items-center gap-3 px-4 py-3 text-gray-700 hover:bg-gray-50 transition text-sm"
                        >
                          <User size={16} />
                          Đăng nhập
                        </button>
                        <button
                          onClick={() => { setUserMenuOpen(false); navigate('/register') }}
                          className="w-full flex items-center gap-3 px-4 py-3 text-gray-700 hover:bg-gray-50 transition text-sm"
                        >
                          <User size={16} />
                          Đăng ký
                        </button>
                      </>
                    )}
                  </div>
                )}
              </div>

              <button
                onClick={() => setMenuOpen(!menuOpen)}
                className="p-2 bg-white/20 rounded-xl hover:bg-white/30 transition"
              >
                {menuOpen ? <X size={20} /> : <Menu size={20} />}
              </button>
            </div>
          </div>

          {/* Nav menu */}
          {menuOpen && (
            <nav className="mt-4 pt-4 border-t border-white/30 grid grid-cols-3 gap-2">
              <button
                onClick={() => { setMenuOpen(false); navigate('/map') }}
                className="flex flex-col items-center gap-1 p-3 bg-white/20 rounded-xl hover:bg-white/30 transition"
              >
                <Navigation size={20} />
                <span className="text-xs font-medium">Bản đồ</span>
              </button>
              <button
                onClick={() => { setMenuOpen(false); navigate('/tours') }}
                className="flex flex-col items-center gap-1 p-3 bg-white/20 rounded-xl hover:bg-white/30 transition"
              >
                <BookOpen size={20} />
                <span className="text-xs font-medium">Tours</span>
              </button>
              <button
                onClick={() => setMenuOpen(false)}
                className="flex flex-col items-center gap-1 p-3 bg-white/20 rounded-xl hover:bg-white/30 transition"
              >
                <Globe size={20} />
                <span className="text-xs font-medium">Ngôn ngữ</span>
              </button>
            </nav>
          )}
        </div>
      </header>

      {/* District Filter */}
      {districts.length > 0 && (
        <div className="bg-white shadow-sm sticky top-[72px] z-40">
          <div className="max-w-2xl mx-auto px-4 py-3">
            <div className="flex gap-2 overflow-x-auto no-scrollbar">
              <button
                onClick={() => setSelectedDistrict('all')}
                className={`px-4 py-1.5 rounded-full text-sm font-medium whitespace-nowrap transition ${
                  selectedDistrict === 'all'
                    ? 'bg-orange-500 text-white'
                    : 'bg-gray-100 text-gray-600 hover:bg-gray-200'
                }`}
              >
                Tất cả ({pois.length})
              </button>
              {districts.map((d) => (
                <button
                  key={d}
                  onClick={() => setSelectedDistrict(d)}
                  className={`px-4 py-1.5 rounded-full text-sm font-medium whitespace-nowrap transition ${
                    selectedDistrict === d
                      ? 'bg-orange-500 text-white'
                      : 'bg-gray-100 text-gray-600 hover:bg-gray-200'
                  }`}
                >
                  {DISTRICT_LABELS[d] || d} ({pois.filter((p) => p.district === d).length})
                </button>
              ))}
            </div>
          </div>
        </div>
      )}

      {/* Main content */}
      <main className="max-w-2xl mx-auto px-4 py-6">
        {/* Nearby POI alert */}
        {activePOI && (
          <div className="mb-4 bg-orange-100 border border-orange-300 rounded-xl p-4">
            <p className="text-sm text-orange-800">
              <strong>{activePOI.name}</strong> gần bạn ({formatDistance(poiDistances[activePOI.id])})
            </p>
            <button
              onClick={() => navigate(`/poi/${activePOI.id}`)}
              className="mt-2 text-sm bg-orange-500 text-white px-4 py-1.5 rounded-lg font-medium hover:bg-orange-600 transition"
            >
              Nghe audio
            </button>
          </div>
        )}

        {/* Stats bar */}
        <div className="grid grid-cols-3 gap-3 mb-6">
          <div className="bg-white rounded-xl p-3 text-center shadow-sm">
            <div className="text-2xl font-bold text-orange-500">{pois.length}</div>
            <div className="text-xs text-gray-500">Địa điểm</div>
          </div>
          <div className="bg-white rounded-xl p-3 text-center shadow-sm">
            <div className="text-2xl font-bold text-amber-500">{tours.length}</div>
            <div className="text-xs text-gray-500">Tours</div>
          </div>
          <div className="bg-white rounded-xl p-3 text-center shadow-sm">
            <div className="text-2xl font-bold text-green-500">{districts.length}</div>
            <div className="text-xs text-gray-500">Khu vực</div>
          </div>
        </div>

        {/* POI list */}
        {filteredPois.length === 0 ? (
          <div className="text-center py-12 text-gray-500">
            <MapPin size={48} className="mx-auto mb-4 text-gray-300" />
            <p>Chưa có địa điểm nào</p>
          </div>
        ) : (
          <div className="space-y-3">
            {filteredPois.map((poi) => {
              const dist = poiDistances[poi.id]
              const isNear = dist !== undefined && dist < 100
              return (
                <div
                  key={poi.id}
                  onClick={() => navigate(`/poi/${poi.id}`)}
                  className={`bg-white rounded-xl shadow-sm overflow-hidden cursor-pointer transition hover:shadow-md hover:scale-[1.01] ${
                    isNear ? 'ring-2 ring-orange-400' : ''
                  }`}
                >
                  <div className="flex gap-3 p-3">
                    <div className="flex-shrink-0 w-16 h-16 bg-orange-100 rounded-xl flex items-center justify-center">
                      <MapPin className="text-orange-500" size={28} />
                    </div>
                    <div className="flex-1 min-w-0">
                      <div className="flex items-start justify-between gap-2">
                        <h3 className="font-bold text-gray-800 line-clamp-1">{poi.name}</h3>
                        {dist !== undefined && (
                          <span className={`text-xs font-medium flex-shrink-0 px-2 py-0.5 rounded-full ${
                            dist < 100 ? 'bg-green-100 text-green-700' : 'bg-gray-100 text-gray-500'
                          }`}>
                            {formatDistance(dist)}
                          </span>
                        )}
                      </div>
                      <p className="text-sm text-gray-500 mt-1 line-clamp-1">
                        {poi.description || poi.district || 'Không có mô tả'}
                      </p>
                      <div className="flex items-center gap-2 mt-2">
                        <span className="text-xs bg-orange-100 text-orange-700 px-2 py-0.5 rounded-full">
                          {poi.district || 'Chưa phân khu'}
                        </span>
                      </div>
                    </div>
                    <div className="flex-shrink-0 flex items-center">
                      <div className="w-10 h-10 bg-orange-500 rounded-full flex items-center justify-center">
                        <Play size={18} className="text-white" fill="white" />
                      </div>
                    </div>
                  </div>
                </div>
              )
            })}
          </div>
        )}
      </main>
    </div>
  )
}
