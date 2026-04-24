import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { BarChart3, Flame, MapPinned, Timer, Eye, Users, TrendingUp, Radio } from 'lucide-react';
import { apiGet, apiGetWithUser, buildQuery } from '../services/apiClient';
import { useUser } from '../contexts/UserContext.jsx';
import { Circle, MapContainer, Marker, Popup, TileLayer } from 'react-leaflet';
import L from 'leaflet';
import markerIcon from 'leaflet/dist/images/marker-icon.png';
import markerIcon2x from 'leaflet/dist/images/marker-icon-2x.png';
import markerShadow from 'leaflet/dist/images/marker-shadow.png';

delete L.Icon.Default.prototype._getIconUrl;
L.Icon.Default.mergeOptions({
  iconRetinaUrl: markerIcon2x,
  iconUrl: markerIcon,
  shadowUrl: markerShadow,
});

export default function DashboardPage() {
  const navigate = useNavigate();
  const { currentUser } = useUser();
  const isAdmin = currentUser?.role === 'Admin';
  const isShopManager = currentUser?.role === 'ShopManager';
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState('');
  const [topPoisByListening, setTopPoisByListening] = useState([]);
  const [topPoisByVisit, setTopPoisByVisit] = useState([]);
  const [pois, setPois] = useState([]);
  const [usage, setUsage] = useState({
    totalVisits: 0,
    totalListens: 0,
    totalListenDuration: 0,
    uniqueVisitors: 0,
    activeCells: 0,
    days: 7
  });
  const [dailyStats, setDailyStats] = useState([]);
  const [heatmap, setHeatmap] = useState([]);
  const [liveSessions, setLiveSessions] = useState({ totalActive: 0, byPoi: [] });

  useEffect(() => {
    let active = true;

    async function loadDashboard() {
      setIsLoading(true);
      setError('');

      try {
        const now = new Date();
        const start = new Date(now);
        start.setDate(start.getDate() - 7);

        // Load POIs - Admin sees all, ShopManager sees only their POIs
        let poiData = [];
        if (isShopManager && currentUser?.id) {
          const managerData = await apiGetWithUser('/users/me/pois', currentUser.id);
          poiData = managerData?.pois || [];
        } else {
          poiData = await apiGet('/pois');
        }

        if (!active) return;

        // Load analytics for both Admin and ShopManager
        let listeningData = [], visitData = [], usageData = {}, heatmapData = [], dailyData = [];

        if (isAdmin) {
          // Admin gets all stats
          [listeningData, visitData, usageData, heatmapData, dailyData] = await Promise.all([
            apiGet('/analytics/top?limit=5'),
            apiGet('/analytics/visits/top?limit=5'),
            apiGet(`/analytics/summary?days=7`),
            apiGet(`/analytics/heatmap${buildQuery({
              startDate: start.toISOString(),
              endDate: now.toISOString(),
              precision: 3,
            })}`),
            apiGet(`/analytics/daily?days=7`)
          ]);
        } else if (isShopManager && currentUser?.id) {
          // ShopManager gets stats filtered by their POIs
          [listeningData, visitData, usageData, heatmapData, dailyData] = await Promise.all([
            apiGet(`/analytics/top?limit=5&managerId=${currentUser.id}`),
            apiGet(`/analytics/visits/top?limit=5&managerId=${currentUser.id}`),
            apiGet(`/analytics/summary?days=7&managerId=${currentUser.id}`),
            apiGet(`/analytics/heatmap${buildQuery({
              startDate: start.toISOString(),
              endDate: now.toISOString(),
              precision: 3,
              managerId: currentUser.id
            })}`),
            apiGet(`/analytics/daily?days=7&managerId=${currentUser.id}`)
          ]);
        }

        if (!active) return;

        setTopPoisByListening(Array.isArray(listeningData) ? listeningData : []);
        setTopPoisByVisit(Array.isArray(visitData) ? visitData : []);
        setUsage({
          totalVisits: usageData?.totalVisits || 0,
          totalListens: usageData?.totalListenings || 0,
          totalListenDuration: usageData?.totalListenDurationSeconds || 0,
          uniqueVisitors: usageData?.uniqueVisitors || 0,
          activeCells: Array.isArray(heatmapData) ? heatmapData.length : 0,
          days: 7
        });
        setDailyStats(Array.isArray(dailyData) ? dailyData : []);
        setHeatmap(Array.isArray(heatmapData) ? heatmapData : []);
        setPois(Array.isArray(poiData) ? poiData : []);
      } catch (loadError) {
        if (!active) return;
        setError(loadError.message || 'Không thể tải dữ liệu tổng quan.');
      } finally {
        if (active) setIsLoading(false);
      }
    }

    loadDashboard();

    // Poll live session counts every 5 seconds
    let pollInterval;
    if (isAdmin || isShopManager) {
      async function pollLiveSessions() {
        try {
          const data = await apiGet('/sessions/active');
          if (active) setLiveSessions(data || { totalActive: 0, byPoi: [] });
        } catch { /* silent */ }
      }
      pollLiveSessions();
      pollInterval = setInterval(pollLiveSessions, 5000);
    }

    return () => { active = false; if (pollInterval) clearInterval(pollInterval); };
  }, [isAdmin, isShopManager, currentUser?.id]);

  const mapCenter = pois.length > 0
    ? [pois[0].latitude, pois[0].longitude]
    : [10.758, 106.69];

  const quickActions = [
    { label: 'Quản lý POI', path: '/pois', roles: ['Admin', 'ShopManager'] },
    { label: 'Quản lý Tour', path: '/tours', roles: ['Admin', 'ShopManager'] },
    { label: 'Thống kê chi tiết', path: '/analytics', roles: ['Admin', 'ShopManager'] },
    { label: 'Lịch sử sử dụng', path: '/usage-history', roles: ['Admin'] },
    { label: 'Subscription', path: '/subscriptions', roles: ['Admin'] },
    { label: 'QR Manager', path: '/qr-manager', roles: ['Admin', 'ShopManager'] },
  ].filter((item) => {
    if (!currentUser?.role) return true;
    return item.roles.includes(currentUser.role);
  });

  const heatmapPoints = heatmap
    .map((cell) => ({
      ...cell,
      latitude: cell.latitudeBucket ?? cell.cellLatitude,
      longitude: cell.longitudeBucket ?? cell.cellLongitude,
    }))
    .filter((cell) => Number.isFinite(cell.latitude) && Number.isFinite(cell.longitude));

  const liveCountByPoi = new Map(
    (liveSessions.byPoi || []).map(p => [p.poiId, p.activeCount])
  );
  const totalLive = liveSessions.totalActive || 0;

  const formatDuration = (seconds) => {
    if (!seconds) return '0s';
    if (seconds < 60) return `${seconds}s`;
    const mins = Math.floor(seconds / 60);
    const secs = seconds % 60;
    return `${mins}m ${secs}s`;
  };

  return (
    <div className='space-y-6 max-w-7xl mx-auto'>
      <div className='bg-white p-5 rounded-2xl shadow-sm border border-pink-100'>
        <h1 className='text-2xl font-bold text-transparent bg-clip-text bg-linear-to-r from-pink-600 to-purple-500'>
          {isShopManager ? 'Bảng điều khiển' : 'Tổng quan hệ thống'}
        </h1>
        <p className='text-sm text-pink-400/90 mt-1'>
          {isShopManager 
            ? `Quản lý ${pois.length} điểm POI của bạn` 
            : 'Theo dõi nhanh dữ liệu vận hành và đi tới các màn quản trị bằng một cú nhấp.'}
        </p>
        <div className='mt-4 flex flex-wrap gap-2'>
          {quickActions.map((action) => (
            <QuickAction
              key={action.path}
              label={action.label}
              onClick={() => navigate(action.path)}
            />
          ))}
        </div>
      </div>

      {error && (
        <div className='rounded-xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700'>
          {error}
        </div>
      )}

      {/* Statistics Cards */}
      {(isAdmin || isShopManager) && (
        <div className='grid grid-cols-2 md:grid-cols-4 gap-4'>
          <MetricCard
            icon={<Eye size={20} />}
            title='Tổng lượt xem'
            value={isLoading ? '...' : usage.totalVisits}
            subtitle={`${usage.uniqueVisitors} khách`}
            onClick={() => navigate('/analytics')}
          />
          <MetricCard
            icon={<Timer size={20} />}
            title='Tổng lượt nghe'
            value={isLoading ? '...' : usage.totalListens}
            subtitle={formatDuration(usage.totalListenDuration)}
            onClick={() => navigate('/analytics')}
          />
          <MetricCard
            icon={<MapPinned size={20} />}
            title='Ô heatmap hoạt động'
            value={isLoading ? '...' : usage.activeCells}
            subtitle='vị trí'
            onClick={() => navigate('/analytics')}
          />
          <MetricCard
            icon={<BarChart3 size={20} />}
            title='Khung phân tích'
            value={isLoading ? '...' : `${usage.days} ngày`}
            subtitle='7 ngày gần nhất'
            onClick={() => navigate('/analytics')}
          />
          <MetricCard
            icon={<Radio size={20} />}
            title='Đang nghe'
            value={totalLive}
            subtitle={totalLive > 0 ? 'du khách' : 'hiện không có ai'}
            onClick={() => navigate('/analytics')}
            highlight={totalLive > 0}
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
          <div className='p-4'>
            <DailyStatsChart data={dailyStats} />
          </div>
        </div>
      )}

      <div className='grid grid-cols-1 lg:grid-cols-2 gap-6'>
        {/* Top POIs by Listening - show for both Admin and ShopManager */}
        {(isAdmin || isShopManager) && (
          <div className='bg-white rounded-2xl shadow-sm border border-pink-100 overflow-hidden'>
            <div className='px-5 py-4 border-b border-pink-100 bg-pink-50/60 flex items-center gap-2'>
              <Flame size={18} className='text-pink-600' />
              <h2 className='font-semibold text-pink-700'>Top POI được nghe nhiều</h2>
            </div>
            <div className='p-4'>
              <table className='w-full text-sm'>
                <thead className='text-gray-500'>
                  <tr>
                    <th className='text-left py-2'>POI</th>
                    <th className='text-right py-2'>Lượt nghe</th>
                    <th className='text-right py-2'>TB (giây)</th>
                  </tr>
                </thead>
                <tbody>
                  {topPoisByListening.map((item) => (
                    <tr key={item.poiId} className='border-t border-pink-50 hover:bg-pink-50/30'>
                      <td className='py-2 font-medium text-gray-700'>{item.poiName}</td>
                      <td className='py-2 text-right text-pink-600 font-semibold'>{item.listeningCount}</td>
                      <td className='py-2 text-right text-gray-600'>
                        {Math.round(item.averageDurationSeconds || 0)}
                      </td>
                    </tr>
                  ))}
                  {!isLoading && topPoisByListening.length === 0 && (
                    <tr>
                      <td className='py-4 text-gray-500' colSpan={3}>
                        Chưa có dữ liệu.
                      </td>
                    </tr>
                  )}
                </tbody>
              </table>
            </div>
          </div>
        )}

        {/* Top POIs by Visit - show for both Admin and ShopManager */}
        {(isAdmin || isShopManager) && (
          <div className='bg-white rounded-2xl shadow-sm border border-pink-100 overflow-hidden'>
            <div className='px-5 py-4 border-b border-pink-100 bg-pink-50/60 flex items-center gap-2'>
              <Users size={18} className='text-purple-600' />
              <h2 className='font-semibold text-purple-700'>Top POI được ghé thăm</h2>
            </div>
            <div className='p-4'>
              <table className='w-full text-sm'>
                <thead className='text-gray-500'>
                  <tr>
                    <th className='text-left py-2'>POI</th>
                    <th className='text-right py-2'>Lượt xem</th>
                    <th className='text-right py-2'>Khách</th>
                  </tr>
                </thead>
                <tbody>
                  {topPoisByVisit.map((item) => (
                    <tr key={item.poiId} className='border-t border-pink-50 hover:bg-purple-50/30'>
                      <td className='py-2 font-medium text-gray-700'>{item.poiName}</td>
                      <td className='py-2 text-right text-purple-600 font-semibold'>{item.visitCount}</td>
                      <td className='py-2 text-right text-gray-600'>
                        {item.uniqueVisitors}
                      </td>
                    </tr>
                  ))}
                  {!isLoading && topPoisByVisit.length === 0 && (
                    <tr>
                      <td className='py-4 text-gray-500' colSpan={3}>
                        Chưa có dữ liệu.
                      </td>
                    </tr>
                  )}
                </tbody>
              </table>
            </div>
          </div>
        )}

        {/* Heatmap - Only show for Admin */}
        {isAdmin && (
          <div className='bg-white rounded-2xl shadow-sm border border-pink-100 overflow-hidden'>
            <div className='px-5 py-4 border-b border-pink-100 bg-pink-50/60'>
              <h2 className='font-semibold text-pink-700'>Heatmap nổi bật</h2>
            </div>
            <div className='p-4 space-y-2'>
              {heatmapPoints.slice(0, 8).map((cell, index) => {
                const nearestPoi = pois.find((poi) => {
                  const latDiff = Math.abs(poi.latitude - cell.latitude);
                  const lngDiff = Math.abs(poi.longitude - cell.longitude);
                  return latDiff < 0.01 && lngDiff < 0.01;
                });
                return (
                  <div
                    key={`${cell.latitude}-${cell.longitude}-${index}`}
                    className='flex items-center justify-between rounded-lg border border-pink-100 px-3 py-2'
                  >
                    <span className='text-sm text-gray-700'>
                      {nearestPoi ? nearestPoi.name : `Lat ${cell.latitude.toFixed(4)} / Lng ${cell.longitude.toFixed(4)}`}
                    </span>
                    <span className='text-xs font-semibold text-pink-700 bg-pink-100 px-2 py-1 rounded'>
                      {cell.pointCount} điểm
                    </span>
                  </div>
                );
              })}
              {!isLoading && heatmap.length === 0 && (
                <div className='text-sm text-gray-500'>Chưa có dữ liệu heatmap.</div>
              )}
            </div>
          </div>
        )}
      </div>

      <div className='bg-white rounded-2xl shadow-sm border border-pink-100 overflow-hidden'>
        <div className='px-5 py-4 border-b border-pink-100 bg-pink-50/60'>
          <h2 className='font-semibold text-pink-700'>
            {isAdmin ? 'Bản đồ POI + Heatmap' : 'Bản đồ POI của bạn'}
          </h2>
        </div>

        <div className='h-115'>
          <MapContainer center={mapCenter} zoom={15} className='h-full w-full'>
            <TileLayer
              attribution='&copy; OpenStreetMap contributors'
              url='https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png'
            />

            {pois.map((poi) => {
              const live = liveCountByPoi.get(poi.id) || 0;
              return (
              <Marker key={poi.id} position={[poi.latitude, poi.longitude]}>
                <Popup>
                  <div className='space-y-1 text-sm'>
                    <div className='font-semibold'>{poi.name}</div>
                    <div>Mã: {poi.code}</div>
                    <div>Ưu tiên: {poi.priority ?? 0}</div>
                    <div>Radius: {poi.triggerRadiusMeters}m</div>
                    {live > 0 && (
                      <div className='text-green-600 font-semibold'>🎧 Đang nghe: {live}</div>
                    )}
                    {poi.mapLink && (
                      <a href={poi.mapLink} target='_blank' rel='noreferrer'>
                        Mở map link
                      </a>
                    )}
                  </div>
                </Popup>
              </Marker>
              );
            })}

            {isAdmin && heatmapPoints.map((cell, index) => {
              const maxCount = Math.max(...heatmapPoints.map(p => p.pointCount), 1);
              const intensity = cell.pointCount / maxCount; // 0..1
              // Color scale: light pink → deep rose
              const r = Math.round(251 + (243 - 251) * intensity);
              const g = Math.round(113 + (63 - 113) * intensity);
              const b = Math.round(133 + (94 - 133) * intensity);
              const fillOpacity = 0.15 + 0.45 * intensity;
              return (
              <Circle
                key={`${cell.latitude}-${cell.longitude}-${index}`}
                center={[cell.latitude, cell.longitude]}
                radius={40 + cell.pointCount * 10}
                pathOptions={{
                  color: `rgb(${r},${g},${b})`,
                  fillColor: `rgb(${r},${g},${b})`,
                  fillOpacity,
                  weight: 1,
                }}
              />
              );
            })}

          </MapContainer>

          {isAdmin && heatmapPoints.length > 0 && (
            <div className='px-4 py-2 flex items-center gap-3 text-xs text-gray-500 border-t border-pink-100'>
              <span>Heatmap:</span>
              <div className='flex items-center gap-1'>
                <div className='w-4 h-3 rounded' style={{ backgroundColor: 'rgba(251,113,133,0.15)', border: '1px solid rgba(251,113,133,0.3)' }} />
                <span>Ít</span>
              </div>
              <div className='flex items-center gap-1'>
                <div className='w-4 h-3 rounded' style={{ backgroundColor: 'rgba(230,75,130,0.40)' }} />
                <span>Trung bình</span>
              </div>
              <div className='flex items-center gap-1'>
                <div className='w-4 h-3 rounded' style={{ backgroundColor: 'rgba(190,24,93,0.70)' }} />
                <span>Nhiều</span>
              </div>
              <span className='ml-auto'>Điểm nóng nhất: {Math.max(...heatmapPoints.map(p => p.pointCount))} lượt</span>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}

function QuickAction({ label, onClick }) {
  return (
    <button
      type='button'
      onClick={onClick}
      className='rounded-lg border border-pink-200 bg-pink-50 px-3 py-1.5 text-sm font-medium text-pink-700 hover:bg-pink-100'
    >
      {label}
    </button>
  );
}

function MetricCard({ icon, title, value, subtitle, onClick, highlight }) {
  return (
    <button
      type='button'
      onClick={onClick}
      className={`bg-white rounded-xl border p-4 shadow-sm text-left hover:shadow-md transition ${highlight ? 'border-green-400 bg-green-50' : 'border-pink-100 hover:border-pink-300'}`}
    >
      <div className={`flex items-center gap-2 ${highlight ? 'text-green-600' : 'text-pink-600'}`}>{icon}</div>
      <p className='text-sm text-gray-500 mt-2'>{title}</p>
      <p className={`text-2xl font-bold mt-1 ${highlight ? 'text-green-700' : 'text-gray-800'}`}>{value}</p>
      {subtitle && <p className='text-xs text-gray-400 mt-1'>{subtitle}</p>}
    </button>
  );
}

// Simple daily stats bar chart
function DailyStatsChart({ data }) {
  if (!data || data.length === 0) return null;

  const maxVisits = Math.max(...data.map(d => d.totalVisits), 1);
  const maxListens = Math.max(...data.map(d => d.totalListens), 1);

  return (
    <div className='space-y-4'>
      {/* Visits chart */}
      <div>
        <h3 className='text-xs text-gray-500 mb-2'>Lượt xem</h3>
        <div className='flex items-end gap-1 h-20'>
          {data.map((day, i) => (
            <div key={i} className='flex-1 flex flex-col items-center justify-end h-full'>
              <div 
                className='w-full bg-purple-400 rounded-t transition-all'
                style={{ height: `${(day.totalVisits / maxVisits) * 100}%`, minHeight: day.totalVisits > 0 ? '4px' : '0' }}
              />
              <span className='text-xs text-gray-400 mt-1'>
                {new Date(day.date).toLocaleDateString('vi-VN', { weekday: 'short' })}
              </span>
            </div>
          ))}
        </div>
      </div>

      {/* Listenings chart */}
      <div>
        <h3 className='text-xs text-gray-500 mb-2'>Lượt nghe</h3>
        <div className='flex items-end gap-1 h-20'>
          {data.map((day, i) => (
            <div key={i} className='flex-1 flex flex-col items-center justify-end h-full'>
              <div 
                className='w-full bg-pink-400 rounded-t transition-all'
                style={{ height: `${(day.totalListens / maxListens) * 100}%`, minHeight: day.totalListens > 0 ? '4px' : '0' }}
              />
              <span className='text-xs text-gray-400 mt-1'>
                {new Date(day.date).toLocaleDateString('vi-VN', { weekday: 'short' })}
              </span>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}
