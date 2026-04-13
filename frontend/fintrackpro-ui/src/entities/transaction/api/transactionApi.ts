import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { apiClient } from '@/shared/api/client'
import { cleanParams } from '@/shared/lib/cleanParams'
import type { PagedResult } from '@/shared/api/types'
import type { Transaction, TransactionType } from '../model/types'

export interface TransactionQueryParams {
  page?: number
  pageSize?: number
  search?: string
  month?: string
  type?: TransactionType | ''
  categoryId?: string
  sortBy?: string
  sortDir?: string
}

export interface TransactionSummaryParams {
  month?: string
  type?: TransactionType | ''
  categoryId?: string
  preferredCurrency?: string
  preferredRate?: number
}

export interface TransactionSummary {
  totalIncome: number
  totalExpense: number
  netBalance: number
}

export interface CreateTransactionPayload {
  type: TransactionType
  amount: number
  currency: string
  categoryId: string
  note: string | null
  budgetMonth: string
}

export function useTransactions(params: TransactionQueryParams = {}) {
  return useQuery({
    queryKey: ['transactions', params],
    queryFn: async () => {
      const { data } = await apiClient.get<PagedResult<Transaction>>(
        '/api/transactions',
        { params: cleanParams(params as Record<string, unknown>) },
      )
      return data
    },
  })
}

export function useTransactionSummary(params: TransactionSummaryParams = {}) {
  return useQuery({
    queryKey: ['transactions', 'summary', params],
    queryFn: async () => {
      const { data } = await apiClient.get<TransactionSummary>(
        '/api/transactions/summary',
        { params: cleanParams(params as Record<string, unknown>) },
      )
      return data
    },
  })
}

export function useCreateTransaction() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (body: CreateTransactionPayload) =>
      apiClient.post('/api/transactions', body),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['transactions'] }),
  })
}

export interface UpdateTransactionPayload {
  type: TransactionType
  amount: number
  currency: string
  category: string
  note?: string | null
  categoryId?: string | null
}

export function useUpdateTransaction() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ id, ...body }: { id: string } & UpdateTransactionPayload) =>
      apiClient.patch(`/api/transactions/${id}`, body),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['transactions'] }),
  })
}

export function useDeleteTransaction() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => apiClient.delete(`/api/transactions/${id}`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['transactions'] }),
  })
}
