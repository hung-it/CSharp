import React, { useState, useEffect } from 'react';
import { Outlet, NavLink, useNavigate } from 'react-router-dom';
import { MapPin, BarChart3, Music, Languages, Route, History, WalletCards, QrCode, LogOut, TrendingUp } from 'lucide-react';
import { useUser } from '../contexts/UserContext.jsx';

export default function Layout() {
  const { currentUser, logout } = useUser();
  const navigate = useNavigate();
  const [showUserMenu, setShowUserMenu] = useState(false);

  const menuItems = [
    { name: 'Tổng quan (Dashboard)', icon: <BarChart3 size={20} />, path: '/', roles: ['Admin', 'ShopManager'] },
    { name: 'Phân tích chi tiết', icon: <TrendingUp size={20} />, path: '/analytics', roles: ['Admin', 'ShopManager'] },
    { name: 'Quản lý POI', icon: <MapPin size={20} />, path: '/pois', roles: ['Admin', 'ShopManager'] },
    { name: 'Quản lý Audio', icon: <Music size={20} />, path: '/audio', roles: ['Admin', 'ShopManager'] },
    { name: 'Quản lý Bản Dịch', icon: <Languages size={20} />, path: '/translations', roles: ['Admin', 'ShopManager'] },
    { name: 'Quản lý Tour', icon: <Route size={20} />, path: '/tours', roles: ['Admin'] },
    { name: 'QR Manager', icon: <QrCode size={20} />, path: '/qr-manager', roles: ['Admin', 'ShopManager'] },
    { name: 'Lịch sử sử dụng', icon: <History size={20} />, path: '/usage-history', roles: ['Admin'] },
    { name: 'Subscription 1/10 USD', icon: <WalletCards size={20} />, path: '/subscriptions', roles: ['Admin'] },
  ];

  const role = currentUser?.role;
  const visibleMenuItems = menuItems.filter((item) => {
    if (!role) {
      return true;
    }

    return item.roles.includes(role);
  });

  function handleLogout() {
    logout();
    setShowUserMenu(false);
    navigate('/login', { replace: true });
  }

  const getRoleLabel = (role) => {
    const roleMap = {
      Admin: 'Quản trị viên',
      ShopManager: 'Quản lý cửa hàng',
      EndUser: 'Người dùng',
    };
    return roleMap[role] || role;
  };

  const getUserInitial = (username) => {
    return username?.[0]?.toUpperCase() || '?';
  };

  return (
    <div className="flex h-screen bg-white">
      {/* Sidebar */}
      <div className="w-64 bg-white shadow-[2px_0_8px_-3px_rgba(236,72,153,0.1)] border-r border-pink-100 z-10 flex flex-col">
        <div className="h-16 flex flex-col justify-center px-6 border-b border-pink-100 bg-white">
          <h1 className="text-xl font-black text-transparent bg-clip-text bg-linear-to-r from-pink-600 to-purple-500 leading-tight">Vĩnh Khánh </h1>
          <p className="text-[10px] text-pink-400 font-medium uppercase tracking-wide">Thuyết Minh Tự Động</p>
        </div>
        
        <nav className="flex-1 p-4 space-y-2">
          {visibleMenuItems.map((item) => (
            <NavLink
              key={item.path}
              to={item.path}
              className={({ isActive }) =>
                `flex items-center space-x-3 w-full p-2.5 rounded-lg transition-all duration-200 ${
                  isActive 
                    ? 'bg-pink-100/80 text-pink-700 font-semibold shadow-sm' 
                    : 'text-gray-500 hover:bg-pink-50 hover:text-pink-600'
                }`
              }
            >
              {item.icon}
              <span>{item.name}</span>
            </NavLink>
          ))}
        </nav>
      </div>

      {/* Main Content */}
      <div className="flex-1 flex flex-col overflow-hidden relative">
        <header className="h-16 bg-white/80 backdrop-blur-md border-b border-pink-100 flex items-center justify-between px-8 z-10 shadow-[0_2px_10px_-3px_rgba(236,72,153,0.1)]">
          <h2 className="text-lg font-bold text-gray-700">{currentUser ? getRoleLabel(currentUser.role) : 'Chọn người dùng'}</h2>
          <div className="relative">
            <button
              type="button"
              onClick={() => setShowUserMenu(!showUserMenu)}
              className="flex items-center gap-3 px-4 py-2 rounded-lg hover:bg-pink-50 transition-colors"
            >
              {currentUser ? (
                <>
                  <div className="text-right">
                    <div className="text-sm font-medium text-gray-700">{currentUser.username}</div>
                    <div className="text-xs text-gray-500">{getRoleLabel(currentUser.role)}</div>
                  </div>
                  <div className="w-9 h-9 rounded-xl bg-linear-to-tr from-pink-500 to-purple-500 flex items-center justify-center text-white font-bold shadow-md">
                    {getUserInitial(currentUser.username)}
                  </div>
                </>
              ) : (
                <>
                  <span className="text-sm font-medium text-gray-500">Chọn tài khoản</span>
                  <div className="w-9 h-9 rounded-xl bg-gray-300 flex items-center justify-center text-white font-bold">?</div>
                </>
              )}
            </button>

            {showUserMenu && (
              <div className="absolute right-0 mt-2 w-56 bg-white rounded-lg shadow-lg border border-pink-100 z-20">
                <div className="p-3 border-b border-pink-100">
                  <div className="font-medium text-gray-700">{currentUser?.username}</div>
                  <div className="text-sm text-gray-500">{getRoleLabel(currentUser?.role)}</div>
                </div>
                <div className="p-2">
                  <button
                    type="button"
                    onClick={handleLogout}
                    className="w-full flex items-center gap-2 px-3 py-2 rounded hover:bg-rose-50 text-sm text-rose-600 font-medium transition"
                  >
                    <LogOut size={16} />
                    Đăng xuất
                  </button>
                </div>
              </div>
            )}
          </div>
        </header>

        <main className="flex-1 overflow-auto p-8 relative bg-white">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
