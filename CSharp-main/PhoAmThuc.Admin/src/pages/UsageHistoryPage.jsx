import { useCallback, useEffect, useState } from 'react';
import { apiGet, apiPost, buildQuery } from '../services/apiClient';

export default function UsageHistoryPage() {
  const [search, setSearch] = useState('');
  const [users, setUsers] = useState([]);
  const [selectedUserId, setSelectedUserId] = useState('');
  const [sessions, setSessions] = useState([]);
  const [error, setError] = useState('');
  const [isLoadingUsers, setIsLoadingUsers] = useState(false);
  const [isLoadingSessions, setIsLoadingSessions] = useState(false);
  const [cleanupMessage, setCleanupMessage] = useState('');

  const handleSearchUsers = useCallback(async (targetSearch = '') => {
    setIsLoadingUsers(true);
    setError('');

    try {
      const userData = await apiGet(`/users${buildQuery({ search: targetSearch, limit: 20 })}`);
      const safeUsers = (Array.isArray(userData) ? userData : []).filter(
        (u) => !u.role || (u.role !== 'Admin' && u.role !== 'ShopManager')
      );
      setUsers(safeUsers);
      setSelectedUserId(safeUsers[0]?.id || '');
    } catch (loadError) {
      setError(loadError.message || 'Không thể tìm người dùng.');
    } finally {
      setIsLoadingUsers(false);
    }
  }, []);

  const handleLoadSessions = useCallback(async (userId) => {
    if (!userId) {
      return;
    }

    setIsLoadingSessions(true);
    setError('');

    try {
      const sessionData = await apiGet(`/sessions${buildQuery({ userId })}`);
      setSessions(Array.isArray(sessionData) ? sessionData : []);
    } catch (loadError) {
      setError(loadError.message || 'Không thể tải lịch sử sử dụng.');
    } finally {
      setIsLoadingSessions(false);
    }
  }, []);

  const handleCleanupAnonUsers = async () => {
    const confirmed = window.confirm('Xóa tất cả user ảo (không có username)? Thao tác này cũng xóa sessions và subscriptions liên quan.');
    if (!confirmed) return;

    setError('');
    setCleanupMessage('');
    setIsLoadingUsers(true);
    try {
      const result = await apiPost('/admin/cleanup-anon-users', {});
      const msg = `Đã xóa ${result.deletedUsers} user ảo, ${result.deletedSessions} sessions, ${result.deletedSubscriptions} subscriptions.`;
      setCleanupMessage(msg);
      await handleSearchUsers('');
    } catch (cleanupError) {
      setError(cleanupError.message || 'Không thể dọn dẹp user ảo.');
    } finally {
      setIsLoadingUsers(false);
    }
  };

  useEffect(() => {
    handleSearchUsers('').catch(() => undefined);
  }, [handleSearchUsers]);

  useEffect(() => {
    if (!selectedUserId) {
      setSessions([]);
      return;
    }

    handleLoadSessions(selectedUserId).catch(() => undefined);
  }, [selectedUserId, handleLoadSessions]);

  const formatDateTime = (utcValue) => {
    if (!utcValue) return '-';
    const d = new Date(utcValue);
    const offset = 7 * 60;
    const local = new Date(d.getTime() + offset * 60 * 1000);
    const pad = (n) => String(n).padStart(2, '0');
    return `${pad(local.getUTCDate())}/${pad(local.getUTCMonth() + 1)}/${local.getUTCFullYear()} ${pad(local.getUTCHours())}:${pad(local.getUTCMinutes())}`;
  };

  return (
    <div className='space-y-6 max-w-7xl mx-auto'>
      <div className='bg-white p-5 rounded-2xl shadow-sm border border-pink-100'>
        <h1 className='text-2xl font-bold text-transparent bg-clip-text bg-linear-to-r from-pink-600 to-purple-500'>
          Lịch sử sử dụng
        </h1>
        <p className='text-sm text-pink-400/90 mt-1'>
          Tra cứu session nghe theo danh sách người dùng dùng chung với màn Subscription.
        </p>
      </div>

      {error && (
        <div className='rounded-xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700'>
          {error}
        </div>
      )}

      <div className='bg-white rounded-2xl border border-pink-100 p-5 shadow-sm space-y-3'>
        <div className='grid grid-cols-1 md:grid-cols-3 gap-3'>
          <input
            className='rounded-xl border border-pink-100 px-3 py-2'
            placeholder='Nhập tên user để tìm'
            value={search}
            onChange={(event) => setSearch(event.target.value)}
          />
          <button
            className='rounded-xl border border-pink-200 text-pink-700 font-semibold px-4 py-2 bg-pink-50/70'
            onClick={() => handleSearchUsers(search)}
            disabled={isLoadingUsers}
          >
            {isLoadingUsers ? 'Đang tìm...' : 'Tìm user'}
          </button>
          <button
            className='rounded-xl bg-linear-to-r from-pink-500 to-rose-500 text-white font-semibold px-4 py-2'
            onClick={() => handleLoadSessions(selectedUserId)}
            disabled={!selectedUserId || isLoadingSessions}
          >
            {isLoadingSessions ? 'Đang tải...' : 'Tải lịch sử'}
          </button>
        </div>

        <div className='flex items-center justify-between'>
          <select
            className='flex-1 rounded-xl border border-pink-100 bg-pink-50/60 px-3 py-2'
            value={selectedUserId}
            onChange={(event) => setSelectedUserId(event.target.value)}
          >
            <option value=''>Chọn user</option>
            {users.map((user) => (
              <option key={user.id} value={user.id}>
                {user.username || user.externalRef} - Tạo lúc: {formatDateTime(user.createdAtUtc)}
              </option>
            ))}
          </select>
          <button
            className='ml-3 rounded-xl border border-rose-200 text-rose-700 font-semibold px-4 py-2 bg-rose-50/70 text-sm'
            onClick={handleCleanupAnonUsers}
            disabled={isLoadingUsers}
          >
            Dọn dẹp user ảo
          </button>
        </div>

        {cleanupMessage && (
          <div className='rounded-xl border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-700'>
            {cleanupMessage}
          </div>
        )}
      </div>

      <div className='bg-white rounded-2xl border border-pink-100 p-5 shadow-sm'>
        <table className='w-full text-sm'>
          <thead>
            <tr className='text-gray-500 border-b border-pink-100'>
              <th className='text-left py-2'>Thời gian bắt đầu</th>
              <th className='text-left py-2'>User</th>
              <th className='text-left py-2'>POI</th>
              <th className='text-right py-2'>Thời lượng</th>
              <th className='text-right py-2'>Nguồn kích hoạt</th>
            </tr>
          </thead>
          <tbody>
            {sessions.map((session) => (
              <tr key={session.id} className='border-b border-pink-50'>
                <td className='py-2'>{formatDateTime(session.startedAtUtc)}</td>
                <td className='py-2'>{session.userName || session.username || session.userExternalRef || session.userId}</td>
                <td className='py-2'>{session.poiName || session.poiId}</td>
                <td className='py-2 text-right'>{session.durationSeconds || 0}s</td>
                <td className='py-2 text-right'>{session.triggerSource}</td>
              </tr>
            ))}
            {sessions.length === 0 && (
              <tr>
                <td colSpan={5} className='py-4 text-gray-500'>
                  Chưa có dữ liệu session.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}
