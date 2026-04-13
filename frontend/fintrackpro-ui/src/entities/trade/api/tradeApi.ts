import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { apiClient } from '@/shared/api/client'
import { cleanParams } from '@/shared/lib/cleanParams'
import type { PagedResult } from '@/shared/api/types'
import type { Trade, TradeDirection, TradeStatus } from '../model/types'

export interface TradeQueryParams {
  page?: number
  pageSize?: number
  search?: string
  status?: TradeStatus | ''
  direction?: TradeDirection | ''
  dateFrom?: string
  dateTo?: string
  sortBy?: string
  sortDir?: string
}

export interface TradeSummaryParams {
  status?: TradeStatus | ''
  direction?: TradeDirection | ''
  dateFrom?: string
  dateTo?: string
  preferredCurrency?: string
  preferredRate?: number
}

export interface TradesSummary {
  totalPnl: number
  winRate: number
  totalTrades: number
  unrealizedPnl: number
}

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

export function useTrades(params: TradeQueryParams = {}) {
  return useQuery({
    queryKey: ['trades', params],
    queryFn: async () => {
      const { data } = await apiClient.get<PagedResult<Trade>>(
        '/api/trades',
        { params: cleanParams(params as Record<string, unknown>) },
      )
      return data
    },
  })
}

export function useTradesSummary(params: TradeSummaryParams = {}, enabled = true) {
  return useQuery({
    queryKey: ['trades', 'summary', params],
    queryFn: async () => {
      const { data } = await apiClient.get<TradesSummary>(
        '/api/trades/summary',
        { params: cleanParams(params as Record<string, unknown>) },
      )
      return data
    },
    enabled,
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
