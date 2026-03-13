import axios from 'axios'
import { env } from '@/shared/config/env'
import { keycloak } from '@/shared/lib/keycloak'
import { useAuthStore } from '@/features/auth'

export const apiClient = axios.create({
  baseURL: env.API_BASE_URL,
  headers: { 'Content-Type': 'application/json' },
})

// Inject the current Keycloak token on every request
apiClient.interceptors.request.use((config) => {
  if (keycloak.token) config.headers.Authorization = `Bearer ${keycloak.token}`
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
        await keycloak.updateToken(0)
        error.config.headers.Authorization = `Bearer ${keycloak.token}`
        return apiClient(error.config)
      } catch {
        useAuthStore.getState().logout()
        keycloak.login()
      }
    }
    return Promise.reject(error)
  },
)
