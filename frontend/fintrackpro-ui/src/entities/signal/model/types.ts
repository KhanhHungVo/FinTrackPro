export type SignalType =
  | 'RsiOversold'
  | 'RsiOverbought'
  | 'VolumeSpike'
  | 'FundingRate'
  | 'EmaCross'
  | 'BbSqueeze'

export interface Signal {
  id: string
  symbol: string
  signalType: SignalType
  message: string
  value: number
  timeframe: string
  isNotified: boolean
  createdAt: string
}

export interface FearGreed {
  value: number
  label: string
  timestamp: string
}

export interface TrendingCoin {
  id: string
  name: string
  symbol: string
  marketCapRank: number
}
