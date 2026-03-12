import { useQuery } from '@tanstack/react-query'
import { apiClient } from '@/shared/api/client'
import type { FearGreed, Signal, TrendingCoin } from '../model/types'

export function useSignals(count = 20) {
  return useQuery({
    queryKey: ['signals', count],
    queryFn: async () => {
      const { data } = await apiClient.get<Signal[]>('/api/signals', { params: { count } })
      return data
    },
    staleTime: 1000 * 60 * 4, // 4 minutes — matches job interval
  })
}

export function useFearGreed() {
  return useQuery({
    queryKey: ['fear-greed'],
    queryFn: async () => {
      const { data } = await apiClient.get<FearGreed>('/api/market/fear-greed')
      return data
    },
    staleTime: 1000 * 60 * 60, // 1 hour
  })
}

export function useTrendingCoins() {
  return useQuery({
    queryKey: ['trending-coins'],
    queryFn: async () => {
      const { data } = await apiClient.get<TrendingCoin[]>('/api/market/trending')
      return data
    },
    staleTime: 1000 * 60 * 15, // 15 minutes
  })
}
