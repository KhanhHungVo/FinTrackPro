import axios from 'axios'
import { env } from '@/shared/config/env'
import { authAdapter } from '@/shared/lib/auth'
import { useAuthStore } from '@/features/auth'

export const apiClient = axios.create({
  baseURL: env.API_BASE_URL,
  headers: { 'Content-Type': 'application/json' },
})

// Inject the current access token on every request.
// Falls back to the Zustand store token when the adapter is not initialized
// (e.g. degraded mode — Keycloak/Auth0 down but a cached JWT is available).
apiClient.interceptors.request.use(async (config) => {
  let token: string | null = null
  try {
    token = await authAdapter.getToken()
  } catch {
    token = useAuthStore.getState().accessToken
  }
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

// On 401: attempt a silent token refresh and retry once; otherwise force re-login.
// The _retried flag prevents the retried request from triggering another retry loop.
apiClient.interceptors.response.use(
  (res) => res,
  async (error) => {
    if (error.response?.status === 401 && !error.config._retried) {
      error.config._retried = true
      try {
        const newToken = await authAdapter.refreshToken()
        error.config.headers.Authorization = `Bearer ${newToken}`
        return apiClient(error.config)
      } catch {
        useAuthStore.getState().logout()
      }
    }
    return Promise.reject(error)
  },
)
