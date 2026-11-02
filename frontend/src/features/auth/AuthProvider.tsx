import { useQueryClient } from '@tanstack/react-query'
import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react'
import { refreshSession } from '../../shared/api/client'
import { session } from '../../shared/api/session'
import type { AuthResponse } from '../../shared/api/types'
import { AuthContext, type AuthContextValue, type AuthStatus } from './AuthContext'
import * as authApi from './authApi'

/** Restores the session from the refresh cookie on load and exposes auth actions. */
export function AuthProvider({ children }: { children: ReactNode }) {
  const queryClient = useQueryClient()
  const [current, setCurrent] = useState<AuthResponse | null>(session.get())
  const [restoring, setRestoring] = useState(session.get() === null)

  useEffect(() => session.subscribe(setCurrent), [])

  useEffect(() => {
    if (session.get() !== null) {
      return
    }
    let active = true
    void refreshSession().finally(() => {
      if (active) {
        setRestoring(false)
      }
    })
    return () => {
      active = false
    }
  }, [])

  const logout = useCallback(async () => {
    await authApi.logout()
    queryClient.clear()
  }, [queryClient])

  const value = useMemo<AuthContextValue>(() => {
    const status: AuthStatus = current !== null ? 'authenticated' : restoring ? 'loading' : 'anonymous'
    return {
      status,
      user: current?.user ?? null,
      login: async (request) => {
        await authApi.login(request)
      },
      register: async (request) => {
        await authApi.register(request)
      },
      logout,
    }
  }, [current, restoring, logout])

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
