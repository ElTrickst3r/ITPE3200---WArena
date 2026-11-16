import { createContext, useContext, useEffect, useMemo, useState } from 'react'
import * as authService from '../services/authService'
import { UNAUTHORIZED_EVENT } from '../services/api'

const AuthContext = createContext(null)

export function AuthProvider({ children }) {
  const [user, setUser] = useState(() => authService.getSavedUser())

  // Log the user out when the API rejects the token, e.g. when it has expired or the role was changed.
  useEffect(() => {
    const onUnauthorized = () => {
      authService.logout()
      setUser(null)
    }
    window.addEventListener(UNAUTHORIZED_EVENT, onUnauthorized)
    return () => window.removeEventListener(UNAUTHORIZED_EVENT, onUnauthorized)
  }, [])

  const value = useMemo(() => {
    // Runs an auth action and refreshes the user from the saved session afterwards.
    const withSessionRefresh = (action) => async (...args) => {
      const result = await action(...args)
      setUser(authService.getSavedUser())
      return result
    }

    return {
      user,
      login: withSessionRefresh(authService.login),
      register: withSessionRefresh(authService.register),
      updateUsername: withSessionRefresh(authService.updateUsername),
      changePassword: withSessionRefresh(authService.changePassword),
      logout() {
        authService.logout()
        setUser(null)
      },
      canManage: user?.role === 'Admin' || user?.role === 'Teacher',
      isAdmin: user?.role === 'Admin'
    }
  }, [user])

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export const useAuth = () => useContext(AuthContext)