import { describe, it, expect, vi, beforeEach } from 'vitest'
import { renderHook, waitFor } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import React from 'react'
import { useTrades, useTradesSummary, useCreateTrade, useDeleteTrade, useClosePosition } from './tradeApi'
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

const mockTrade = {
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
}

const mockPagedTrades = {
  items: [mockTrade],
  page: 1,
  pageSize: 20,
  totalCount: 1,
  totalPages: 1,
  hasPreviousPage: false,
  hasNextPage: false,
}

beforeEach(() => vi.clearAllMocks())

describe('useTrades', () => {
  it('fetches paged trades from /api/trades', async () => {
    vi.mocked(apiClient.get).mockResolvedValue({ data: mockPagedTrades })

    const { result } = renderHook(() => useTrades(), { wrapper: createWrapper() })

    await waitFor(() => expect(result.current.isSuccess).toBe(true))

    expect(apiClient.get).toHaveBeenCalledWith('/api/trades', { params: {} })
    expect(result.current.data?.items).toEqual(mockPagedTrades.items)
    expect(result.current.data?.totalCount).toBe(1)
  })

  it('passes status and sortBy params', async () => {
    vi.mocked(apiClient.get).mockResolvedValue({ data: mockPagedTrades })

    const { result } = renderHook(
      () => useTrades({ status: 'Open', sortBy: 'pnl', sortDir: 'desc' }),
      { wrapper: createWrapper() },
    )

    await waitFor(() => expect(result.current.isSuccess).toBe(true))

    expect(apiClient.get).toHaveBeenCalledWith('/api/trades', {
      params: { status: 'Open', sortBy: 'pnl', sortDir: 'desc' },
    })
  })
})

describe('useTradesSummary', () => {
  it('fetches summary from /api/trades/summary', async () => {
    const mockSummary = { totalPnl: 495, winRate: 100, totalTrades: 1, unrealizedPnl: 0 }
    vi.mocked(apiClient.get).mockResolvedValue({ data: mockSummary })

    const { result } = renderHook(() => useTradesSummary(), { wrapper: createWrapper() })

    await waitFor(() => expect(result.current.isSuccess).toBe(true))

    expect(apiClient.get).toHaveBeenCalledWith('/api/trades/summary', { params: {} })
    expect(result.current.data).toEqual(mockSummary)
  })

  it('passes preferredCurrency and preferredRate to the summary endpoint', async () => {
    const mockSummary = { totalPnl: 12_375_000, winRate: 100, totalTrades: 1, unrealizedPnl: 0 }
    vi.mocked(apiClient.get).mockResolvedValue({ data: mockSummary })

    const { result } = renderHook(
      () => useTradesSummary({ preferredCurrency: 'VND', preferredRate: 25000 }),
      { wrapper: createWrapper() },
    )

    await waitFor(() => expect(result.current.isSuccess).toBe(true))

    expect(apiClient.get).toHaveBeenCalledWith('/api/trades/summary', {
      params: { preferredCurrency: 'VND', preferredRate: 25000 },
    })
    expect(result.current.data).toEqual(mockSummary)
  })
})

describe('useCreateTrade', () => {
  it('posts to /api/trades with status field', async () => {
    vi.mocked(apiClient.post).mockResolvedValue({ data: 'new-guid' })
    vi.mocked(apiClient.get).mockResolvedValue({ data: mockPagedTrades })

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
    vi.mocked(apiClient.get).mockResolvedValue({ data: mockPagedTrades })

    const { result } = renderHook(() => useDeleteTrade(), { wrapper: createWrapper() })

    result.current.mutate('tr1')

    await waitFor(() => expect(result.current.isSuccess).toBe(true))

    expect(apiClient.delete).toHaveBeenCalledWith('/api/trades/tr1')
  })
})

describe('useClosePosition', () => {
  it('patches the close endpoint', async () => {
    vi.mocked(apiClient.patch).mockResolvedValue({ data: { ...mockTrade, status: 'Closed' } })
    vi.mocked(apiClient.get).mockResolvedValue({ data: mockPagedTrades })

    const { result } = renderHook(() => useClosePosition(), { wrapper: createWrapper() })

    result.current.mutate({ id: 'tr1', exitPrice: 65000, fees: 5 })

    await waitFor(() => expect(result.current.isSuccess).toBe(true))

    expect(apiClient.patch).toHaveBeenCalledWith('/api/trades/tr1/close', { exitPrice: 65000, fees: 5 })
  })
})
