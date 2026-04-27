import React, { createContext, useContext, useState, useEffect, useCallback, useRef } from 'react'

const GeolocationContext = createContext(null)

const DEFAULT_LOCATION = { lat: 10.7769, lng: 106.7009 }

export function GeolocationProvider({ children }) {
  const [location, setLocation] = useState(DEFAULT_LOCATION)
  const [watching, setWatching] = useState(false)
  const [error, setError] = useState(null)
  const watchIdRef = useRef(null)

  const stopWatching = useCallback(() => {
    if (watchIdRef.current !== null) {
      navigator.geolocation.clearWatch(watchIdRef.current)
      watchIdRef.current = null
      setWatching(false)
    }
  }, [])

  const startWatching = useCallback(() => {
    if (!navigator.geolocation) {
      setError('Trình duyệt không hỗ trợ định vị.')
      return
    }

    stopWatching()

    navigator.geolocation.getCurrentPosition(
      (pos) => {
        setLocation({ lat: pos.coords.latitude, lng: pos.coords.longitude })
      },
      (err) => {
        console.warn('Geolocation error:', err.message)
        setError(err.message)
      },
      { enableHighAccuracy: true, timeout: 10000, maximumAge: 30000 }
    )

    watchIdRef.current = navigator.geolocation.watchPosition(
      (pos) => {
        setLocation({ lat: pos.coords.latitude, lng: pos.coords.longitude })
        setError(null)
      },
      (err) => {
        console.warn('Watch position error:', err.message)
      },
      { enableHighAccuracy: true, timeout: 15000, maximumAge: 5000 }
    )
    setWatching(true)
  }, [stopWatching])

  useEffect(() => {
    startWatching()
    return () => stopWatching()
  }, [startWatching, stopWatching])

  const value = {
    location,
    setLocation,
    watching,
    startWatching,
    stopWatching,
    error,
    DEFAULT_LOCATION,
  }

  return <GeolocationContext.Provider value={value}>{children}</GeolocationContext.Provider>
}

export function useGeolocation() {
  const ctx = useContext(GeolocationContext)
  if (!ctx) throw new Error('useGeolocation must be used within GeolocationProvider')
  return ctx
}
