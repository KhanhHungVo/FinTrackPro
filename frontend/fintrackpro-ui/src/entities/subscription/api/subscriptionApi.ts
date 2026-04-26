import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { apiClient } from '@/shared/api/client'
import type {
  AdminActivatePayload,
  AdminUser,
  PagedResult,
  SubscriptionStatus,
  CreateCheckoutSessionPayload,
  CreatePortalSessionPayload,
} from '../model/types'

export function useSubscriptionStatus() {
  return useQuery({
    queryKey: ['subscription-status'],
    queryFn: async () => {
      const { data } = await apiClient.get<SubscriptionStatus>('/api/subscription/status')
      return data
    },
  })
}

export function useCreateCheckoutSession() {
  return useMutation({
    mutationFn: async (body: CreateCheckoutSessionPayload) => {
      const { data } = await apiClient.post<{ sessionUrl: string }>(
        '/api/subscription/checkout',
        body,
      )
      return data
    },
  })
}

export function useCreatePortalSession() {
  return useMutation({
    mutationFn: async (body: CreatePortalSessionPayload) => {
      const { data } = await apiClient.post<{ portalUrl: string }>(
        '/api/subscription/portal',
        body,
      )
      return data
    },
  })
}

export function useAdminUsers(page: number, emailFilter?: string) {
  return useQuery({
    queryKey: ['adminUsers', page, emailFilter],
    queryFn: async () => {
      const params = new URLSearchParams({ page: String(page), pageSize: '20' })
      if (emailFilter) params.set('email', emailFilter)
      const { data } = await apiClient.get<PagedResult<AdminUser>>(`/api/admin/users?${params}`)
      return data
    },
  })
}

export function useAdminActivateSubscription() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async ({ userId, period }: { userId: string; period: AdminActivatePayload['period'] }) => {
      const { data } = await apiClient.post<SubscriptionStatus>(
        `/api/admin/users/${userId}/subscription`,
        { period },
      )
      return data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['adminUsers'] })
    },
  })
}

export function useAdminRevokeSubscription() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (userId: string) => {
      await apiClient.delete(`/api/admin/users/${userId}/subscription`)
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['adminUsers'] })
    },
  })
}
