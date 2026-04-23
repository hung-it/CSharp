const DEFAULT_API_BASE_URL = 'http://localhost:5140/api/v1';

export const API_BASE_URL =
  (import.meta.env.VITE_API_BASE_URL || DEFAULT_API_BASE_URL).replace(/\/$/, '');

function getUserId() {
  try {
    const stored = localStorage.getItem('currentUser');
    if (stored) {
      const user = JSON.parse(stored);
      return user?.id || null;
    }
  } catch {
    // Ignore errors
  }
  return null;
}

export function buildQuery(params = {}) {
  const search = new URLSearchParams();

  Object.entries(params).forEach(([key, value]) => {
    if (value === undefined || value === null || value === '') {
      return;
    }

    search.set(key, String(value));
  });

  const query = search.toString();
  return query ? `?${query}` : '';
}

async function apiRequest(path, method, options = {}) {
  const userId = options.userId || getUserId();
  const headers = {
    Accept: 'application/json',
    ...options.headers,
  };

  if (userId) {
    headers['X-User-Id'] = userId;
  }

  const response = await fetch(`${API_BASE_URL}${path}`, {
    method,
    headers,
    ...options,
  });

  if (!response.ok) {
    const message = await tryReadErrorMessage(response);
    throw new Error(message || `Request failed: ${response.status}`);
  }

  if (response.status === 204) {
    return null;
  }

  const contentType = response.headers.get('content-type') || '';
  if (!contentType.includes('application/json')) {
    return null;
  }

  return response.json();
}

export function apiGet(path, options = {}) {
  return apiRequest(path, 'GET', options);
}

export function apiGetWithUser(path, userId, options = {}) {
  return apiRequest(path, 'GET', { ...options, userId });
}

export function apiPost(path, body, options = {}) {
  return apiRequest(path, 'POST', {
    ...options,
    headers: {
      'Content-Type': 'application/json',
      ...options.headers,
    },
    body: JSON.stringify(body),
  });
}

export function apiPostWithUser(path, body, userId, options = {}) {
  return apiRequest(path, 'POST', {
    ...options,
    userId,
    headers: {
      'Content-Type': 'application/json',
      ...options.headers,
    },
    body: JSON.stringify(body),
  });
}

export function apiPostForm(path, formData, options = {}) {
  return apiRequest(path, 'POST', {
    ...options,
    body: formData,
  });
}

export function apiPut(path, body, options = {}) {
  return apiRequest(path, 'PUT', {
    ...options,
    headers: {
      'Content-Type': 'application/json',
      ...options.headers,
    },
    body: JSON.stringify(body),
  });
}

export function apiPatch(path, body, options = {}) {
  return apiRequest(path, 'PATCH', {
    ...options,
    headers: {
      'Content-Type': 'application/json',
      ...options.headers,
    },
    body: JSON.stringify(body),
  });
}

export function apiDelete(path, options = {}) {
  return apiRequest(path, 'DELETE', options);
}

async function tryReadErrorMessage(response) {
  try {
    const data = await response.json();
    if (typeof data?.message === 'string') {
      return data.message;
    }
    if (typeof data?.title === 'string') {
      return data.title;
    }
  } catch {
    // Ignore non-JSON error payloads.
  }

  return null;
}
