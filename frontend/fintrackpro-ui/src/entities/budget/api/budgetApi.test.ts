import { describe, it, expect, vi, beforeEach } from 'vitest'
import { renderHook, waitFor } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import React from 'react'
import { useBudgets, useCreateBudget, useUpdateBudget, useDeleteBudget } from './budgetApi'
import { apiClient } from '@/shared/api/client'

vi.mock('@/shared/api/client', () => ({
  apiClient: {
    get: vi.fn(),
    post: vi.fn(),
    patch: vi.fn(),
    delete: vi.fn(),
  },
}))

function createWrapper() {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  return ({ children }: { children: React.ReactNode }) =>
    React.createElement(QueryClientProvider, { client: qc }, children)
}

const mockBudgets = [
  { id: 'b1', category: 'Food', limitAmount: 500, month: '2026-03', createdAt: '2026-03-01T00:00:00Z' },
]

beforeEach(() => vi.clearAllMocks())

describe('useBudgets', () => {
  it('fetches budgets for the given month', async () => {
    vi.mocked(apiClient.get).mockResolvedValue({ data: mockBudgets })

    const { result } = renderHook(() => useBudgets('2026-03'), { wrapper: createWrapper() })

    await waitFor(() => expect(result.current.isSuccess).toBe(true))

    expect(apiClient.get).toHaveBeenCalledWith('/api/budgets/2026-03')
    expect(result.current.data).toEqual(mockBudgets)
  })
})

describe('useCreateBudget', () => {
  it('posts to /api/budgets', async () => {
    vi.mocked(apiClient.post).mockResolvedValue({ data: 'new-guid' })
    vi.mocked(apiClient.get).mockResolvedValue({ data: [] })

    const { result } = renderHook(() => useCreateBudget(), { wrapper: createWrapper() })

    result.current.mutate({ category: 'Food', limitAmount: 500, month: '2026-03' })

    await waitFor(() => expect(result.current.isSuccess).toBe(true))

    expect(apiClient.post).toHaveBeenCalledWith('/api/budgets', {
      category: 'Food',
      limitAmount: 500,
      month: '2026-03',
    })
  })
})

describe('useUpdateBudget', () => {
  it('patches the correct budget URL', async () => {
    vi.mocked(apiClient.patch).mockResolvedValue({})
    vi.mocked(apiClient.get).mockResolvedValue({ data: [] })

    const { result } = renderHook(() => useUpdateBudget(), { wrapper: createWrapper() })

    result.current.mutate({ id: 'b1', limitAmount: 750 })

    await waitFor(() => expect(result.current.isSuccess).toBe(true))

    expect(apiClient.patch).toHaveBeenCalledWith('/api/budgets/b1', { limitAmount: 750 })
  })
})

describe('useDeleteBudget', () => {
  it('deletes the correct budget URL', async () => {
    vi.mocked(apiClient.delete).mockResolvedValue({})
    vi.mocked(apiClient.get).mockResolvedValue({ data: [] })

    const { result } = renderHook(() => useDeleteBudget(), { wrapper: createWrapper() })

    result.current.mutate('b1')

    await waitFor(() => expect(result.current.isSuccess).toBe(true))

    expect(apiClient.delete).toHaveBeenCalledWith('/api/budgets/b1')
  })
})
