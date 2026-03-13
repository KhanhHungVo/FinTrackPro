import { type ReactNode, useEffect, useState } from 'react'
import { keycloak } from '@/shared/lib/keycloak'
import { useAuthStore } from '@/features/auth'

// Module-level flag — prevents double-init in React Strict Mode
let initStarted = false

export function KeycloakProvider({ children }: { children: ReactNode }) {
  const [initialized, setInitialized] = useState(false)
  const [error, setError] = useState(false)
  const setToken = useAuthStore((s) => s.setToken)
  const setProfile = useAuthStore((s) => s.setProfile)

  useEffect(() => {
    if (initStarted) return
    initStarted = true

    keycloak
      .init({ onLoad: 'login-required', pkceMethod: 'S256' })
      .then((authenticated) => {
        if (authenticated && keycloak.token) {
          setToken(keycloak.token)
          setProfile(
            keycloak.idTokenParsed?.name
              ?? keycloak.idTokenParsed?.preferred_username
              ?? '',
            keycloak.idTokenParsed?.email ?? '',
          )
        }
        // If not authenticated, 'login-required' has already redirected to Keycloak
        setInitialized(true)
      })
      .catch(() => {
        setError(true)
        setInitialized(true) // unblock rendering so the error UI is shown
      })

    keycloak.onTokenExpired = () => {
      keycloak
        .updateToken(30)
        .then((refreshed) => {
          if (refreshed && keycloak.token) setToken(keycloak.token)
        })
        .catch(() => keycloak.login())
    }
  }, []) // eslint-disable-line react-hooks/exhaustive-deps

  if (!initialized) return null
  if (error) {
    return (
      <div style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: '100vh', fontFamily: 'sans-serif' }}>
        <p>Unable to connect to the authentication server. Please try again later.</p>
      </div>
    )
  }
  return <>{children}</>
}
