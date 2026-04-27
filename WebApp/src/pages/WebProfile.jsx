import React from 'react'
import { useNavigate, Link } from 'react-router-dom'
import { useAuth } from '../context/AuthContext.jsx'
import {
  ArrowLeft, User, Crown, Star, LogOut, Shield, Mic2,
  Volume2, VolumeX, MapPin, BookOpen
} from 'lucide-react'

const PLAN_FEATURES = {
  basic: [
    { icon: Volume2, label: 'Audio Tiếng Việt', included: true },
    { icon: Mic2, label: 'Audio Tiếng Anh', included: false },
    { icon: MapPin, label: 'Bản đồ tất cả địa điểm', included: true },
    { icon: BookOpen, label: 'Tất cả tour', included: true },
  ],
  premium: [
    { icon: Volume2, label: 'Audio Tiếng Việt', included: true },
    { icon: Mic2, label: 'Audio Tiếng Anh', included: true },
    { icon: MapPin, label: 'Bản đồ tất cả địa điểm', included: true },
    { icon: BookOpen, label: 'Tất cả tour', included: true },
  ],
}

export default function WebProfile() {
  const navigate = useNavigate()
  const { user, subscription, isPremium, logout } = useAuth()

  const handleLogout = async () => {
    await logout()
    navigate('/')
  }

  if (!user) {
    return (
      <div className="min-h-screen bg-gray-50 flex items-center justify-center">
        <div className="text-center">
          <p className="text-gray-500 mb-4">Vui lòng đăng nhập để xem hồ sơ.</p>
          <Link to="/login" className="px-6 py-2 bg-orange-500 text-white rounded-xl font-medium">
            Đăng nhập
          </Link>
        </div>
      </div>
    )
  }

  const plan = isPremium ? 'premium' : 'basic'
  const features = PLAN_FEATURES[plan]

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Header */}
      <header className="bg-gradient-to-r from-orange-500 to-amber-500 text-white shadow-lg sticky top-0 z-50">
        <div className="flex items-center gap-3 px-4 py-3">
          <button
            onClick={() => navigate(-1)}
            className="p-2 bg-white/20 rounded-xl hover:bg-white/30 transition"
          >
            <ArrowLeft size={20} />
          </button>
          <div className="flex-1">
            <h1 className="font-bold text-lg">Hồ sơ</h1>
          </div>
        </div>
      </header>

      <main className="max-w-2xl mx-auto px-4 py-6 space-y-4">
        {/* User card */}
        <div className="bg-white rounded-2xl shadow-sm p-6">
          <div className="flex items-center gap-4">
            <div className={`w-16 h-16 rounded-full flex items-center justify-center text-2xl font-bold ${
              isPremium ? 'bg-gradient-to-br from-amber-400 to-orange-500 text-white' : 'bg-gray-200 text-gray-500'
            }`}>
              {user.username ? user.username.charAt(0).toUpperCase() : '?'}
            </div>
            <div className="flex-1">
              <h2 className="text-xl font-bold text-gray-800">{user.username || 'Người dùng'}</h2>
              <p className="text-sm text-gray-500 capitalize">
                {user.role === 'owner' ? 'Chủ quán' : user.role === 'admin' ? 'Quản trị viên' : 'Khách'}
              </p>
            </div>
            {isPremium && (
              <div className="bg-amber-100 text-amber-700 px-3 py-1.5 rounded-full flex items-center gap-1.5">
                <Crown size={14} />
                <span className="text-xs font-bold">PREMIUM</span>
              </div>
            )}
          </div>
        </div>

        {/* Subscription plan */}
        <div className="bg-white rounded-2xl shadow-sm p-6">
          <h3 className="font-bold text-gray-800 mb-4 flex items-center gap-2">
            {isPremium ? (
              <>
                <Crown size={18} className="text-amber-500" />
                Gói Premium đang hoạt động
              </>
            ) : (
              <>
                <Star size={18} className="text-gray-400" />
                Gói Basic
              </>
            )}
          </h3>

          <div className="space-y-3">
            {features.map((f, i) => {
              const Icon = f.icon
              return (
                <div key={i} className="flex items-center gap-3">
                  <div className={`w-8 h-8 rounded-lg flex items-center justify-center flex-shrink-0 ${
                    f.included ? 'bg-green-100' : 'bg-gray-100'
                  }`}>
                    <Icon size={16} className={f.included ? 'text-green-600' : 'text-gray-300'} />
                  </div>
                  <span className={`text-sm ${f.included ? 'text-gray-700' : 'text-gray-400'}`}>
                    {f.label}
                  </span>
                  {f.included ? (
                    <span className="ml-auto text-green-500 text-xs font-semibold">✓</span>
                  ) : (
                    <span className="ml-auto text-gray-300 text-xs">Premium</span>
                  )}
                </div>
              )
            })}
          </div>

          {!isPremium && (
            <div className="mt-4 bg-amber-50 border border-amber-200 rounded-xl p-4">
              <p className="text-sm text-amber-800 font-medium mb-2">Nâng cấp lên Premium</p>
              <p className="text-xs text-amber-600 mb-3">
                Gói Premium cho phép nghe audio Tiếng Anh, nhiều tính năng độc quyền.
                Liên hệ quản trị viên để nâng cấp.
              </p>
              <div className="flex items-center gap-2 text-xs text-amber-700 bg-amber-100 rounded-lg px-3 py-2">
                <Shield size={14} />
                <span>Quản trị viên: <strong>admin</strong> / mật khẩu: <strong>1</strong></span>
              </div>
            </div>
          )}

          {isPremium && (
            <div className="mt-4 bg-gradient-to-r from-amber-50 to-orange-50 border border-amber-200 rounded-xl p-4">
              <p className="text-sm text-amber-800 font-medium flex items-center gap-2">
                <Crown size={16} className="text-amber-500" />
                Bạn đang sử dụng gói Premium
              </p>
              <p className="text-xs text-amber-600 mt-1">
                Cảm ơn bạn đã ủng hộ Phố Ăm Thực Audio Guide!
              </p>
            </div>
          )}
        </div>

        {/* Account info */}
        <div className="bg-white rounded-2xl shadow-sm p-6">
          <h3 className="font-bold text-gray-800 mb-4">Thông tin tài khoản</h3>
          <div className="space-y-3">
            <div className="flex justify-between">
              <span className="text-sm text-gray-500">Tên đăng nhập</span>
              <span className="text-sm font-medium text-gray-800">{user.username}</span>
            </div>
            <div className="flex justify-between">
              <span className="text-sm text-gray-500">Ngôn ngữ</span>
              <span className="text-sm font-medium text-gray-800">
                {user.preferredLanguage === 'vi' ? 'Tiếng Việt' : 'English'}
              </span>
            </div>
            <div className="flex justify-between">
              <span className="text-sm text-gray-500">Gói dịch vụ</span>
              <span className={`text-sm font-bold ${isPremium ? 'text-amber-600' : 'text-gray-600'}`}>
                {isPremium ? 'Premium' : 'Basic'}
              </span>
            </div>
            <div className="flex justify-between">
              <span className="text-sm text-gray-500">Trạng thái</span>
              <span className="text-sm font-medium text-green-600">Hoạt động</span>
            </div>
          </div>
        </div>

        {/* Logout */}
        <button
          onClick={handleLogout}
          className="w-full py-3 bg-white border border-red-200 text-red-600 font-semibold rounded-xl hover:bg-red-50 transition flex items-center justify-center gap-2"
        >
          <LogOut size={18} />
          Đăng xuất
        </button>

        {/* Back home */}
        <button
          onClick={() => navigate('/')}
          className="w-full py-3 bg-gray-100 text-gray-600 font-medium rounded-xl hover:bg-gray-200 transition text-center"
        >
          ← Quay về trang chủ
        </button>
      </main>
    </div>
  )
}
