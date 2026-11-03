import { createContext, useContext, useMemo, useState } from 'react'
import * as authService from '../services/authService'

const AuthContext = createContext(null)

export function AuthProvider({ children }) {
  const [user, setUser] = useState(() => authService.getSavedUser())

  const value = useMemo(() => ({
    user,
    async login(email, password) { const result = await authService.login(email, password); setUser(authService.getSavedUser()); return result },
    async register(username, email, password) { const result = await authService.register(username, email, password); setUser(authService.getSavedUser()); return result },
    async updateUsername(username) { const result = await authService.updateUsername(username); setUser(authService.getSavedUser()); return result },
    async changePassword(currentPassword, newPassword) { const result = await authService.changePassword(currentPassword, newPassword); setUser(authService.getSavedUser()); return result },
    logout() { authService.logout(); setUser(null) },
    canManage: user?.role === 'Admin' || user?.role === 'Teacher',
    isAdmin: user?.role === 'Admin'
  }), [user])

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export const useAuth = () => useContext(AuthContext)
