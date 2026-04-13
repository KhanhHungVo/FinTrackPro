import { describe, it, expect, vi, beforeEach } from 'vitest'
import { renderHook, waitFor } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import React from 'react'
import { useTransactions, useTransactionSummary, useCreateTransaction, useDeleteTransaction } from './transactionApi'
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

const mockPagedTransactions = {
  items: [
    {
      id: 't1',
      type: 'Expense',
      amount: 120.5,
      currency: 'USD',
      rateToUsd: 1,
      category: 'Food',
      note: 'Grocery run',
      budgetMonth: '2026-03',
      createdAt: '2026-03-12T10:00:00Z',
    },
  ],
  page: 1,
  pageSize: 20,
  totalCount: 1,
  totalPages: 1,
  hasPreviousPage: false,
  hasNextPage: false,
}

beforeEach(() => vi.clearAllMocks())

describe('useTransactions', () => {
  it('fetches paged transactions without params', async () => {
    vi.mocked(apiClient.get).mockResolvedValue({ data: mockPagedTransactions })

    const { result } = renderHook(() => useTransactions(), { wrapper: createWrapper() })

    await waitFor(() => expect(result.current.isSuccess).toBe(true))

    expect(apiClient.get).toHaveBeenCalledWith('/api/transactions', { params: {} })
    expect(result.current.data?.items).toEqual(mockPagedTransactions.items)
    expect(result.current.data?.totalCount).toBe(1)
  })

  it('passes month param when provided', async () => {
    vi.mocked(apiClient.get).mockResolvedValue({ data: mockPagedTransactions })

    const { result } = renderHook(() => useTransactions({ month: '2026-03' }), { wrapper: createWrapper() })

    await waitFor(() => expect(result.current.isSuccess).toBe(true))

    expect(apiClient.get).toHaveBeenCalledWith('/api/transactions', { params: { month: '2026-03' } })
  })

  it('passes page, pageSize, sortBy, sortDir params', async () => {
    vi.mocked(apiClient.get).mockResolvedValue({ data: mockPagedTransactions })

    const { result } = renderHook(
      () => useTransactions({ page: 2, pageSize: 5, sortBy: 'amount', sortDir: 'asc' }),
      { wrapper: createWrapper() },
    )

    await waitFor(() => expect(result.current.isSuccess).toBe(true))

    expect(apiClient.get).toHaveBeenCalledWith('/api/transactions', {
      params: { page: 2, pageSize: 5, sortBy: 'amount', sortDir: 'asc' },
    })
  })
})

describe('useTransactionSummary', () => {
  it('fetches summary from /api/transactions/summary', async () => {
    const mockSummary = { totalIncome: 1000, totalExpense: 400, netBalance: 600 }
    vi.mocked(apiClient.get).mockResolvedValue({ data: mockSummary })

    const { result } = renderHook(() => useTransactionSummary({ month: '2026-03' }), { wrapper: createWrapper() })

    await waitFor(() => expect(result.current.isSuccess).toBe(true))

    expect(apiClient.get).toHaveBeenCalledWith('/api/transactions/summary', { params: { month: '2026-03' } })
    expect(result.current.data).toEqual(mockSummary)
  })

  it('passes preferredCurrency and preferredRate to the summary endpoint', async () => {
    const mockSummary = { totalIncome: 25_000_000, totalExpense: 5_000_000, netBalance: 20_000_000 }
    vi.mocked(apiClient.get).mockResolvedValue({ data: mockSummary })

    const { result } = renderHook(
      () => useTransactionSummary({ month: '2026-03', preferredCurrency: 'VND', preferredRate: 25000 }),
      { wrapper: createWrapper() },
    )

    await waitFor(() => expect(result.current.isSuccess).toBe(true))

    expect(apiClient.get).toHaveBeenCalledWith('/api/transactions/summary', {
      params: { month: '2026-03', preferredCurrency: 'VND', preferredRate: 25000 },
    })
    expect(result.current.data).toEqual(mockSummary)
  })
})

describe('useCreateTransaction', () => {
  it('posts to /api/transactions', async () => {
    vi.mocked(apiClient.post).mockResolvedValue({ data: 'new-guid' })
    vi.mocked(apiClient.get).mockResolvedValue({ data: mockPagedTransactions })

    const { result } = renderHook(() => useCreateTransaction(), { wrapper: createWrapper() })

    result.current.mutate({
      type: 'Expense',
      amount: 120.5,
      categoryId: 'some-category-guid',
      note: 'Grocery run',
      budgetMonth: '2026-03',
      currency: 'USD',
    })

    await waitFor(() => expect(result.current.isSuccess).toBe(true))

    expect(apiClient.post).toHaveBeenCalledWith('/api/transactions', expect.objectContaining({
      amount: 120.5,
      categoryId: 'some-category-guid',
    }))
  })
})

describe('useDeleteTransaction', () => {
  it('deletes the correct transaction URL', async () => {
    vi.mocked(apiClient.delete).mockResolvedValue({})
    vi.mocked(apiClient.get).mockResolvedValue({ data: mockPagedTransactions })

    const { result } = renderHook(() => useDeleteTransaction(), { wrapper: createWrapper() })

    result.current.mutate('t1')

    await waitFor(() => expect(result.current.isSuccess).toBe(true))

    expect(apiClient.delete).toHaveBeenCalledWith('/api/transactions/t1')
  })
})
