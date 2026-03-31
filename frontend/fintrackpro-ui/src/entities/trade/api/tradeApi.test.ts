import { describe, it, expect, vi, beforeEach } from 'vitest'
import { renderHook, waitFor } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import React from 'react'
import { useTrades, useCreateTrade, useDeleteTrade, useClosePosition } from './tradeApi'
import { apiClient } from '@/shared/api/client'

vi.mock('@/shared/api/client', () => ({
  apiClient: {
    get: vi.fn(),
    post: vi.fn(),
    delete: vi.fn(),
    put: vi.fn(),
    patch: vi.fn(),
  },
}))

function createWrapper() {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  return ({ children }: { children: React.ReactNode }) =>
    React.createElement(QueryClientProvider, { client: qc }, children)
}

const mockTrades = [
  {
    id: 'tr1',
    symbol: 'BTCUSDT',
    direction: 'Long',
    status: 'Closed',
    entryPrice: 60000,
    exitPrice: 65000,
    currentPrice: null,
    positionSize: 0.1,
    fees: 5,
    currency: 'USD',
    rateToUsd: 1,
    result: 495,
    unrealizedResult: null,
    notes: null,
    createdAt: '2026-03-10T14:00:00Z',
  },
]

beforeEach(() => vi.clearAllMocks())

describe('useTrades', () => {
  it('fetches trades from /api/trades', async () => {
    vi.mocked(apiClient.get).mockResolvedValue({ data: mockTrades })

    const { result } = renderHook(() => useTrades(), { wrapper: createWrapper() })

    await waitFor(() => expect(result.current.isSuccess).toBe(true))

    expect(apiClient.get).toHaveBeenCalledWith('/api/trades')
    expect(result.current.data).toEqual(mockTrades)
  })
})

describe('useCreateTrade', () => {
  it('posts to /api/trades with status field', async () => {
    vi.mocked(apiClient.post).mockResolvedValue({ data: 'new-guid' })
    vi.mocked(apiClient.get).mockResolvedValue({ data: [] })

    const { result } = renderHook(() => useCreateTrade(), { wrapper: createWrapper() })

    result.current.mutate({
      symbol: 'BTCUSDT',
      direction: 'Long',
      status: 'Open',
      entryPrice: 60000,
      exitPrice: null,
      currentPrice: 62000,
      positionSize: 0.1,
      fees: 0,
      currency: 'USD',
      notes: null,
    })

    await waitFor(() => expect(result.current.isSuccess).toBe(true))

    expect(apiClient.post).toHaveBeenCalledWith('/api/trades', expect.objectContaining({
      symbol: 'BTCUSDT',
      status: 'Open',
      exitPrice: null,
    }))
  })
})

describe('useDeleteTrade', () => {
  it('deletes the correct trade URL', async () => {
    vi.mocked(apiClient.delete).mockResolvedValue({})
    vi.mocked(apiClient.get).mockResolvedValue({ data: [] })

    const { result } = renderHook(() => useDeleteTrade(), { wrapper: createWrapper() })

    result.current.mutate('tr1')

    await waitFor(() => expect(result.current.isSuccess).toBe(true))

    expect(apiClient.delete).toHaveBeenCalledWith('/api/trades/tr1')
  })
})

describe('useClosePosition', () => {
  it('patches the close endpoint', async () => {
    vi.mocked(apiClient.patch).mockResolvedValue({ data: { ...mockTrades[0], status: 'Closed' } })
    vi.mocked(apiClient.get).mockResolvedValue({ data: [] })

    const { result } = renderHook(() => useClosePosition(), { wrapper: createWrapper() })

    result.current.mutate({ id: 'tr1', exitPrice: 65000, fees: 5 })

    await waitFor(() => expect(result.current.isSuccess).toBe(true))

    expect(apiClient.patch).toHaveBeenCalledWith('/api/trades/tr1/close', { exitPrice: 65000, fees: 5 })
  })
})
