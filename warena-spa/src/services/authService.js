import { api } from './api'

const USER_KEY = 'warena_user'
const TOKEN_KEY = 'warena_token'

export async function login(email, password) {
  const result = await api.post('/api/auth/login', { email, password })
  saveSession(result)
  return result
}

export async function register(username, email, password) {
  const result = await api.post('/api/auth/register', { username, email, password })
  saveSession(result)
  return result
}

export async function updateUsername(username) {
  const result = await api.put('/api/auth/username', { username })
  saveSession(result)
  return result
}

export async function changePassword(currentPassword, newPassword) {
  const result = await api.put('/api/auth/password', { currentPassword, newPassword })
  saveSession(result)
  return result
}

export function saveSession(result) {
  localStorage.setItem(TOKEN_KEY, result.token)
  localStorage.setItem(USER_KEY, JSON.stringify({
    userId: result.userId,
    username: result.username ?? result.displayName,
    email: result.email,
    role: result.role,
    totalXp: result.totalXp,
    currentStreak: result.currentStreak
  }))
}

export function getSavedUser() {
  try {
    const saved = JSON.parse(localStorage.getItem(USER_KEY) || 'null')
    if (saved && !saved.username && saved.displayName) saved.username = saved.displayName
    return saved
  } catch { return null }
}

export function logout() {
  localStorage.removeItem(TOKEN_KEY)
  localStorage.removeItem(USER_KEY)
}
