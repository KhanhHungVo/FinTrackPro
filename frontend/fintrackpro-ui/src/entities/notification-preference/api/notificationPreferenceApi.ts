import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { apiClient } from '@/shared/api/client'
import type { NotificationPreference } from '../model/types'

export function useNotificationPreference() {
  return useQuery({
    queryKey: ['notification-preference'],
    queryFn: async () => {
      const { data } = await apiClient.get<NotificationPreference | null>(
        '/api/notifications/preferences',
      )
      return data
    },
  })
}

export function useSaveNotificationPreference() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (body: { telegramChatId: string; isEnabled: boolean }) =>
      apiClient.post('/api/notifications/preferences', body),
    onSuccess: () =>
      qc.invalidateQueries({ queryKey: ['notification-preference'] }),
  })
}
