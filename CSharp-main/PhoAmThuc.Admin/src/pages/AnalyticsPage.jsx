import { useEffect, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { apiGet, apiGetWithUser, buildQuery } from '../services/apiClient';
import { useUser } from '../contexts/UserContext.jsx';
import { Eye, Timer, Users, MapPin, TrendingUp, Calendar, BarChart3 } from 'lucide-react';

export default function AnalyticsPage() {
  const [searchParams] = useSearchParams();
  const { currentUser } = useUser();
  const isAdmin = currentUser?.role === 'Admin';
  const isShopManager = currentUser?.role === 'ShopManager';
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState('');
  const [days, setDays] = useState(7);
  const [selectedPoiId, setSelectedPoiId] = useState(searchParams.get('poiId') || '');
  
  // Summary data
  const [summary, setSummary] = useState(null);
  
  // POI list for selection
  const [pois, setPois] = useState([]);
  
  // Detailed stats
  const [visitStats, setVisitStats] = useState([]);
  const [listeningStats, setListeningStats] = useState([]);
  const [dailyStats, setDailyStats] = useState([]);
  const [tourStats, setTourStats] = useState([]);
  const [geofenceStats, setGeofenceStats] = useState([]);
  
  // Selected POI detail
  const [selectedPoiDetail, setSelectedPoiDetail] = useState(null);
  
  // Visit history
  const [visitHistory, setVisitHistory] = useState([]);

  useEffect(() => {
    let active = true;
    setIsLoading(true);

    async function loadAnalytics() {
      try {
        const managerId = isShopManager ? currentUser?.id : null;

        // Load POIs
        let poiData = [];
        if (isShopManager && currentUser?.id) {
          const managerData = await apiGetWithUser('/users/me/pois', currentUser.id);
          poiData = managerData?.pois || [];
        } else {
          poiData = await apiGet('/pois');
        }

        if (!active) return;
        setPois(Array.isArray(poiData) ? poiData : []);

        // Load analytics data
        const [
          summaryData,
          visitData,
          listeningData,
          dailyData,
          tourData,
          geofenceData
        ] = await Promise.all([
          apiGet(`/analytics/summary?days=${days}${managerId ? `&managerId=${managerId}` : ''}`),
          apiGet(`/analytics/visits/pois${managerId ? `?managerId=${managerId}` : ''}`),
          apiGet(`/analytics/pois${managerId ? `?managerId=${managerId}` : ''}`),
          apiGet(`/analytics/daily?days=${days}${managerId ? `&managerId=${managerId}` : ''}`),
          apiGet(`/analytics/tours${managerId ? `?managerId=${managerId}` : ''}`),
          apiGet(`/analytics/geofence${managerId ? `?managerId=${managerId}` : ''}`)
        ]);

        if (!active) return;

        setSummary(summaryData);
        setVisitStats(Array.isArray(visitData) ? visitData : []);
        setListeningStats(Array.isArray(listeningData) ? listeningData : []);
        setDailyStats(Array.isArray(dailyData) ? dailyData : []);
        setTourStats(Array.isArray(tourData) ? tourData : []);
        setGeofenceStats(Array.isArray(geofenceData) ? geofenceData : []);

        // Load selected POI detail if poiId is in URL
        if (selectedPoiId) {
          const poiDetail = await apiGet(`/analytics/visits/pois/${selectedPoiId}`);
          if (active && poiDetail) setSelectedPoiDetail(poiDetail);
          
          // Load visit history for this POI
          const visits = await apiGet(`/visits?poiId=${selectedPoiId}&limit=100`);
          if (active) setVisitHistory(Array.isArray(visits) ? visits : []);
        }
      } catch (loadError) {
        if (!active) return;
        setError(loadError.message || 'Không thể tải dữ liệu phân tích.');
      } finally {
        if (active) setIsLoading(false);
      }
    }

    if (isAdmin || isShopManager) {
      loadAnalytics();
    }

    return () => { active = false; };
  }, [days, isAdmin, isShopManager, currentUser?.id, selectedPoiId]);

  const formatDuration = (seconds) => {
    if (!seconds) return '0s';
    if (seconds < 60) return `${seconds}s`;
    const mins = Math.floor(seconds / 60);
    const secs = seconds % 60;
    return `${mins}m ${secs}s`;
  };

  const handlePoiSelect = (poiId) => {
    setSelectedPoiId(poiId);
  };

  if (!isAdmin && !isShopManager) {
    return (
      <div className='p-6 text-center text-gray-500'>
        Bạn không có quyền truy cập trang này.
      </div>
    );
  }

  return (
    <div className='space-y-6 max-w-7xl mx-auto'>
      {/* Header */}
      <div className='bg-white p-5 rounded-2xl shadow-sm border border-pink-100'>
        <div className='flex items-center justify-between'>
          <div>
            <h1 className='text-2xl font-bold text-transparent bg-clip-text bg-linear-to-r from-pink-600 to-purple-500'>
              Phân tích chi tiết
            </h1>
            <p className='text-sm text-pink-400/90 mt-1'>
              {isShopManager ? 'Thống kê hoạt động của các điểm POI của bạn' : 'Tổng quan thống kê toàn hệ thống'}
            </p>
          </div>
          
          {/* Days selector */}
          <div className='flex items-center gap-2'>
            <Calendar size={18} className='text-pink-600' />
            <select
              className='rounded-lg border border-pink-100 px-3 py-2'
              value={days}
              onChange={(e) => setDays(parseInt(e.target.value))}
            >
              <option value={7}>7 ngày</option>
              <option value={14}>14 ngày</option>
              <option value={30}>30 ngày</option>
              <option value={90}>90 ngày</option>
            </select>
          </div>
        </div>
      </div>

      {error && (
        <div className='rounded-xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700'>
          {error}
        </div>
      )}

      {/* Summary Cards */}
      {summary && (
        <div className='grid grid-cols-2 md:grid-cols-4 gap-4'>
          <SummaryCard
            icon={<Eye size={20} />}
            title='Tổng lượt xem'
            value={summary.totalVisits || 0}
            subtitle={`${summary.uniqueVisitors || 0} khách duy nhất`}
          />
          <SummaryCard
            icon={<Timer size={20} />}
            title='Tổng lượt nghe'
            value={summary.totalListenings || 0}
            subtitle={formatDuration(summary.totalListenDurationSeconds || 0)}
          />
          <SummaryCard
            icon={<Users size={20} />}
            title='Lượt xem Tour'
            value={summary.tourViews || 0}
            subtitle={`${summary.activePois || 0} POIs hoạt động`}
          />
          <SummaryCard
            icon={<MapPin size={20} />}
            title='Sự kiện Geofence'
            value={summary.geofenceEvents || 0}
            subtitle={`${summary.newVisitors || 0} khách mới`}
          />
        </div>
      )}

      {/* Daily Stats Chart */}
      {dailyStats.length > 0 && (
        <div className='bg-white rounded-2xl shadow-sm border border-pink-100 overflow-hidden'>
          <div className='px-5 py-4 border-b border-pink-100 bg-pink-50/60 flex items-center gap-2'>
            <TrendingUp size={18} className='text-pink-600' />
            <h2 className='font-semibold text-pink-700'>Biểu đồ hoạt động theo ngày</h2>
          </div>
          <div className='p-4 overflow-x-auto'>
            <DailyChart data={dailyStats} />
          </div>
        </div>
      )}

      <div className='grid grid-cols-1 lg:grid-cols-2 gap-6'>
        {/* Visit Stats by POI */}
        <div className='bg-white rounded-2xl shadow-sm border border-pink-100 overflow-hidden'>
          <div className='px-5 py-4 border-b border-pink-100 bg-pink-50/60 flex items-center gap-2'>
            <Eye size={18} className='text-pink-600' />
            <h2 className='font-semibold text-pink-700'>Lượt xem theo POI</h2>
          </div>
          <div className='overflow-x-auto'>
            <table className='w-full text-sm'>
              <thead className='text-gray-500 bg-gray-50'>
                <tr>
                  <th className='text-left py-3 px-4'>POI</th>
                  <th className='text-right py-3 px-2'>Lượt xem</th>
                  <th className='text-right py-3 px-2'>Khách</th>
                  <th className='text-right py-3 px-2'>TB thời gian</th>
                </tr>
              </thead>
              <tbody>
                {visitStats.map((item) => (
                  <tr 
                    key={item.poiId} 
                    className={`border-t border-pink-50 hover:bg-pink-50/30 cursor-pointer ${selectedPoiId === item.poiId ? 'bg-pink-50' : ''}`}
                    onClick={() => handlePoiSelect(item.poiId)}
                  >
                    <td className='py-3 px-4'>
                      <div className='font-medium text-gray-700'>{item.poiName}</div>
                      <div className='text-xs text-gray-400'>{item.poiCode}</div>
                    </td>
                    <td className='py-3 px-2 text-right text-pink-600 font-semibold'>{item.visitCount}</td>
                    <td className='py-3 px-2 text-right text-gray-600'>{item.uniqueVisitors}</td>
                    <td className='py-3 px-2 text-right text-gray-600'>{formatDuration(Math.round(item.averageDurationSeconds))}</td>
                  </tr>
                ))}
                {visitStats.length === 0 && (
                  <tr>
                    <td colSpan={4} className='py-8 text-center text-gray-500'>
                      Chưa có dữ liệu
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        </div>

        {/* Listening Stats by POI */}
        <div className='bg-white rounded-2xl shadow-sm border border-pink-100 overflow-hidden'>
          <div className='px-5 py-4 border-b border-pink-100 bg-pink-50/60 flex items-center gap-2'>
            <Timer size={18} className='text-pink-600' />
            <h2 className='font-semibold text-pink-700'>Lượt nghe theo POI</h2>
          </div>
          <div className='overflow-x-auto'>
            <table className='w-full text-sm'>
              <thead className='text-gray-500 bg-gray-50'>
                <tr>
                  <th className='text-left py-3 px-4'>POI</th>
                  <th className='text-right py-3 px-2'>Lượt nghe</th>
                  <th className='text-right py-3 px-2'>TB giây</th>
                </tr>
              </thead>
              <tbody>
                {listeningStats.map((item) => (
                  <tr key={item.poiId} className='border-t border-pink-50 hover:bg-pink-50/30'>
                    <td className='py-3 px-4'>
                      <div className='font-medium text-gray-700'>{item.poiName}</div>
                      <div className='text-xs text-gray-400'>{item.poiCode}</div>
                    </td>
                    <td className='py-3 px-2 text-right text-pink-600 font-semibold'>{item.listeningCount}</td>
                    <td className='py-3 px-2 text-right text-gray-600'>{Math.round(item.averageDurationSeconds)}s</td>
                  </tr>
                ))}
                {listeningStats.length === 0 && (
                  <tr>
                    <td colSpan={3} className='py-8 text-center text-gray-500'>
                      Chưa có dữ liệu
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        </div>
      </div>

      {/* Tour Stats */}
      {tourStats.length > 0 && (
        <div className='bg-white rounded-2xl shadow-sm border border-pink-100 overflow-hidden'>
          <div className='px-5 py-4 border-b border-pink-100 bg-pink-50/60 flex items-center gap-2'>
            <BarChart3 size={18} className='text-pink-600' />
            <h2 className='font-semibold text-pink-700'>Thống kê Tour</h2>
          </div>
          <div className='overflow-x-auto'>
            <table className='w-full text-sm'>
              <thead className='text-gray-500 bg-gray-50'>
                <tr>
                  <th className='text-left py-3 px-4'>Tour</th>
                  <th className='text-right py-3 px-2'>Lượt xem</th>
                  <th className='text-right py-3 px-2'>Người xem</th>
                  <th className='text-right py-3 px-2'>TB thời gian</th>
                  <th className='text-right py-3 px-2'>POI ghé</th>
                  <th className='text-right py-3 px-2'>Nghe audio</th>
                </tr>
              </thead>
              <tbody>
                {tourStats.map((item) => (
                  <tr key={item.tourId} className='border-t border-pink-50 hover:bg-pink-50/30'>
                    <td className='py-3 px-4'>
                      <div className='font-medium text-gray-700'>{item.tourName}</div>
                      <div className='text-xs text-gray-400'>{item.tourCode}</div>
                    </td>
                    <td className='py-3 px-2 text-right text-pink-600 font-semibold'>{item.viewCount}</td>
                    <td className='py-3 px-2 text-right text-gray-600'>{item.uniqueViewers}</td>
                    <td className='py-3 px-2 text-right text-gray-600'>{formatDuration(item.averageDurationSeconds)}</td>
                    <td className='py-3 px-2 text-right text-gray-600'>{item.totalPoiVisited}</td>
                    <td className='py-3 px-2 text-right text-gray-600'>{item.totalAudioListened}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* Selected POI Detail */}
      {selectedPoiDetail && (
        <div className='bg-white rounded-2xl shadow-sm border border-pink-100 overflow-hidden'>
          <div className='px-5 py-4 border-b border-pink-100 bg-pink-50/60 flex items-center justify-between'>
            <h2 className='font-semibold text-pink-700'>
              Chi tiết: {selectedPoiDetail.poiName}
            </h2>
            <button
              onClick={() => setSelectedPoiId('')}
              className='text-sm text-pink-600 hover:text-pink-800'
            >
              Đóng
            </button>
          </div>
          <div className='p-4'>
            <div className='grid grid-cols-2 md:grid-cols-4 gap-4 mb-6'>
              <PoiStatCard title='Lượt xem' value={selectedPoiDetail.visitCount} />
              <PoiStatCard title='Khách duy nhất' value={selectedPoiDetail.uniqueVisitors} />
              <PoiStatCard title='TB thời gian' value={formatDuration(Math.round(selectedPoiDetail.averageDurationSeconds))} />
              <PoiStatCard title='Lượt nghe' value={selectedPoiDetail.audioListenedCount} />
            </div>

            {/* Visit History */}
            <h3 className='font-semibold text-gray-700 mb-3'>Lịch sử lượt xem gần đây</h3>
            <div className='overflow-x-auto'>
              <table className='w-full text-sm'>
                <thead className='text-gray-500 bg-gray-50'>
                  <tr>
                    <th className='text-left py-2 px-3'>Thời gian</th>
                    <th className='text-left py-2 px-3'>Người dùng</th>
                    <th className='text-left py-2 px-3'>Nguồn</th>
                    <th className='text-right py-2 px-3'>Thời gian ở</th>
                    <th className='text-right py-2 px-3'>Nghe audio</th>
                  </tr>
                </thead>
                <tbody>
                  {visitHistory.slice(0, 20).map((visit) => (
                    <tr key={visit.id} className='border-t border-pink-50'>
                      <td className='py-2 px-3'>{new Date(visit.visitedAtUtc).toLocaleString('vi-VN')}</td>
                      <td className='py-2 px-3'>{visit.username || visit.userId}</td>
                      <td className='py-2 px-3'>
                        <span className={`px-2 py-0.5 rounded text-xs ${
                          visit.triggerSource === 'QrCode' ? 'bg-green-100 text-green-700' :
                          visit.triggerSource === 'Map' ? 'bg-blue-100 text-blue-700' :
                          visit.triggerSource === 'Tour' ? 'bg-purple-100 text-purple-700' :
                          'bg-gray-100 text-gray-700'
                        }`}>
                          {visit.triggerSource}
                        </span>
                      </td>
                      <td className='py-2 px-3 text-right'>{formatDuration(visit.durationSeconds)}</td>
                      <td className='py-2 px-3 text-right'>
                        {visit.listenedToAudio ? (
                          <span className='text-green-600'>✓ {visit.listeningSessionCount}x</span>
                        ) : (
                          <span className='text-gray-400'>-</span>
                        )}
                      </td>
                    </tr>
                  ))}
                  {visitHistory.length === 0 && (
                    <tr>
                      <td colSpan={5} className='py-4 text-center text-gray-500'>
                        Chưa có lịch sử
                      </td>
                    </tr>
                  )}
                </tbody>
              </table>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

function SummaryCard({ icon, title, value, subtitle }) {
  return (
    <div className='bg-white rounded-xl border border-pink-100 p-4 shadow-sm'>
      <div className='flex items-center gap-2 text-pink-600'>{icon}</div>
      <p className='text-sm text-gray-500 mt-2'>{title}</p>
      <p className='text-2xl font-bold text-gray-800 mt-1'>
        {typeof value === 'number' ? value.toLocaleString('vi-VN') : value}
      </p>
      {subtitle && <p className='text-xs text-gray-400 mt-1'>{subtitle}</p>}
    </div>
  );
}

function PoiStatCard({ title, value }) {
  return (
    <div className='bg-pink-50 rounded-lg p-3'>
      <p className='text-xs text-gray-500'>{title}</p>
      <p className='text-xl font-bold text-pink-700 mt-1'>
        {typeof value === 'number' ? value.toLocaleString('vi-VN') : value}
      </p>
    </div>
  );
}

function DailyChart({ data }) {
  if (!data || data.length === 0) return null;

  const maxVisits = Math.max(...data.map(d => d.totalVisits), 1);
  const maxListens = Math.max(...data.map(d => d.totalListens), 1);

  return (
    <div className='space-y-6'>
      {/* Visits bar chart */}
      <div>
        <h3 className='text-xs text-gray-500 mb-3'>Lượt xem theo ngày</h3>
        <div className='flex items-end gap-2 h-32'>
          {data.map((day, i) => (
            <div key={i} className='flex-1 flex flex-col items-center justify-end h-full group'>
              <div className='w-full bg-purple-400 hover:bg-purple-500 rounded-t transition-all cursor-pointer relative' 
                   style={{ height: `${Math.max((day.totalVisits / maxVisits) * 100, 2)}%` }}>
                <div className='absolute -top-8 left-1/2 -translate-x-1/2 bg-gray-800 text-white text-xs px-2 py-1 rounded opacity-0 group-hover:opacity-100 whitespace-nowrap'>
                  {day.totalVisits} lượt xem
                </div>
              </div>
              <span className='text-xs text-gray-400 mt-2'>
                {new Date(day.date).toLocaleDateString('vi-VN', { day: 'numeric', month: 'short' })}
              </span>
            </div>
          ))}
        </div>
      </div>

      {/* Listenings bar chart */}
      <div>
        <h3 className='text-xs text-gray-500 mb-3'>Lượt nghe theo ngày</h3>
        <div className='flex items-end gap-2 h-32'>
          {data.map((day, i) => (
            <div key={i} className='flex-1 flex flex-col items-center justify-end h-full group'>
              <div className='w-full bg-pink-400 hover:bg-pink-500 rounded-t transition-all cursor-pointer relative' 
                   style={{ height: `${Math.max((day.totalListens / maxListens) * 100, 2)}%` }}>
                <div className='absolute -top-8 left-1/2 -translate-x-1/2 bg-gray-800 text-white text-xs px-2 py-1 rounded opacity-0 group-hover:opacity-100 whitespace-nowrap'>
                  {day.totalListens} lượt nghe
                </div>
              </div>
              <span className='text-xs text-gray-400 mt-2'>
                {new Date(day.date).toLocaleDateString('vi-VN', { day: 'numeric', month: 'short' })}
              </span>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}
