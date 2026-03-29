import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { apiClient } from '@/shared/api/client'
import type { Budget } from '../model/types'

export function useBudgets(month: string) {
  return useQuery({
    queryKey: ['budgets', month],
    queryFn: async () => {
      const { data } = await apiClient.get<Budget[]>(`/api/budgets/${month}`)
      return data
    },
  })
}

export function useCreateBudget() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (body: { category: string; limitAmount: number; currency: string; month: string }) =>
      apiClient.post('/api/budgets', body),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['budgets'] }),
  })
}

export function useUpdateBudget() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ id, limitAmount }: { id: string; limitAmount: number }) =>
      apiClient.patch(`/api/budgets/${id}`, { limitAmount }),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['budgets'] }),
  })
}

export function useDeleteBudget() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => apiClient.delete(`/api/budgets/${id}`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['budgets'] }),
  })
}
