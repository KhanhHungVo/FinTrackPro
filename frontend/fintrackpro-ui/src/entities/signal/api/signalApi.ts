import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { apiClient } from '@/shared/api/client'
import type { FearGreed, MarketCapCoin, Signal, TrendingCoin } from '../model/types'

const MARKET_POLL_MS = 120_000 // aligns with backend CoinGecko cache TTL (60s) + buffer

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

export function useDismissSignal() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => apiClient.patch(`/api/signals/${id}/dismiss`),

    onMutate: async (id) => {
      await queryClient.cancelQueries({ queryKey: ['signals'] })

      const snapshots = queryClient.getQueriesData<Signal[]>({ queryKey: ['signals'] })

      queryClient.setQueriesData<Signal[]>({ queryKey: ['signals'] }, (prev) =>
        prev ? prev.filter((s) => s.id !== id) : prev,
      )

      return { snapshots }
    },

    onError: (_err, _id, context) => {
      context?.snapshots.forEach(([key, data]) => queryClient.setQueryData(key, data))
    },

    onSettled: () => {
      queryClient.invalidateQueries({ queryKey: ['signals'] })
    },
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
    staleTime: MARKET_POLL_MS,
    refetchInterval: MARKET_POLL_MS,
  })
}

export function useMarketCapCoins() {
  return useQuery({
    queryKey: ['market-cap-coins'],
    queryFn: async () => {
      const { data } = await apiClient.get<MarketCapCoin[]>('/api/market/marketcap')
      return data
    },
    staleTime: MARKET_POLL_MS,
    refetchInterval: MARKET_POLL_MS,
  })
}
