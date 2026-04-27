import React, { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext.jsx'
import { LogIn, UserPlus, Eye, EyeOff, MapPin, Navigation, Headphones, BookOpen } from 'lucide-react'

export default function WebLanding() {
  const navigate = useNavigate()
  const { login, register, logout, loading: authLoading, error } = useAuth()

  const [mode, setMode] = useState('choice') // 'choice' | 'login' | 'register'
  const [form, setForm] = useState({ username: '', password: '', confirmPassword: '' })
  const [showPw, setShowPw] = useState(false)
  const [submitting, setSubmitting] = useState(false)
  const [localError, setLocalError] = useState('')

  const handleChange = (e) => {
    setForm((f) => ({ ...f, [e.target.name]: e.target.value }))
    setLocalError('')
  }

  const handleGuest = async () => {
    // Clear any existing session first (important when already logged in)
    await logout()
    try {
      const res = await fetch(`${import.meta.env.VITE_API_BASE_URL || 'http://localhost:5140'}/api/v1/users/anonymous`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
      })
      const data = await res.json()
      if (data.success && data.id) {
        const userData = {
          id: data.id,
          username: data.username,
          role: 'EndUser',
          preferredLanguage: 'vi',
        }
        sessionStorage.setItem('userId', data.id)
        sessionStorage.setItem('userData', JSON.stringify(userData))
        // Force reload to reinitialize AuthContext with new session
        window.location.href = '/home'
        return
      }
    } catch {
      // Continue as guest even if anonymous creation fails
    }
    navigate('/home')
  }

  const handleLogin = async (e) => {
    e.preventDefault()
    if (!form.username.trim() || !form.password) {
      setLocalError('Vui lòng nhập đầy đủ thông tin.')
      return
    }
    setSubmitting(true)
    try {
      await login({ username: form.username.trim(), password: form.password })
      navigate('/home')
    } catch {
      setLocalError('Sai tên đăng nhập hoặc mật khẩu.')
    } finally {
      setSubmitting(false)
    }
  }

  const handleRegister = async (e) => {
    e.preventDefault()
    setLocalError('')
    if (!form.username.trim()) { setLocalError('Vui lòng nhập tên đăng nhập.'); return }
    if (form.username.trim().length < 3) { setLocalError('Tên đăng nhập phải có ít nhất 3 ký tự.'); return }
    if (!form.password) { setLocalError('Vui lòng nhập mật khẩu.'); return }
    if (form.password.length < 4) { setLocalError('Mật khẩu phải có ít nhất 4 ký tự.'); return }
    if (form.password !== form.confirmPassword) { setLocalError('Mật khẩu xác nhận không khớp.'); return }
    setSubmitting(true)
    try {
      await register({ username: form.username.trim(), password: form.password, preferredLanguage: 'vi' })
      navigate('/home')
    } catch {
      setLocalError('Tên đăng nhập đã tồn tại. Vui lòng chọn tên khác.')
    } finally {
      setSubmitting(false)
    }
  }

  if (mode === 'login') {
    return (
      <div className="min-h-screen bg-gradient-to-br from-orange-50 via-amber-50 to-orange-100 flex items-center justify-center p-4">
        <div className="w-full max-w-sm">
          <div className="text-center mb-8">
            <div className="w-16 h-16 bg-gradient-to-br from-orange-500 to-amber-500 rounded-2xl flex items-center justify-center mx-auto mb-4 shadow-lg">
              <span className="text-2xl">🍜</span>
            </div>
            <h1 className="text-2xl font-bold text-gray-800">Phố Ăm Thực</h1>
            <p className="text-gray-500 text-sm mt-1">Audio Guide — Đăng nhập</p>
          </div>

          <div className="bg-white rounded-2xl shadow-xl p-6">
            <button onClick={() => setMode('choice')} className="text-sm text-gray-400 hover:text-gray-600 mb-4 transition">
              ← Quay lại
            </button>

            <form onSubmit={handleLogin} className="space-y-4">
              <div>
                <label className="block text-sm font-semibold text-gray-700 mb-1.5">Tên đăng nhập</label>
                <input type="text" name="username" value={form.username} onChange={handleChange}
                  autoComplete="username" placeholder="Nhập tên đăng nhập"
                  className="w-full px-4 py-3 bg-gray-50 border border-gray-200 rounded-xl text-gray-800 placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-orange-400 transition" />
              </div>
              <div>
                <label className="block text-sm font-semibold text-gray-700 mb-1.5">Mật khẩu</label>
                <div className="relative">
                  <input type={showPw ? 'text' : 'password'} name="password" value={form.password} onChange={handleChange}
                    autoComplete="current-password" placeholder="Nhập mật khẩu"
                    className="w-full px-4 py-3 bg-gray-50 border border-gray-200 rounded-xl text-gray-800 placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-orange-400 transition pr-12" />
                  <button type="button" onClick={() => setShowPw(!showPw)}
                    className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-400 hover:text-gray-600 transition">
                    {showPw ? <EyeOff size={20} /> : <Eye size={20} />}
                  </button>
                </div>
              </div>
              {(localError || error) && (
                <div className="bg-red-50 border border-red-200 text-red-700 text-sm rounded-xl px-4 py-3">{localError || error}</div>
              )}
              <button type="submit" disabled={submitting || authLoading}
                className="w-full py-3 bg-gradient-to-r from-orange-500 to-amber-500 text-white font-bold rounded-xl hover:from-orange-600 hover:to-amber-600 transition disabled:opacity-60 flex items-center justify-center gap-2">
                {submitting ? <div className="w-5 h-5 border-2 border-white border-t-transparent rounded-full animate-spin" /> : <><LogIn size={18} /> Đăng nhập</>}
              </button>
            </form>

            <div className="mt-4 text-center">
              <p className="text-sm text-gray-500">
                Chưa có tài khoản?{' '}
                <button onClick={() => { setMode('register'); setForm({ username: '', password: '', confirmPassword: '' }); setLocalError('') }}
                  className="text-orange-600 font-semibold hover:text-orange-700 transition">
                  Đăng ký ngay
                </button>
              </p>
            </div>
          </div>
        </div>
      </div>
    )
  }

  if (mode === 'register') {
    return (
      <div className="min-h-screen bg-gradient-to-br from-orange-50 via-amber-50 to-orange-100 flex items-center justify-center p-4">
        <div className="w-full max-w-sm">
          <div className="text-center mb-8">
            <div className="w-16 h-16 bg-gradient-to-br from-orange-500 to-amber-500 rounded-2xl flex items-center justify-center mx-auto mb-4 shadow-lg">
              <span className="text-2xl">🍜</span>
            </div>
            <h1 className="text-2xl font-bold text-gray-800">Phố Ăm Thực</h1>
            <p className="text-gray-500 text-sm mt-1">Audio Guide — Đăng ký tài khoản</p>
          </div>

          <div className="bg-white rounded-2xl shadow-xl p-6">
            <button onClick={() => setMode('choice')} className="text-sm text-gray-400 hover:text-gray-600 mb-4 transition">
              ← Quay lại
            </button>

            <form onSubmit={handleRegister} className="space-y-4">
              <div>
                <label className="block text-sm font-semibold text-gray-700 mb-1.5">Tên đăng nhập</label>
                <input type="text" name="username" value={form.username} onChange={handleChange}
                  autoComplete="username" placeholder="Ít nhất 3 ký tự" maxLength={50}
                  className="w-full px-4 py-3 bg-gray-50 border border-gray-200 rounded-xl text-gray-800 placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-orange-400 transition" />
              </div>
              <div>
                <label className="block text-sm font-semibold text-gray-700 mb-1.5">Mật khẩu</label>
                <div className="relative">
                  <input type={showPw ? 'text' : 'password'} name="password" value={form.password} onChange={handleChange}
                    autoComplete="new-password" placeholder="Ít nhất 4 ký tự"
                    className="w-full px-4 py-3 bg-gray-50 border border-gray-200 rounded-xl text-gray-800 placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-orange-400 transition pr-12" />
                  <button type="button" onClick={() => setShowPw(!showPw)}
                    className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-400 hover:text-gray-600 transition">
                    {showPw ? <EyeOff size={20} /> : <Eye size={20} />}
                  </button>
                </div>
              </div>
              <div>
                <label className="block text-sm font-semibold text-gray-700 mb-1.5">Xác nhận mật khẩu</label>
                <input type={showPw ? 'text' : 'password'} name="confirmPassword" value={form.confirmPassword} onChange={handleChange}
                  autoComplete="new-password" placeholder="Nhập lại mật khẩu"
                  className="w-full px-4 py-3 bg-gray-50 border border-gray-200 rounded-xl text-gray-800 placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-orange-400 transition" />
              </div>
              {localError && <div className="bg-red-50 border border-red-200 text-red-700 text-sm rounded-xl px-4 py-3">{localError}</div>}
              <button type="submit" disabled={submitting || authLoading}
                className="w-full py-3 bg-gradient-to-r from-orange-500 to-amber-500 text-white font-bold rounded-xl hover:from-orange-600 hover:to-amber-600 transition disabled:opacity-60 flex items-center justify-center gap-2">
                {submitting ? <div className="w-5 h-5 border-2 border-white border-t-transparent rounded-full animate-spin" /> : <><UserPlus size={18} /> Đăng ký</>}
              </button>
            </form>

            <div className="mt-4 bg-orange-50 border border-orange-200 rounded-xl p-3">
              <p className="text-xs font-semibold text-orange-700 mb-2">Gói Basic (mặc định)</p>
              <ul className="text-xs text-orange-600 space-y-1">
                <li>✓ Audio Tiếng Việt</li>
                <li>✓ Bản đồ và danh sách địa điểm</li>
                <li>✓ Tham gia tour</li>
                <li>✗ Audio Tiếng Anh (cần gói Premium)</li>
              </ul>
            </div>

            <div className="mt-4 text-center">
              <p className="text-sm text-gray-500">
                Đã có tài khoản?{' '}
                <button onClick={() => { setMode('login'); setForm({ username: '', password: '', confirmPassword: '' }); setLocalError('') }}
                  className="text-orange-600 font-semibold hover:text-orange-700 transition">
                  Đăng nhập
                </button>
              </p>
            </div>
          </div>
        </div>
      </div>
    )
  }

  // Main landing - choice screen
  return (
    <div className="min-h-screen bg-gradient-to-br from-orange-50 via-amber-50 to-orange-100 flex items-center justify-center p-4">
      <div className="w-full max-w-sm">
        {/* Logo */}
        <div className="text-center mb-10">
          <div className="w-20 h-20 bg-gradient-to-br from-orange-500 to-amber-500 rounded-3xl flex items-center justify-center mx-auto mb-5 shadow-xl">
            <span className="text-4xl">🍜</span>
          </div>
          <h1 className="text-3xl font-bold text-gray-800">Phố Ăm Thực</h1>
          <p className="text-gray-500 text-sm mt-2">Audio Guide — Khám phá ẩm thực Chợ Lớn</p>
        </div>

        {/* Features preview */}
        <div className="grid grid-cols-3 gap-3 mb-8">
          {[
            { icon: <MapPin size={20} className="text-orange-500" />, label: 'Địa điểm' },
            { icon: <Headphones size={20} className="text-amber-500" />, label: 'Audio Guide' },
            { icon: <BookOpen size={20} className="text-orange-500" />, label: 'Tours' },
          ].map((f, i) => (
            <div key={i} className="bg-white/70 backdrop-blur rounded-2xl p-4 text-center shadow-sm">
              <div className="flex justify-center mb-2">{f.icon}</div>
              <p className="text-xs font-medium text-gray-600">{f.label}</p>
            </div>
          ))}
        </div>

        {/* Action buttons */}
        <div className="space-y-3">
          {/* Continue as guest */}
          <button
            onClick={handleGuest}
            className="w-full py-4 bg-white text-gray-700 font-bold rounded-2xl shadow-sm hover:shadow-md hover:bg-gray-50 transition flex items-center justify-center gap-3 border border-gray-100"
          >
            <Eye size={20} />
            Tiếp tục xem (Khách)
          </button>

          <div className="flex items-center gap-3 my-1">
            <div className="flex-1 h-px bg-gray-300" />
            <span className="text-xs text-gray-400 font-medium">hoặc</span>
            <div className="flex-1 h-px bg-gray-300" />
          </div>

          {/* Login */}
          <button
            onClick={() => { setMode('login'); setForm({ username: '', password: '', confirmPassword: '' }) }}
            className="w-full py-4 bg-gradient-to-r from-orange-500 to-amber-500 text-white font-bold rounded-2xl shadow-lg hover:from-orange-600 hover:to-amber-600 transition flex items-center justify-center gap-3"
          >
            <LogIn size={20} />
            Đăng nhập
          </button>

          {/* Register */}
          <button
            onClick={() => { setMode('register'); setForm({ username: '', password: '', confirmPassword: '' }) }}
            className="w-full py-4 bg-white text-orange-600 font-bold rounded-2xl shadow-sm hover:shadow-md hover:bg-orange-50 transition flex items-center justify-center gap-3 border-2 border-orange-200"
          >
            <UserPlus size={20} />
            Đăng ký tài khoản mới
          </button>
        </div>

        {/* Footer note */}
        <p className="text-center text-xs text-gray-400 mt-8">
          Bằng cách tiếp tục, bạn đồng ý với các điều khoản sử dụng của Phố Ăm Thực Audio Guide.
        </p>
      </div>
    </div>
  )
}
