import React, { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { ArrowLeft, BookOpen, MapPin, ChevronRight } from 'lucide-react'
import { api, endpoints } from '../config/api.js'

export default function WebTourList() {
  const navigate = useNavigate()
  const [tours, setTours] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)

  useEffect(() => {
    api.get(endpoints.tours)
      .then((data) => {
        const list = Array.isArray(data) ? data : (data.tours || [])
        setTours(list)
        setLoading(false)
      })
      .catch(() => {
        setError('Không thể tải danh sách tour.')
        setLoading(false)
      })
  }, [])

  if (loading) {
    return (
      <div className="h-screen bg-gray-50 flex items-center justify-center">
        <div className="w-16 h-16 border-4 border-orange-400 border-t-transparent rounded-full animate-spin" />
      </div>
    )
  }

  return (
    <div className="min-h-screen bg-gray-50">
      <header className="bg-gradient-to-r from-orange-500 to-amber-500 text-white shadow-lg sticky top-0 z-50">
        <div className="flex items-center gap-3 px-4 py-3">
          <button
            onClick={() => navigate(-1)}
            className="p-2 bg-white/20 rounded-xl hover:bg-white/30 transition"
          >
            <ArrowLeft size={20} />
          </button>
          <div>
            <h1 className="font-bold text-lg">Tours</h1>
            <p className="text-orange-100 text-xs">{tours.length} tuyến đường</p>
          </div>
        </div>
      </header>

      <main className="max-w-2xl mx-auto px-4 py-6">
        {error && (
          <div className="bg-red-50 text-red-600 p-4 rounded-xl mb-4">{error}</div>
        )}
        {tours.length === 0 ? (
          <div className="text-center py-12 text-gray-500">
            <BookOpen size={48} className="mx-auto mb-4 text-gray-300" />
            <p>Chưa có tour nào</p>
          </div>
        ) : (
          <div className="space-y-3">
            {tours.map((tour) => (
              <div
                key={tour.id}
                onClick={() => navigate(`/tour/${tour.id}`)}
                className="bg-white rounded-xl shadow-sm p-4 cursor-pointer hover:shadow-md hover:scale-[1.01] transition"
              >
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-3">
                    <div className="w-12 h-12 bg-orange-100 rounded-xl flex items-center justify-center">
                      <BookOpen className="text-orange-500" size={24} />
                    </div>
                    <div>
                      <h3 className="font-bold text-gray-800">{tour.name}</h3>
                      {tour.code && (
                        <p className="text-xs text-gray-500">#{tour.code}</p>
                      )}
                    </div>
                  </div>
                  <ChevronRight size={20} className="text-gray-400" />
                </div>
                {tour.description && (
                  <p className="mt-3 text-sm text-gray-600 line-clamp-2">{tour.description}</p>
                )}
              </div>
            ))}
          </div>
        )}
      </main>
    </div>
  )
}
