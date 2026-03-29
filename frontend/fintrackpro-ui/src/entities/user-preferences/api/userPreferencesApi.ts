import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { apiClient } from '@/shared/api/client'
import type { UserPreferences } from '../model/types'

export function useUserPreferences() {
  return useQuery({
    queryKey: ['user-preferences'],
    queryFn: async () => {
      const { data } = await apiClient.get<UserPreferences>('/api/users/preferences')
      return data
    },
  })
}

export function useUpdateUserPreferences() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (body: UserPreferences) =>
      apiClient.patch('/api/users/preferences', body),
    onSuccess: () =>
      qc.invalidateQueries({ queryKey: ['user-preferences'] }),
  })
}
