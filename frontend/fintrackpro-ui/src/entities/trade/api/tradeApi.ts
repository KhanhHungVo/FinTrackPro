import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { apiClient } from '@/shared/api/client'
import type { Trade, TradeDirection, TradeStatus } from '../model/types'

export interface CreateTradePayload {
  symbol: string
  direction: TradeDirection
  status: TradeStatus
  entryPrice: number
  exitPrice: number | null
  currentPrice: number | null
  positionSize: number
  fees: number
  currency: string
  notes: string | null
}

export interface UpdateTradePayload {
  symbol: string
  direction: TradeDirection
  status: TradeStatus
  entryPrice: number
  exitPrice: number | null
  currentPrice: number | null
  positionSize: number
  fees: number
  currency: string
  notes: string | null
}

export interface ClosePositionPayload {
  exitPrice: number
  fees: number
}

export function useTrades() {
  return useQuery({
    queryKey: ['trades'],
    queryFn: async () => {
      const { data } = await apiClient.get<Trade[]>('/api/trades')
      return data
    },
  })
}

export function useCreateTrade() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (body: CreateTradePayload) =>
      apiClient.post('/api/trades', body),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['trades'] }),
  })
}

export function useDeleteTrade() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => apiClient.delete(`/api/trades/${id}`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['trades'] }),
  })
}

export function useUpdateTrade() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ id, ...body }: { id: string } & UpdateTradePayload) =>
      apiClient.put<Trade>(`/api/trades/${id}`, body),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['trades'] }),
  })
}

export function useClosePosition() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ id, ...body }: { id: string } & ClosePositionPayload) =>
      apiClient.patch<Trade>(`/api/trades/${id}/close`, body),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['trades'] }),
  })
}
