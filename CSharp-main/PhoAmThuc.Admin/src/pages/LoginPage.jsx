import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useUser } from '../contexts/UserContext.jsx';
import { setCurrentUser as setApiCurrentUser } from '../services/apiClient.js';

export default function LoginPage() {
  const [externalRef, setExternalRef] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const { setUser } = useUser();
  const navigate = useNavigate();

  const handleSubmit = async (e) => {
    e.preventDefault();
    setLoading(true);
    setError('');

    try {
      const response = await fetch('http://localhost:5140/api/v1/users/resolve', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ 
          externalRef: externalRef.trim(),
          preferredLanguage: 'vi'
        })
      });

      if (!response.ok) {
        throw new Error('Đăng nhập thất bại');
      }

      const user = await response.json();
      
      // Fetch full user details với role
      const userDetailsResponse = await fetch(`http://localhost:5140/api/v1/users/${user.id}`);
      if (userDetailsResponse.ok) {
        const fullUser = await userDetailsResponse.json();
        setApiCurrentUser(user.id);
        setUser(fullUser);
        navigate('/');
      } else {
        // Fallback: use the resolved user
        setApiCurrentUser(user.id);
        setUser(user);
        navigate('/');
      }
    } catch (err) {
      setError(err.message || 'Có lỗi xảy ra');
    } finally {
      setLoading(false);
    }
  };

  const demoUsers = [
    { ref: 'ADMIN', role: 'Admin', color: 'bg-red-100 text-red-700' },
    { ref: 'SHOP_MANAGER_01', role: 'Shop Manager', color: 'bg-blue-100 text-blue-700' },
    { ref: 'SHOP_MANAGER_02', role: 'Shop Manager', color: 'bg-blue-100 text-blue-700' },
    { ref: 'SHOP_MANAGER_03', role: 'Shop Manager', color: 'bg-blue-100 text-blue-700' },
    { ref: 'USER_DEMO', role: 'End User', color: 'bg-green-100 text-green-700' },
  ];

  return (
    <div className="min-h-screen bg-gradient-to-br from-pink-50 to-purple-50 flex items-center justify-center p-4">
      <div className="bg-white rounded-2xl shadow-2xl max-w-md w-full p-8 border border-pink-100">
        <div className="text-center mb-8">
          <h1 className="text-3xl font-black text-transparent bg-clip-text bg-gradient-to-r from-pink-600 to-purple-500 mb-1">
            Phố Ẩm Thực Vĩnh Khánh
          </h1>
        </div>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label className="block text-sm font-semibold text-gray-700 mb-2">
              Tên đăng nhập
            </label>
            <input
              type="text"
              value={externalRef}
              onChange={(e) => setExternalRef(e.target.value)}
              placeholder="Nhập external ref..."
              className="w-full px-4 py-2.5 border border-pink-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-pink-500 focus:ring-offset-2 transition"
              disabled={loading}
              required
            />
          </div>

          {error && (
            <div className="p-3 bg-red-50 border border-red-200 rounded-lg">
              <p className="text-sm text-red-700">{error}</p>
            </div>
          )}

          <button
            type="submit"
            disabled={loading || !externalRef.trim()}
            className="w-full bg-gradient-to-r from-pink-600 to-purple-600 hover:from-pink-700 hover:to-purple-700 disabled:from-gray-400 disabled:to-gray-400 text-white font-semibold py-2.5 rounded-lg transition shadow-lg hover:shadow-xl"
          >
            {loading ? 'Đang đăng nhập...' : 'Đăng Nhập'}
          </button>
        </form>

        <div className="mt-8 pt-6 border-t border-pink-100">
          <p className="text-sm font-semibold text-gray-700 mb-3">Demo Users:</p>
          <div className="space-y-2">
            {demoUsers.map((user) => (
              <button
                key={user.ref}
                onClick={() => setExternalRef(user.ref)}
                className="w-full p-3 text-left border border-pink-200 rounded-lg hover:bg-pink-50 hover:border-pink-300 transition"
              >
                <div className="flex items-center justify-between">
                  <span className="font-medium text-gray-800">{user.ref}</span>
                  <span className={`text-xs px-2 py-1 rounded-full font-medium ${
                    user.ref === 'ADMIN_USER' 
                      ? 'bg-red-100 text-red-700'
                      : user.ref.includes('SHOP_MANAGER')
                      ? 'bg-purple-100 text-purple-700'
                      : 'bg-pink-100 text-pink-700'
                  }`}>
                    {user.role}
                  </span>
                </div>
              </button>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
}
