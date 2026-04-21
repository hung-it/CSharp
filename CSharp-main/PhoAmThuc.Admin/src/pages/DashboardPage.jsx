import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { BarChart3, Flame, MapPinned, Timer } from 'lucide-react';
import { apiGet, buildQuery } from '../services/apiClient';
import { useUser } from '../contexts/UserContext.jsx';
import { Circle, MapContainer, Marker, Popup, Polyline, TileLayer } from 'react-leaflet';
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
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState('');
  const [topPois, setTopPois] = useState([]);
  const [pois, setPois] = useState([]);
  const [usage, setUsage] = useState({ totalListens: 0, activeCells: 0, days: 7 });
  const [heatmap, setHeatmap] = useState([]);
  const [anonymousRef, setAnonymousRef] = useState('demo-route-01');
  const [routePoints, setRoutePoints] = useState([]);
  const [isLoadingRoute, setIsLoadingRoute] = useState(false);

  useEffect(() => {
    let active = true;

    async function loadDashboard() {
      setIsLoading(true);
      setError('');

      try {
        const now = new Date();
        const start = new Date(now);
        start.setDate(start.getDate() - 7);

        const [topData, usageData, heatmapData, poiData] = await Promise.all([
          apiGet('/analytics/top?limit=5'),
          apiGet('/analytics/usage?days=7'),
          apiGet(`/analytics/heatmap${buildQuery({
            startDate: start.toISOString(),
            endDate: now.toISOString(),
            precision: 3,
          })}`),
          apiGet('/pois')
        ]);

        if (!active) {
          return;
        }

        setTopPois(Array.isArray(topData) ? topData : []);
        setUsage(usageData || { totalListens: 0, activeCells: 0, days: 7 });
        setHeatmap(Array.isArray(heatmapData) ? heatmapData : []);
        setPois(Array.isArray(poiData) ? poiData : []);
      } catch (loadError) {
        if (!active) {
          return;
        }

        setError(loadError.message || 'Không thể tải dữ liệu tổng quan.');
      } finally {
        if (active) {
          setIsLoading(false);
        }
      }
    }

    loadDashboard();

    return () => {
      active = false;
    };
  }, []);

  async function handleLoadRoute() {
    const ref = anonymousRef.trim();
    if (!ref) {
      return;
    }

    setIsLoadingRoute(true);
    setError('');

    try {
      const routeData = await apiGet(`/routes/anonymous/${encodeURIComponent(ref)}`);
      setRoutePoints(Array.isArray(routeData) ? routeData : []);
    } catch (loadError) {
      setError(loadError.message || 'Không thể tải tuyến ẩn danh.');
    } finally {
      setIsLoadingRoute(false);
    }
  }

  const mapCenter = pois.length > 0
    ? [pois[0].latitude, pois[0].longitude]
    : [10.758, 106.69];

  const routePath = routePoints
    .map((point) => [point.latitude, point.longitude])
    .filter((point) => Number.isFinite(point[0]) && Number.isFinite(point[1]));

  const quickActions = [
    { label: 'Quản lý POI', path: '/pois', roles: ['Admin', 'ShopManager'] },
    { label: 'Quản lý Tour', path: '/tours', roles: ['Admin', 'ShopManager'] },
    { label: 'Lịch sử sử dụng', path: '/usage-history', roles: ['Admin'] },
    { label: 'Subscription', path: '/subscriptions', roles: ['Admin'] },
    { label: 'QR Manager', path: '/qr-manager', roles: ['Admin', 'ShopManager'] },
  ].filter((item) => {
    if (!currentUser?.role) {
      return true;
    }

    return item.roles.includes(currentUser.role);
  });

  const heatmapPoints = heatmap
    .map((cell) => {
      const latitude = cell.latitudeBucket ?? cell.cellLatitude;
      const longitude = cell.longitudeBucket ?? cell.cellLongitude;

      return {
        ...cell,
        latitude,
        longitude,
      };
    })
    .filter((cell) => Number.isFinite(cell.latitude) && Number.isFinite(cell.longitude));

  return (
    <div className='space-y-6 max-w-7xl mx-auto'>
      <div className='bg-white p-5 rounded-2xl shadow-sm border border-pink-100'>
        <h1 className='text-2xl font-bold text-transparent bg-clip-text bg-linear-to-r from-pink-600 to-purple-500'>
          Tổng quan hệ thống
        </h1>
        <p className='text-sm text-pink-400/90 mt-1'>
          Theo dõi nhanh dữ liệu vận hành và đi tới các màn quản trị bằng một cú nhấp.
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

      <div className='grid grid-cols-1 md:grid-cols-3 gap-4'>
        <MetricCard
          icon={<Timer size={20} />}
          title='Tổng lượt nghe'
          value={isLoading ? '...' : usage.totalListens}
          onClick={() => navigate('/usage-history')}
        />
        <MetricCard
          icon={<MapPinned size={20} />}
          title='Ô heatmap hoạt động'
          value={isLoading ? '...' : usage.activeCells}
          onClick={() => navigate('/usage-history')}
        />
        <MetricCard
          icon={<BarChart3 size={20} />}
          title='Khung phân tích'
          value={isLoading ? '...' : `${usage.days} ngày`}
          onClick={() => navigate('/usage-history')}
        />
      </div>

      <div className='grid grid-cols-1 lg:grid-cols-2 gap-6'>
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
                {topPois.map((item) => (
                  <tr key={item.poiId} className='border-t border-pink-50'>
                    <td className='py-2 font-medium text-gray-700'>{item.poiName}</td>
                    <td className='py-2 text-right text-pink-600 font-semibold'>{item.listeningCount}</td>
                    <td className='py-2 text-right text-gray-600'>
                      {Math.round(item.averageDurationSeconds || 0)}
                    </td>
                  </tr>
                ))}
                {!isLoading && topPois.length === 0 && (
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

        <div className='bg-white rounded-2xl shadow-sm border border-pink-100 overflow-hidden'>
          <div className='px-5 py-4 border-b border-pink-100 bg-pink-50/60'>
            <h2 className='font-semibold text-pink-700'>Heatmap nổi bật</h2>
          </div>
          <div className='p-4 space-y-2'>
            {heatmapPoints.slice(0, 8).map((cell, index) => (
              <div
                key={`${cell.latitude}-${cell.longitude}-${index}`}
                className='flex items-center justify-between rounded-lg border border-pink-100 px-3 py-2'
              >
                <span className='text-sm text-gray-700'>
                  Lat {cell.latitude} / Lng {cell.longitude}
                </span>
                <span className='text-xs font-semibold text-pink-700 bg-pink-100 px-2 py-1 rounded'>
                  {cell.pointCount} points
                </span>
              </div>
            ))}
            {!isLoading && heatmap.length === 0 && (
              <div className='text-sm text-gray-500'>Chưa có dữ liệu heatmap.</div>
            )}
          </div>
        </div>
      </div>

      <div className='bg-white rounded-2xl shadow-sm border border-pink-100 overflow-hidden'>
        <div className='px-5 py-4 border-b border-pink-100 bg-pink-50/60 flex flex-wrap items-center gap-3 justify-between'>
          <h2 className='font-semibold text-pink-700'>Bản đồ POI + Heatmap + Route</h2>
          <div className='flex flex-wrap items-center gap-2'>
            <input
              className='rounded-lg border border-pink-100 px-3 py-1.5 text-sm'
              value={anonymousRef}
              onChange={(event) => setAnonymousRef(event.target.value)}
              placeholder='anonymousRef để vẽ route'
            />
            <button
              type='button'
              onClick={handleLoadRoute}
              disabled={isLoadingRoute}
              className='rounded-lg border border-pink-200 bg-pink-50 px-3 py-1.5 text-sm font-medium text-pink-700 disabled:opacity-60'
            >
              {isLoadingRoute ? 'Đang tải route...' : 'Tải route ẩn danh'}
            </button>
          </div>
        </div>

        <div className='h-115'>
          <MapContainer center={mapCenter} zoom={15} className='h-full w-full'>
            <TileLayer
              attribution='&copy; OpenStreetMap contributors'
              url='https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png'
            />

            {pois.map((poi) => (
              <Marker key={poi.id} position={[poi.latitude, poi.longitude]}>
                <Popup>
                  <div className='space-y-1 text-sm'>
                    <div className='font-semibold'>{poi.name}</div>
                    <div>Mã: {poi.code}</div>
                    <div>Ưu tiên: {poi.priority ?? 0}</div>
                    <div>Radius: {poi.triggerRadiusMeters}m</div>
                    {poi.mapLink && (
                      <a href={poi.mapLink} target='_blank' rel='noreferrer'>
                        Mở map link
                      </a>
                    )}
                  </div>
                </Popup>
              </Marker>
            ))}

            {heatmapPoints.map((cell, index) => (
              <Circle
                key={`${cell.latitude}-${cell.longitude}-${index}`}
                center={[cell.latitude, cell.longitude]}
                radius={40 + cell.pointCount * 8}
                pathOptions={{
                  color: '#f43f5e',
                  fillColor: '#fb7185',
                  fillOpacity: 0.2,
                  weight: 1,
                }}
              />
            ))}

            {routePath.length > 1 && (
              <Polyline
                positions={routePath}
                pathOptions={{ color: '#2563eb', weight: 4, opacity: 0.8 }}
              />
            )}
          </MapContainer>
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

function MetricCard({ icon, title, value, onClick }) {
  return (
    <button
      type='button'
      onClick={onClick}
      className='bg-white rounded-xl border border-pink-100 p-4 shadow-sm text-left hover:border-pink-300 hover:shadow-md transition'
    >
      <div className='flex items-center gap-2 text-pink-600'>{icon}</div>
      <p className='text-sm text-gray-500 mt-2'>{title}</p>
      <p className='text-2xl font-bold text-gray-800 mt-1'>{value}</p>
    </button>
  );
}
