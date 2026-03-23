import { describe, it, expect, vi, beforeEach } from 'vitest'
import { renderHook, waitFor } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import React from 'react'
import { useTransactions, useCreateTransaction, useDeleteTransaction } from './transactionApi'
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

const mockTransactions = [
  {
    id: 't1',
    type: 'Expense',
    amount: 120.5,
    category: 'Food',
    note: 'Grocery run',
    budgetMonth: '2026-03',
    createdAt: '2026-03-12T10:00:00Z',
  },
]

beforeEach(() => vi.clearAllMocks())

describe('useTransactions', () => {
  it('fetches all transactions without month filter', async () => {
    vi.mocked(apiClient.get).mockResolvedValue({ data: mockTransactions })

    const { result } = renderHook(() => useTransactions(), { wrapper: createWrapper() })

    await waitFor(() => expect(result.current.isSuccess).toBe(true))

    expect(apiClient.get).toHaveBeenCalledWith('/api/transactions', { params: {} })
    expect(result.current.data).toEqual(mockTransactions)
  })

  it('passes month param when provided', async () => {
    vi.mocked(apiClient.get).mockResolvedValue({ data: mockTransactions })

    const { result } = renderHook(() => useTransactions('2026-03'), { wrapper: createWrapper() })

    await waitFor(() => expect(result.current.isSuccess).toBe(true))

    expect(apiClient.get).toHaveBeenCalledWith('/api/transactions', { params: { month: '2026-03' } })
  })
})

describe('useCreateTransaction', () => {
  it('posts to /api/transactions', async () => {
    vi.mocked(apiClient.post).mockResolvedValue({ data: 'new-guid' })
    vi.mocked(apiClient.get).mockResolvedValue({ data: [] })

    const { result } = renderHook(() => useCreateTransaction(), { wrapper: createWrapper() })

    result.current.mutate({
      type: 'Expense',
      amount: 120.5,
      category: 'Food',
      note: 'Grocery run',
      budgetMonth: '2026-03',
    } as any)

    await waitFor(() => expect(result.current.isSuccess).toBe(true))

    expect(apiClient.post).toHaveBeenCalledWith('/api/transactions', expect.objectContaining({
      amount: 120.5,
      category: 'Food',
    }))
  })
})

describe('useDeleteTransaction', () => {
  it('deletes the correct transaction URL', async () => {
    vi.mocked(apiClient.delete).mockResolvedValue({})
    vi.mocked(apiClient.get).mockResolvedValue({ data: [] })

    const { result } = renderHook(() => useDeleteTransaction(), { wrapper: createWrapper() })

    result.current.mutate('t1')

    await waitFor(() => expect(result.current.isSuccess).toBe(true))

    expect(apiClient.delete).toHaveBeenCalledWith('/api/transactions/t1')
  })
})
