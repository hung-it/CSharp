import { useEffect, useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useUser } from '../contexts/UserContext.jsx';
import { apiGet, apiPost, buildQuery } from '../services/apiClient';

const ROLE_OPTIONS = [
  { value: 'ShopManager', label: 'Shop Manager' },
  { value: 'Admin', label: 'Admin' },
];

export default function LoginPage() {
  const navigate = useNavigate();
  const { currentUser, setUser } = useUser();

  const [users, setUsers] = useState([]);
  const [externalRef, setExternalRef] = useState('');
  const [preferredLanguage, setPreferredLanguage] = useState('vi');
  const [role, setRole] = useState('ShopManager');
  const [error, setError] = useState('');
  const [isLoading, setIsLoading] = useState(false);

  useEffect(() => {
    if (currentUser) {
      navigate('/', { replace: true });
    }
  }, [currentUser, navigate]);

  useEffect(() => {
    let active = true;

    async function loadUsers() {
      try {
        const data = await apiGet(`/users${buildQuery({ limit: 100 })}`);
        if (!active) {
          return;
        }

        const safeUsers = Array.isArray(data) ? data : [];
        setUsers(safeUsers);

        if (!externalRef && safeUsers[0]?.externalRef) {
          setExternalRef(safeUsers[0].externalRef);
          setPreferredLanguage(safeUsers[0].preferredLanguage || 'vi');
        }
      } catch {
        // Keep page usable in manual mode even if user listing fails.
      }
    }

    loadUsers();

    return () => {
      active = false;
    };
  }, [externalRef]);

  const selectedUser = useMemo(
    () => users.find((item) => item.externalRef === externalRef),
    [users, externalRef]
  );

  async function handleSeedUsers() {
    setError('');
    setIsLoading(true);

    try {
      await apiPost('/users/demo-seed', {});
      const data = await apiGet(`/users${buildQuery({ limit: 100 })}`);
      const safeUsers = Array.isArray(data) ? data : [];
      setUsers(safeUsers);
      if (safeUsers[0]?.externalRef) {
        setExternalRef(safeUsers[0].externalRef);
        setPreferredLanguage(safeUsers[0].preferredLanguage || 'vi');
      }
    } catch (seedError) {
      setError(seedError.message || 'Không thể tạo user demo.');
    } finally {
      setIsLoading(false);
    }
  }

  async function handleLogin(event) {
    event.preventDefault();

    if (!externalRef.trim()) {
      setError('Vui lòng nhập externalRef.');
      return;
    }

    setError('');
    setIsLoading(true);

    try {
      const resolved = await apiPost('/users/resolve', {
        externalRef: externalRef.trim(),
        preferredLanguage: preferredLanguage || 'vi',
      });

      setUser({
        id: resolved.id,
        externalRef: resolved.externalRef,
        preferredLanguage: resolved.preferredLanguage,
        role,
      });

      navigate('/', { replace: true });
    } catch (loginError) {
      setError(loginError.message || 'Không thể đăng nhập.');
    } finally {
      setIsLoading(false);
    }
  }

  return (
    <div className='min-h-screen bg-linear-to-br from-pink-50 via-white to-violet-50 flex items-center justify-center px-4'>
      <div className='w-full max-w-xl rounded-2xl border border-pink-100 bg-white p-6 shadow-lg space-y-5'>
        <div>
          <h1 className='text-2xl font-bold text-transparent bg-clip-text bg-linear-to-r from-pink-600 to-purple-500'>
            Đăng nhập CMS
          </h1>
          <p className='text-sm text-gray-500 mt-1'>
            Luồng login demo cho môi trường đồ án: resolve user từ backend và gán role ở frontend.
          </p>
        </div>

        {error && (
          <div className='rounded-lg border border-rose-200 bg-rose-50 px-3 py-2 text-sm text-rose-700'>
            {error}
          </div>
        )}

        <form className='space-y-4' onSubmit={handleLogin}>
          <div className='grid grid-cols-1 md:grid-cols-2 gap-3'>
            <label className='text-sm text-gray-700'>
              Chọn user có sẵn
              <select
                className='mt-1 w-full rounded-lg border border-pink-100 px-3 py-2'
                value={externalRef}
                onChange={(event) => {
                  const next = event.target.value;
                  setExternalRef(next);
                  const found = users.find((item) => item.externalRef === next);
                  if (found?.preferredLanguage) {
                    setPreferredLanguage(found.preferredLanguage);
                  }
                }}
              >
                <option value=''>-- Chọn user hoặc nhập tay --</option>
                {users.map((user) => (
                  <option key={user.id} value={user.externalRef}>
                    {user.externalRef}
                  </option>
                ))}
              </select>
            </label>

            <label className='text-sm text-gray-700'>
              Role đăng nhập
              <select
                className='mt-1 w-full rounded-lg border border-pink-100 px-3 py-2'
                value={role}
                onChange={(event) => setRole(event.target.value)}
              >
                {ROLE_OPTIONS.map((option) => (
                  <option key={option.value} value={option.value}>
                    {option.label}
                  </option>
                ))}
              </select>
            </label>
          </div>

          <div className='grid grid-cols-1 md:grid-cols-2 gap-3'>
            <label className='text-sm text-gray-700'>
              ExternalRef
              <input
                className='mt-1 w-full rounded-lg border border-pink-100 px-3 py-2'
                placeholder='VD: DEMO_ADMIN_01'
                value={externalRef}
                onChange={(event) => setExternalRef(event.target.value)}
                required
              />
            </label>

            <label className='text-sm text-gray-700'>
              Ngôn ngữ ưu tiên
              <input
                className='mt-1 w-full rounded-lg border border-pink-100 px-3 py-2'
                placeholder='vi'
                value={preferredLanguage}
                onChange={(event) => setPreferredLanguage(event.target.value)}
              />
            </label>
          </div>

          {selectedUser && (
            <div className='rounded-lg border border-pink-100 bg-pink-50/70 px-3 py-2 text-xs text-pink-700'>
              User đã chọn: {selectedUser.externalRef} | PreferredLanguage: {selectedUser.preferredLanguage}
            </div>
          )}

          <div className='flex flex-wrap gap-2 justify-end'>
            <button
              type='button'
              onClick={handleSeedUsers}
              disabled={isLoading}
              className='rounded-lg border border-pink-200 bg-pink-50 px-4 py-2 text-sm font-semibold text-pink-700 disabled:opacity-60'
            >
              Tạo/Nạp user demo
            </button>
            <button
              type='submit'
              disabled={isLoading}
              className='rounded-lg bg-linear-to-r from-pink-500 to-rose-500 px-4 py-2 text-sm font-semibold text-white disabled:opacity-60'
            >
              {isLoading ? 'Đang đăng nhập...' : 'Đăng nhập'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
