import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useUser } from '../contexts/UserContext.jsx';
import { apiPost } from '../services/apiClient';

export default function LoginPage() {
  const navigate = useNavigate();
  const { setUser } = useUser();

  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [isLoading, setIsLoading] = useState(false);

  // Password change modal state
  const [showChangePassword, setShowChangePassword] = useState(false);
  const [changePasswordForm, setChangePasswordForm] = useState({
    currentPassword: '',
    newPassword: '',
    confirmPassword: ''
  });
  const [changePasswordError, setChangePasswordError] = useState('');
  const [changePasswordSuccess, setChangePasswordSuccess] = useState('');
  const [isChangingPassword, setIsChangingPassword] = useState(false);

  useEffect(() => {
    const stored = localStorage.getItem('currentUser');
    if (stored) {
      navigate('/', { replace: true });
    }
  }, [navigate]);

  async function handleLogin(event) {
    event.preventDefault();

    if (!username.trim()) {
      setError('Vui lòng nhập tên đăng nhập.');
      return;
    }

    if (!password.trim()) {
      setError('Vui lòng nhập mật khẩu.');
      return;
    }

    setError('');
    setIsLoading(true);

    try {
      const resolved = await apiPost('/users/resolve', {
        username: username.trim(),
        password: password.trim()
      });

      if (!resolved.success) {
        setError(resolved.message || 'Tên đăng nhập hoặc mật khẩu không đúng.');
        setIsLoading(false);
        return;
      }

      setUser({
        id: resolved.id,
        username: resolved.username,
        preferredLanguage: resolved.preferredLanguage,
        role: resolved.role,
      });

      navigate('/', { replace: true });
    } catch (loginError) {
      setError(loginError.message || 'Không thể đăng nhập.');
    } finally {
      setIsLoading(false);
    }
  }

  async function handleChangePassword(event) {
    event.preventDefault();
    setChangePasswordError('');
    setChangePasswordSuccess('');

    if (changePasswordForm.newPassword !== changePasswordForm.confirmPassword) {
      setChangePasswordError('Mật khẩu mới không khớp.');
      return;
    }

    if (changePasswordForm.newPassword.length < 1) {
      setChangePasswordError('Mật khẩu mới không được để trống.');
      return;
    }

    setIsChangingPassword(true);

    try {
      const result = await apiPost('/users/change-password', {
        username: username.trim(),
        currentPassword: changePasswordForm.currentPassword,
        newPassword: changePasswordForm.newPassword
      });

      if (result.success) {
        setChangePasswordSuccess('Đổi mật khẩu thành công!');
        setChangePasswordForm({ currentPassword: '', newPassword: '', confirmPassword: '' });
        setTimeout(() => {
          setShowChangePassword(false);
          setChangePasswordSuccess('');
        }, 1500);
      } else {
        setChangePasswordError(result.message || 'Không thể đổi mật khẩu.');
      }
    } catch (err) {
      setChangePasswordError(err.message || 'Đã xảy ra lỗi khi đổi mật khẩu.');
    } finally {
      setIsChangingPassword(false);
    }
  }

  return (
    <div className='min-h-screen bg-linear-to-br from-pink-50 via-white to-violet-50 flex items-center justify-center px-4'>
      <div className='w-full max-w-md rounded-2xl border border-pink-100 bg-white p-6 shadow-lg'>
        <div className='mb-5'>
          <h1 className='text-2xl font-bold text-gray-800'>
            Đăng nhập CMS
          </h1>
          <p className='text-sm text-gray-500 mt-1'>
            Nhập tên đăng nhập và mật khẩu để bắt đầu
          </p>
        </div>

        {error && (
          <div className='rounded-lg border border-rose-200 bg-rose-50 px-3 py-2 text-sm text-rose-700'>
            {error}
          </div>
        )}

        <form className='space-y-4' onSubmit={handleLogin}>
          <label className='block'>
            <span className='text-sm font-medium text-gray-700'>Tên đăng nhập</span>
            <input
              className='mt-1 w-full rounded-lg border border-pink-100 px-3 py-2 text-sm text-gray-700 focus:outline-none focus:ring-2 focus:ring-pink-400 transition-all'
              placeholder='VD: admin, owner1, owner2...'
              value={username}
              onChange={(event) => setUsername(event.target.value)}
              required
            />
          </label>

          <label className='block'>
            <span className='text-sm font-medium text-gray-700'>Mật khẩu</span>
            <input
              type='password'
              className='mt-1 w-full rounded-lg border border-pink-100 px-3 py-2 text-sm text-gray-700 focus:outline-none focus:ring-2 focus:ring-pink-400 transition-all'
              placeholder='Nhập mật khẩu'
              value={password}
              onChange={(event) => setPassword(event.target.value)}
              required
            />
          </label>

          <button
            type='submit'
            disabled={isLoading}
            className='w-full rounded-lg bg-pink-500 px-4 py-2.5 text-sm font-semibold text-white hover:bg-pink-600 transition-colors disabled:opacity-60'
          >
            {isLoading ? 'Đang đăng nhập...' : 'Đăng nhập'}
          </button>

          <button
            type='button'
            onClick={() => {
              if (!username.trim()) {
                setError('Vui lòng nhập tên đăng nhập trước.');
                return;
              }
              setShowChangePassword(true);
              setChangePasswordError('');
              setChangePasswordSuccess('');
            }}
            className='w-full rounded-lg border border-pink-200 bg-pink-50 px-4 py-2 text-sm font-semibold text-pink-600 hover:bg-pink-100 transition-colors'
          >
            Đổi mật khẩu
          </button>
        </form>

        <div className='pt-2 text-xs text-gray-400 text-center space-y-1'>
          <p>Tài khoản demo (mật khẩu: 1):</p>
          <p>- <strong>admin</strong>: Quản trị viên</p>
          <p>- <strong>owner1</strong>, <strong>owner2</strong>, <strong>owner3</strong>: Shop Manager</p>
        </div>
      </div>

      {/* Change Password Modal */}
      {showChangePassword && (
        <div className='fixed inset-0 bg-black/50 flex items-center justify-center z-50 px-4'>
          <div className='bg-white rounded-2xl p-6 w-full max-w-sm shadow-xl'>
            <h2 className='text-xl font-bold text-gray-800 mb-4'>Đổi mật khẩu</h2>
            <p className='text-sm text-gray-500 mb-4'>Tài khoản: <strong>{username}</strong></p>

            {changePasswordError && (
              <div className='rounded-lg border border-rose-200 bg-rose-50 px-3 py-2 text-sm text-rose-700 mb-3'>
                {changePasswordError}
              </div>
            )}

            {changePasswordSuccess && (
              <div className='rounded-lg border border-emerald-200 bg-emerald-50 px-3 py-2 text-sm text-emerald-700 mb-3'>
                {changePasswordSuccess}
              </div>
            )}

            <form onSubmit={handleChangePassword} className='space-y-4'>
              <label className='block'>
                <span className='text-sm font-medium text-gray-700'>Mật khẩu hiện tại</span>
                <input
                  type='password'
                  className='mt-1 w-full rounded-lg border border-pink-100 px-3 py-2 text-sm text-gray-700 focus:outline-none focus:ring-2 focus:ring-pink-400 transition-all'
                  value={changePasswordForm.currentPassword}
                  onChange={(e) => setChangePasswordForm(f => ({ ...f, currentPassword: e.target.value }))}
                  required
                />
              </label>

              <label className='block'>
                <span className='text-sm font-medium text-gray-700'>Mật khẩu mới</span>
                <input
                  type='password'
                  className='mt-1 w-full rounded-lg border border-pink-100 px-3 py-2 text-sm text-gray-700 focus:outline-none focus:ring-2 focus:ring-pink-400 transition-all'
                  value={changePasswordForm.newPassword}
                  onChange={(e) => setChangePasswordForm(f => ({ ...f, newPassword: e.target.value }))}
                  required
                />
              </label>

              <label className='block'>
                <span className='text-sm font-medium text-gray-700'>Xác nhận mật khẩu mới</span>
                <input
                  type='password'
                  className='mt-1 w-full rounded-lg border border-pink-100 px-3 py-2 text-sm text-gray-700 focus:outline-none focus:ring-2 focus:ring-pink-400 transition-all'
                  value={changePasswordForm.confirmPassword}
                  onChange={(e) => setChangePasswordForm(f => ({ ...f, confirmPassword: e.target.value }))}
                  required
                />
              </label>

              <div className='flex gap-3 pt-2'>
                <button
                  type='button'
                  onClick={() => {
                    setShowChangePassword(false);
                    setChangePasswordForm({ currentPassword: '', newPassword: '', confirmPassword: '' });
                    setChangePasswordError('');
                    setChangePasswordSuccess('');
                  }}
                  className='flex-1 rounded-lg border border-gray-200 px-4 py-2 text-sm font-semibold text-gray-600 hover:bg-gray-50 transition-colors'
                >
                  Hủy
                </button>
                <button
                  type='submit'
                  disabled={isChangingPassword}
                  className='flex-1 rounded-lg bg-pink-500 px-4 py-2 text-sm font-semibold text-white hover:bg-pink-600 transition-colors disabled:opacity-60'
                >
                  {isChangingPassword ? 'Đang đổi...' : 'Đổi mật khẩu'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
