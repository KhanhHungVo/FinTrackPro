import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { apiClient } from '@/shared/api/client'
import type {
  TransactionCategory,
  CreateTransactionCategoryPayload,
  UpdateTransactionCategoryPayload,
} from '../model/types'
import type { TransactionType } from '@/entities/transaction/model/types'

export function useTransactionCategories(type?: TransactionType) {
  return useQuery({
    queryKey: ['transaction-categories', type],
    queryFn: async () => {
      const params = type ? { type } : {}
      const { data } = await apiClient.get<TransactionCategory[]>('/api/transaction-categories', { params })
      return data
    },
    staleTime: 5 * 60 * 1000, // 5 minutes
  })
}

export function useCreateTransactionCategory() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (body: CreateTransactionCategoryPayload) =>
      apiClient.post<string>('/api/transaction-categories', body),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['transaction-categories'] }),
  })
}

export function useUpdateTransactionCategory() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ id, ...body }: UpdateTransactionCategoryPayload & { id: string }) =>
      apiClient.patch(`/api/transaction-categories/${id}`, body),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['transaction-categories'] }),
  })
}

export function useDeleteTransactionCategory() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => apiClient.delete(`/api/transaction-categories/${id}`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['transaction-categories'] }),
  })
}
