import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { apiClient } from '@/shared/api/client'
import type { Transaction, TransactionType } from '../model/types'

export interface CreateTransactionPayload {
  type: TransactionType
  amount: number
  currency: string
  categoryId: string
  note: string | null
  budgetMonth: string
}

export function useTransactions(month?: string) {
  return useQuery({
    queryKey: ['transactions', month],
    queryFn: async () => {
      const params = month ? { month } : {}
      const { data } = await apiClient.get<Transaction[]>('/api/transactions', { params })
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

export function useDeleteTransaction() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => apiClient.delete(`/api/transactions/${id}`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['transactions'] }),
  })
}
