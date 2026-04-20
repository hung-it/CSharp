import { useCallback, useEffect, useState } from 'react';
import { apiDelete, apiGet, apiPut } from '../services/apiClient';

export default function TranslationManager() {
  const [contentKey, setContentKey] = useState('');
  const [languageCode, setLanguageCode] = useState('vi');
  const [value, setValue] = useState('');

  const [filterContentKey, setFilterContentKey] = useState('');
  const [filterLanguageCode, setFilterLanguageCode] = useState('');

  const [translations, setTranslations] = useState([]);
  const [error, setError] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [isSaving, setIsSaving] = useState(false);

  const handleLoadTranslations = useCallback(async (contentKeyFilter = '', languageCodeFilter = '') => {
    setIsLoading(true);
    setError('');

    try {
      const query = new URLSearchParams();
      const key = contentKeyFilter.trim();
      const language = languageCodeFilter.trim();
      if (key) {
        query.set('contentKey', key);
      }
      if (language) {
        query.set('languageCode', language);
      }

      const result = await apiGet(`/translations${query.size > 0 ? `?${query.toString()}` : ''}`);
      setTranslations(Array.isArray(result) ? result : []);
    } catch (loadError) {
      setError(loadError.message || 'Không thể tải danh sách bản dịch.');
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    handleLoadTranslations('', '').catch(() => undefined);
  }, [handleLoadTranslations]);

  async function handleUpsert(event) {
    event.preventDefault();
    setError('');
    setIsSaving(true);

    try {
      await apiPut('/translations', {
        contentKey: contentKey.trim(),
        languageCode: languageCode.trim(),
        value: value.trim(),
      });

      setFilterContentKey(contentKey.trim());
      setFilterLanguageCode(languageCode.trim());
      await handleLoadTranslations(contentKey.trim(), languageCode.trim());
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
    if (!confirmed) {
      return;
    }

    setError('');

    try {
      await apiDelete(
        `/translations/${encodeURIComponent(item.contentKey)}/${encodeURIComponent(item.languageCode)}`
      );
      await handleLoadTranslations(filterContentKey, filterLanguageCode);
    } catch (deleteError) {
      setError(deleteError.message || 'Không thể xóa bản dịch.');
    }
  }

  function handleSelect(item) {
    setContentKey(item.contentKey || '');
    setLanguageCode(item.languageCode || 'vi');
    setValue(item.value || '');
  }

  return (
    <div className='space-y-6 max-w-7xl mx-auto'>
      <div className='bg-white p-5 rounded-2xl shadow-sm border border-pink-100'>
        <h1 className='text-2xl font-bold text-transparent bg-clip-text bg-gradient-to-r from-pink-600 to-purple-500'>
          Quản lý bản dịch
        </h1>
        <p className='text-sm text-pink-400/90 mt-1'>
          Quản lý content key theo ngôn ngữ cho audio guide. Bảng bên dưới có thể lọc và chọn nhanh để chỉnh sửa.
        </p>
      </div>

      {error && (
        <div className='rounded-xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700'>
          {error}
        </div>
      )}

      <div className='bg-white rounded-2xl border border-pink-100 p-5 shadow-sm space-y-3'>
        <h2 className='font-semibold text-pink-700'>Bộ lọc danh sách</h2>
        <div className='grid grid-cols-1 md:grid-cols-3 gap-3'>
          <input
            className='w-full rounded-xl border border-pink-100 px-3 py-2'
            placeholder='Lọc theo content key'
            value={filterContentKey}
            onChange={(event) => setFilterContentKey(event.target.value)}
          />
          <input
            className='w-full rounded-xl border border-pink-100 px-3 py-2'
            placeholder='Lọc theo mã ngôn ngữ (ví dụ: vi, en)'
            value={filterLanguageCode}
            onChange={(event) => setFilterLanguageCode(event.target.value)}
          />
          <button
            type='button'
            className='rounded-xl border border-pink-200 text-pink-700 font-semibold px-4 py-2 bg-pink-50/70'
            onClick={() => handleLoadTranslations(filterContentKey, filterLanguageCode)}
            disabled={isLoading}
          >
            {isLoading ? 'Đang tải...' : 'Tải danh sách'}
          </button>
        </div>
      </div>

      <form className='bg-white rounded-2xl border border-pink-100 p-5 shadow-sm space-y-3' onSubmit={handleUpsert}>
        <h2 className='font-semibold text-pink-700'>Thêm hoặc cập nhật bản dịch</h2>
        <input
          className='w-full rounded-xl border border-pink-100 px-3 py-2'
          placeholder='Content key'
          value={contentKey}
          onChange={(event) => setContentKey(event.target.value)}
          required
        />
        <div className='grid grid-cols-1 md:grid-cols-2 gap-3'>
          <input
            className='rounded-xl border border-pink-100 px-3 py-2'
            placeholder='Mã ngôn ngữ'
            value={languageCode}
            onChange={(event) => setLanguageCode(event.target.value)}
            required
          />
        </div>
        <textarea
          className='w-full rounded-xl border border-pink-100 px-3 py-2 min-h-24'
          placeholder='Nội dung bản dịch'
          value={value}
          onChange={(event) => setValue(event.target.value)}
          required
        />
        <button
          type='submit'
          disabled={isSaving}
          className='rounded-xl bg-gradient-to-r from-pink-500 to-rose-500 text-white font-semibold px-4 py-2 disabled:opacity-60'
        >
          {isSaving ? 'Đang lưu...' : 'Lưu bản dịch'}
        </button>
      </form>

      <div className='bg-white rounded-2xl border border-pink-100 p-5 shadow-sm'>
        <h2 className='font-semibold text-pink-700 mb-2'>Danh sách bản dịch</h2>
        <table className='w-full text-sm'>
          <thead>
            <tr className='text-gray-500 border-b border-pink-100'>
              <th className='text-left py-2'>Content key</th>
              <th className='text-left py-2'>Ngôn ngữ</th>
              <th className='text-left py-2'>Nội dung</th>
              <th className='text-right py-2'>Thao tác</th>
            </tr>
          </thead>
          <tbody>
            {translations.map((item) => (
              <tr key={item.id} className='border-b border-pink-50'>
                <td className='py-2 text-gray-700'>{item.contentKey}</td>
                <td className='py-2'>{item.languageCode}</td>
                <td className='py-2 text-gray-700'>{item.value}</td>
                <td className='py-2 text-right'>
                  <button
                    className='mr-2 text-xs rounded-lg border border-pink-200 px-2 py-1 text-pink-700 bg-pink-50'
                    onClick={() => handleSelect(item)}
                  >
                    Chọn sửa
                  </button>
                  <button
                    className='text-xs rounded-lg border border-rose-200 px-2 py-1 text-rose-700 bg-rose-50'
                    onClick={() => handleDelete(item)}
                  >
                    Xóa
                  </button>
                </td>
              </tr>
            ))}
            {translations.length === 0 && (
              <tr>
                <td colSpan={4} className='py-4 text-gray-500'>
                  Chưa có dữ liệu. Hãy dùng bộ lọc rồi bấm Tải danh sách.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}
