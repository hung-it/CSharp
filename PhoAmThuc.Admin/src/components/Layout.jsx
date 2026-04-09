import React from 'react';
import { Outlet, NavLink } from 'react-router-dom';
import { MapPin, BarChart3, Settings, Music, QrCode } from 'lucide-react';

export default function Layout() {
  const menuItems = [
    { name: 'Tổng quan (Dashboard)', icon: <BarChart3 size={20} />, path: '/' },
    { name: 'Quản lý POI', icon: <MapPin size={20} />, path: '/pois' },
    { name: 'Quản lý Audio/Dịch', icon: <Music size={20} />, path: '/audio' },
    { name: 'Trình tạo QR Code', icon: <QrCode size={20} />, path: '/qr' },
    { name: 'Cài đặt', icon: <Settings size={20} />, path: '/settings' },
  ];

  return (
    <div className="flex h-screen bg-white">
      {/* Sidebar */}
      <div className="w-64 bg-white shadow-[2px_0_8px_-3px_rgba(236,72,153,0.1)] border-r border-pink-100 z-10 flex flex-col">
        <div className="h-16 flex flex-col justify-center px-6 border-b border-pink-100 bg-white">
          <h1 className="text-xl font-black text-transparent bg-clip-text bg-gradient-to-r from-pink-600 to-purple-500 leading-tight">Vĩnh Khánh </h1>
          <p className="text-[10px] text-pink-400 font-medium uppercase tracking-wide">Thuyết Minh Tự Động</p>
        </div>
        
        <nav className="flex-1 p-4 space-y-2">
          {menuItems.map((item) => (
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
          <h2 className="text-lg font-bold text-gray-700">Quản trị viên</h2>
          <div className="flex items-center gap-3">
            <span className="text-sm font-medium text-gray-500">Xin chào, Admin</span>
            <div className="w-9 h-9 rounded-xl bg-gradient-to-tr from-pink-500 to-purple-500 flex items-center justify-center text-white font-bold shadow-md hover:shadow-lg transition-all cursor-pointer">A</div>
          </div>
        </header>

        <main className="flex-1 overflow-auto p-8 relative bg-white">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
