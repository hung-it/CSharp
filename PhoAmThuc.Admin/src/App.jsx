import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import Layout from './components/Layout';
import PoiList from './pages/PoiList';

function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path='/' element={<Layout />}>
          <Route index element={<div className='p-4 bg-white rounded shadow text-xl text-gray-500 flex justify-center items-center h-64'>Dashboard - Thống kê Analytics (Coming soon)</div>} />
          <Route path='pois' element={<PoiList />} />
          <Route path='audio' element={<div className='p-4 bg-white rounded shadow text-gray-500'>Quản lý Audio/Dịch (Coming soon)</div>} />
          <Route path='qr' element={<div className='p-4 bg-white rounded shadow text-gray-500'>Tạo QR Code (Coming soon)</div>} />
          <Route path='settings' element={<div className='p-4 bg-white rounded shadow text-gray-500'>Cài đặt (Coming soon)</div>} />
          <Route path='*' element={<Navigate to='/' replace />} />
        </Route>
      </Routes>
    </BrowserRouter>
  );
}

export default App;
