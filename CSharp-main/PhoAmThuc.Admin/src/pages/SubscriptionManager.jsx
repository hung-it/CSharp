import { useEffect, useMemo, useState } from 'react';
import { apiDelete, apiGet, apiPatch, apiPost, buildQuery } from '../services/apiClient';

export default function SubscriptionManager() {
  const [username, setUsername] = useState('');
  const [resolvedUser, setResolvedUser] = useState(null);
  const [demoUsers, setDemoUsers] = useState([]);
  const [featureSegments, setFeatureSegments] = useState([]);
  const [segmentCode, setSegmentCode] = useState('');
  const [hasAccessResult, setHasAccessResult] = useState(null);
  const [subscriptions, setSubscriptions] = useState([]);
  const [editingSubscriptionId, setEditingSubscriptionId] = useState('');
  const [createForm, setCreateForm] = useState({
    planTier: '10',
    isActive: true,
  });
  const [editForm, setEditForm] = useState({
    planTier: '10',
    isActive: true,
    expiresAtUtc: '',
  });
  const [error, setError] = useState('');
  const [isLoading, setIsLoading] = useState(false);

  useEffect(() => {
    loadInitialData().catch(() => undefined);
  }, []);

  const visibleSubscriptions = useMemo(() => {
    if (!resolvedUser?.id) {
      return subscriptions;
    }

    return subscriptions.filter((item) => item.userId === resolvedUser.id);
  }, [subscriptions, resolvedUser]);

  async function loadInitialData() {
    setIsLoading(true);
    setError('');
    try {
      const [segmentList, subscriptionList, userList] = await Promise.all([
        apiGet('/feature-segments'),
        apiGet(`/subscriptions${buildQuery({ limit: 50 })}`),
        apiGet(`/users${buildQuery({ limit: 50 })}`),
      ]);

      const safeSegments = Array.isArray(segmentList) ? segmentList : [];
      setFeatureSegments(safeSegments);
      setSegmentCode(safeSegments[0]?.code || '');
      setSubscriptions(Array.isArray(subscriptionList) ? subscriptionList : []);
      const regularUsers = (Array.isArray(userList) ? userList : []).filter(
        (u) => !u.role || (u.role !== 'Admin' && u.role !== 'ShopManager')
      );
      setDemoUsers(regularUsers);
    } catch (loadError) {
      setError(loadError.message || 'Không thể tải dữ liệu khởi tạo.');
    } finally {
      setIsLoading(false);
    }
  }

  async function handleResolveUser(event) {
    event.preventDefault();
    if (!username.trim()) {
      return;
    }
    setError('');
    setIsLoading(true);

    try {
      const response = await apiPost('/users/resolve', {
        username: username.trim(),
        preferredLanguage: 'vi',
      });

      if (!response?.success) {
        setError(response?.message || 'Không thể resolve user.');
        setResolvedUser(null);
        setHasAccessResult(null);
        return;
      }

      setResolvedUser(response);
      setHasAccessResult(null);
    } catch (resolveError) {
      setError(resolveError.message || 'Không thể resolve user.');
      setResolvedUser(null);
    } finally {
      setIsLoading(false);
    }
  }

  function applyAmountByPlan(planTier, target = 'create') {
    if (target === 'create') {
      setCreateForm((prev) => ({ ...prev, planTier }));
      return;
    }

    setEditForm((prev) => ({ ...prev, planTier }));
  }

  function getAmountByPlan(planTier) {
    return planTier === '1' || planTier === 'Basic' ? 1 : 10;
  }

  async function handleCreateSubscription(event) {
    event.preventDefault();
    if (!resolvedUser?.id) {
      return;
    }

    setError('');
    setIsLoading(true);

    try {
      await apiPost('/subscriptions', {
        userId: resolvedUser.id,
        planTier: createForm.planTier,
        amountUsd: getAmountByPlan(createForm.planTier),
        isActive: Boolean(createForm.isActive),
        expiresAtUtc: null,
      });
      await handleRefreshSubscriptions();
    } catch (activateError) {
      setError(activateError.message || 'Tạo subscription thất bại.');
    } finally {
      setIsLoading(false);
    }
  }

  function startEditSubscription(item) {
    setEditingSubscriptionId(item.id);
    setEditForm({
      planTier: item.planTier === 'Basic' ? '1' : '10',
      isActive: Boolean(item.isActive),
      expiresAtUtc: item.expiresAtUtc ? item.expiresAtUtc.slice(0, 16) : '',
    });
  }

  function cancelEditSubscription() {
    setEditingSubscriptionId('');
  }

  async function handleUpdateSubscription(subscriptionId) {
    setError('');
    setIsLoading(true);

    try {
      await apiPatch(`/subscriptions/${subscriptionId}`, {
        planTier: editForm.planTier,
        amountUsd: getAmountByPlan(editForm.planTier),
        isActive: Boolean(editForm.isActive),
        expiresAtUtc: editForm.expiresAtUtc || null,
      });

      cancelEditSubscription();
      await handleRefreshSubscriptions();
    } catch (updateError) {
      setError(updateError.message || 'Cập nhật subscription thất bại.');
    } finally {
      setIsLoading(false);
    }
  }

  async function handleDeleteSubscription(item) {
    const confirmed = window.confirm(`Bạn có chắc muốn xóa subscription ${item.id} không?`);
    if (!confirmed) {
      return;
    }

    setError('');
    setIsLoading(true);

    try {
      await apiDelete(`/subscriptions/${item.id}`);
      if (editingSubscriptionId === item.id) {
        cancelEditSubscription();
      }
      await handleRefreshSubscriptions();
    } catch (deleteError) {
      setError(deleteError.message || 'Xóa subscription thất bại.');
    } finally {
      setIsLoading(false);
    }
  }

  async function handleCheckAccess() {
    if (!resolvedUser?.id) {
      return;
    }

    setError('');
    setIsLoading(true);

    try {
      const result = await apiGet(
        `/subscriptions/users/${resolvedUser.id}/access/${encodeURIComponent(segmentCode.trim())}`
      );
      setHasAccessResult(result?.hasAccess ? 'Có truy cập' : 'Không có truy cập');
    } catch (checkError) {
      setError(checkError.message || 'Kiểm tra truy cập thất bại.');
    } finally {
      setIsLoading(false);
    }
  }

  async function handleRefreshSubscriptions() {
    setError('');
    setIsLoading(true);
    try {
      const list = await apiGet(`/subscriptions${buildQuery({ limit: 50 })}`);
      setSubscriptions(Array.isArray(list) ? list : []);
    } catch (loadError) {
      setError(loadError.message || 'Không thể tải danh sách subscription.');
    } finally {
      setIsLoading(false);
    }
  }

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
          Quản lý gói thuê bao
        </h1>
        <p className='text-sm text-pink-400/90 mt-1'>
          CRUD subscription + kiểm tra quyền segment. Amount mặc định theo gói: Basic = 1 USD, Premium = 10 USD.
        </p>
      </div>

      {error && (
        <div className='rounded-xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700'>
          {error}
        </div>
      )}

      <div className='bg-white rounded-2xl border border-pink-100 p-5 shadow-sm space-y-3'>
        <div className='flex items-center gap-3'>
          <select
            className='flex-1 rounded-xl border border-pink-100 px-3 py-2'
            value={username}
            onChange={(event) => setUsername(event.target.value)}
          >
            <option value=''>Chọn user</option>
            {demoUsers.map((user) => (
              <option key={user.id} value={user.username}>
                {user.username}
              </option>
            ))}
          </select>
          <button
            className='rounded-xl border border-pink-200 text-pink-700 font-semibold px-4 py-2 bg-pink-50/70'
            onClick={async () => {
              const users = await apiGet(`/users${buildQuery({ limit: 50 })}`);
              const safeUsers = (Array.isArray(users) ? users : []).filter(
                (u) => !u.role || (u.role !== 'Admin' && u.role !== 'ShopManager')
              );
              setDemoUsers(safeUsers);
            }}
            disabled={isLoading}
          >
            {isLoading ? 'Đang tải...' : 'Tải danh sách user'}
          </button>
        </div>
      </div>

      <div className='bg-white rounded-2xl border border-pink-100 p-5 shadow-sm grid grid-cols-1 md:grid-cols-3 gap-3'>
        <input
          className='rounded-xl border border-pink-100 px-3 py-2'
          placeholder='Tên đăng nhập'
          value={username}
          onChange={(event) => setUsername(event.target.value)}
        />
        <div className='rounded-xl border border-amber-200 bg-amber-50 px-3 py-2 text-xs text-amber-800'>
          Date trong danh sách thuê bao lấy từ thời điểm hệ thống tạo gói (ActivatedAtUtc).
        </div>
        <button
          type='button'
          disabled={isLoading || !username.trim()}
          className='rounded-xl bg-linear-to-r from-pink-500 to-rose-500 text-white font-semibold px-4 py-2 disabled:opacity-60'
          onClick={async (event) => {
            event.preventDefault();
            await handleResolveUser(event);
          }}
        >
          {isLoading ? 'Đang xử lý...' : 'Tìm hoặc tạo user'}
        </button>
      </div>

      {resolvedUser && (
        <div className='bg-white rounded-2xl border border-pink-100 p-5 shadow-sm space-y-3'>
          <div className='text-sm text-gray-700'>
            Người dùng: <span className='font-semibold'>{resolvedUser.username}</span> ({resolvedUser.id})
          </div>

          <form className='grid grid-cols-1 md:grid-cols-2 gap-3' onSubmit={handleCreateSubscription}>
            <select
              className='rounded-xl border border-pink-100 px-3 py-2'
              value={createForm.planTier}
              onChange={(event) => applyAmountByPlan(event.target.value, 'create')}
            >
              <option value='1'>Basic (1 USD)</option>
              <option value='10'>Premium (10 USD)</option>
            </select>
            <label className='inline-flex items-center gap-2 rounded-xl border border-pink-100 px-3 py-2 text-sm text-gray-700'>
              <input
                type='checkbox'
                checked={createForm.isActive}
                onChange={(event) => setCreateForm((prev) => ({ ...prev, isActive: event.target.checked }))}
              />
              Active
            </label>
            <button
              type='submit'
              disabled={isLoading || !resolvedUser?.id}
              className='rounded-xl border border-pink-200 text-pink-700 font-semibold px-4 py-2 bg-pink-50/70 disabled:opacity-60 md:col-span-2'
            >
              Tạo subscription
            </button>
          </form>

          <div className='grid grid-cols-1 md:grid-cols-3 gap-3'>
            <select
              className='rounded-xl border border-pink-100 px-3 py-2 md:col-span-2'
              value={segmentCode}
              onChange={(event) => setSegmentCode(event.target.value)}
            >
              <option value=''>Chọn segment để kiểm tra</option>
              {featureSegments.map((segment) => (
                <option key={segment.id} value={segment.code}>
                  {segment.code}
                </option>
              ))}
            </select>
            <button
              type='button'
              className='rounded-xl border border-pink-200 text-pink-700 font-semibold px-4 py-2 bg-pink-50/70 disabled:opacity-60'
              onClick={handleCheckAccess}
              disabled={isLoading || !segmentCode || !resolvedUser?.id}
            >
              Kiểm tra quyền truy cập
            </button>
          </div>

          <div className='text-xs text-gray-500'>
            Segment code chỉ dùng để test quyền Premium theo từng tính năng, không phải trường bắt buộc khi tạo subscription.
          </div>

          {hasAccessResult && <div className='text-sm text-gray-700'>Kết quả truy cập: {hasAccessResult}</div>}
        </div>
      )}

      <div className='bg-white rounded-2xl border border-pink-100 p-5 shadow-sm'>
        <div className='flex items-center justify-between mb-3'>
          <h2 className='font-semibold text-pink-700'>Danh sách thuê bao</h2>
          <button
            className='rounded-xl border border-pink-200 text-pink-700 font-semibold px-4 py-2 bg-pink-50/70'
            onClick={handleRefreshSubscriptions}
            disabled={isLoading}
          >
            {isLoading ? 'Đang tải...' : 'Tải danh sách'}
          </button>
        </div>

        <table className='w-full text-sm'>
          <thead>
            <tr className='text-gray-500 border-b border-pink-100'>
              <th className='text-left py-2'>User</th>
              <th className='text-left py-2'>Plan</th>
              <th className='text-right py-2'>Date</th>
              <th className='text-right py-2'>Active</th>
              <th className='text-right py-2'>Thao tác</th>
            </tr>
          </thead>
          <tbody>
            {visibleSubscriptions.map((item) => (
              <tr key={item.id} className='border-b border-pink-50'>
                <td className='py-2'>{item.username || item.userExternalRef || item.userId}</td>
                <td className='py-2'>
                  {editingSubscriptionId === item.id ? (
                    <select
                      className='rounded-lg border border-pink-100 px-2 py-1'
                      value={editForm.planTier}
                      onChange={(event) => applyAmountByPlan(event.target.value, 'edit')}
                    >
                      <option value='1'>Basic</option>
                      <option value='10'>Premium</option>
                    </select>
                  ) : (
                    item.planTier
                  )}
                </td>
                <td className='py-2 text-right'>
                  {formatDateTime(item.activatedAtUtc)}
                </td>
                <td className='py-2 text-right'>
                  {editingSubscriptionId === item.id ? (
                    <label className='inline-flex items-center gap-1 text-xs'>
                      <input
                        type='checkbox'
                        checked={editForm.isActive}
                        onChange={(event) => setEditForm((prev) => ({ ...prev, isActive: event.target.checked }))}
                      />
                      Active
                    </label>
                  ) : item.isActive ? (
                    'Có'
                  ) : (
                    'Không'
                  )}
                </td>
                <td className='py-2 text-right'>
                  {editingSubscriptionId === item.id ? (
                    <div className='flex justify-end gap-2'>
                      <button
                        type='button'
                        className='rounded-lg border border-gray-200 px-2 py-1 text-xs'
                        onClick={cancelEditSubscription}
                      >
                        Hủy
                      </button>
                      <button
                        type='button'
                        className='rounded-lg border border-pink-200 px-2 py-1 text-xs text-pink-700'
                        onClick={() => handleUpdateSubscription(item.id)}
                      >
                        Lưu
                      </button>
                    </div>
                  ) : (
                    <div className='flex justify-end gap-2'>
                      <button
                        type='button'
                        className='rounded-lg border border-pink-200 px-2 py-1 text-xs text-pink-700 bg-pink-50'
                        onClick={() => startEditSubscription(item)}
                      >
                        Sửa
                      </button>
                      <button
                        type='button'
                        className='rounded-lg border border-rose-200 px-2 py-1 text-xs text-rose-700 bg-rose-50'
                        onClick={() => handleDeleteSubscription(item)}
                      >
                        Xóa
                      </button>
                    </div>
                  )}
                </td>
              </tr>
            ))}
            {visibleSubscriptions.length === 0 && (
              <tr>
                <td colSpan={5} className='py-4 text-gray-500'>
                  Chưa có dữ liệu. Bấm Tải danh sách.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}
