import React, { useState } from 'react';
import { Search, Plus, MapPin, Edit2, Trash2 } from 'lucide-react';

const MOCK_DATA = [
  { id: 1, name: 'Cổng chào Vĩnh Khánh', lat: 10.7578, lng: 106.7024, radius: 20, priority: 1, status: 'Hoạt động' },
  { id: 2, name: 'Khu hải sản ốc Tuyết', lat: 10.7565, lng: 106.7011, radius: 15, priority: 2, status: 'Hoạt động' },
  { id: 3, name: 'Điểm giao Nguyễn Hữu Hào', lat: 10.7550, lng: 106.7000, radius: 30, priority: 1, status: 'Tạm dừng' },
];

export default function PoiList() {
  const [searchTerm, setSearchTerm] = useState('');

  return (
    <div className="space-y-6 max-w-7xl mx-auto">
      {/* Header & Actions */}
      <div className="flex flex-col md:flex-row justify-between items-start md:items-center gap-4 bg-white p-5 rounded-2xl shadow-sm border border-pink-100">
        <div>
          <h1 className="text-2xl font-bold text-transparent bg-clip-text bg-gradient-to-r from-pink-600 to-purple-500">Quản lý Điểm POI</h1>
          <p className="text-sm text-pink-400/90 mt-1">Thiết lập tọa độ, bán kính và nội dung cho các điểm thuyết minh</p>
        </div>
        <button className="bg-gradient-to-r from-pink-500 to-rose-500 hover:from-pink-600 hover:to-rose-600 text-white px-5 py-2.5 rounded-xl flex items-center gap-2 shadow-md hover:shadow-lg transition-all transform hover:-translate-y-0.5">
          <Plus size={20} />
          <span className="font-semibold">Thêm POI mới</span>
        </button>
      </div>

      {/* Filter and Search Bar */}
      <div className="bg-white p-4 rounded-xl shadow-sm border border-pink-100 flex gap-4">
        <div className="relative flex-1 group">
          <Search className="absolute left-4 top-1/2 -translate-y-1/2 text-pink-300 group-focus-within:text-pink-500 transition-colors" size={20} />
          <input
            type="text"
            placeholder="Tìm kiếm POI theo tên hoặc tọa độ..."
            className="w-full pl-12 pr-4 py-3 bg-pink-50/50 border border-pink-100 rounded-xl focus:outline-none focus:ring-2 focus:ring-pink-400 focus:bg-white transition-all text-gray-700 placeholder-pink-300"
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
          />
        </div>
        <select className="bg-pink-50/50 border border-pink-100 rounded-xl px-5 py-3 focus:outline-none focus:ring-2 focus:ring-pink-400 focus:bg-white transition-all text-pink-700 font-medium cursor-pointer">
          <option value="all">💎 Tất cả trạng thái</option>
          <option value="active">✨ Đang hoạt động</option>
          <option value="paused">💤 Tạm dừng</option>
        </select>
      </div>

      {/* Table Data */}
      <div className="bg-white rounded-2xl shadow-sm border border-pink-100 overflow-hidden">
        <table className="w-full text-left border-collapse">
          <thead>
            <tr className="bg-pink-50/80 text-pink-600 text-sm border-b border-pink-100 uppercase tracking-wider">
              <th className="px-6 py-5 font-bold">Tên địa điểm (POI)</th>
              <th className="px-6 py-5 font-bold">Tọa độ (Lat/Lng)</th>
              <th className="px-6 py-5 font-bold text-center">Bán kính (m)</th>
              <th className="px-6 py-5 font-bold text-center">Độ ưu tiên</th>
              <th className="px-6 py-5 font-bold text-center">Trạng thái</th>
              <th className="px-6 py-5 font-bold text-right">Thao tác</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-pink-50">
            {MOCK_DATA.map((poi) => (
              <tr key={poi.id} className="hover:bg-pink-50/60 transition-all duration-200 group">
                <td className="px-6 py-5">
                  <div className="flex items-center gap-4">
                    <div className="p-2.5 bg-gradient-to-br from-pink-100 to-rose-50 text-pink-600 rounded-xl shadow-sm group-hover:scale-110 transition-transform">
                      <MapPin size={20} className="fill-pink-100/50" />
                    </div>
                    <span className="font-bold text-gray-700">{poi.name}</span>
                  </div>
                </td>
                <td className="px-6 py-5 text-sm font-medium text-pink-500/80">
                  <span className="bg-pink-50 px-2 py-1 rounded-md">{poi.lat}</span> 
                  <span className="mx-1 text-gray-300">,</span> 
                  <span className="bg-pink-50 px-2 py-1 rounded-md">{poi.lng}</span>
                </td>
                <td className="px-6 py-5 text-center">
                  <span className="inline-block bg-gradient-to-r from-blue-50 to-indigo-50 border border-blue-100 text-blue-700 font-bold px-3 py-1.5 rounded-lg text-xs shadow-sm">
                    {poi.radius}m
                  </span>
                </td>
                <td className="px-6 py-5 text-center">
                  <span className={`inline-flex w-8 h-8 rounded-xl text-sm mx-auto items-center justify-center font-black shadow-sm ${
                    poi.priority === 1 
                      ? 'bg-gradient-to-br from-rose-400 to-pink-500 text-white' 
                      : 'bg-gray-50 border border-gray-100 text-gray-500'
                  }`}>
                    {poi.priority}
                  </span>
                </td>
                <td className="px-6 py-5 text-center">
                  <span className={`inline-flex items-center gap-2 px-3 py-1.5 rounded-full text-xs font-bold shadow-sm ${
                    poi.status === 'Hoạt động' 
                      ? 'bg-gradient-to-r from-emerald-50 to-green-50 border border-green-100 text-green-600' 
                      : 'bg-gray-50 border border-gray-100 text-gray-500'
                  }`}>
                    <span className={`w-2 h-2 rounded-full shadow-sm ${poi.status === 'Hoạt động' ? 'bg-green-500 animate-pulse' : 'bg-gray-400'}`}></span>
                    {poi.status}
                  </span>
                </td>
                <td className="px-6 py-5 text-right">
                  <div className="flex justify-end gap-2">
                    <button className="p-2 text-pink-300 hover:text-pink-600 hover:bg-pink-50 rounded-lg transition-all" title="Chỉnh sửa">
                      <Edit2 size={18} />
                    </button>
                    <button className="p-2 text-pink-300 hover:text-rose-600 hover:bg-rose-50 rounded-lg transition-all" title="Xóa">
                      <Trash2 size={18} />
                    </button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>

        {/* Empty State / Or Pagination here */}
      </div>
    </div>
  );
}