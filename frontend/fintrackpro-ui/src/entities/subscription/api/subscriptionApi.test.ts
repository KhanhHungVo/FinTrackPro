import { describe, it, expect, vi, beforeEach } from 'vitest'
import { renderHook, waitFor } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import React from 'react'
import {
  useSubscriptionStatus,
  useCreateCheckoutSession,
  useCreatePortalSession,
  useAdminUsers,
  useAdminActivateSubscription,
  useAdminRevokeSubscription,
} from './subscriptionApi'
import { apiClient } from '@/shared/api/client'

vi.mock('@/shared/api/client', () => ({
  apiClient: {
    get: vi.fn(),
    post: vi.fn(),
    delete: vi.fn(),
  },
}))

function createWrapper() {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  return ({ children }: { children: React.ReactNode }) =>
    React.createElement(QueryClientProvider, { client: qc }, children)
}

beforeEach(() => vi.clearAllMocks())

describe('useSubscriptionStatus', () => {
  it('fetches subscription status', async () => {
    const mockStatus = { plan: 'Pro', isActive: true, expiresAt: '2027-04-06T00:00:00Z' }
    vi.mocked(apiClient.get).mockResolvedValue({ data: mockStatus })

    const { result } = renderHook(() => useSubscriptionStatus(), {
      wrapper: createWrapper(),
    })

    await waitFor(() => expect(result.current.isSuccess).toBe(true))

    expect(apiClient.get).toHaveBeenCalledWith('/api/subscription/status')
    expect(result.current.data).toEqual(mockStatus)
  })

  it('returns Free plan data', async () => {
    const mockStatus = { plan: 'Free', isActive: false, expiresAt: null }
    vi.mocked(apiClient.get).mockResolvedValue({ data: mockStatus })

    const { result } = renderHook(() => useSubscriptionStatus(), {
      wrapper: createWrapper(),
    })

    await waitFor(() => expect(result.current.isSuccess).toBe(true))
    expect(result.current.data?.plan).toBe('Free')
  })
})

describe('useCreateCheckoutSession', () => {
  it('posts to /api/subscription/checkout', async () => {
    const mockResponse = { sessionUrl: 'https://checkout.stripe.com/pay/cs_test_123' }
    vi.mocked(apiClient.post).mockResolvedValue({ data: mockResponse })

    const { result } = renderHook(() => useCreateCheckoutSession(), {
      wrapper: createWrapper(),
    })

    result.current.mutate({
      successUrl: 'https://app.example.com/settings?subscribed=1',
      cancelUrl: 'https://app.example.com/pricing',
    })

    await waitFor(() => expect(result.current.isSuccess).toBe(true))

    expect(apiClient.post).toHaveBeenCalledWith('/api/subscription/checkout', {
      successUrl: 'https://app.example.com/settings?subscribed=1',
      cancelUrl: 'https://app.example.com/pricing',
    })
    expect(result.current.data?.sessionUrl).toBe(mockResponse.sessionUrl)
  })
})

describe('useCreatePortalSession', () => {
  it('posts to /api/subscription/portal', async () => {
    const mockResponse = { portalUrl: 'https://billing.stripe.com/session/bps_test_123' }
    vi.mocked(apiClient.post).mockResolvedValue({ data: mockResponse })

    const { result } = renderHook(() => useCreatePortalSession(), {
      wrapper: createWrapper(),
    })

    result.current.mutate({ returnUrl: 'https://app.example.com/settings' })

    await waitFor(() => expect(result.current.isSuccess).toBe(true))

    expect(apiClient.post).toHaveBeenCalledWith('/api/subscription/portal', {
      returnUrl: 'https://app.example.com/settings',
    })
    expect(result.current.data?.portalUrl).toBe(mockResponse.portalUrl)
  })
})

describe('useAdminUsers', () => {
  it('calls GET /api/admin/users with page and emailFilter', async () => {
    const pagedResult = { items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0, hasPreviousPage: false, hasNextPage: false }
    vi.mocked(apiClient.get).mockResolvedValue({ data: pagedResult })

    const { result } = renderHook(() => useAdminUsers(2, 'alice'), { wrapper: createWrapper() })

    await waitFor(() => expect(result.current.isSuccess).toBe(true))

    expect(apiClient.get).toHaveBeenCalledWith(expect.stringContaining('page=2'))
    expect(apiClient.get).toHaveBeenCalledWith(expect.stringContaining('email=alice'))
  })
})

describe('useAdminActivateSubscription', () => {
  it('calls POST /api/admin/users/{userId}/subscription with period', async () => {
    const userId = '00000000-0000-0000-0000-000000000001'
    vi.mocked(apiClient.post).mockResolvedValue({ data: { plan: 1, isActive: true, expiresAt: null } })

    const { result } = renderHook(() => useAdminActivateSubscription(), { wrapper: createWrapper() })

    result.current.mutate({ userId, period: 'Monthly' })

    await waitFor(() => expect(result.current.isSuccess).toBe(true))

    expect(apiClient.post).toHaveBeenCalledWith(
      `/api/admin/users/${userId}/subscription`,
      { period: 'Monthly' },
    )
  })
})

describe('useAdminRevokeSubscription', () => {
  it('calls DELETE /api/admin/users/{userId}/subscription', async () => {
    const userId = '00000000-0000-0000-0000-000000000002'
    vi.mocked(apiClient.delete).mockResolvedValue({})

    const { result } = renderHook(() => useAdminRevokeSubscription(), { wrapper: createWrapper() })

    result.current.mutate(userId)

    await waitFor(() => expect(result.current.isSuccess).toBe(true))

    expect(apiClient.delete).toHaveBeenCalledWith(`/api/admin/users/${userId}/subscription`)
  })
})
