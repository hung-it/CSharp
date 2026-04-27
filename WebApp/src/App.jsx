import { Routes, Route, Navigate } from 'react-router-dom'
import { useAuth } from './context/AuthContext.jsx'
import WebLanding from './pages/WebLanding.jsx'
import WebHome from './pages/WebHome.jsx'
import WebMap from './pages/WebMap.jsx'
import WebPoiDetail from './pages/WebPoiDetail.jsx'
import WebTourList from './pages/WebTourList.jsx'
import WebTourDetail from './pages/WebTourDetail.jsx'
import WebQRHandler from './pages/WebQRHandler.jsx'
import WebLogin from './pages/WebLogin.jsx'
import WebRegister from './pages/WebRegister.jsx'
import WebProfile from './pages/WebProfile.jsx'

function App() {
  const { user, loading } = useAuth()

  if (loading) {
    return (
      <div className="h-screen bg-gradient-to-br from-orange-50 to-amber-50 flex items-center justify-center">
        <div className="w-12 h-12 border-4 border-orange-400 border-t-transparent rounded-full animate-spin" />
      </div>
    )
  }

  return (
    <Routes>
      <Route path="/" element={user ? <Navigate to="/home" replace /> : <WebLanding />} />
      <Route path="/home" element={user ? <WebHome /> : <Navigate to="/" replace />} />
      <Route path="/map" element={<WebMap />} />
      <Route path="/poi/:id" element={<WebPoiDetail />} />
      <Route path="/tours" element={<WebTourList />} />
      <Route path="/tour/:id" element={<WebTourDetail />} />
      <Route path="/qr/:payload" element={<WebQRHandler />} />
      <Route path="/qr" element={<WebQRHandler />} />
      <Route path="/login" element={<WebLogin />} />
      <Route path="/register" element={<WebRegister />} />
      <Route path="/profile" element={<WebProfile />} />
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}

export default App
