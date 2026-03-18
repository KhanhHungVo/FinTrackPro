import axios from 'axios'
import { env } from '@/shared/config/env'
import { authAdapter } from '@/shared/lib/auth'
import { useAuthStore } from '@/features/auth'

export const apiClient = axios.create({
  baseURL: env.API_BASE_URL,
  headers: { 'Content-Type': 'application/json' },
})

// Inject the current access token on every request
apiClient.interceptors.request.use(async (config) => {
  try {
    const token = await authAdapter.getToken()
    config.headers.Authorization = `Bearer ${token}`
  } catch {
    // Not authenticated — let the request proceed without a token
    // (API will return 401 which triggers the response interceptor below)
  }
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
