import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useRef,
  useState,
  type ReactNode,
} from 'react'
import {
  AuthenticationDetails,
  CognitoUser,
  CognitoUserAttribute,
  type CognitoUserSession,
} from 'amazon-cognito-identity-js'
import { cognitoConfigured, userPool } from './cognitoPool'
import { getAuthHeaders, setIdTokenGetter } from './authToken'

type AuthContextValue = {
  cognitoConfigured: boolean
  isAuthenticated: boolean
  isAdmin: boolean
  loading: boolean
  email: string | null
  error: string | null
  needsNewPassword: boolean
  signIn: (email: string, password: string) => Promise<void>
  completeNewPassword: (newPassword: string) => Promise<void>
  signOut: () => void
  signUp: (email: string, password: string) => Promise<void>
  confirmSignUp: (email: string, code: string) => Promise<void>
  inviteUser: (email: string) => Promise<void>
  clearError: () => void
}

function extractAdminFromAccessToken(accessJwt: string | undefined): boolean {
  if (!accessJwt) return false
  try {
    const [, payload] = accessJwt.split('.')
    if (!payload) return false
    const normalized = payload.replace(/-/g, '+').replace(/_/g, '/')
    const pad = normalized.length % 4 === 0 ? '' : '='.repeat(4 - (normalized.length % 4))
    const json = atob(normalized + pad)
    const decoded = JSON.parse(json) as { 'cognito:groups'?: unknown }
    const raw = decoded['cognito:groups']
    const groups: string[] = Array.isArray(raw)
      ? raw.map(String)
      : typeof raw === 'string'
      ? raw.split(',')
      : []
    return groups.some((g) => g.trim().toLowerCase() === 'admins')
  } catch {
    return false
  }
}

const AuthContext = createContext<AuthContextValue | null>(null)

function getCognitoUser(email: string): CognitoUser {
  if (!userPool) throw new Error('Cognito is not configured.')
  return new CognitoUser({ Username: email, Pool: userPool })
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [loading, setLoading] = useState(() => cognitoConfigured)
  const [isAuthenticated, setIsAuthenticated] = useState(false)
  const [isAdmin, setIsAdmin] = useState(false)
  const [email, setEmail] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [needsNewPassword, setNeedsNewPassword] = useState(false)
  const newPasswordUserRef = useRef<CognitoUser | null>(null)

  const refreshSession = useCallback(() => {
    if (!userPool) {
      setLoading(false)
      return
    }
    const u = userPool.getCurrentUser()
    if (!u) {
      setIsAuthenticated(false)
      setIsAdmin(false)
      setEmail(null)
      setLoading(false)
      return
    }
    u.getSession((err: Error | undefined, session: CognitoUserSession | null) => {
      if (err || !session?.isValid()) {
        setIsAuthenticated(false)
        setIsAdmin(false)
        setEmail(null)
        setLoading(false)
        return
      }
      setIsAuthenticated(true)
      setIsAdmin(extractAdminFromAccessToken(session.getAccessToken().getJwtToken()))
      const id = session.getIdToken().payload?.email as string | undefined
      setEmail(id ?? u.getUsername())
      setLoading(false)
    })
  }, [])

  useEffect(() => {
    queueMicrotask(() => {
      refreshSession()
    })
  }, [refreshSession])

  const getIdToken = useCallback(async (): Promise<string | null> => {
    if (!userPool) return null
    const u = userPool.getCurrentUser()
    if (!u) return null
    return new Promise((resolve) => {
      u.getSession((err: Error | undefined, session: CognitoUserSession | null) => {
        if (err || !session?.isValid()) {
          resolve(null)
          return
        }
        // Access token carries cognito:groups for admin invite; same issuer as ID token.
        resolve(session.getAccessToken().getJwtToken())
      })
    })
  }, [])

  useEffect(() => {
    setIdTokenGetter(getIdToken)
    return () => setIdTokenGetter(async () => null)
  }, [getIdToken])

  const signIn = useCallback(async (e: string, password: string) => {
    setError(null)
    setNeedsNewPassword(false)
    newPasswordUserRef.current = null
    const u = getCognitoUser(e.trim())
    const details = new AuthenticationDetails({
      Username: e.trim(),
      Password: password,
    })
    let passwordChallenge = false
    await new Promise<void>((resolve, reject) => {
      u.authenticateUser(details, {
        onSuccess: () => resolve(),
        onFailure: (err) => reject(err),
        newPasswordRequired: () => {
          newPasswordUserRef.current = u
          passwordChallenge = true
          setNeedsNewPassword(true)
          resolve()
        },
      })
    })
    if (!passwordChallenge) await Promise.resolve(refreshSession())
  }, [refreshSession])

  const completeNewPassword = useCallback(
    async (newPassword: string) => {
      setError(null)
      const u = newPasswordUserRef.current
      if (!u) throw new Error('No pending password challenge.')
      await new Promise<void>((resolve, reject) => {
        u.completeNewPasswordChallenge(
          newPassword,
          [],
          {
            onSuccess: () => resolve(),
            onFailure: (err) => reject(err),
          },
        )
      })
      setNeedsNewPassword(false)
      newPasswordUserRef.current = null
      refreshSession()
    },
    [refreshSession],
  )

  const signOut = useCallback(() => {
    if (!userPool) return
    const u = userPool.getCurrentUser()
    u?.signOut()
    setIsAuthenticated(false)
    setIsAdmin(false)
    setEmail(null)
    setNeedsNewPassword(false)
    newPasswordUserRef.current = null
  }, [])

  const signUp = useCallback(async (e: string, password: string) => {
    setError(null)
    const pool = userPool
    if (!pool) throw new Error('Cognito is not configured.')
    await new Promise<void>((resolve, reject) => {
      pool.signUp(
        e.trim(),
        password,
        [new CognitoUserAttribute({ Name: 'email', Value: e.trim() })],
        [],
        (err) => (err ? reject(err) : resolve()),
      )
    })
  }, [])

  const confirmSignUp = useCallback(async (e: string, code: string) => {
    setError(null)
    const u = getCognitoUser(e.trim())
    await new Promise<void>((resolve, reject) => {
      u.confirmRegistration(code.trim(), true, (err) => (err ? reject(err) : resolve()))
    })
  }, [])

  const inviteUser = useCallback(async (inviteEmail: string) => {
    setError(null)
    const base = import.meta.env.VITE_API_URL ?? ''
    const res = await fetch(`${base}/api/auth/invite`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        ...(await getAuthHeaders()),
      },
      body: JSON.stringify({ email: inviteEmail.trim() }),
    })
    if (!res.ok) {
      const j = (await res.json().catch(() => ({}))) as { error?: string }
      throw new Error(j.error ?? res.statusText)
    }
  }, [])

  const value = useMemo<AuthContextValue>(
    () => ({
      cognitoConfigured,
      isAuthenticated,
      isAdmin,
      loading,
      email,
      error,
      needsNewPassword,
      signIn,
      completeNewPassword,
      signOut,
      signUp,
      confirmSignUp,
      inviteUser,
      clearError: () => setError(null),
    }),
    [
      isAuthenticated,
      isAdmin,
      loading,
      email,
      error,
      needsNewPassword,
      signIn,
      completeNewPassword,
      signOut,
      signUp,
      confirmSignUp,
      inviteUser,
    ],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

// eslint-disable-next-line react-refresh/only-export-components -- hook paired with AuthProvider
export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used within AuthProvider')
  return ctx
}
