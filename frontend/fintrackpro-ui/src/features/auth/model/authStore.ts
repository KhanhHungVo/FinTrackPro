import { create } from 'zustand'
import { authAdapter } from '@/shared/lib/auth'

interface AuthState {
  accessToken: string | null
  displayName: string | null
  email: string | null
  isAuthenticated: boolean
  isAdmin: boolean
  setToken: (token: string) => void
  setProfile: (displayName: string, email: string) => void
  setIsAdmin: (isAdmin: boolean) => void
  logout: () => void
}

export const useAuthStore = create<AuthState>((set) => ({
  accessToken: null,
  displayName: null,
  email: null,
  isAuthenticated: false,
  isAdmin: false,
  setToken: (token) => {
    localStorage.setItem('access_token', token)
    set({ accessToken: token, isAuthenticated: true })
  },
  setProfile: (displayName, email) => set({ displayName, email }),
  setIsAdmin: (isAdmin) => set({ isAdmin }),
  // Clears local state and ends the IAM provider session
  logout: () => {
    localStorage.removeItem('access_token')
    set({ accessToken: null, displayName: null, email: null, isAuthenticated: false, isAdmin: false })
    authAdapter.logout(window.location.origin)
  },
}))
