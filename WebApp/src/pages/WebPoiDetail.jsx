import React, { useEffect, useState, useRef, useCallback } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { ArrowLeft, Play, Pause, SkipBack, SkipForward, Globe, MapPin, Clock, Headphones, ChevronDown, Lock, Crown } from 'lucide-react'
import { api, endpoints } from '../config/api.js'
import { useAuth } from '../context/AuthContext.jsx'

const LANG_LABELS = {
  vi: 'Tiếng Việt',
  en: 'English',
}

const isEnglishAudio = (langCode) => langCode === 'en'

const isEnglishLocked = (audio, isPremium) => {
  return isEnglishAudio(audio.languageCode) && !isPremium
}

function formatDuration(seconds) {
  if (!seconds) return '0:00'
  const m = Math.floor(seconds / 60)
  const s = Math.round(seconds % 60)
  return `${m}:${s.toString().padStart(2, '0')}`
}

function formatDistance(meters) {
  if (!meters && meters !== 0) return ''
  if (meters < 1000) return `${Math.round(meters)}m`
  return `${(meters / 1000).toFixed(1)}km`
}

function haversineDistance(lat1, lng1, lat2, lng2) {
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

export default function WebPoiDetail() {
  const { id } = useParams()
  const navigate = useNavigate()
  const { userId, preferredLanguage, isPremium } = useAuth()

  const [poi, setPoi] = useState(null)
  const [audios, setAudios] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)

  // Player state
  const [isPlaying, setIsPlaying] = useState(false)
  const [currentAudio, setCurrentAudio] = useState(null)
  const [currentLang, setCurrentLang] = useState(preferredLanguage || 'vi')
  const [progress, setProgress] = useState(0)
  const [duration, setDuration] = useState(0)
  const [userLocation, setUserLocation] = useState(null)

  const audioRef = useRef(null)
  const progressIntervalRef = useRef(null)
  const sessionIdRef = useRef(null)
  const visitIdRef = useRef(null)
  const listenStartTimeRef = useRef(null)

  // Fetch POI data
  useEffect(() => {
    if (!id) return
    Promise.all([
      api.get(endpoints.poiDetail(id)),
      api.get(endpoints.poiAudios(id)),
    ]).then(([poiData, audioData]) => {
      setPoi(poiData)
      const audioList = Array.isArray(audioData) ? audioData : []
      setAudios(audioList)

      // Pick default audio based on preferred language
      const langAudio = audioList.find((a) => a.languageCode === currentLang)
      if (langAudio) {
        setCurrentAudio(langAudio)
        setDuration(langAudio.durationSeconds || 0)
      } else if (audioList.length > 0) {
        setCurrentAudio(audioList[0])
        setDuration(audioList[0].durationSeconds || 0)
      }
      setLoading(false)
    }).catch((err) => {
      setError('Không tìm thấy địa điểm.')
      setLoading(false)
    })
  }, [id])

  // Get user location for distance
  useEffect(() => {
    if (!navigator.geolocation) return
    navigator.geolocation.getCurrentPosition(
      (pos) => setUserLocation({ lat: pos.coords.latitude, lng: pos.coords.longitude }),
      () => {},
      { enableHighAccuracy: true }
    )
  }, [])

  // Start visit session
  useEffect(() => {
    if (!userId || !poi) return
    api.post(endpoints.visitStart, {
      userId,
      poiId: poi.id,
      triggerSource: 'Manual',
      pageSource: 'Web',
      latitude: userLocation?.lat || poi.latitude,
      longitude: userLocation?.lng || poi.longitude,
    }).then((data) => {
      visitIdRef.current = data.id
    }).catch(() => {})

    return () => {
      if (visitIdRef.current) {
        api.post(endpoints.visitEnd(visitIdRef.current), {}).catch(() => {})
      }
    }
  }, [userId, poi])

  const handlePlayPause = useCallback(() => {
    if (!currentAudio) return

    if (isPlaying) {
      // Pause
      if (audioRef.current) audioRef.current.pause()
      setIsPlaying(false)
      clearInterval(progressIntervalRef.current)

      // End listening session
      if (sessionIdRef.current && listenStartTimeRef.current) {
        const elapsed = Math.round((Date.now() - listenStartTimeRef.current) / 1000)
        api.post(endpoints.sessionEnd(sessionIdRef.current), { durationSeconds: elapsed }).catch(() => {})
        sessionIdRef.current = null
      }
    } else {
      // Play
      if (!audioRef.current) {
        const audioUrl = currentAudio.filePath.startsWith('http')
          ? currentAudio.filePath
          : `${import.meta.env.VITE_API_BASE_URL || 'http://localhost:5140'}${currentAudio.filePath}`

        audioRef.current = new Audio(audioUrl)
        audioRef.current.addEventListener('loadedmetadata', () => {
          setDuration(audioRef.current.duration)
        })
        audioRef.current.addEventListener('ended', () => {
          setIsPlaying(false)
          setProgress(0)
          clearInterval(progressIntervalRef.current)
          if (sessionIdRef.current) {
            const elapsed = Math.round(audioRef.current?.duration || 0)
            api.post(endpoints.sessionEnd(sessionIdRef.current), { durationSeconds: elapsed }).catch(() => {})
            sessionIdRef.current = null
          }
        })
      }

      audioRef.current.play()
      setIsPlaying(true)
      listenStartTimeRef.current = Date.now()

      // Start listening session
      api.post(endpoints.sessionStart, {
        userId,
        poiId: poi.id,
        triggerSource: 'Manual',
      }).then((data) => {
        sessionIdRef.current = data.id
      }).catch(() => {})

      progressIntervalRef.current = setInterval(() => {
        if (audioRef.current) {
          setProgress(audioRef.current.currentTime)
        }
      }, 500)
    }
  }, [isPlaying, currentAudio, userId, poi])

  const handleSeek = useCallback((e) => {
    const rect = e.currentTarget.getBoundingClientRect()
    const x = e.clientX - rect.left
    const ratio = x / rect.width
    const newTime = ratio * duration
    if (audioRef.current) {
      audioRef.current.currentTime = newTime
      setProgress(newTime)
    }
  }, [duration])

  const handleLanguageChange = useCallback((langCode) => {
    // Stop current
    if (isPlaying && audioRef.current) {
      audioRef.current.pause()
      setIsPlaying(false)
      clearInterval(progressIntervalRef.current)
    }

    setCurrentLang(langCode)
    setProgress(0)
    setCurrentAudio(audios.find((a) => a.languageCode === langCode) || null)
    audioRef.current = null
  }, [isPlaying, audios])

  // Cleanup on unmount
  useEffect(() => {
    return () => {
      if (audioRef.current) {
        audioRef.current.pause()
        audioRef.current = null
      }
      clearInterval(progressIntervalRef.current)
      if (sessionIdRef.current) {
        const elapsed = Math.round((Date.now() - (listenStartTimeRef.current || Date.now())) / 1000)
        api.post(endpoints.sessionEnd(sessionIdRef.current), { durationSeconds: elapsed }).catch(() => {})
      }
    }
  }, [])

  if (loading) {
    return (
      <div className="h-screen bg-gray-50 flex items-center justify-center">
        <div className="text-center">
          <div className="w-16 h-16 border-4 border-orange-400 border-t-transparent rounded-full animate-spin mx-auto" />
          <p className="mt-4 text-gray-600 font-medium">Đang tải...</p>
        </div>
      </div>
    )
  }

  if (error || !poi) {
    return (
      <div className="h-screen bg-gray-50 flex items-center justify-center p-4">
        <div className="text-center">
          <p className="text-red-500 font-medium">{error || 'Không tìm thấy địa điểm'}</p>
          <button onClick={() => navigate('/')} className="mt-4 px-6 py-2 bg-orange-500 text-white rounded-xl">
            Quay về
          </button>
        </div>
      </div>
    )
  }

  const distance = userLocation
    ? haversineDistance(userLocation.lat, userLocation.lng, poi.latitude, poi.longitude)
    : null

  return (
    <div className="min-h-screen bg-gray-50 pb-32">
      {/* Header */}
      <header className="bg-gradient-to-r from-orange-500 to-amber-500 text-white shadow-lg sticky top-0 z-50">
        <div className="flex items-center gap-3 px-4 py-3">
          <button
            onClick={() => navigate(-1)}
            className="p-2 bg-white/20 rounded-xl hover:bg-white/30 transition"
          >
            <ArrowLeft size={20} />
          </button>
          <div className="flex-1 min-w-0">
            <h1 className="font-bold text-lg truncate">{poi.name}</h1>
            <div className="flex items-center gap-2 text-orange-100 text-xs">
              <MapPin size={12} />
              <span>{poi.district || 'Chưa phân khu'}</span>
              {distance !== null && (
                <>
                  <span>•</span>
                  <span>{formatDistance(distance)}</span>
                </>
              )}
            </div>
          </div>
        </div>
      </header>

      {/* Content */}
      <main className="max-w-2xl mx-auto px-4 py-6">
        {/* POI image / icon */}
        <div className="bg-white rounded-2xl shadow-sm overflow-hidden mb-4">
          {poi.imageUrl ? (
            <img src={poi.imageUrl} alt={poi.name} className="w-full h-48 object-cover" />
          ) : (
            <div className="w-full h-48 bg-orange-100 flex items-center justify-center">
              <MapPin size={64} className="text-orange-300" />
            </div>
          )}
          <div className="p-4">
            <h2 className="text-xl font-bold text-gray-800">{poi.name}</h2>
            {poi.description && (
              <p className="mt-2 text-gray-600 leading-relaxed">{poi.description}</p>
            )}
            <div className="mt-3 flex flex-wrap gap-2">
              <span className="text-xs bg-orange-100 text-orange-700 px-3 py-1 rounded-full">
                {poi.district || 'Chưa phân khu'}
              </span>
              {poi.code && (
                <span className="text-xs bg-gray-100 text-gray-600 px-3 py-1 rounded-full">
                  #{poi.code}
                </span>
              )}
            </div>
            {poi.mapLink && (
              <a
                href={poi.mapLink}
                target="_blank"
                rel="noopener noreferrer"
                className="mt-3 inline-flex items-center gap-1 text-sm text-blue-600 hover:text-blue-700"
              >
                <MapPin size={14} /> Mở trên Google Maps
              </a>
            )}
          </div>
        </div>

        {/* Language selector */}
        {audios.length > 0 && (
          <div className="bg-white rounded-2xl shadow-sm p-4 mb-4">
            <div className="flex items-center gap-2 mb-3">
              <Globe size={18} className="text-gray-500" />
              <span className="text-sm font-semibold text-gray-700">Ngôn ngữ</span>
              {!isPremium && (
                <span className="ml-auto text-xs bg-amber-100 text-amber-700 px-2 py-0.5 rounded-full flex items-center gap-1">
                  <Crown size={10} />
                  English = Premium
                </span>
              )}
            </div>
            <div className="flex gap-2 flex-wrap">
              {audios.map((audio) => {
                const locked = isEnglishLocked(audio, isPremium)
                return (
                  <button
                    key={audio.id}
                    onClick={() => {
                      if (locked) return
                      handleLanguageChange(audio.languageCode)
                    }}
                    disabled={locked}
                    className={`relative px-4 py-2 rounded-xl text-sm font-medium transition ${
                      currentLang === audio.languageCode && !locked
                        ? 'bg-orange-500 text-white'
                        : locked
                        ? 'bg-gray-100 text-gray-400 cursor-not-allowed'
                        : 'bg-gray-100 text-gray-600 hover:bg-gray-200'
                    }`}
                  >
                    {LANG_LABELS[audio.languageCode] || audio.languageCode.toUpperCase()}
                    {audio.isTextToSpeech && !locked && (
                      <span className="ml-1 text-xs opacity-75">(TTS)</span>
                    )}
                    {locked && (
                      <span className="ml-1.5 flex items-center gap-0.5">
                        <Lock size={12} />
                      </span>
                    )}
                  </button>
                )
              })}
            </div>

            {!isPremium && audios.some((a) => isEnglishAudio(a.languageCode)) && (
              <button
                onClick={() => navigate('/profile')}
                className="mt-3 w-full py-2 border border-amber-300 bg-amber-50 text-amber-700 rounded-xl text-sm font-semibold hover:bg-amber-100 transition flex items-center justify-center gap-2"
              >
                <Crown size={14} />
                Nâng cấp Premium để nghe Tiếng Anh
              </button>
            )}
          </div>
        )}

        {/* No audio message */}
        {audios.length === 0 && (
          <div className="bg-white rounded-2xl shadow-sm p-6 mb-4 text-center">
            <Headphones size={40} className="text-gray-300 mx-auto mb-3" />
            <p className="text-gray-500">Chưa có audio cho địa điểm này</p>
          </div>
        )}
      </main>

      {/* Fixed audio player */}
      {currentAudio && (
        <div className="fixed bottom-0 left-0 right-0 bg-white border-t border-gray-200 shadow-2xl z-50">
          <div className="max-w-2xl mx-auto">
            {/* Progress bar */}
            <div
              className="h-1 bg-gray-200 cursor-pointer"
              onClick={handleSeek}
            >
              <div
                className="h-full bg-orange-500 transition-all"
                style={{ width: `${duration ? (progress / duration) * 100 : 0}%` }}
              />
            </div>

            {/* Player controls */}
            <div className="flex items-center gap-4 px-4 py-3">
              <div className="flex-1 min-w-0">
                <p className="font-bold text-gray-800 truncate text-sm">{poi.name}</p>
                <p className="text-xs text-gray-500">
                  {currentLang === 'vi' ? 'Tiếng Việt' : 'English'}
                </p>
              </div>

              <div className="flex items-center gap-2">
                <button className="p-2 text-gray-500 hover:text-gray-700 transition">
                  <SkipBack size={20} />
                </button>
                <button
                  onClick={handlePlayPause}
                  className="w-14 h-14 bg-orange-500 text-white rounded-full flex items-center justify-center hover:bg-orange-600 transition shadow-lg"
                >
                  {isPlaying ? <Pause size={24} fill="white" /> : <Play size={24} fill="white" />}
                </button>
                <button className="p-2 text-gray-500 hover:text-gray-700 transition">
                  <SkipForward size={20} />
                </button>
              </div>

              <div className="text-xs text-gray-500 w-16 text-right font-mono">
                {formatDuration(Math.round(progress))} / {formatDuration(Math.round(duration))}
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
