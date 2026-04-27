import React, { createContext, useContext, useState, useEffect, useCallback, useRef } from 'react'
import { api, endpoints } from '../config/api.js'

const AuthContext = createContext(null)

export function AuthProvider({ children }) {
  const [user, setUser] = useState(null)
  const [subscription, setSubscription] = useState(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)
  const fetchSubRef = useRef(null)

  // isPremium: hasActive=true AND active planTier is "Premium" (backend uses PascalCase string)
  const isPremium = subscription?.hasActive === true &&
    (subscription?.active?.planTier === 'Premium' || subscription?.active?.tier === 'Premium')

  const fetchSubscription = useCallback(async (userId) => {
    try {
      const sub = await api.get(endpoints.subscription(userId))
      setSubscription(sub)
    } catch {
      setSubscription({ hasActive: false })
    }
  }, [])

  fetchSubRef.current = fetchSubscription

  // On mount: if we have a stored userId, load user data + subscription
  useEffect(() => {
    const storedUserId = sessionStorage.getItem('userId')
    const storedUserData = sessionStorage.getItem('userData')

    if (!storedUserId) {
      setLoading(false)
      return
    }

    // Restore user from sessionStorage
    if (storedUserData) {
      try {
        const parsed = JSON.parse(storedUserData)
        setUser(parsed)
        fetchSubscription(parsed.id)
      } catch {
        sessionStorage.removeItem('userData')
      }
    }

    // Always try to refresh from server (handles registered users)
    const loadFromServer = async () => {
      try {
        const data = await api.get(endpoints.userMe(storedUserId))
        // Normalize response
        const normalized = {
          id: data.Id || data.id,
          username: data.Username || data.username,
          role: data.role || 'EndUser',
          preferredLanguage: data.PreferredLanguage || data.preferredLanguage || 'vi',
        }
        setUser(normalized)
        sessionStorage.setItem('userData', JSON.stringify(normalized))
        fetchSubscription(normalized.id)
      } catch {
        // Anonymous user — no server record needed
      }
    }

    loadFromServer().finally(() => setLoading(false))
  }, [fetchSubscription])

  const register = useCallback(async ({ username, password, preferredLanguage = 'vi' }) => {
    setError(null)
    try {
      const data = await api.post(endpoints.register, {
        Username: username,
        Password: password,
        PreferredLanguage: preferredLanguage,
      })

      if (data.success === false) {
        setError(data.message || 'Đăng ký thất bại.')
        throw new Error(data.message)
      }

      // Backend returns Username (PascalCase), role, PreferredLanguage
      const userData = {
        id: data.id,
        username: data.Username || data.username || username,
        role: data.role || 'EndUser',
        preferredLanguage: data.PreferredLanguage || data.preferredLanguage || preferredLanguage,
      }
      sessionStorage.setItem('userId', data.id)
      sessionStorage.setItem('userData', JSON.stringify(userData))
      setUser(userData)
      setSubscription({ hasActive: false })
      return userData
    } catch (err) {
      if (!err.message) {
        setError('Tên đăng nhập đã tồn tại.')
      }
      throw err
    }
  }, [])

  const login = useCallback(async ({ username, password }) => {
    setError(null)
    try {
      const data = await api.post(endpoints.login, {
        Username: username,
        Password: password,
      })

      if (data.success === false) {
        setError(data.message || 'Sai tên đăng nhập hoặc mật khẩu.')
        throw new Error(data.message)
      }

      // Backend returns Username (PascalCase), role, PreferredLanguage
      const userData = {
        id: data.id,
        username: data.Username || data.username || username,
        role: data.role || 'EndUser',
        preferredLanguage: data.PreferredLanguage || data.preferredLanguage || 'vi',
      }
      sessionStorage.setItem('userId', data.id)
      sessionStorage.setItem('userData', JSON.stringify(userData))
      setUser(userData)
      fetchSubscription(data.id)
      return userData
    } catch (err) {
      if (!err.message) {
        setError('Sai tên đăng nhập hoặc mật khẩu.')
      }
      throw err
    }
  }, [fetchSubscription])

  const logout = useCallback(() => {
    sessionStorage.removeItem('userId')
    sessionStorage.removeItem('userData')
    setUser(null)
    setSubscription(null)
  }, [])

  const value = {
    user,
    subscription,
    isPremium,
    loading,
    error,
    userId: user?.id,
    username: user?.username,
    role: user?.role,
    preferredLanguage: user?.preferredLanguage || 'vi',
    register,
    login,
    logout,
    clearError: () => setError(null),
    refetch: () => {
      const uid = sessionStorage.getItem('userId')
      if (uid) {
        api.get(endpoints.userMe(uid)).then((data) => {
          const normalized = {
            id: data.Id || data.id,
            username: data.Username || data.username,
            role: data.role || 'EndUser',
            preferredLanguage: data.PreferredLanguage || data.preferredLanguage || 'vi',
          }
          setUser(normalized)
          sessionStorage.setItem('userData', JSON.stringify(normalized))
        }).catch(() => {})
        fetchSubscription(uid)
      }
    },
  }

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used within AuthProvider')
  return ctx
}
