import { type ReactNode, useCallback, useEffect, useState } from 'react'
import { authAdapter, extractRoles, ADMIN_ROLE } from '@/shared/lib/auth'
import { env } from '@/shared/config/env'
import { useAuthStore } from '@/features/auth'
import {
  AuthErrorScreen,
  AuthLoadingSplash,
  type AuthErrorType,
} from '@/shared/ui/AuthErrorScreen'
import { AuthDegradedBanner } from '@/shared/ui/AuthDegradedBanner'

// Module-level flag — prevents double-init in React Strict Mode
let initStarted = false

// Decode JWT payload without a library
function parseJwtPayload(token: string): Record<string, unknown> | null {
  try {
    return JSON.parse(
      atob(token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/')),
    )
  } catch {
    return null
  }
}

// Read localStorage token, return it only if not expired (30s buffer)
function getCachedToken(): { token: string; expiresAt: Date } | null {
  const token = localStorage.getItem('access_token')
  if (!token) return null
  const payload = parseJwtPayload(token)
  if (!payload || typeof payload.exp !== 'number') return null
  const expiresAt = new Date(payload.exp * 1000)
  if (expiresAt.getTime() - 30_000 < Date.now()) return null // expired or <30s left
  return { token, expiresAt }
}

interface AuthError {
  type: AuthErrorType
  timestamp: Date
}

function classifyAuthError(err: unknown): AuthErrorType {
  if (!navigator.onLine) return 'network'
  if (err instanceof TypeError) return 'network' // Failed to fetch / NetworkError
  if (err instanceof Error) {
    if (
      err.name === 'AbortError' ||
      err.message.toLowerCase().includes('timeout')
    )
      return 'timeout'
    if (/40[134]/.test(err.message)) return 'config' // 401/403/404 from provider
  }
  if (typeof err === 'string') {
    if (/network|fetch/i.test(err)) return 'network'
    if (/timeout/i.test(err)) return 'timeout'
  }
  return 'unknown'
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [initialized, setInitialized] = useState(false)
  const [authError, setAuthError] = useState<AuthError | null>(null)
  const [retryCount, setRetryCount] = useState(0)
  const [degraded, setDegraded] = useState<{ expiresAt: Date } | null>(null)
  const setToken = useAuthStore((s) => s.setToken)
  const setProfile = useAuthStore((s) => s.setProfile)
  const setIsAdmin = useAuthStore((s) => s.setIsAdmin)

  const runInit = useCallback(() => {
    const publicRoute = window.location.pathname === '/'
    const provider = env.AUTH_PROVIDER === 'auth0' ? 'auth0' : 'keycloak' as const

    // E2E bypass: Playwright injects localStorage['e2e_bypass'] = '1' alongside
    // a pre-issued JWT. This skips the provider SDK init (which would redirect
    // to the login page) and goes straight to degraded mode using the cached
    // token. Real users never have this flag set — the app never writes it.
    // The backend still validates the JWT on every request independently.
    if (localStorage.getItem('e2e_bypass') === '1') {
      const cached = getCachedToken()
      if (cached) {
        const payload = parseJwtPayload(cached.token)!
        setToken(cached.token)
        setProfile(
          (payload.name as string) ??
            (payload.preferred_username as string) ??
            '',
          (payload.email as string) ?? '',
        )
        setIsAdmin(extractRoles(payload, provider).includes(ADMIN_ROLE))
        setDegraded({ expiresAt: cached.expiresAt })
        setInitialized(true)
        return
      }
    }

    authAdapter
      .init({ publicRoute })
      .then((profile) => {
        if (profile === null) {
          // Unauthenticated on a public route — render the page without a token
          setInitialized(true)
          return
        }
        authAdapter.getToken().then((token) => {
          setToken(token)
          setProfile(profile.displayName, profile.email)
          setIsAdmin(authAdapter.getRoles().includes(ADMIN_ROLE))
        })
        setInitialized(true)
      })
      .catch((err) => {
        initStarted = false // allow re-init on retry
        const cached = getCachedToken()
        if (cached) {
          // Provider is down but we have a valid JWT — let user continue in degraded mode
          const payload = parseJwtPayload(cached.token)!
          setToken(cached.token)
          setProfile(
            (payload.name as string) ??
              (payload.preferred_username as string) ??
              '',
            (payload.email as string) ?? '',
          )
          setIsAdmin(extractRoles(payload, provider).includes(ADMIN_ROLE))
          setDegraded({ expiresAt: cached.expiresAt })
        } else if (publicRoute) {
          // Provider unreachable on a public route — still render the landing page
        } else {
          // No usable token — must show error screen
          setAuthError({ type: classifyAuthError(err), timestamp: new Date() })
          setRetryCount((c) => c + 1)
        }
        setInitialized(true)
      })
  }, [setToken, setProfile, setIsAdmin])

  useEffect(() => {
    if (initStarted) return
    initStarted = true
    runInit() // eslint-disable-line react-hooks/set-state-in-effect
  }, [runInit])

  const handleRetry = useCallback(() => {
    setAuthError(null)
    setInitialized(false)
    initStarted = true
    runInit()
  }, [runInit])

  if (!initialized) return <AuthLoadingSplash />

  if (authError) {
    return (
      <AuthErrorScreen
        errorType={authError.type}
        onRetry={handleRetry}
        retryCount={retryCount}
        maxRetries={3}
        autoRetrySeconds={30}
      />
    )
  }

  return (
    <>
      {degraded && <AuthDegradedBanner tokenExpiresAt={degraded.expiresAt} />}
      {children}
    </>
  )
}
