import { createContext, useContext, useEffect, useMemo, useState } from 'react'
import { type AuthResponse, type AuthUser, getCurrentUser, setAccessToken } from '../api/client'

const storageKey = 'orgwiki.access-token'
type AuthContextValue = {
  user: AuthUser | null
  isLoading: boolean
  completeAuthentication: (response: AuthResponse) => void
  logout: () => void
}

const AuthContext = createContext<AuthContextValue | null>(null)

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(null)
  const [isLoading, setIsLoading] = useState(true)

  useEffect(() => {
    const token = localStorage.getItem(storageKey)
    if (!token) {
      setIsLoading(false)
      return
    }

    setAccessToken(token)
    getCurrentUser()
      .then(setUser)
      .catch(() => {
        localStorage.removeItem(storageKey)
        setAccessToken(null)
      })
      .finally(() => setIsLoading(false))
  }, [])

  const value = useMemo<AuthContextValue>(() => ({
    user,
    isLoading,
    completeAuthentication: response => {
      localStorage.setItem(storageKey, response.accessToken)
      setAccessToken(response.accessToken)
      setUser(response.user)
    },
    logout: () => {
      localStorage.removeItem(storageKey)
      setAccessToken(null)
      setUser(null)
    }
  }), [user, isLoading])

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const context = useContext(AuthContext)
  if (!context) throw new Error('useAuth must be used inside AuthProvider.')
  return context
}
