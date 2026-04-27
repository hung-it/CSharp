import React, { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext.jsx'
import { UserPlus, Eye, EyeOff, CheckCircle } from 'lucide-react'

export default function WebRegister() {
  const navigate = useNavigate()
  const { register, loading: authLoading } = useAuth()

  const [form, setForm] = useState({ username: '', password: '', confirmPassword: '' })
  const [showPw, setShowPw] = useState(false)
  const [submitting, setSubmitting] = useState(false)
  const [localError, setLocalError] = useState('')
  const [success, setSuccess] = useState(false)

  const handleChange = (e) => {
    setForm((f) => ({ ...f, [e.target.name]: e.target.value }))
    setLocalError('')
  }

  const handleSubmit = async (e) => {
    e.preventDefault()
    setLocalError('')

    if (!form.username.trim()) {
      setLocalError('Vui lòng nhập tên đăng nhập.')
      return
    }
    if (form.username.trim().length < 3) {
      setLocalError('Tên đăng nhập phải có ít nhất 3 ký tự.')
      return
    }
    if (!form.password) {
      setLocalError('Vui lòng nhập mật khẩu.')
      return
    }
    if (form.password.length < 4) {
      setLocalError('Mật khẩu phải có ít nhất 4 ký tự.')
      return
    }
    if (form.password !== form.confirmPassword) {
      setLocalError('Mật khẩu xác nhận không khớp.')
      return
    }

    setSubmitting(true)
    try {
      await register({
        username: form.username.trim(),
        password: form.password,
        preferredLanguage: 'vi',
      })
      setSuccess(true)
      setTimeout(() => navigate('/home'), 1500)
    } catch {
      setLocalError('Tên đăng nhập đã tồn tại. Vui lòng chọn tên khác.')
    } finally {
      setSubmitting(false)
    }
  }

  if (success) {
    return (
      <div className="min-h-screen bg-gradient-to-br from-orange-50 via-amber-50 to-orange-100 flex items-center justify-center p-4">
        <div className="text-center">
          <div className="w-20 h-20 bg-green-100 rounded-full flex items-center justify-center mx-auto mb-6">
            <CheckCircle size={40} className="text-green-500" />
          </div>
          <h2 className="text-2xl font-bold text-gray-800 mb-2">Đăng ký thành công!</h2>
          <p className="text-gray-500 mb-2">Tài khoản của bạn đã được tạo với gói Basic.</p>
          <p className="text-gray-400 text-sm">Đang chuyển về trang chủ...</p>
        </div>
      </div>
    )
  }

  return (
    <div className="min-h-screen bg-gradient-to-br from-orange-50 via-amber-50 to-orange-100 flex items-center justify-center p-4">
      <div className="w-full max-w-sm">
        {/* Logo */}
        <div className="text-center mb-8">
          <div className="w-16 h-16 bg-gradient-to-br from-orange-500 to-amber-500 rounded-2xl flex items-center justify-center mx-auto mb-4 shadow-lg">
            <span className="text-2xl">🍜</span>
          </div>
          <h1 className="text-2xl font-bold text-gray-800">Phố Ăm Thực</h1>
          <p className="text-gray-500 text-sm mt-1">Audio Guide — Đăng ký tài khoản</p>
        </div>

        {/* Form card */}
        <div className="bg-white rounded-2xl shadow-xl p-6">
          <form onSubmit={handleSubmit} className="space-y-4">
            <div>
              <label className="block text-sm font-semibold text-gray-700 mb-1.5">Tên đăng nhập</label>
              <input
                type="text"
                name="username"
                value={form.username}
                onChange={handleChange}
                autoComplete="username"
                placeholder="Ít nhất 3 ký tự"
                maxLength={50}
                className="w-full px-4 py-3 bg-gray-50 border border-gray-200 rounded-xl text-gray-800 placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-orange-400 focus:border-transparent transition"
              />
            </div>

            <div>
              <label className="block text-sm font-semibold text-gray-700 mb-1.5">Mật khẩu</label>
              <div className="relative">
                <input
                  type={showPw ? 'text' : 'password'}
                  name="password"
                  value={form.password}
                  onChange={handleChange}
                  autoComplete="new-password"
                  placeholder="Ít nhất 4 ký tự"
                  className="w-full px-4 py-3 bg-gray-50 border border-gray-200 rounded-xl text-gray-800 placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-orange-400 focus:border-transparent transition pr-12"
                />
                <button
                  type="button"
                  onClick={() => setShowPw(!showPw)}
                  className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-400 hover:text-gray-600 transition"
                >
                  {showPw ? <EyeOff size={20} /> : <Eye size={20} />}
                </button>
              </div>
            </div>

            <div>
              <label className="block text-sm font-semibold text-gray-700 mb-1.5">Xác nhận mật khẩu</label>
              <input
                type={showPw ? 'text' : 'password'}
                name="confirmPassword"
                value={form.confirmPassword}
                onChange={handleChange}
                autoComplete="new-password"
                placeholder="Nhập lại mật khẩu"
                className="w-full px-4 py-3 bg-gray-50 border border-gray-200 rounded-xl text-gray-800 placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-orange-400 focus:border-transparent transition"
              />
            </div>

            {localError && (
              <div className="bg-red-50 border border-red-200 text-red-700 text-sm rounded-xl px-4 py-3">
                {localError}
              </div>
            )}

            <button
              type="submit"
              disabled={submitting || authLoading}
              className="w-full py-3 bg-gradient-to-r from-orange-500 to-amber-500 text-white font-bold rounded-xl hover:from-orange-600 hover:to-amber-600 transition disabled:opacity-60 flex items-center justify-center gap-2"
            >
              {submitting ? (
                <div className="w-5 h-5 border-2 border-white border-t-transparent rounded-full animate-spin" />
              ) : (
                <>
                  <UserPlus size={18} />
                  Đăng ký
                </>
              )}
            </button>
          </form>

          {/* Package info */}
          <div className="mt-4 bg-orange-50 border border-orange-200 rounded-xl p-3">
            <p className="text-xs font-semibold text-orange-700 mb-2">Gói Basic (mặc định)</p>
            <ul className="text-xs text-orange-600 space-y-1">
              <li>✓ Nghe audio Tiếng Việt</li>
              <li>✓ Xem bản đồ và danh sách địa điểm</li>
              <li>✓ Tham gia tour</li>
              <li>✗ Nghe audio Tiếng Anh (cần gói Premium)</li>
            </ul>
          </div>

          <div className="mt-4 text-center">
            <p className="text-sm text-gray-500">
              Đã có tài khoản?{' '}
              <Link to="/login" className="text-orange-600 font-semibold hover:text-orange-700 transition">
                Đăng nhập
              </Link>
            </p>
          </div>

          <div className="mt-4 pt-4 border-t border-gray-100 text-center">
            <Link to="/" className="text-sm text-gray-400 hover:text-gray-600 transition">
              ← Quay về trang chủ
            </Link>
          </div>
        </div>
      </div>
    </div>
  )
}
