import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { UserProvider } from './contexts/UserContext.jsx';
import { AdminRoute, ProtectedRoute, ShopManagerRoute } from './components/ProtectedRoute.jsx';
import Layout from './components/Layout.jsx';
import LoginPage from './pages/LoginPage.jsx';
import PoiList from './pages/PoiList.jsx';
import DashboardPage from './pages/DashboardPage.jsx';
import AnalyticsPage from './pages/AnalyticsPage.jsx';
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
            <Route
              index
              element={
                <ShopManagerRoute>
                  <DashboardPage />
                </ShopManagerRoute>
              }
            />
            <Route
              path='analytics'
              element={
                <ShopManagerRoute>
                  <AnalyticsPage />
                </ShopManagerRoute>
              }
            />
            <Route
              path='pois'
              element={
                <ShopManagerRoute>
                  <PoiList />
                </ShopManagerRoute>
              }
            />
            <Route
              path='audio'
              element={
                <ShopManagerRoute>
                  <AudioManager />
                </ShopManagerRoute>
              }
            />
            <Route
              path='translations'
              element={
                <ShopManagerRoute>
                  <TranslationManager />
                </ShopManagerRoute>
              }
            />
            <Route
              path='tours'
              element={
                <ShopManagerRoute>
                  <TourManager />
                </ShopManagerRoute>
              }
            />
            <Route
              path='qr-manager'
              element={
                <ShopManagerRoute>
                  <QrManager />
                </ShopManagerRoute>
              }
            />
            <Route
              path='usage-history'
              element={
                <AdminRoute>
                  <UsageHistoryPage />
                </AdminRoute>
              }
            />
            <Route
              path='subscriptions'
              element={
                <AdminRoute>
                  <SubscriptionManager />
                </AdminRoute>
              }
            />
            <Route path='*' element={<Navigate to='/' replace />} />
          </Route>
        </Routes>
      </BrowserRouter>
    </UserProvider>
  );
}

export default App;
