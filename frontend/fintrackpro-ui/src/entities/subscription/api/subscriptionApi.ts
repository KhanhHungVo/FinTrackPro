import { useQuery, useMutation } from '@tanstack/react-query'
import { apiClient } from '@/shared/api/client'
import type {
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
