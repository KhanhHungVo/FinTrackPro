import axios from 'axios'
import { env } from '@/shared/config/env'

// Build the Keycloak Authorization Code login URL so that on 401 the user is
// sent directly to Keycloak rather than an internal /login route that doesn't exist.
const keycloakLoginUrl =
  `${env.KEYCLOAK_URL}/realms/${env.KEYCLOAK_REALM}/protocol/openid-connect/auth` +
  `?client_id=${env.KEYCLOAK_CLIENT_ID}&response_type=code&redirect_uri=${encodeURIComponent(window.location.origin)}`

export const apiClient = axios.create({
  baseURL: env.API_BASE_URL,
  headers: { 'Content-Type': 'application/json' },
})

// Inject Bearer token from localStorage (set after Keycloak login)
apiClient.interceptors.request.use((config) => {
  const token = localStorage.getItem('access_token')
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

apiClient.interceptors.response.use(
  (res) => res,
  (error) => {
    if (error.response?.status === 401) {
      localStorage.removeItem('access_token')
      // TODO: review full auth flow (Keycloak JS adapter vs manual redirect)
      // window.location.href = '/login'  // broken: /login is not a defined route
      // Redirect to Keycloak login instead of the non-existent internal /login route
      window.location.href = keycloakLoginUrl
    }
    return Promise.reject(error)
  },
)
