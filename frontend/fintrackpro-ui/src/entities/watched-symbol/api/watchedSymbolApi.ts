import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { apiClient } from '@/shared/api/client'
import type { WatchedSymbol, WatchlistAnalysisItem } from '../model/types'

const WATCHLIST_POLL_MS = 120_000 // aligns with backend Binance cache TTL (60s) + buffer

export function useWatchedSymbols() {
  return useQuery({
    queryKey: ['watched-symbols'],
    queryFn: async () => {
      const { data } = await apiClient.get<WatchedSymbol[]>('/api/watchedsymbols')
      return data
    },
  })
}

export function useAddWatchedSymbol() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (symbol: string) =>
      apiClient.post('/api/watchedsymbols', { symbol }),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['watched-symbols'] }),
  })
}

export function useRemoveWatchedSymbol() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => apiClient.delete(`/api/watchedsymbols/${id}`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['watched-symbols'] }),
  })
}

export function useWatchlistAnalysis() {
  return useQuery({
    queryKey: ['watchlist-analysis'],
    queryFn: async () => {
      const { data } = await apiClient.get<WatchlistAnalysisItem[]>('/api/watchedsymbols/analysis')
      return data
    },
    staleTime: WATCHLIST_POLL_MS,
    refetchInterval: WATCHLIST_POLL_MS,
  })
}
