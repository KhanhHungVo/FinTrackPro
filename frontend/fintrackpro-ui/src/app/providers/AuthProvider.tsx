import { type ReactNode, useCallback, useEffect, useState } from 'react'
import { authAdapter } from '@/shared/lib/auth'
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

  function runInit() {
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
        setDegraded({ expiresAt: cached.expiresAt })
        setInitialized(true)
        return
      }
    }

    authAdapter
      .init()
      .then((profile) => {
        authAdapter.getToken().then((token) => {
          setToken(token)
          setProfile(profile.displayName, profile.email)
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
          setDegraded({ expiresAt: cached.expiresAt })
        } else {
          // No usable token — must show error screen
          setAuthError({ type: classifyAuthError(err), timestamp: new Date() })
          setRetryCount((c) => c + 1)
        }
        setInitialized(true)
      })
  }

  useEffect(() => {
    if (initStarted) return
    initStarted = true
    runInit()
  }, []) // eslint-disable-line react-hooks/exhaustive-deps

  const handleRetry = useCallback(() => {
    setAuthError(null)
    setInitialized(false)
    initStarted = true
    runInit()
  }, []) // eslint-disable-line react-hooks/exhaustive-deps

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
