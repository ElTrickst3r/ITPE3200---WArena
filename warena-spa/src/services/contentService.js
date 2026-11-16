import { api } from './api'

// Builds a query string from the filters that have a value, e.g. "?search=html&difficulty=Easy".
function toQuery(filters) {
  const params = new URLSearchParams()
  Object.entries(filters).forEach(([key, value]) => {
    if (value !== '' && value != null) params.set(key, value)
  })
  const query = params.toString()
  return query ? `?${query}` : ''
}

// API calls for content, grouped by entity.
export const categoryService = {
  getAll: () => api.get('/api/categories'),
  get: id => api.get(`/api/categories/${id}`),
  create: data => api.post('/api/categories', data),
  update: (id, data) => api.put(`/api/categories/${id}`, data),
  remove: id => api.del(`/api/categories/${id}`)
}

export const questionService = {
  getAll: () => api.get('/api/questions'),
  search: filters => api.get(`/api/questions${toQuery(filters)}`),
  // Includes the correct answer, so it is only available to teachers and admins.
  getForEdit: id => api.get(`/api/questions/${id}/manage`),
  create: data => api.post('/api/questions', data),
  update: (id, data) => api.put(`/api/questions/${id}`, data),
  remove: id => api.del(`/api/questions/${id}`)
}

export const challengeSetService = {
  getAll: () => api.get('/api/challenge-sets'),
  search: filters => api.get(`/api/challenge-sets${toQuery(filters)}`),
  get: id => api.get(`/api/challenge-sets/${id}`),
  create: data => api.post('/api/challenge-sets', data),
  update: (id, data) => api.put(`/api/challenge-sets/${id}`, data),
  remove: id => api.del(`/api/challenge-sets/${id}`)
}