export type TradeDirection = 'Long' | 'Short'

export interface Trade {
  id: string
  symbol: string
  direction: TradeDirection
  entryPrice: number
  exitPrice: number
  positionSize: number
  fees: number
  currency: string
  rateToUsd: number
  result: number
  notes: string | null
  createdAt: string
}
