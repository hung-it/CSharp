import React, { useEffect, useState } from 'react'
import { useParams, useNavigate, useSearchParams } from 'react-router-dom'
import { ArrowLeft, Loader2 } from 'lucide-react'
import { api, endpoints } from '../config/api.js'
import { useAuth } from '../context/AuthContext.jsx'

export default function WebQRHandler() {
  const { payload } = useParams()
  const [searchParams] = useSearchParams()
  const navigate = useNavigate()
  const { userId } = useAuth()

  const [status, setStatus] = useState('loading') // loading | redirecting | error
  const [message, setMessage] = useState('')
  const [redirectUrl, setRedirectUrl] = useState('')

  useEffect(() => {
    if (!userId) return

    const qrPayload = payload || searchParams.get('p') || searchParams.get('q') || ''
    if (!qrPayload) {
      setStatus('error')
      setMessage('Mã QR không hợp lệ hoặc thiếu dữ liệu.')
      return
    }

    // Track geofence event when QR is scanned
    api.post(endpoints.geofenceEvent, {
      userId,
      poiId: '00000000-0000-0000-0000-000000000000',
      eventType: 'Enter',
      latitude: 0,
      longitude: 0,
      distanceFromCenterMeters: 0,
      anonymousRef: qrPayload,
    }).catch(() => {})

    // Start a visit for the QR scan
    api.post(endpoints.visitStart, {
      userId,
      poiId: '00000000-0000-0000-0000-000000000000',
      triggerSource: 'QRCode',
      pageSource: 'Web',
    }).catch(() => {})

    // Call QR start endpoint to resolve POI
    api.post(endpoints.qrStart, {
      userId,
      qrPayload,
      languageCode: 'vi',
    })
      .then((data) => {
        if (data.content && data.content.poiId) {
          const poiId = data.content.poiId
          setStatus('redirecting')
          setRedirectUrl(`/poi/${poiId}`)
          setMessage(`Tìm thấy: ${data.content.poiName || 'Địa điểm'}`)

          // Start listening session
          if (data.session) {
            // session started
          }

          // Redirect after brief delay
          setTimeout(() => {
            navigate(`/poi/${poiId}`)
          }, 1500)
        } else {
          setStatus('error')
          setMessage('Không tìm thấy nội dung cho mã QR này.')
        }
      })
      .catch((err) => {
        setStatus('error')
        setMessage('Mã QR không hợp lệ hoặc đã hết hạn.')
      })
  }, [userId, payload, searchParams])

  return (
    <div className="min-h-screen bg-gradient-to-br from-orange-50 to-amber-50 flex items-center justify-center p-4">
      <div className="bg-white rounded-2xl shadow-2xl p-8 max-w-sm w-full text-center">
        {status === 'loading' && (
          <>
            <div className="w-20 h-20 border-4 border-orange-400 border-t-transparent rounded-full animate-spin mx-auto mb-6" />
            <h2 className="text-xl font-bold text-gray-800 mb-2">Đang xử lý mã QR...</h2>
            <p className="text-gray-500 text-sm">Vui lòng chờ trong giây lát</p>
          </>
        )}

        {status === 'redirecting' && (
          <>
            <div className="w-20 h-20 bg-green-100 rounded-full flex items-center justify-center mx-auto mb-6">
              <span className="text-4xl">✓</span>
            </div>
            <h2 className="text-xl font-bold text-green-700 mb-2">Đã xác nhận!</h2>
            <p className="text-gray-600 text-sm mb-4">{message}</p>
            <p className="text-orange-500 font-medium animate-pulse">Đang chuyển hướng...</p>
          </>
        )}

        {status === 'error' && (
          <>
            <div className="w-20 h-20 bg-red-100 rounded-full flex items-center justify-center mx-auto mb-6">
              <span className="text-4xl text-red-500">✕</span>
            </div>
            <h2 className="text-xl font-bold text-red-700 mb-2">Không tìm thấy</h2>
            <p className="text-gray-600 text-sm mb-6">{message}</p>
            <button
              onClick={() => navigate('/')}
              className="px-6 py-3 bg-orange-500 text-white rounded-xl font-bold hover:bg-orange-600 transition"
            >
              Quay về trang chủ
            </button>
          </>
        )}
      </div>
    </div>
  )
}
