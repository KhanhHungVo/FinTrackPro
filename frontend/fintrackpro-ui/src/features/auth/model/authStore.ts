import { create } from 'zustand'
import { keycloak } from '@/shared/lib/keycloak'

interface AuthState {
  accessToken: string | null
  displayName: string | null
  email: string | null
  isAuthenticated: boolean
  setToken: (token: string) => void
  setProfile: (displayName: string, email: string) => void
  logout: () => void
}

export const useAuthStore = create<AuthState>((set) => ({
  accessToken: null,
  displayName: null,
  email: null,
  isAuthenticated: false,
  setToken: (token) => {
    localStorage.setItem('access_token', token)
    set({ accessToken: token, isAuthenticated: true })
  },
  setProfile: (displayName, email) => set({ displayName, email }),
  // Clears local state and ends the Keycloak session
  logout: () => {
    localStorage.removeItem('access_token')
    set({ accessToken: null, displayName: null, email: null, isAuthenticated: false })
    if (keycloak.authenticated) {
      keycloak.logout({ redirectUri: window.location.origin })
    }
  },
}))
