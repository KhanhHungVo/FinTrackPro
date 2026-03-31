export type TradeDirection = 'Long' | 'Short'
export type TradeStatus = 'Open' | 'Closed'

export interface Trade {
  id: string
  symbol: string
  direction: TradeDirection
  status: TradeStatus
  entryPrice: number
  exitPrice: number | null
  currentPrice: number | null
  positionSize: number
  fees: number
  currency: string
  rateToUsd: number
  result: number
  unrealizedResult: number | null
  notes: string | null
  createdAt: string
}
