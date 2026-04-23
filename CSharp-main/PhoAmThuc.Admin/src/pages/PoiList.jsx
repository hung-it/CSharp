import { useCallback, useEffect, useMemo, useState } from 'react';
import { Search, Plus, MapPin, Edit2, Trash2, Building2, Users, X } from 'lucide-react';
import { apiDelete, apiGet, apiGetWithUser, apiPatch, apiPost, apiPostWithUser } from '../services/apiClient';
import { useUser } from '../contexts/UserContext.jsx';

export default function PoiList() {
  const { currentUser } = useUser();
  const isAdmin = currentUser?.role === 'Admin';
  const isShopManager = currentUser?.role === 'ShopManager';

  const [ownerInfo, setOwnerInfo] = useState(null);
  const [allOwners, setAllOwners] = useState([]);
  const [searchTerm, setSearchTerm] = useState('');
  const [poiData, setPoiData] = useState([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState('');
  const [isSaving, setIsSaving] = useState(false);

  const [showCreateForm, setShowCreateForm] = useState(false);
  const [createForm, setCreateForm] = useState(() => ({
    code: '',
    name: '',
    latitude: '',
    longitude: '',
    district: '',
    priority: '0',
    imageUrl: '',
    mapLink: '',
    description: '',
    triggerRadiusMeters: '60',
    ownerId: ''
  }));

  const [editingPoiId, setEditingPoiId] = useState('');
  const [editForm, setEditForm] = useState(() => ({
    code: '',
    name: '',
    latitude: '',
    longitude: '',
    district: '',
    priority: '0',
    imageUrl: '',
    mapLink: '',
    description: '',
    triggerRadiusMeters: '60'
  }));

  // Shop manager creation modal
  const [showShopManagerModal, setShowShopManagerModal] = useState(false);
  const [shopManagerForm, setShopManagerForm] = useState({
    username: '',
    password: ''
  });
  const [isCreatingShopManager, setIsCreatingShopManager] = useState(false);

  const loadOwners = useCallback(async () => {
    if (!isAdmin) return;

    try {
      const managersData = await apiGet('/users/shop-managers');
      setAllOwners(Array.isArray(managersData) ? managersData : []);
    } catch (err) {
      console.error('Error loading owners:', err);
    }
  }, [isAdmin]);

  const loadPois = useCallback(async () => {
    setIsLoading(true);
    setError('');

    try {
      let query = '/pois';
      // Shop Manager chỉ thấy POIs của mình
      if (isShopManager && currentUser?.id) {
        query = `/pois?managerId=${currentUser.id}`;
      }

      const data = await apiGet(query);
      setPoiData(Array.isArray(data) ? data : []);
    } catch (loadError) {
      setError(loadError.message || 'Không thể tải dữ liệu POI từ backend.');
    } finally {
      setIsLoading(false);
    }
  }, [isShopManager, currentUser?.id]);

  // Load POIs cho Shop Manager từ API mới
  useEffect(() => {
    async function loadOwnerPois() {
      if (isShopManager && currentUser?.id) {
        try {
          const data = await apiGetWithUser('/users/me/pois', currentUser.id);
          setOwnerInfo(data);
          if (data.pois) {
            setPoiData(data.pois);
          }
        } catch {
          setOwnerInfo({ hasPois: false });
        }
      }
    }
    loadOwnerPois();
  }, [isShopManager, currentUser?.id]);

  useEffect(() => {
    loadOwners();
  }, [loadOwners]);

  useEffect(() => {
    if (isAdmin) {
      loadPois().catch(() => undefined);
    }
  }, [loadPois, isAdmin]);

  useEffect(() => {
    if (!showCreateForm) {
      return;
    }

    setEditingPoiId('');
    // Reset owner selection for Admin when opening create form
    if (isAdmin) {
      setCreateForm((current) => ({ ...current, ownerId: '' }));
    }
  }, [showCreateForm, isAdmin]);

  function startEdit(poi) {
    setShowCreateForm(false);
    setEditingPoiId(poi.id);
    setEditForm({
      code: poi.code || '',
      name: poi.name || '',
      latitude: String(poi.latitude ?? ''),
      longitude: String(poi.longitude ?? ''),
      district: poi.district || '',
      priority: String(poi.priority ?? 0),
      imageUrl: poi.imageUrl || '',
      mapLink: poi.mapLink || '',
      description: poi.description || '',
      triggerRadiusMeters: String(poi.triggerRadiusMeters ?? 60)
    });
  }

  function cancelEdit() {
    setEditingPoiId('');
    setEditForm({
      code: '',
      name: '',
      latitude: '',
      longitude: '',
      district: '',
      priority: '0',
      imageUrl: '',
      mapLink: '',
      description: '',
      triggerRadiusMeters: '60'
    });
  }

  async function handleCreatePoi(event) {
    event.preventDefault();
    setError('');
    setIsSaving(true);

    try {
      const payload = {
        code: createForm.code.trim(),
        name: createForm.name.trim(),
        latitude: Number(createForm.latitude),
        longitude: Number(createForm.longitude),
        district: normalizeNullableText(createForm.district),
        priority: Number(createForm.priority),
        imageUrl: normalizeNullableText(createForm.imageUrl),
        mapLink: normalizeNullableText(createForm.mapLink),
        description: normalizeNullableText(createForm.description),
        triggerRadiusMeters: Number(createForm.triggerRadiusMeters)
      };

      // Admin có thể chọn Owner cho POI mới
      if (isAdmin && createForm.ownerId) {
        payload.managerUserId = createForm.ownerId;
      }

      await apiPost('/pois', payload);

      setShowCreateForm(false);
      setCreateForm({
        code: '',
        name: '',
        latitude: '',
        longitude: '',
        district: '',
        priority: '0',
        imageUrl: '',
        mapLink: '',
        description: '',
        triggerRadiusMeters: '60',
        ownerId: ''
      });
      await loadPois();
      await loadOwners();
    } catch (saveError) {
      setError(saveError.message || 'Không thể tạo POI mới.');
    } finally {
      setIsSaving(false);
    }
  }

  async function handleCreateShopManager(event) {
    event.preventDefault();
    setError('');
    setIsCreatingShopManager(true);

    try {
      const userPayload = {
        username: shopManagerForm.username.trim(),
        preferredLanguage: 'vi',
        password: shopManagerForm.password.trim()
      };
      await apiPostWithUser('/users/create-shop-manager', userPayload, currentUser.id);

      setShowShopManagerModal(false);
      setShopManagerForm({ username: '', password: '' });
      await loadOwners();
    } catch (saveError) {
      setError(saveError.message || 'Không thể tạo Shop Manager.');
    } finally {
      setIsCreatingShopManager(false);
    }
  }

  async function handleUpdatePoi(event) {
    event.preventDefault();
    if (!editingPoiId) {
      return;
    }

    setError('');
    setIsSaving(true);

    try {
      await apiPatch(`/pois/${editingPoiId}`, {
        code: editForm.code.trim(),
        name: editForm.name.trim(),
        latitude: Number(editForm.latitude),
        longitude: Number(editForm.longitude),
        district: normalizeNullableText(editForm.district),
        priority: Number(editForm.priority),
        imageUrl: normalizeNullableText(editForm.imageUrl),
        mapLink: normalizeNullableText(editForm.mapLink),
        description: normalizeNullableText(editForm.description),
        triggerRadiusMeters: Number(editForm.triggerRadiusMeters),
      });
      cancelEdit();
      await loadPois();
    } catch (saveError) {
      setError(saveError.message || 'Không thể cập nhật POI.');
    } finally {
      setIsSaving(false);
    }
  }

  async function handleDeletePoi(poi) {
    const confirmed = window.confirm(`Bạn có chắc muốn xóa POI "${poi.name}" không?`);
    if (!confirmed) {
      return;
    }

    setError('');
    setIsSaving(true);

    try {
      await apiDelete(`/pois/${poi.id}`);
      if (editingPoiId === poi.id) {
        cancelEdit();
      }
      await loadPois();
    } catch (deleteError) {
      setError(deleteError.message || 'Không thể xóa POI.');
    } finally {
      setIsSaving(false);
    }
  }

  const filteredData = useMemo(() => {
    const keyword = searchTerm.trim().toLowerCase();
    if (!keyword) {
      return poiData;
    }

    return poiData.filter((poi) =>
      [poi.name, poi.code, poi.district]
        .filter(Boolean)
        .some((value) => value.toLowerCase().includes(keyword))
    );
  }, [poiData, searchTerm]);

  return (
    <div className='space-y-6 max-w-7xl mx-auto'>
      {/* Shop Manager Creation Modal */}
      {showShopManagerModal && (
        <div className='fixed inset-0 bg-black/50 flex items-center justify-center z-50'>
          <div className='bg-white rounded-2xl p-6 w-full max-w-md shadow-xl'>
            <div className='flex justify-between items-center mb-4'>
              <h2 className='text-xl font-bold text-gray-800'>Tạo Shop Manager mới</h2>
              <button
                type='button'
                onClick={() => setShowShopManagerModal(false)}
                className='p-2 hover:bg-gray-100 rounded-lg'
              >
                <X size={20} />
              </button>
            </div>

            <form onSubmit={handleCreateShopManager} className='space-y-4'>
              {error && (
                <div className='rounded-lg border border-rose-200 bg-rose-50 px-3 py-2 text-sm text-rose-700'>
                  {error}
                </div>
              )}

              <label className='block'>
                <span className='text-sm font-medium text-gray-700'>Tên đăng nhập</span>
                <input
                  className='mt-1 w-full rounded-lg border border-pink-100 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-pink-400'
                  placeholder='VD: owner1'
                  value={shopManagerForm.username}
                  onChange={(event) => setShopManagerForm((current) => ({ ...current, username: event.target.value }))}
                  required
                />
              </label>

              <label className='block'>
                <span className='text-sm font-medium text-gray-700'>Mật khẩu</span>
                <input
                  type='password'
                  className='mt-1 w-full rounded-lg border border-pink-100 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-pink-400'
                  placeholder='VD: 123'
                  value={shopManagerForm.password}
                  onChange={(event) => setShopManagerForm((current) => ({ ...current, password: event.target.value }))}
                  required
                />
              </label>

              <div className='flex gap-3 pt-2'>
                <button
                  type='button'
                  onClick={() => setShowShopManagerModal(false)}
                  className='flex-1 rounded-lg border border-gray-200 px-4 py-2 text-sm font-medium text-gray-600 hover:bg-gray-50'
                >
                  Hủy
                </button>
                <button
                  type='submit'
                  disabled={isCreatingShopManager}
                  className='flex-1 rounded-lg bg-pink-500 px-4 py-2 text-sm font-medium text-white hover:bg-pink-600 disabled:opacity-60'
                >
                  {isCreatingShopManager ? 'Đang tạo...' : 'Tạo Shop Manager'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      <div className="flex flex-col md:flex-row justify-between items-start md:items-center gap-4 bg-white p-5 rounded-2xl shadow-sm border border-pink-100">
        <div>
          <h1 className="text-2xl font-bold text-gray-800">Quản lý Điểm POI</h1>
          {isShopManager && ownerInfo?.hasPois && (
            <div className='mt-2 text-sm text-gray-600'>
              <Building2 size={14} className="inline mr-1" />
              Bạn đang quản lý <strong>{ownerInfo.totalCount}</strong> cửa hàng (POIs)
            </div>
          )}
          {isShopManager && !ownerInfo?.hasPois && (
            <p className='text-sm text-orange-500 mt-1'>Bạn chưa có cửa hàng nào. Liên hệ Admin để được cấp quyền quản lý.</p>
          )}
          {isAdmin && (
            <div className='flex flex-wrap gap-2 mt-2'>
              <button
                type='button'
                onClick={() => setShowShopManagerModal(true)}
                className='flex items-center gap-1.5 text-xs font-medium text-pink-600 hover:text-pink-700 hover:bg-pink-50 px-3 py-1.5 rounded-lg transition-colors'
              >
                <Users size={14} />
                Tạo Shop Manager
              </button>
            </div>
          )}
          <p className='text-sm text-gray-400 mt-1'>Thiết lập tọa độ, bán kính và nội dung cho các điểm thuyết minh</p>
        </div>
        {isAdmin && (
        <button
          type='button'
          className='bg-pink-500 text-white px-5 py-2.5 rounded-xl flex items-center gap-2 shadow-md hover:bg-pink-600 transition-colors'
          onClick={() => {
            setShowCreateForm((current) => !current);
            cancelEdit();
          }}
        >
          <Plus size={20} />
          <span className='font-semibold'>{showCreateForm ? 'Đóng form tạo' : 'Thêm POI mới'}</span>
        </button>
        )}
      </div>

      {showCreateForm && (
        <form
          onSubmit={handleCreatePoi}
          className='bg-white p-4 rounded-xl shadow-sm border border-pink-100 grid grid-cols-1 md:grid-cols-2 gap-4'
        >
          {isAdmin && (
            <label className='md:col-span-2'>
              <span className='text-sm font-medium text-gray-700'>Chủ cửa hàng (Shop Manager)</span>
              <select
                className='mt-1 w-full rounded-lg border border-pink-100 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-pink-400'
                value={createForm.ownerId}
                onChange={(event) => setCreateForm((current) => ({ ...current, ownerId: event.target.value }))}
              >
                <option value=''>-- Chọn chủ cửa hàng --</option>
                {allOwners.map((owner) => (
                  <option key={owner.id} value={owner.id}>
                    {owner.username} ({owner.hasShop ? 'Đã có cửa hàng' : 'Chưa có cửa hàng'})
                  </option>
                ))}
              </select>
            </label>
          )}

          <InputField
            label='Mã POI'
            value={createForm.code}
            onChange={(value) => setCreateForm((current) => ({ ...current, code: value }))}
            required
          />
          <InputField
            label='Tên POI'
            value={createForm.name}
            onChange={(value) => setCreateForm((current) => ({ ...current, name: value }))}
            required
          />
          <InputField
            label='Vĩ độ'
            value={createForm.latitude}
            type='number'
            step='any'
            onChange={(value) => setCreateForm((current) => ({ ...current, latitude: value }))}
            required
          />
          <InputField
            label='Kinh độ'
            value={createForm.longitude}
            type='number'
            step='any'
            onChange={(value) => setCreateForm((current) => ({ ...current, longitude: value }))}
            required
          />
          <InputField
            label='Khu vực'
            value={createForm.district}
            onChange={(value) => setCreateForm((current) => ({ ...current, district: value }))}
          />
          <InputField
            label='Mức ưu tiên'
            value={createForm.priority}
            type='number'
            min='0'
            onChange={(value) => setCreateForm((current) => ({ ...current, priority: value }))}
            required
          />
          <InputField
            label='Bán kính kích hoạt (m)'
            value={createForm.triggerRadiusMeters}
            type='number'
            min='5'
            onChange={(value) => setCreateForm((current) => ({ ...current, triggerRadiusMeters: value }))}
            required
          />
          <InputField
            label='Ảnh minh họa (URL)'
            value={createForm.imageUrl}
            onChange={(value) => setCreateForm((current) => ({ ...current, imageUrl: value }))}
            placeholder='https://...'
          />
          <InputField
            label='Map link'
            value={createForm.mapLink}
            onChange={(value) => setCreateForm((current) => ({ ...current, mapLink: value }))}
            placeholder='https://maps.google.com/?q=...'
          />
          <label className='md:col-span-2'>
            <span className='text-sm font-medium text-gray-700'>Mô tả</span>
            <textarea
              className='mt-1 w-full rounded-lg border border-pink-100 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-pink-400'
              rows={3}
              value={createForm.description}
              onChange={(event) => setCreateForm((current) => ({ ...current, description: event.target.value }))}
            />
          </label>
          <div className='md:col-span-2 flex justify-end'>
            <button
              type='submit'
              disabled={isSaving}
              className='rounded-lg bg-pink-500 px-4 py-2 text-sm font-medium text-white hover:bg-pink-600 disabled:opacity-60'
            >
              {isSaving ? 'Đang lưu...' : 'Lưu POI mới'}
            </button>
          </div>
        </form>
      )}

      <div className="bg-white p-4 rounded-xl shadow-sm border border-pink-100 flex gap-4">
        <div className="relative flex-1">
          <Search className="absolute left-4 top-1/2 -translate-y-1/2 text-gray-400" size={20} />
          <input
            type="text"
            placeholder='Tìm theo tên, mã POI, quận/phường...'
            className="w-full pl-12 pr-4 py-3 bg-gray-50 border border-pink-100 rounded-xl focus:outline-none focus:ring-2 focus:ring-pink-400 text-gray-700"
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
          />
        </div>
        <div className="bg-gray-50 border border-pink-100 rounded-xl px-5 py-3 text-gray-700 font-medium">
          {isLoading ? 'Đang tải...' : `Tổng: ${filteredData.length} POI`}
        </div>
      </div>

      {error && (
        <div className="rounded-xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700">
          {error}
        </div>
      )}

      <div className="bg-white rounded-2xl shadow-sm border border-pink-100 overflow-hidden">
        <table className="w-full text-left border-collapse">
          <thead>
            <tr className="bg-gray-50 text-gray-600 text-sm border-b border-pink-100 uppercase tracking-wider">
              <th className="px-6 py-5 font-bold">Tên địa điểm (POI)</th>
              <th className="px-6 py-5 font-bold">Mã</th>
              <th className="px-6 py-5 font-bold">Tọa độ (Lat/Lng)</th>
              <th className="px-6 py-5 font-bold text-center">Ưu tiên</th>
              <th className="px-6 py-5 font-bold text-center">Bán kính (m)</th>
              <th className="px-6 py-5 font-bold">Khu vực</th>
              <th className="px-6 py-5 font-bold">Media</th>
              <th className="px-6 py-5 font-bold text-right">Thao tác</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-100">
            {filteredData.map((poi) => (
              <tr key={poi.id} className='hover:bg-gray-50 transition-all'>
                <td className="px-6 py-5">
                  <div className="flex items-center gap-4">
                    <div className="p-2.5 bg-pink-100 text-pink-600 rounded-xl">
                      <MapPin size={20} />
                    </div>
                    <span className="font-bold text-gray-700">{poi.name}</span>
                  </div>
                  {editingPoiId === poi.id && (
                    <form onSubmit={handleUpdatePoi} className='mt-3 grid grid-cols-1 gap-2'>
                      <InputField
                        label='Mã POI'
                        value={editForm.code}
                        onChange={(value) => setEditForm((current) => ({ ...current, code: value }))}
                        required
                      />
                      <InputField
                        label='Tên POI'
                        value={editForm.name}
                        onChange={(value) => setEditForm((current) => ({ ...current, name: value }))}
                        required
                      />
                      <div className='grid grid-cols-1 md:grid-cols-2 gap-2'>
                        <InputField
                          label='Vĩ độ'
                          type='number'
                          step='any'
                          value={editForm.latitude}
                          onChange={(value) => setEditForm((current) => ({ ...current, latitude: value }))}
                          required
                        />
                        <InputField
                          label='Kinh độ'
                          type='number'
                          step='any'
                          value={editForm.longitude}
                          onChange={(value) => setEditForm((current) => ({ ...current, longitude: value }))}
                          required
                        />
                      </div>
                      <div className='grid grid-cols-1 md:grid-cols-2 gap-2'>
                        <InputField
                          label='Khu vực'
                          value={editForm.district}
                          onChange={(value) => setEditForm((current) => ({ ...current, district: value }))}
                        />
                        <InputField
                          label='Mức ưu tiên'
                          type='number'
                          min='0'
                          value={editForm.priority}
                          onChange={(value) => setEditForm((current) => ({ ...current, priority: value }))}
                          required
                        />
                      </div>
                      <label className='text-sm font-medium text-gray-700'>
                        Mô tả
                        <textarea
                          className='mt-1 w-full rounded-lg border border-pink-100 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-pink-400'
                          rows={2}
                          value={editForm.description}
                          onChange={(event) => setEditForm((current) => ({ ...current, description: event.target.value }))}
                        />
                      </label>
                      <div className='grid grid-cols-1 md:grid-cols-2 gap-2'>
                        <InputField
                          label='Ảnh minh họa (URL)'
                          value={editForm.imageUrl}
                          onChange={(value) => setEditForm((current) => ({ ...current, imageUrl: value }))}
                        />
                        <InputField
                          label='Map link'
                          value={editForm.mapLink}
                          onChange={(value) => setEditForm((current) => ({ ...current, mapLink: value }))}
                        />
                      </div>
                      <InputField
                        label='Bán kính kích hoạt (m)'
                        value={editForm.triggerRadiusMeters}
                        type='number'
                        min='5'
                        onChange={(value) => setEditForm((current) => ({ ...current, triggerRadiusMeters: value }))}
                        required
                      />
                      <div className='flex justify-end gap-2'>
                        <button
                          type='button'
                          onClick={cancelEdit}
                          className='rounded-lg border border-gray-200 px-3 py-1.5 text-xs font-medium text-gray-600 hover:bg-gray-50'
                        >
                          Hủy
                        </button>
                        <button
                          type='submit'
                          disabled={isSaving}
                          className='rounded-lg bg-pink-500 px-3 py-1.5 text-xs font-medium text-white hover:bg-pink-600 disabled:opacity-60'
                        >
                          {isSaving ? 'Đang lưu...' : 'Lưu chỉnh sửa'}
                        </button>
                      </div>
                    </form>
                  )}
                </td>
                <td className="px-6 py-5 text-sm font-semibold text-pink-600">
                  {poi.code}
                </td>
                <td className="px-6 py-5 text-sm font-medium text-gray-600">
                  <span className="bg-gray-100 px-2 py-1 rounded">{poi.latitude}</span>
                  <span className="mx-1">,</span>
                  <span className="bg-gray-100 px-2 py-1 rounded">{poi.longitude}</span>
                </td>
                <td className="px-6 py-5 text-center text-sm font-semibold text-gray-700">
                  {poi.priority ?? 0}
                </td>
                <td className="px-6 py-5 text-center">
                  <span className='inline-block bg-pink-100 text-pink-700 font-bold px-3 py-1.5 rounded-lg text-xs'>
                    {poi.triggerRadiusMeters}m
                  </span>
                </td>
                <td className="px-6 py-5 text-sm font-medium text-gray-600">
                  {poi.district || 'Chưa phân loại'}
                </td>
                <td className="px-6 py-5 text-xs text-gray-600 align-top">
                  <div className='space-y-2'>
                    {poi.imageUrl ? (
                      <a href={poi.imageUrl} target='_blank' rel='noreferrer' className='text-pink-600 hover:underline block'>
                        Ảnh
                      </a>
                    ) : (
                      <span className='text-gray-400'>Chưa có ảnh</span>
                    )}
                    <div>
                      {poi.mapLink ? (
                        <a href={poi.mapLink} target='_blank' rel='noreferrer' className='text-emerald-600 hover:underline block'>
                          Mở bản đồ
                        </a>
                      ) : (
                        <span className='text-gray-400'>Chưa có map</span>
                      )}
                    </div>
                  </div>
                </td>
                <td className="px-6 py-5 text-right">
                  <div className="flex justify-end gap-2">
                    <button
                      type='button'
                      className='p-2 text-gray-400 hover:text-pink-600 hover:bg-pink-50 rounded-lg transition-all'
                      title='Chỉnh sửa'
                      onClick={() => startEdit(poi)}
                    >
                      <Edit2 size={18} />
                    </button>
                    <button
                      type='button'
                      className='p-2 text-gray-400 hover:text-rose-600 hover:bg-rose-50 rounded-lg transition-all'
                      title='Xóa'
                      onClick={() => handleDeletePoi(poi)}
                    >
                      <Trash2 size={18} />
                    </button>
                  </div>
                </td>
              </tr>
            ))}
            {!isLoading && filteredData.length === 0 && (
              <tr>
                <td colSpan={8} className="px-6 py-8 text-center text-gray-500">
                  Không có POI nào.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}

function InputField({ label, value, onChange, type = 'text', ...props }) {
  return (
    <label className='text-sm font-medium text-gray-700'>
      {label}
      <input
        type={type}
        value={value}
        onChange={(event) => onChange(event.target.value)}
        className='mt-1 w-full rounded-lg border border-pink-100 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-pink-400'
        {...props}
      />
    </label>
  );
}

function normalizeNullableText(value) {
  const trimmed = value.trim();
  return trimmed.length === 0 ? null : trimmed;
}
