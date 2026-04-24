import { useCallback, useEffect, useMemo, useState } from 'react';
import { Plus, Search, X, Languages } from 'lucide-react';
import { apiDelete, apiGet, apiGetWithUser, apiPut } from '../services/apiClient';
import { useUser } from '../contexts/UserContext.jsx';

export default function TranslationManager() {
  const { currentUser } = useUser();
  const isAdmin = currentUser?.role === 'Admin';
  const isShopManager = currentUser?.role === 'ShopManager';
  const [shopInfo, setShopInfo] = useState(null);
  const [pois, setPois] = useState([]);

  const [showForm, setShowForm] = useState(false);
  const [contentKey, setContentKey] = useState('');
  const [languageCode, setLanguageCode] = useState('vi');
  const [value, setValue] = useState('');
  const [selectedPoiId, setSelectedPoiId] = useState('');
  const [selectedField, setSelectedField] = useState('name');

  const [filterPoiName, setFilterPoiName] = useState('');
  const [filterLanguageCode, setFilterLanguageCode] = useState('');

  const [translations, setTranslations] = useState([]);
  const [error, setError] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [isSaving, setIsSaving] = useState(false);

  // Load POIs for ShopManager from new API
  useEffect(() => {
    async function loadOwnerPois() {
      if (isShopManager && currentUser?.id) {
        try {
          const data = await apiGetWithUser('/users/me/pois', currentUser.id);
          setShopInfo(data);
          if (data.pois) {
            setPois(data.pois);
          }
        } catch {
          setShopInfo({ hasPois: false });
        }
      }
    }
    loadOwnerPois();
  }, [isShopManager, currentUser?.id]);

  // Load all POIs for Admin
  useEffect(() => {
    async function loadAllPois() {
      if (isAdmin) {
        try {
          const poiData = await apiGet('/pois');
          setPois(Array.isArray(poiData) ? poiData : []);
        } catch {
          setPois([]);
        }
      }
    }
    loadAllPois();
  }, [isAdmin]);

  // Build allowed POI IDs for filtering
  const allowedPoiIds = useMemo(() => {
    if (isAdmin) return null;
    return new Set(pois.map(p => p.id));
  }, [isAdmin, pois]);

  // POI map for quick lookup
  const poiMap = useMemo(() => {
    const map = {};
    pois.forEach(p => { map[p.id] = p; });
    return map;
  }, [pois]);

  // Get POI name from content key
  const getPoiNameFromKey = useCallback((contentKey) => {
    if (!contentKey || !contentKey.startsWith('poi.')) return null;
    const parts = contentKey.split('.');
    if (parts.length >= 2) {
      const poiId = parts[1];
      const poi = poiMap[poiId];
      return poi ? poi.name : null;
    }
    return null;
  }, [poiMap]);

  // Filter translations based on POI name search
  const filteredTranslations = useMemo(() => {
    if (!filterPoiName.trim()) return translations;

    const keyword = filterPoiName.trim().toLowerCase();
    return translations.filter(t => {
      const poiName = getPoiNameFromKey(t.contentKey);
      if (poiName) {
        return poiName.toLowerCase().includes(keyword);
      }
      return false;
    });
  }, [translations, filterPoiName, getPoiNameFromKey]);

  const handleLoadTranslations = useCallback(async (languageCodeFilter = '') => {
    setIsLoading(true);
    setError('');

    try {
      const query = new URLSearchParams();
      if (languageCodeFilter.trim()) {
        query.set('languageCode', languageCodeFilter.trim());
      }

      const result = await apiGet(`/translations${query.size > 0 ? `?${query.toString()}` : ''}`);
      let allTranslations = Array.isArray(result) ? result : [];

      // Filter translations for ShopManager
      const allowed = allowedPoiIds;
      if (allowed !== null) {
        allTranslations = allTranslations.filter(t => {
          if (t.contentKey && t.contentKey.startsWith('poi.')) {
            const parts = t.contentKey.split('.');
            if (parts.length >= 2) {
              return allowed.has(parts[1]);
            }
          }
          return true;
        });
      }

      setTranslations(allTranslations);
    } catch (loadError) {
      setError(loadError.message || 'Không thể tải danh sách bản dịch.');
    } finally {
      setIsLoading(false);
    }
  }, [allowedPoiIds]);

  useEffect(() => {
    handleLoadTranslations('').catch(() => undefined);
  }, [handleLoadTranslations]);

  async function handleUpsert(event) {
    event.preventDefault();
    setError('');
    setIsSaving(true);

    try {
      let key = contentKey.trim();

      // Auto-generate content key if POI is selected
      if (selectedPoiId && selectedField) {
        key = `poi.${selectedPoiId}.${selectedField}`;
      }

      if (!key) {
        setError('Vui lòng chọn POI hoặc nhập content key.');
        setIsSaving(false);
        return;
      }

      await apiPut('/translations', {
        contentKey: key,
        languageCode: languageCode.trim(),
        value: value.trim(),
      });

      // Reset form
      setContentKey('');
      setSelectedPoiId('');
      setSelectedField('name');
      setValue('');
      setShowForm(false);

      // Reload translations
      await handleLoadTranslations(languageCode.trim());
    } catch (submitError) {
      setError(submitError.message || 'Không thể lưu bản dịch.');
    } finally {
      setIsSaving(false);
    }
  }

  async function handleDelete(item) {
    const confirmed = window.confirm(
      `Bạn có chắc muốn xóa bản dịch "${item.contentKey}" (${item.languageCode}) không?`
    );
    if (!confirmed) return;

    setError('');

    try {
      await apiDelete(
        `/translations/${encodeURIComponent(item.contentKey)}/${encodeURIComponent(item.languageCode)}`
      );
      await handleLoadTranslations(filterLanguageCode);
    } catch (deleteError) {
      setError(deleteError.message || 'Không thể xóa bản dịch.');
    }
  }

  function handleSelect(item) {
    setContentKey(item.contentKey || '');
    setLanguageCode(item.languageCode || 'vi');
    setValue(item.value || '');
    setSelectedPoiId('');
    setSelectedField('name');
    setShowForm(true);
  }

  function handleSelectPoi(poi) {
    setSelectedPoiId(poi.id);
    setContentKey(`poi.${poi.id}.${selectedField}`);
  }

  // Update content key when field changes
  useEffect(() => {
    if (selectedPoiId && selectedField) {
      setContentKey(`poi.${selectedPoiId}.${selectedField}`);
    }
  }, [selectedPoiId, selectedField]);

  // Update content key when POI selection changes
  function handlePoiChange(e) {
    const poiId = e.target.value;
    setSelectedPoiId(poiId);
    if (poiId && selectedField) {
      setContentKey(`poi.${poiId}.${selectedField}`);
    }
  }

  // Update content key when field changes
  function handleFieldChange(e) {
    const field = e.target.value;
    setSelectedField(field);
    if (selectedPoiId && field) {
      setContentKey(`poi.${selectedPoiId}.${field}`);
    }
  }

  return (
    <div className='space-y-6 max-w-7xl mx-auto'>
      {/* Header */}
      <div className='flex flex-col md:flex-row justify-between items-start md:items-center gap-4 bg-white p-5 rounded-2xl shadow-sm border border-pink-100'>
        <div>
          <h1 className='text-2xl font-bold text-transparent bg-clip-text bg-linear-to-r from-pink-600 to-purple-500 flex items-center gap-2'>
            <Languages size={24} className="text-pink-500" />
            Quản lý bản dịch
          </h1>
          {isShopManager && pois.length > 0 && (
            <p className='text-sm text-pink-400/90 mt-1'>
              Đang quản lý bản dịch cho {pois.length} điểm POI
            </p>
          )}
        </div>
        <button
          type='button'
          className='bg-pink-500 text-white px-5 py-2.5 rounded-xl flex items-center gap-2 shadow-md hover:bg-pink-600 transition-colors'
          onClick={() => {
            setShowForm((current) => !current);
            if (!showForm) {
              // Reset form when opening
              setContentKey('');
              setSelectedPoiId('');
              setSelectedField('name');
              setLanguageCode('vi');
              setValue('');
            }
          }}
        >
          {showForm ? <X size={20} /> : <Plus size={20} />}
          <span className='font-semibold'>{showForm ? 'Đóng form' : 'Thêm bản dịch'}</span>
        </button>
      </div>

      {error && (
        <div className='rounded-xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700'>
          {error}
        </div>
      )}

      {/* Add/Edit Form */}
      {showForm && (
        <form className='bg-white rounded-2xl border border-pink-100 p-5 shadow-sm' onSubmit={handleUpsert}>
          <h2 className='font-semibold text-gray-700 mb-4'>Thêm hoặc cập nhật bản dịch</h2>

          <div className='grid grid-cols-1 md:grid-cols-2 gap-4 mb-4'>
            {/* POI Selection */}
            <label className='text-sm font-medium text-gray-700'>
              Chọn POI
              <select
                className='mt-1 w-full rounded-lg border border-pink-100 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-pink-400'
                value={selectedPoiId}
                onChange={handlePoiChange}
              >
                <option value=''>-- Chọn POI --</option>
                {pois.map((poi) => (
                  <option key={poi.id} value={poi.id}>
                    {poi.name} ({poi.code})
                  </option>
                ))}
              </select>
            </label>

            {/* Field Selection - only name, description is content */}
            <label className='text-sm font-medium text-gray-700'>
              Trường cần dịch
              <select
                className='mt-1 w-full rounded-lg border border-pink-100 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-pink-400'
                value={selectedField}
                onChange={handleFieldChange}
              >
                <option value='name'>Tên (name)</option>
              </select>
            </label>
          </div>

          {/* Content Key (read-only) */}
          {showForm && contentKey && (
            <div className='mb-4'>
              <label className='text-sm font-medium text-gray-700'>
                Content Key
                <input
                  className='mt-1 w-full rounded-lg border border-pink-100 px-3 py-2 text-sm bg-gray-50 text-gray-600'
                  value={contentKey}
                  readOnly
                />
              </label>
            </div>
          )}

          <div className='grid grid-cols-1 md:grid-cols-2 gap-4 mb-4'>
            <label className='text-sm font-medium text-gray-700'>
              Ngôn ngữ
              <select
                className='mt-1 w-full rounded-lg border border-pink-100 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-pink-400'
                value={languageCode}
                onChange={(e) => setLanguageCode(e.target.value)}
              >
                <option value='vi'>Tiếng Việt (vi)</option>
                <option value='en'>English (en)</option>
                <option value='zh'>中文 (zh)</option>
              </select>
            </label>
          </div>

          <div className='mb-4'>
            <label className='text-sm font-medium text-gray-700'>
              Nội dung bản dịch
              <textarea
                className='mt-1 w-full rounded-lg border border-pink-100 px-3 py-2 text-sm min-h-24 focus:outline-none focus:ring-2 focus:ring-pink-400'
                placeholder='Nhập nội dung bản dịch...'
                value={value}
                onChange={(e) => setValue(e.target.value)}
                required
              />
            </label>
          </div>

          <div className='flex justify-end gap-3'>
            <button
              type='button'
              onClick={() => {
                setShowForm(false);
                setContentKey('');
                setSelectedPoiId('');
                setValue('');
              }}
              className='px-4 py-2 text-sm font-medium text-gray-600 border border-gray-200 rounded-lg hover:bg-gray-50'
            >
              Hủy
            </button>
            <button
              type='submit'
              disabled={isSaving}
              className='px-6 py-2 text-sm font-semibold text-white bg-pink-500 rounded-lg hover:bg-pink-600 disabled:opacity-60'
            >
              {isSaving ? 'Đang lưu...' : 'Lưu bản dịch'}
            </button>
          </div>
        </form>
      )}

      {/* Filter */}
      <div className='bg-white rounded-2xl border border-pink-100 p-5 shadow-sm'>
        <h2 className='font-semibold text-pink-700 mb-3 flex items-center gap-2'>
          <Search size={16} />
          Bộ lọc
        </h2>
        <div className='grid grid-cols-1 md:grid-cols-3 gap-3'>
          <div className='relative'>
            <Search size={18} className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400" />
            <input
              className='w-full pl-10 pr-4 py-2 rounded-lg border border-pink-100 text-sm focus:outline-none focus:ring-2 focus:ring-pink-400'
              placeholder='Tìm theo tên POI...'
              value={filterPoiName}
              onChange={(e) => setFilterPoiName(e.target.value)}
            />
          </div>
          <select
            className='rounded-lg border border-pink-100 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-pink-400'
            value={filterLanguageCode}
            onChange={(e) => setFilterLanguageCode(e.target.value)}
          >
            <option value=''>Tất cả ngôn ngữ</option>
            <option value='vi'>Tiếng Việt</option>
            <option value='en'>English</option>
            <option value='zh'>中文</option>
          </select>
          <button
            type='button'
            className='rounded-lg border border-pink-200 text-pink-700 font-medium px-4 py-2 bg-pink-50 hover:bg-pink-100 transition-colors'
            onClick={() => handleLoadTranslations(filterLanguageCode)}
            disabled={isLoading}
          >
            {isLoading ? 'Đang tải...' : 'Tải danh sách'}
          </button>
        </div>
      </div>

      {/* Translations Table */}
      <div className='bg-white rounded-2xl border border-pink-100 p-5 shadow-sm'>
        <h2 className='font-semibold text-gray-700 mb-3'>
          Danh sách bản dịch
          <span className='text-gray-400 font-normal text-sm ml-2'>
            ({filteredTranslations.length} bản dịch)
          </span>
        </h2>
        <div className='overflow-x-auto'>
          <table className='w-full text-sm'>
            <thead>
              <tr className='text-gray-500 border-b border-pink-100 text-xs uppercase'>
                <th className='text-left py-3 px-3'>POI</th>
                <th className='text-left py-3 px-3'>Ngôn ngữ</th>
                <th className='text-left py-3 px-3'>Nội dung</th>
                <th className='text-right py-3 px-3'>Thao tác</th>
              </tr>
            </thead>
            <tbody>
              {filteredTranslations.map((item) => (
                  <tr key={`${item.contentKey}-${item.languageCode}`} className='border-b border-pink-50 hover:bg-pink-50/30'>
                    <td className='py-3 px-3 text-gray-700 font-medium'>
                      {item.poiName || <span className='text-gray-400 italic'>Chưa gán POI</span>}
                    </td>
                    <td className='py-3 px-3'>
                      <span className='inline-block bg-pink-100 text-pink-700 px-2 py-0.5 rounded text-xs font-semibold uppercase'>
                        {item.languageCode}
                      </span>
                    </td>
                    <td className='py-3 px-3 text-gray-700 max-w-md truncate' title={item.value}>
                      {item.value}
                    </td>
                    <td className='py-3 px-3 text-right whitespace-nowrap'>
                      <button
                        className='mr-1 text-xs rounded-lg border border-pink-200 px-2 py-1 text-pink-700 bg-pink-50 hover:bg-pink-100 transition-colors'
                        onClick={() => handleSelect(item)}
                      >
                        Sửa
                      </button>
                      <button
                        className='text-xs rounded-lg border border-rose-200 px-2 py-1 text-rose-700 bg-rose-50 hover:bg-rose-100 transition-colors'
                        onClick={() => handleDelete(item)}
                      >
                        Xóa
                      </button>
                    </td>
                  </tr>
                ))}
              {filteredTranslations.length === 0 && (
                <tr>
                  <td colSpan={4} className='py-8 text-center text-gray-500'>
                    Chưa có bản dịch nào.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}
