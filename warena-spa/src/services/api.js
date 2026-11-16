const API_BASE = import.meta.env.VITE_API_BASE_URL || ''

// Fired when the API rejects the stored token, so the app can log the user out.
export const UNAUTHORIZED_EVENT = 'warena:unauthorized'

export function getToken() {
  return localStorage.getItem('warena_token')
}

// Extracts a readable message from the different error formats the API can return.
function getErrorMessage(data) {
  if (typeof data === 'string' && data) return data
  if (data?.message) return data.message
  if (Array.isArray(data?.errors)) return data.errors.join(', ')
  if (data?.errors && typeof data.errors === 'object') return Object.values(data.errors).flat().join(', ')
  return data?.detail || data?.title || 'Request failed.'
}

export async function apiRequest(path, options = {}) {
  const token = getToken()
  const headers = { 'Content-Type': 'application/json', ...(options.headers || {}) }
  if (token) headers.Authorization = `Bearer ${token}`

  let response
  try {
    response = await fetch(`${API_BASE}${path}`, { ...options, headers })
  } catch {
    throw new Error('Could not reach the server. Please check that the API is running and try again.')
  }

  if (response.status === 401 && token) {
    window.dispatchEvent(new Event(UNAUTHORIZED_EVENT))
  }
  if (response.status === 204) return null

  const contentType = response.headers.get('content-type') || ''
  const data = contentType.includes('json') ? await response.json() : await response.text()
  if (!response.ok) {
    // A 5xx response without a body usually means the API is down and the dev proxy answered instead.
    if (response.status >= 500 && !data) {
      throw new Error('The server is not responding. Please try again in a moment.')
    }
    throw new Error(getErrorMessage(data))
  }
  return data
}

export const api = {
  get: (path) => apiRequest(path),
  post: (path, body) => apiRequest(path, { method: 'POST', body: JSON.stringify(body) }),
  put: (path, body) => apiRequest(path, { method: 'PUT', body: JSON.stringify(body) }),
  del: (path) => apiRequest(path, { method: 'DELETE' })
}