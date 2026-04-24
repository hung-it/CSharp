import { useEffect, useMemo, useState } from 'react';
import { apiDelete, apiGet, apiPatch, apiPost } from '../services/apiClient';

export default function TourManager() {
  const [tours, setTours] = useState([]);
  const [pois, setPois] = useState([]);
  const [selectedTourId, setSelectedTourId] = useState('');
  const [stops, setStops] = useState([]);
  const [error, setError] = useState('');
  const [isSaving, setIsSaving] = useState(false);

  const [newTour, setNewTour] = useState({ name: '', description: '' });
  const [newStop, setNewStop] = useState({ poiId: '', sequence: 1, description: '' });

  const [editTour, setEditTour] = useState({ name: '', description: '' });
  const [editingStopId, setEditingStopId] = useState('');
  const [editStop, setEditStop] = useState({ poiId: '', sequence: 1, description: '' });

  useEffect(() => {
    let active = true;

    async function loadInitialData() {
      try {
        const [tourData, poiData] = await Promise.all([apiGet('/tours'), apiGet('/pois')]);
        if (!active) {
          return;
        }

        const safeTours = Array.isArray(tourData) ? tourData : [];
        setTours(safeTours);
        setPois(Array.isArray(poiData) ? poiData : []);

        if (safeTours.length > 0) {
          setSelectedTourId(safeTours[0].id);
        }
      } catch (loadError) {
        if (active) {
          setError(loadError.message || 'Không thể tải dữ liệu tour.');
        }
      }
    }

    loadInitialData();
    return () => {
      active = false;
    };
  }, []);

  useEffect(() => {
    let active = true;

    async function loadStops() {
      if (!selectedTourId) {
        setStops([]);
        return;
      }

      try {
        const data = await apiGet(`/tours/${selectedTourId}/stops`);
        if (active) {
          setStops(Array.isArray(data) ? data : []);
        }
      } catch (loadError) {
        if (active) {
          setError(loadError.message || 'Không thể tải điểm dừng của tour.');
        }
      }
    }

    loadStops();

    return () => {
      active = false;
    };
  }, [selectedTourId]);

  const selectedTour = useMemo(
    () => tours.find((tour) => tour.id === selectedTourId),
    [tours, selectedTourId]
  );

  useEffect(() => {
    setEditTour({
      name: selectedTour?.name || '',
      description: selectedTour?.description || ''
    });
  }, [selectedTourId, selectedTour?.name, selectedTour?.description]);

  async function reloadStops() {
    if (!selectedTourId) {
      setStops([]);
      return;
    }

    const refreshed = await apiGet(`/tours/${selectedTourId}/stops`);
    setStops(Array.isArray(refreshed) ? refreshed : []);
  }

  async function handleCreateTour(event) {
    event.preventDefault();
    setError('');
    setIsSaving(true);

    try {
      const created = await apiPost('/tours', {
        name: newTour.name.trim(),
        description: newTour.description.trim() || null,
      });
      const safeTour = created || null;
      const nextTours = [...tours, safeTour].filter(Boolean);
      setTours(nextTours);
      if (safeTour?.id) {
        setSelectedTourId(safeTour.id);
      }
      setNewTour({ name: '', description: '' });
    } catch (createError) {
      setError(createError.message || 'Tạo tour thất bại.');
    } finally {
      setIsSaving(false);
    }
  }

  async function handleUpdateTour(event) {
    event.preventDefault();
    if (!selectedTourId) {
      return;
    }

    setError('');
    setIsSaving(true);

    try {
      await apiPatch(`/tours/${selectedTourId}`, {
        name: editTour.name.trim(),
        description: editTour.description.trim() || null,
      });

      const refreshedTours = await apiGet('/tours');
      setTours(Array.isArray(refreshedTours) ? refreshedTours : []);
    } catch (updateError) {
      setError(updateError.message || 'Không thể cập nhật tour.');
    } finally {
      setIsSaving(false);
    }
  }

  async function handleDeleteTour() {
    if (!selectedTourId || !selectedTour) {
      return;
    }

    const confirmed = window.confirm(`Bạn có chắc muốn xóa tour "${selectedTour.name}" không?`);
    if (!confirmed) {
      return;
    }

    setError('');
    setIsSaving(true);

    try {
      await apiDelete(`/tours/${selectedTourId}`);
      const refreshedTours = await apiGet('/tours');
      const safeTours = Array.isArray(refreshedTours) ? refreshedTours : [];
      setTours(safeTours);
      setSelectedTourId(safeTours[0]?.id || '');
      setStops([]);
    } catch (deleteError) {
      setError(deleteError.message || 'Không thể xóa tour.');
    } finally {
      setIsSaving(false);
    }
  }

  async function handleAddStop(event) {
    event.preventDefault();
    if (!selectedTourId) {
      return;
    }

    setError('');
    setIsSaving(true);

    try {
      await apiPost(`/tours/${selectedTourId}/stops`, {
        poiId: newStop.poiId,
        sequence: Number(newStop.sequence),
        description: newStop.description.trim() || null,
      });
      await reloadStops();
      setNewStop((prev) => ({ ...prev, description: '' }));
    } catch (addStopError) {
      setError(addStopError.message || 'Thêm điểm dừng thất bại.');
    } finally {
      setIsSaving(false);
    }
  }

  function startEditStop(stop) {
    setEditingStopId(stop.id);
    setEditStop({
      poiId: stop.poiId || '',
      sequence: stop.sequence,
      description: stop.description || ''
    });
  }

  async function handleUpdateStop(event) {
    event.preventDefault();
    if (!selectedTourId || !editingStopId) {
      return;
    }

    setError('');
    setIsSaving(true);

    try {
      await apiPatch(`/tours/${selectedTourId}/stops/${editingStopId}`, {
        poiId: editStop.poiId,
        sequence: Number(editStop.sequence),
        description: editStop.description.trim() || null,
      });
      setEditingStopId('');
      await reloadStops();
    } catch (updateError) {
      setError(updateError.message || 'Không thể cập nhật stop.');
    } finally {
      setIsSaving(false);
    }
  }

  async function handleDeleteStop(stopId) {
    if (!selectedTourId) {
      return;
    }

    const confirmed = window.confirm('Bạn có chắc muốn xóa stop này không?');
    if (!confirmed) {
      return;
    }

    setError('');
    setIsSaving(true);

    try {
      await apiDelete(`/tours/${selectedTourId}/stops/${stopId}`);
      if (editingStopId === stopId) {
        setEditingStopId('');
      }
      await reloadStops();
    } catch (deleteError) {
      setError(deleteError.message || 'Không thể xóa stop.');
    } finally {
      setIsSaving(false);
    }
  }

  return (
    <div className='space-y-6 max-w-7xl mx-auto'>
      <div className='bg-white p-5 rounded-2xl shadow-sm border border-pink-100'>
        <h1 className='text-2xl font-bold text-transparent bg-clip-text bg-gradient-to-r from-pink-600 to-purple-500'>
          Quản lý Tour
        </h1>
        <p className='text-sm text-pink-400/90 mt-1'>
          Tạo, cập nhật, xóa tour và cấu hình danh sách stop theo thứ tự.
        </p>
      </div>

      {error && (
        <div className='rounded-xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700'>
          {error}
        </div>
      )}

      <form className='bg-white rounded-2xl border border-pink-100 p-5 shadow-sm grid grid-cols-1 md:grid-cols-3 gap-3' onSubmit={handleCreateTour}>
        <input
          className='rounded-xl border border-pink-100 px-3 py-2'
          placeholder='Tên tour'
          value={newTour.name}
          onChange={(event) => setNewTour((prev) => ({ ...prev, name: event.target.value }))}
          required
        />
        <input
          className='rounded-xl border border-pink-100 px-3 py-2'
          placeholder='Mô tả'
          value={newTour.description}
          onChange={(event) => setNewTour((prev) => ({ ...prev, description: event.target.value }))}
        />
        <button
          type='submit'
          disabled={isSaving}
          className='rounded-xl bg-gradient-to-r from-pink-500 to-rose-500 text-white font-semibold px-4 py-2 disabled:opacity-60'
        >
          {isSaving ? 'Đang lưu...' : 'Tạo tour'}
        </button>
      </form>

      <div className='bg-white rounded-2xl border border-pink-100 p-5 shadow-sm space-y-4'>
        <label className='block text-sm text-gray-600'>
          Chọn tour
          <select
            className='mt-1 w-full rounded-xl border border-pink-100 bg-pink-50/60 px-3 py-2'
            value={selectedTourId}
            onChange={(event) => setSelectedTourId(event.target.value)}
          >
            {tours.map((tour) => (
              <option key={tour.id} value={tour.id}>
                {tour.name} ({tour.code})
              </option>
            ))}
          </select>
        </label>

        {selectedTour && (
          <form className='grid grid-cols-1 md:grid-cols-4 gap-3' onSubmit={handleUpdateTour}>
            <input
              className='rounded-xl border border-pink-100 px-3 py-2'
              value={selectedTour.code}
              disabled
            />
            <input
              className='rounded-xl border border-pink-100 px-3 py-2'
              value={editTour.name}
              onChange={(event) => setEditTour((prev) => ({ ...prev, name: event.target.value }))}
              required
            />
            <input
              className='rounded-xl border border-pink-100 px-3 py-2'
              value={editTour.description}
              onChange={(event) => setEditTour((prev) => ({ ...prev, description: event.target.value }))}
              placeholder='Mô tả tour'
            />
            <div className='flex gap-2'>
              <button
                type='submit'
                disabled={isSaving}
                className='flex-1 rounded-xl border border-pink-200 text-pink-700 font-semibold px-4 py-2 bg-pink-50/70 disabled:opacity-60'
              >
                Cập nhật tour
              </button>
              <button
                type='button'
                disabled={isSaving}
                onClick={handleDeleteTour}
                className='rounded-xl border border-rose-200 text-rose-700 font-semibold px-4 py-2 bg-rose-50 disabled:opacity-60'
              >
                Xóa
              </button>
            </div>
          </form>
        )}

        <form className='grid grid-cols-1 md:grid-cols-4 gap-3' onSubmit={handleAddStop}>
          <select
            className='rounded-xl border border-pink-100 px-3 py-2'
            value={newStop.poiId}
            onChange={(event) => setNewStop((prev) => ({ ...prev, poiId: event.target.value }))}
            required
          >
            <option value=''>Chọn POI</option>
            {pois.map((poi) => (
              <option key={poi.id} value={poi.id}>
                {poi.name}
              </option>
            ))}
          </select>
          <input
            type='number'
            min='1'
            className='rounded-xl border border-pink-100 px-3 py-2'
            value={newStop.sequence}
            onChange={(event) => setNewStop((prev) => ({ ...prev, sequence: event.target.value }))}
            required
          />
          <input
            className='rounded-xl border border-pink-100 px-3 py-2'
            placeholder='Mô tả stop'
            value={newStop.description}
            onChange={(event) => setNewStop((prev) => ({ ...prev, description: event.target.value }))}
          />
          <button
            type='submit'
            className='rounded-xl border border-pink-200 text-pink-700 font-semibold px-4 py-2 bg-pink-50/70 disabled:opacity-60'
            disabled={!selectedTourId || isSaving}
          >
            Thêm stop
          </button>
        </form>

        <h2 className='font-semibold text-pink-700'>Stop của tour: {selectedTour ? selectedTour.name : 'Chưa chọn'}</h2>
        <table className='w-full text-sm'>
          <thead>
            <tr className='text-gray-500 border-b border-pink-100'>
              <th className='text-left py-2'>Thứ tự</th>
              <th className='text-left py-2'>POI</th>
              <th className='text-left py-2'>Mô tả stop</th>
              <th className='text-right py-2'>Thao tác</th>
            </tr>
          </thead>
          <tbody>
            {stops.map((stop) => (
              <tr key={stop.id} className='border-b border-pink-50'>
                <td className='py-2'>{stop.sequence}</td>
                <td className='py-2'>{stop.poi?.name || stop.poiId}</td>
                <td className='py-2 text-gray-700'>
                  {editingStopId === stop.id ? (
                    <form className='grid grid-cols-1 md:grid-cols-3 gap-2' onSubmit={handleUpdateStop}>
                      <select
                        className='rounded-lg border border-pink-100 px-2 py-1'
                        value={editStop.poiId}
                        onChange={(event) => setEditStop((prev) => ({ ...prev, poiId: event.target.value }))}
                        required
                      >
                        <option value=''>Chọn POI</option>
                        {pois.map((poi) => (
                          <option key={poi.id} value={poi.id}>
                            {poi.name}
                          </option>
                        ))}
                      </select>
                      <input
                        type='number'
                        min='1'
                        className='rounded-lg border border-pink-100 px-2 py-1'
                        value={editStop.sequence}
                        onChange={(event) => setEditStop((prev) => ({ ...prev, sequence: event.target.value }))}
                        required
                      />
                      <input
                        className='rounded-lg border border-pink-100 px-2 py-1'
                        value={editStop.description}
                        onChange={(event) =>
                          setEditStop((prev) => ({ ...prev, description: event.target.value }))
                        }
                        placeholder='Mô tả stop'
                      />
                    </form>
                  ) : (
                    stop.description || '-'
                  )}
                </td>
                <td className='py-2 text-right'>
                  {editingStopId === stop.id ? (
                    <div className='flex justify-end gap-2'>
                      <button
                        type='button'
                        className='rounded-lg border border-gray-200 px-2 py-1 text-xs'
                        onClick={() => setEditingStopId('')}
                      >
                        Hủy
                      </button>
                      <button
                        type='button'
                        className='rounded-lg border border-pink-200 px-2 py-1 text-xs text-pink-700'
                        onClick={(event) => handleUpdateStop(event)}
                      >
                        Lưu
                      </button>
                    </div>
                  ) : (
                    <div className='flex justify-end gap-2'>
                      <button
                        type='button'
                        className='rounded-lg border border-pink-200 px-2 py-1 text-xs text-pink-700 bg-pink-50'
                        onClick={() => startEditStop(stop)}
                      >
                        Sửa
                      </button>
                      <button
                        type='button'
                        className='rounded-lg border border-rose-200 px-2 py-1 text-xs text-rose-700 bg-rose-50'
                        onClick={() => handleDeleteStop(stop.id)}
                      >
                        Xóa
                      </button>
                    </div>
                  )}
                </td>
              </tr>
            ))}
            {stops.length === 0 && (
              <tr>
                <td colSpan={4} className='py-4 text-gray-500'>
                  Chưa có stop.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}
