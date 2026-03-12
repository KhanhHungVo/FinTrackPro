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
    mutationFn: (body: { category: string; limitAmount: number; month: string }) =>
      apiClient.post('/api/budgets', body),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['budgets'] }),
  })
}
