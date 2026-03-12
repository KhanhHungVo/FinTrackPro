import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { apiClient } from '@/shared/api/client'
import type { Transaction } from '../model/types'

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
    mutationFn: (body: Omit<Transaction, 'id' | 'createdAt'>) =>
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
