import { Navigate } from 'react-router-dom';
import { useUser } from '../contexts/UserContext.jsx';

export function ProtectedRoute({ children }) {
  const { currentUser } = useUser();

  if (!currentUser) {
    return <Navigate to="/login" replace />;
  }

  return children;
}

export function AdminRoute({ children }) {
  const { currentUser } = useUser();

  if (!currentUser) {
    return <Navigate to="/login" replace />;
  }

  if (currentUser.role && currentUser.role !== 'Admin') {
    return <Navigate to="/" replace />;
  }

  return children;
}

export function ShopManagerRoute({ children }) {
  const { currentUser } = useUser();

  if (!currentUser) {
    return <Navigate to="/login" replace />;
  }

  if (currentUser.role && currentUser.role !== 'ShopManager' && currentUser.role !== 'Admin') {
    return <Navigate to="/" replace />;
  }

  return children;
}
