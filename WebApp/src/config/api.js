const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5140'

const makeHeaders = (extra = {}) => ({
  'Content-Type': 'application/json',
  ...extra,
})

async function apiFetch(path, options = {}) {
  const userId = sessionStorage.getItem('userId')
  const headers = makeHeaders(options.headers || {})
  if (userId) {
    headers['X-User-Id'] = userId
  }

  const res = await fetch(`${API_BASE_URL}${path}`, {
    ...options,
    headers,
  })

  if (!res.ok) {
    const text = await res.text().catch(() => res.statusText)
    throw new Error(`${res.status}: ${text}`)
  }

  return res.json()
}

export const api = {
  get: (path) => apiFetch(path),
  post: (path, body) => apiFetch(path, { method: 'POST', body: JSON.stringify(body) }),
  patch: (path, body) => apiFetch(path, { method: 'PATCH', body: JSON.stringify(body) }),
  delete: (path) => apiFetch(path, { method: 'DELETE' }),
}

export { API_BASE_URL }

export const endpoints = {
  health: '/api/v1/health',

  // Auth
  anonymous: '/api/v1/users/anonymous',
  resolve: '/api/v1/users/resolve',
  register: '/api/v1/users/register',
  // Auth — backend uses /resolve for login (accepts username+password)
  login: '/api/v1/users/resolve',
  logout: null, // not implemented on backend
  userMe: (id) => `/api/v1/users/${id}`,

  // POIs
  pois: '/api/v1/pois',
  poiDetail: (id) => `/api/v1/pois/${id}`,
  poiAudios: (id) => `/api/v1/pois/${id}/audios`,

  // Tours
  tours: '/api/v1/tours',
  tourDetail: (id) => `/api/v1/tours/${id}`,
  tourStops: (id) => `/api/v1/tours/${id}/stops`,

  // Sessions
  sessionStart: '/api/v1/sessions/start',
  sessionEnd: (id) => `/api/v1/sessions/${id}/end`,
  sessionsActive: '/api/v1/sessions/active',

  // QR
  qrStart: '/api/v1/qr/start',

  // Visits
  visitStart: '/api/v1/visits/start',
  visitEnd: (id) => `/api/v1/visits/${id}/end`,
  visitAudio: (id) => `/api/v1/visits/${id}/audio`,

  // Geofence
  geofenceEvaluate: '/api/v1/geofence/evaluate',
  geofenceEvent: '/api/v1/geofence/events',

  // Tour View Tracking
  tourViewStart: (id) => `/api/v1/tours/${id}/view/start`,
  tourViewEnd: (tourId, viewId) => `/api/v1/tours/${tourId}/view/${viewId}/end`,

  // Subscriptions — backend uses GET /subscriptions/users/{userId}/active
  subscription: (userId) => `/api/v1/subscriptions/users/${userId}/active`,
  subscriptionActivate: '/api/v1/subscriptions/activate',
}
