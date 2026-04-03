import { describe, it, expect, vi, beforeEach } from 'vitest'
import { renderHook, waitFor, act } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import React from 'react'
import {
  useTransactionCategories,
  useCreateTransactionCategory,
  useUpdateTransactionCategory,
  useDeleteTransactionCategory,
} from './transactionCategoryApi'
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

const mockCategories = [
  {
    id: 'cat-1',
    slug: 'food_beverage',
    labelEn: 'Food & Beverage',
    labelVi: 'Ăn uống',
    icon: '🍜',
    type: 'Expense',
    isSystem: true,
    sortOrder: 1,
  },
]

beforeEach(() => vi.clearAllMocks())

describe('useTransactionCategories', () => {
  it('fetches all without type filter', async () => {
    vi.mocked(apiClient.get).mockResolvedValue({ data: mockCategories })

    const { result } = renderHook(() => useTransactionCategories(), { wrapper: createWrapper() })

    await waitFor(() => expect(result.current.isSuccess).toBe(true))

    expect(apiClient.get).toHaveBeenCalledWith('/api/transaction-categories', { params: {} })
    expect(result.current.data).toEqual(mockCategories)
  })

  it('passes type param when provided', async () => {
    vi.mocked(apiClient.get).mockResolvedValue({ data: mockCategories })

    const { result } = renderHook(() => useTransactionCategories('Expense'), { wrapper: createWrapper() })

    await waitFor(() => expect(result.current.isSuccess).toBe(true))

    expect(apiClient.get).toHaveBeenCalledWith('/api/transaction-categories', { params: { type: 'Expense' } })
  })
})

describe('useCreateTransactionCategory', () => {
  it('posts to /api/transaction-categories', async () => {
    vi.mocked(apiClient.post).mockResolvedValue({ data: 'new-guid' })
    vi.mocked(apiClient.get).mockResolvedValue({ data: [] })

    const { result } = renderHook(() => useCreateTransactionCategory(), { wrapper: createWrapper() })

    await act(async () => {
      result.current.mutate({
        type: 'Expense',
        slug: 'pet_care',
        labelEn: 'Pet Care',
        labelVi: 'Thú cưng',
        icon: '🐶',
      })
    })

    await waitFor(() => expect(result.current.isSuccess).toBe(true))

    expect(apiClient.post).toHaveBeenCalledWith('/api/transaction-categories', expect.objectContaining({
      slug: 'pet_care',
    }))
  })
})

describe('useUpdateTransactionCategory', () => {
  it('patches the correct id', async () => {
    vi.mocked(apiClient.patch).mockResolvedValue({})
    vi.mocked(apiClient.get).mockResolvedValue({ data: [] })

    const { result } = renderHook(() => useUpdateTransactionCategory(), { wrapper: createWrapper() })

    await act(async () => {
      result.current.mutate({ id: 'cat-1', labelEn: 'Pets', labelVi: 'Thú nuôi', icon: '🐾' })
    })

    await waitFor(() => expect(result.current.isSuccess).toBe(true))

    expect(apiClient.patch).toHaveBeenCalledWith('/api/transaction-categories/cat-1',
      expect.objectContaining({ labelEn: 'Pets' }))
  })
})

describe('useDeleteTransactionCategory', () => {
  it('deletes the correct id', async () => {
    vi.mocked(apiClient.delete).mockResolvedValue({})
    vi.mocked(apiClient.get).mockResolvedValue({ data: [] })

    const { result } = renderHook(() => useDeleteTransactionCategory(), { wrapper: createWrapper() })

    await act(async () => {
      result.current.mutate('cat-1')
    })

    await waitFor(() => expect(result.current.isSuccess).toBe(true))

    expect(apiClient.delete).toHaveBeenCalledWith('/api/transaction-categories/cat-1')
  })
})
