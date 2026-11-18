import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react'
import { useQueryClient } from '@tanstack/react-query'
import { api, refreshSession, session, type Schema } from './api'

type User = Schema<'MeDto'>
type Status = 'loading' | 'anonymous' | 'authenticated'

type AuthContextValue = {
  status: Status
  user: User | null
  /** Verdadeiro se o usuário possui ao menos uma das permissões informadas (sempre revalidado pelo servidor). */
  can: (...anyOf: string[]) => boolean
  isAdmin: boolean
  isSuperAdmin: boolean
  /** Clube em que o superadministrador está operando. */
  clubId: string | null
  selectClub: (clubId: string | null) => void
  login: (email: string, password: string) => Promise<User>
  logout: () => Promise<void>
  reload: () => Promise<void>
}

const AuthContext = createContext<AuthContextValue | null>(null)

export function AuthProvider({ children }: { children: ReactNode }) {
  const queryClient = useQueryClient()
  const [status, setStatus] = useState<Status>('loading')
  const [user, setUser] = useState<User | null>(null)
  const [clubId, setClubId] = useState<string | null>(session.clubId)

  const drop = useCallback(() => {
    session.clear()
    setUser(null)
    setClubId(null)
    setStatus('anonymous')
    queryClient.clear()
  }, [queryClient])

  useEffect(() => {
    session.onLost(drop)
    let active = true
    refreshSession().then((auth) => {
      if (!active) return
      if (auth) { setUser(auth.user); setStatus('authenticated') } else setStatus('anonymous')
    })
    return () => { active = false }
  }, [drop])

  const value = useMemo<AuthContextValue>(() => ({
    status,
    user,
    clubId: user?.kind === 'SuperAdmin' ? clubId : user?.clubId ?? null,
    isAdmin: user?.kind === 'ClubAdmin' || user?.kind === 'SuperAdmin',
    isSuperAdmin: user?.kind === 'SuperAdmin',
    can: (...anyOf) => !!user && anyOf.some((p) => user.permissions.includes(p)),
    selectClub: (id) => {
      session.clubId = id
      setClubId(id)
      queryClient.clear()
    },
    login: async (email, password) => {
      const auth = await api.post<Schema<'AuthResponse'>>('/api/auth/login', { email, password, device: 'Web' })
      session.store(auth)
      setUser(auth.user)
      setStatus('authenticated')
      return auth.user
    },
    logout: async () => {
      const refreshToken = session.refreshToken
      try { if (refreshToken) await api.post('/api/auth/logout', { refreshToken }) } catch { /* a sessão local é encerrada de qualquer forma */ }
      drop()
    },
    reload: async () => setUser(await api.get<User>('/api/auth/me')),
  }), [status, user, clubId, queryClient, drop])

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const context = useContext(AuthContext)
  if (!context) throw new Error('useAuth deve ser usado dentro de AuthProvider')
  return context
}
