import { useRef, useCallback } from 'react'
import { api, endpoints } from '../config/api.js'
import { useAuth } from '../context/AuthContext.jsx'

export function useSession() {
  const { userId } = useAuth()
  const activeSessionRef = useRef(null)
  const totalListenTimeRef = useRef(0)
  const listenCountRef = useRef(0)

  const startSession = useCallback(async (poiId, triggerSource = 'Manual') => {
    if (!userId || activeSessionRef.current) return null
    try {
      const data = await api.post(endpoints.sessionStart, {
        userId,
        poiId,
        triggerSource,
      })
      activeSessionRef.current = data.id
      listenCountRef.current += 1
      return data
    } catch (err) {
      console.warn('Session start failed:', err)
      return null
    }
  }, [userId])

  const endSession = useCallback(async (durationSeconds = 0) => {
    if (!activeSessionRef.current) return
    try {
      await api.post(endpoints.sessionEnd(activeSessionRef.current), {
        durationSeconds,
      })
      totalListenTimeRef.current += durationSeconds
    } catch (err) {
      console.warn('Session end failed:', err)
    }
    activeSessionRef.current = null
  }, [])

  const trackListenTime = useCallback((seconds) => {
    totalListenTimeRef.current += seconds
  }, [])

  return {
    startSession,
    endSession,
    trackListenTime,
    activeSessionId: activeSessionRef,
    getTotalListenTime: () => totalListenTimeRef.current,
    getListenCount: () => listenCountRef.current,
  }
}
