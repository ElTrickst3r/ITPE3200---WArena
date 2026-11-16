import { api } from './api'

// API calls for the current user's practice answers and progress.
export const progressService = {
  getDashboard: () => api.get('/api/progress/dashboard'),
  getHistory: () => api.get('/api/progress/history'),
  getAchievements: () => api.get('/api/progress/achievements'),
  answerQuestion: (questionId, answer) => api.post(`/api/progress/questions/${questionId}/answer`, { answer })
}