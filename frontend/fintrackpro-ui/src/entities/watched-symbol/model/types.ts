export interface WatchedSymbol {
  id: string
  symbol: string
  createdAt: string
}

export interface WatchlistAnalysisItem {
  symbol: string
  price: number | null
  change24h: number | null
  rsiDaily: number | null
  rsiWeekly: number | null
}
