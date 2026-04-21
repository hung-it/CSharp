import { useCallback, useEffect, useMemo, useState } from 'react';
import { Search, Plus, MapPin, Edit2, Trash2 } from 'lucide-react';
import { apiDelete, apiGet, apiPatch, apiPost } from '../services/apiClient';

export default function PoiList() {
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
    triggerRadiusMeters: '60'
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

  const loadPois = useCallback(async () => {
    setIsLoading(true);
    setError('');

    try {
      const data = await apiGet('/pois');
      setPoiData(Array.isArray(data) ? data : []);
    } catch (loadError) {
      setError(loadError.message || 'Không thể tải dữ liệu POI từ backend.');
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    loadPois().catch(() => undefined);
  }, [loadPois]);

  useEffect(() => {
    if (!showCreateForm) {
      return;
    }

    setEditingPoiId('');
  }, [showCreateForm]);

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
      await apiPost('/pois', {
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
      });

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
        triggerRadiusMeters: '60'
      });
      await loadPois();
    } catch (saveError) {
      setError(saveError.message || 'Không thể tạo POI mới.');
    } finally {
      setIsSaving(false);
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
      <div className="flex flex-col md:flex-row justify-between items-start md:items-center gap-4 bg-white p-5 rounded-2xl shadow-sm border border-pink-100">
        <div>
          <h1 className="text-2xl font-bold text-transparent bg-clip-text bg-linear-to-r from-pink-600 to-purple-500">Quản lý Điểm POI</h1>
          <p className='text-sm text-pink-400/90 mt-1'>Thiết lập tọa độ, bán kính và nội dung cho các điểm thuyết minh</p>
        </div>
        <button
          type='button'
          className='bg-linear-to-r from-pink-500 to-rose-500 text-white px-5 py-2.5 rounded-xl flex items-center gap-2 shadow-md'
          onClick={() => {
            setShowCreateForm((current) => !current);
            cancelEdit();
          }}
        >
          <Plus size={20} />
          <span className='font-semibold'>{showCreateForm ? 'Đóng form tạo' : 'Thêm POI mới'}</span>
        </button>
      </div>

      {showCreateForm && (
        <form
          onSubmit={handleCreatePoi}
          className='bg-white p-4 rounded-xl shadow-sm border border-pink-100 grid grid-cols-1 md:grid-cols-2 gap-4'
        >
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
          <label className='md:col-span-2 text-sm font-medium text-pink-700'>
            Mô tả
            <textarea
              className='mt-1 w-full rounded-lg border border-pink-100 px-3 py-2 text-sm text-gray-700'
              rows={3}
              value={createForm.description}
              onChange={(event) => setCreateForm((current) => ({ ...current, description: event.target.value }))}
            />
          </label>
          <div className='md:col-span-2 flex justify-end'>
            <button
              type='submit'
              disabled={isSaving}
              className='rounded-lg bg-pink-600 px-4 py-2 text-sm font-semibold text-white disabled:opacity-60'
            >
              {isSaving ? 'Đang lưu...' : 'Lưu POI mới'}
            </button>
          </div>
        </form>
      )}

      <div className="bg-white p-4 rounded-xl shadow-sm border border-pink-100 flex gap-4">
        <div className="relative flex-1 group">
          <Search className="absolute left-4 top-1/2 -translate-y-1/2 text-pink-300 group-focus-within:text-pink-500 transition-colors" size={20} />
          <input
            type="text"
            placeholder='Tìm theo tên, mã POI, quận/phường...'
            className="w-full pl-12 pr-4 py-3 bg-pink-50/50 border border-pink-100 rounded-xl focus:outline-none focus:ring-2 focus:ring-pink-400 focus:bg-white transition-all text-gray-700 placeholder-pink-300"
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
          />
        </div>
        <div className="bg-pink-50/50 border border-pink-100 rounded-xl px-5 py-3 text-pink-700 font-medium">
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
            <tr className="bg-pink-50/80 text-pink-600 text-sm border-b border-pink-100 uppercase tracking-wider">
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
          <tbody className="divide-y divide-pink-50">
            {filteredData.map((poi) => (
              <tr key={poi.id} className='hover:bg-pink-50/60 transition-all duration-200 group'>
                <td className="px-6 py-5">
                  <div className="flex items-center gap-4">
                    <div className="p-2.5 bg-linear-to-br from-pink-100 to-rose-50 text-pink-600 rounded-xl shadow-sm group-hover:scale-110 transition-transform">
                      <MapPin size={20} className="fill-pink-100/50" />
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
                      <label className='text-sm font-medium text-pink-700'>
                        Mô tả
                        <textarea
                          className='mt-1 w-full rounded-lg border border-pink-100 px-3 py-2 text-sm text-gray-700'
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
                          className='rounded-lg border border-gray-200 px-3 py-1.5 text-xs font-semibold text-gray-600'
                        >
                          Hủy
                        </button>
                        <button
                          type='submit'
                          disabled={isSaving}
                          className='rounded-lg bg-pink-600 px-3 py-1.5 text-xs font-semibold text-white disabled:opacity-60'
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
                <td className="px-6 py-5 text-sm font-medium text-pink-500/80">
                  <span className="bg-pink-50 px-2 py-1 rounded-md">{poi.latitude}</span>
                  <span className="mx-1 text-gray-300">,</span>
                  <span className="bg-pink-50 px-2 py-1 rounded-md">{poi.longitude}</span>
                </td>
                <td className="px-6 py-5 text-center text-sm font-semibold text-violet-700">
                  {poi.priority ?? 0}
                </td>
                <td className="px-6 py-5 text-center">
                  <span className='inline-block bg-linear-to-r from-pink-50 to-rose-100 border border-pink-200 text-pink-700 font-bold px-3 py-1.5 rounded-lg text-xs shadow-sm'>
                    {poi.triggerRadiusMeters}m
                  </span>
                </td>
                <td className="px-6 py-5 text-sm font-medium text-gray-600">
                  {poi.district || 'Chưa phân loại'}
                </td>
                <td className="px-6 py-5 text-xs text-gray-600 align-top">
                  <div className='space-y-2'>
                    {poi.imageUrl ? (
                      <a href={poi.imageUrl} target='_blank' rel='noreferrer' className='text-pink-600 hover:underline'>
                        Ảnh
                      </a>
                    ) : (
                      <span className='text-gray-400'>Chưa có ảnh</span>
                    )}
                    <div>
                      {poi.mapLink ? (
                        <a href={poi.mapLink} target='_blank' rel='noreferrer' className='text-emerald-600 hover:underline'>
                          Mở bản đồ
                        </a>
                      ) : (
                        <span className='text-gray-400'>Chưa có map link</span>
                      )}
                    </div>
                  </div>
                </td>
                <td className="px-6 py-5 text-right">
                  <div className="flex justify-end gap-2">
                    <button
                      type='button'
                      className='p-2 text-pink-300 hover:text-pink-600 hover:bg-pink-50 rounded-lg transition-all'
                      title='Chỉnh sửa'
                      onClick={() => startEdit(poi)}
                    >
                      <Edit2 size={18} />
                    </button>
                    <button
                      type='button'
                      className='p-2 text-pink-300 hover:text-rose-600 hover:bg-rose-50 rounded-lg transition-all'
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
                  Không có POI phù hợp bộ lọc.
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
    <label className='text-sm font-medium text-pink-700'>
      {label}
      <input
        type={type}
        value={value}
        onChange={(event) => onChange(event.target.value)}
        className='mt-1 w-full rounded-lg border border-pink-100 px-3 py-2 text-sm text-gray-700'
        {...props}
      />
    </label>
  );
}

function normalizeNullableText(value) {
  const trimmed = value.trim();
  return trimmed.length === 0 ? null : trimmed;
}
