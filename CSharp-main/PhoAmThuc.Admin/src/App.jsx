import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { UserProvider } from './contexts/UserContext.jsx';
import { ProtectedRoute } from './components/ProtectedRoute.jsx';
import Layout from './components/Layout.jsx';
import LoginPage from './pages/LoginPage.jsx';
import PoiList from './pages/PoiList.jsx';
import DashboardPage from './pages/DashboardPage.jsx';
import AudioManager from './pages/AudioManager.jsx';
import TranslationManager from './pages/TranslationManager.jsx';
import TourManager from './pages/TourManager.jsx';
import UsageHistoryPage from './pages/UsageHistoryPage.jsx';
import SubscriptionManager from './pages/SubscriptionManager.jsx';
import QrManager from './pages/QrManager.jsx';

function App() {
  return (
    <UserProvider>
      <BrowserRouter>
        <Routes>
          <Route path='/login' element={<LoginPage />} />
          <Route
            path='/'
            element={
              <ProtectedRoute>
                <Layout />
              </ProtectedRoute>
            }
          >
            <Route index element={<DashboardPage />} />
            <Route path='pois' element={<PoiList />} />
            <Route path='audio' element={<AudioManager />} />
            <Route path='translations' element={<TranslationManager />} />
            <Route path='tours' element={<TourManager />} />
            <Route path='usage-history' element={<UsageHistoryPage />} />
            <Route path='subscriptions' element={<SubscriptionManager />} />
            <Route path='qr-manager' element={<QrManager />} />
            <Route path='*' element={<Navigate to='/' replace />} />
          </Route>
        </Routes>
      </BrowserRouter>
    </UserProvider>
  );
}

export default App;
