import { api } from './api'

// API calls for user administration. All endpoints require the Admin role.
export const adminService = {
  getUsers: () => api.get('/api/admin/users'),
  updateUser: (id, data) => api.put(`/api/admin/users/${id}`, data),
  deleteUser: id => api.del(`/api/admin/users/${id}`)
}