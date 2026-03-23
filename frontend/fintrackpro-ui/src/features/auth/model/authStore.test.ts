import { describe, it, expect, beforeEach, vi } from 'vitest'
import { useAuthStore } from './authStore'

// Mock the auth adapter to prevent real Keycloak/Auth0 calls
vi.mock('@/shared/lib/auth', () => ({
  authAdapter: {
    getToken: vi.fn(),
    refreshToken: vi.fn(),
    logout: vi.fn(),
  },
}))

beforeEach(() => {
  // Reset store state between tests
  useAuthStore.setState({
    accessToken: null,
    displayName: null,
    email: null,
    isAuthenticated: false,
  })
  localStorage.clear()
})

describe('authStore', () => {
  it('initial state: not authenticated', () => {
    const state = useAuthStore.getState()
    expect(state.isAuthenticated).toBe(false)
    expect(state.accessToken).toBeNull()
    expect(state.displayName).toBeNull()
    expect(state.email).toBeNull()
  })

  it('setToken: sets token and marks authenticated', () => {
    useAuthStore.getState().setToken('my-jwt')

    const state = useAuthStore.getState()
    expect(state.accessToken).toBe('my-jwt')
    expect(state.isAuthenticated).toBe(true)
    expect(localStorage.getItem('access_token')).toBe('my-jwt')
  })

  it('setProfile: sets display name and email', () => {
    useAuthStore.getState().setProfile('Alice', 'alice@example.com')

    const state = useAuthStore.getState()
    expect(state.displayName).toBe('Alice')
    expect(state.email).toBe('alice@example.com')
  })

  it('logout: clears token and authentication state', () => {
    useAuthStore.getState().setToken('my-jwt')
    useAuthStore.getState().setProfile('Alice', 'alice@example.com')

    useAuthStore.getState().logout()

    const state = useAuthStore.getState()
    expect(state.accessToken).toBeNull()
    expect(state.isAuthenticated).toBe(false)
    expect(state.displayName).toBeNull()
    expect(state.email).toBeNull()
    expect(localStorage.getItem('access_token')).toBeNull()
  })
})
