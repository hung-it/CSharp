import { useRef, useCallback } from 'react'
import { api, endpoints } from '../config/api.js'
import { useAuth } from '../context/AuthContext.jsx'

export function useVisit() {
  const { userId } = useAuth()
  const activeVisitRef = useRef(null)
  const audioStatsRef = useRef({ count: 0, duration: 0 })

  const startVisit = useCallback(async (poiId, lat, lng) => {
    if (!userId) return null
    try {
      const data = await api.post(endpoints.visitStart, {
        userId,
        poiId,
        triggerSource: 'Map',
        pageSource: 'Web',
        latitude: lat,
        longitude: lng,
      })
      activeVisitRef.current = data.id
      audioStatsRef.current = { count: 0, duration: 0 }
      return data
    } catch (err) {
      console.warn('Visit start failed:', err)
      return null
    }
  }, [userId])

  const endVisit = useCallback(async () => {
    if (!activeVisitRef.current) return
    try {
      await api.post(endpoints.visitEnd(activeVisitRef.current))
    } catch (err) {
      console.warn('Visit end failed:', err)
    }
    activeVisitRef.current = null
  }, [])

  const updateAudioStats = useCallback(async (listenedCount, totalDuration) => {
    if (!activeVisitRef.current) return
    audioStatsRef.current = { count: listenedCount, duration: totalDuration }
    try {
      await api.post(endpoints.visitAudio(activeVisitRef.current), {
        listeningSessionCount: listenedCount,
        totalListenDurationSeconds: totalDuration,
      })
    } catch (err) {
      console.warn('Visit audio update failed:', err)
    }
  }, [])

  return { startVisit, endVisit, updateAudioStats, activeVisitId: activeVisitRef }
}
