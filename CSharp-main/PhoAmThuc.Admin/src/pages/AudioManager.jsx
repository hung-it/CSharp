import { useEffect, useMemo, useState } from 'react';
import { apiDelete, apiGet, apiPatch, apiPost, apiPostForm } from '../services/apiClient';

export default function AudioManager() {
  const [pois, setPois] = useState([]);
  const [selectedPoiId, setSelectedPoiId] = useState('');
  const [audios, setAudios] = useState([]);
  const [error, setError] = useState('');
  const [isLoading, setIsLoading] = useState(true);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isUploadingCreate, setIsUploadingCreate] = useState(false);
  const [isUploadingEdit, setIsUploadingEdit] = useState(false);
  const [editingAudioId, setEditingAudioId] = useState('');
  const [form, setForm] = useState({
    languageCode: 'vi',
    filePath: '/audio/sample-vi.mp3',
    durationSeconds: 90,
    isTextToSpeech: false,
  });
  const [editForm, setEditForm] = useState({
    languageCode: 'vi',
    filePath: '',
    durationSeconds: 60,
    isTextToSpeech: false,
  });

  useEffect(() => {
    let active = true;

    async function loadPois() {
      setIsLoading(true);
      setError('');
      try {
        const poiData = await apiGet('/pois');
        if (!active) {
          return;
        }

        const safePois = Array.isArray(poiData) ? poiData : [];
        setPois(safePois);
        if (safePois.length > 0) {
          setSelectedPoiId(safePois[0].id);
        }
      } catch (loadError) {
        if (active) {
          setError(loadError.message || 'Khong the tai danh sach POI.');
        }
      } finally {
        if (active) {
          setIsLoading(false);
        }
      }
    }

    loadPois();

    return () => {
      active = false;
    };
  }, []);

  useEffect(() => {
    let active = true;

    async function loadAudios() {
      if (!selectedPoiId) {
        setAudios([]);
        return;
      }

      try {
        const data = await apiGet(`/pois/${selectedPoiId}/audios`);
        if (active) {
          setAudios(Array.isArray(data) ? data : []);
        }
      } catch (loadError) {
        if (active) {
          setError(loadError.message || 'Khong the tai audio theo POI.');
        }
      }
    }

    loadAudios();

    return () => {
      active = false;
    };
  }, [selectedPoiId]);

  const selectedPoi = useMemo(
    () => pois.find((poi) => poi.id === selectedPoiId),
    [pois, selectedPoiId]
  );

  async function reloadAudios() {
    if (!selectedPoiId) {
      setAudios([]);
      return;
    }

    const refreshed = await apiGet(`/pois/${selectedPoiId}/audios`);
    setAudios(Array.isArray(refreshed) ? refreshed : []);
  }

  async function handleAssignAudio(event) {
    event.preventDefault();
    if (!selectedPoiId) {
      return;
    }

    setIsSubmitting(true);
    setError('');

    try {
      if (!form.filePath.trim()) {
        throw new Error('Vui lòng upload file audio trước khi thêm.');
      }

      await apiPost(`/pois/${selectedPoiId}/audios`, {
        languageCode: form.languageCode.trim(),
        filePath: form.filePath.trim(),
        durationSeconds: Number(form.durationSeconds),
        isTextToSpeech: Boolean(form.isTextToSpeech),
      });

      await reloadAudios();
      setForm((prev) => ({ ...prev, filePath: '', durationSeconds: 60, isTextToSpeech: false }));
    } catch (submitError) {
      setError(submitError.message || 'Thêm audio thất bại.');
    } finally {
      setIsSubmitting(false);
    }
  }

  function startEditAudio(audio) {
    setEditingAudioId(audio.id);
    setEditForm({
      languageCode: audio.languageCode || 'vi',
      filePath: audio.filePath || '',
      durationSeconds: audio.durationSeconds || 60,
      isTextToSpeech: Boolean(audio.isTextToSpeech),
    });
  }

  async function handleUploadCreateAudio(event) {
    const file = event.target.files?.[0];
    if (!file) {
      return;
    }

    setError('');
    setIsUploadingCreate(true);

    try {
      const formData = new FormData();
      formData.append('file', file);
      const uploaded = await apiPostForm('/uploads/audio', formData);
      setForm((prev) => ({ ...prev, filePath: uploaded?.filePath || '' }));
    } catch (uploadError) {
      setError(uploadError.message || 'Upload audio thất bại.');
    } finally {
      setIsUploadingCreate(false);
      event.target.value = '';
    }
  }

  async function handleUploadEditAudio(event) {
    const file = event.target.files?.[0];
    if (!file) {
      return;
    }

    setError('');
    setIsUploadingEdit(true);

    try {
      const formData = new FormData();
      formData.append('file', file);
      const uploaded = await apiPostForm('/uploads/audio', formData);
      setEditForm((prev) => ({ ...prev, filePath: uploaded?.filePath || '' }));
    } catch (uploadError) {
      setError(uploadError.message || 'Upload audio thất bại.');
    } finally {
      setIsUploadingEdit(false);
      event.target.value = '';
    }
  }

  function cancelEditAudio() {
    setEditingAudioId('');
    setEditForm({
      languageCode: 'vi',
      filePath: '',
      durationSeconds: 60,
      isTextToSpeech: false,
    });
  }

  async function handleUpdateAudio(event) {
    event.preventDefault();
    if (!selectedPoiId || !editingAudioId) {
      return;
    }

    setError('');
    setIsSubmitting(true);

    try {
      await apiPatch(`/pois/${selectedPoiId}/audios/${editingAudioId}`, {
        languageCode: editForm.languageCode.trim(),
        filePath: editForm.filePath.trim(),
        durationSeconds: Number(editForm.durationSeconds),
        isTextToSpeech: Boolean(editForm.isTextToSpeech),
      });

      cancelEditAudio();
      await reloadAudios();
    } catch (updateError) {
      setError(updateError.message || 'Cập nhật audio thất bại.');
    } finally {
      setIsSubmitting(false);
    }
  }

  async function handleDeleteAudio(audio) {
    if (!selectedPoiId) {
      return;
    }

    const confirmed = window.confirm(`Bạn có chắc muốn xóa audio ${audio.languageCode} không?`);
    if (!confirmed) {
      return;
    }

    setError('');
    setIsSubmitting(true);

    try {
      await apiDelete(`/pois/${selectedPoiId}/audios/${audio.id}`);
      if (editingAudioId === audio.id) {
        cancelEditAudio();
      }
      await reloadAudios();
    } catch (deleteError) {
      setError(deleteError.message || 'Xóa audio thất bại.');
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <div className='space-y-6 max-w-7xl mx-auto'>
      <div className='bg-white p-5 rounded-2xl shadow-sm border border-pink-100'>
        <h1 className='text-2xl font-bold text-transparent bg-clip-text bg-linear-to-r from-pink-600 to-purple-500'>
          Quản lý Audio
        </h1>
        <p className='text-sm text-pink-400/90 mt-1'>
          Quản lý thư viện audio thuyết minh cho từng điểm POI, đảm bảo đúng ngôn ngữ và nội dung phát.
        </p>
      </div>

      {error && (
        <div className='rounded-xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700'>
          {error}
        </div>
      )}

      <div className='bg-white rounded-2xl border border-pink-100 p-5 shadow-sm space-y-4'>
        <label className='block text-sm text-gray-600'>
          Chọn POI
          <select
            className='mt-1 w-full rounded-xl border border-pink-100 bg-pink-50/60 px-3 py-2'
            value={selectedPoiId}
            onChange={(event) => setSelectedPoiId(event.target.value)}
            disabled={isLoading}
          >
            {pois.map((poi) => (
              <option key={poi.id} value={poi.id}>
                {poi.name} ({poi.code})
              </option>
            ))}
          </select>
        </label>

        <form className='grid grid-cols-1 md:grid-cols-5 gap-3' onSubmit={handleAssignAudio}>
          <input
            className='rounded-xl border border-pink-100 px-3 py-2'
            placeholder='Ngôn ngữ (vi/en)'
            value={form.languageCode}
            onChange={(event) => setForm((prev) => ({ ...prev, languageCode: event.target.value }))}
            required
          />
          <div className='md:col-span-2 rounded-xl border border-pink-100 px-3 py-2 bg-pink-50/40'>
            <div className='text-xs text-gray-500'>File audio đã chọn</div>
            <div className='text-sm text-gray-700 truncate'>
              {form.filePath || 'Chưa upload'}
            </div>
            <label className='mt-2 inline-flex cursor-pointer items-center rounded-lg border border-pink-200 bg-white px-3 py-1.5 text-xs font-medium text-pink-700'>
              {isUploadingCreate ? 'Đang upload...' : 'Upload file audio'}
              <input
                type='file'
                accept='.mp3,.wav,.m4a,.ogg,.aac,audio/*'
                onChange={handleUploadCreateAudio}
                className='hidden'
                disabled={isUploadingCreate}
              />
            </label>
          </div>
          <input
            type='number'
            min='1'
            className='rounded-xl border border-pink-100 px-3 py-2'
            placeholder='Thời lượng (giây)'
            value={form.durationSeconds}
            onChange={(event) => setForm((prev) => ({ ...prev, durationSeconds: event.target.value }))}
            required
          />
          <button
            type='submit'
            className='rounded-xl bg-linear-to-r from-pink-500 to-rose-500 text-white font-semibold px-4 py-2 disabled:opacity-60'
            disabled={isSubmitting || !selectedPoiId}
          >
            {isSubmitting ? 'Đang lưu...' : 'Thêm audio'}
          </button>

          <label className='md:col-span-4 inline-flex items-center gap-2 text-sm text-gray-600'>
            <input
              type='checkbox'
              checked={form.isTextToSpeech}
              onChange={(event) => setForm((prev) => ({ ...prev, isTextToSpeech: event.target.checked }))}
            />
            Là nội dung TTS
          </label>
        </form>
      </div>

      <div className='bg-white rounded-2xl border border-pink-100 p-5 shadow-sm'>
        <h2 className='font-semibold text-pink-700'>
          Audio của POI: {selectedPoi ? selectedPoi.name : 'Chưa chọn'}
        </h2>
        <table className='w-full text-sm mt-3'>
          <thead>
            <tr className='text-gray-500 border-b border-pink-100'>
              <th className='text-left py-2'>Ngôn ngữ</th>
              <th className='text-left py-2'>Đường dẫn</th>
              <th className='text-right py-2'>Thời lượng</th>
              <th className='text-right py-2'>TTS</th>
              <th className='text-right py-2'>Thao tác</th>
            </tr>
          </thead>
          <tbody>
            {audios.map((audio) => (
              <tr key={audio.id} className='border-b border-pink-50'>
                <td className='py-2'>
                  {editingAudioId === audio.id ? (
                    <input
                      className='w-full rounded-lg border border-pink-100 px-2 py-1'
                      value={editForm.languageCode}
                      onChange={(event) =>
                        setEditForm((prev) => ({ ...prev, languageCode: event.target.value }))
                      }
                    />
                  ) : (
                    audio.languageCode
                  )}
                </td>
                <td className='py-2 text-gray-700'>
                  {editingAudioId === audio.id ? (
                    <div className='space-y-1'>
                      <div className='truncate text-xs text-gray-600'>{editForm.filePath || 'Chưa upload'}</div>
                      <label className='inline-flex cursor-pointer items-center rounded-lg border border-pink-200 px-2 py-1 text-[11px] text-pink-700'>
                        {isUploadingEdit ? 'Uploading...' : 'Upload mới'}
                        <input
                          type='file'
                          accept='.mp3,.wav,.m4a,.ogg,.aac,audio/*'
                          onChange={handleUploadEditAudio}
                          className='hidden'
                          disabled={isUploadingEdit}
                        />
                      </label>
                    </div>
                  ) : (
                    audio.filePath
                  )}
                </td>
                <td className='py-2 text-right'>
                  {editingAudioId === audio.id ? (
                    <input
                      type='number'
                      min='1'
                      className='w-24 rounded-lg border border-pink-100 px-2 py-1 text-right'
                      value={editForm.durationSeconds}
                      onChange={(event) =>
                        setEditForm((prev) => ({ ...prev, durationSeconds: event.target.value }))
                      }
                    />
                  ) : (
                    `${audio.durationSeconds}s`
                  )}
                </td>
                <td className='py-2 text-right'>
                  {editingAudioId === audio.id ? (
                    <label className='inline-flex items-center gap-1 text-xs'>
                      <input
                        type='checkbox'
                        checked={editForm.isTextToSpeech}
                        onChange={(event) =>
                          setEditForm((prev) => ({ ...prev, isTextToSpeech: event.target.checked }))
                        }
                      />
                      TTS
                    </label>
                  ) : audio.isTextToSpeech ? (
                    'Có'
                  ) : (
                    'Không'
                  )}
                </td>
                <td className='py-2 text-right'>
                  {editingAudioId === audio.id ? (
                    <div className='flex justify-end gap-2'>
                      <button
                        type='button'
                        className='rounded-lg border border-gray-200 px-2 py-1 text-xs'
                        onClick={cancelEditAudio}
                      >
                        Hủy
                      </button>
                      <button
                        type='button'
                        className='rounded-lg border border-pink-200 px-2 py-1 text-xs text-pink-700'
                        onClick={handleUpdateAudio}
                      >
                        Lưu
                      </button>
                    </div>
                  ) : (
                    <div className='flex justify-end gap-2'>
                      <button
                        type='button'
                        className='rounded-lg border border-pink-200 px-2 py-1 text-xs text-pink-700 bg-pink-50'
                        onClick={() => startEditAudio(audio)}
                      >
                        Sửa
                      </button>
                      <button
                        type='button'
                        className='rounded-lg border border-rose-200 px-2 py-1 text-xs text-rose-700 bg-rose-50'
                        onClick={() => handleDeleteAudio(audio)}
                      >
                        Xóa
                      </button>
                    </div>
                  )}
                </td>
              </tr>
            ))}
            {audios.length === 0 && (
              <tr>
                <td colSpan={5} className='py-4 text-gray-500'>
                  Chưa có audio cho POI này.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}
