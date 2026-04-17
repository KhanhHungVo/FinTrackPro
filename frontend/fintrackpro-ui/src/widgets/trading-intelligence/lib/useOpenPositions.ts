import { useMemo } from 'react'
import { useTrades } from '@/entities/trade'
import { useExchangeRates } from '@/entities/exchange-rate'
import { useLocaleStore } from '@/features/locale'
import { convertAmount } from '@/shared/lib/convertAmount'

export interface PositionItem {
  id: string
  symbol: string
  unrealizedPnl: number
  unrealizedPct: number
  capitalDeployed: number
  portfolioWeight: number
}

export interface RiskSignal {
  message: string
}

export function useOpenPositions() {
  const currency = useLocaleStore((s) => s.currency)
  const { data: tradesData, isLoading } = useTrades({ status: 'Open', pageSize: 100 })
  const { data: rates } = useExchangeRates([currency])
  const preferredRate = rates?.[currency] ?? 1

  const result = useMemo(() => {
    const trades = tradesData?.items?.filter((t) => t.status === 'Open') ?? []

    if (trades.length === 0) return {
      positions: [],
      winning: [],
      losing: [],
      totalCapital: 0,
      totalUnrealized: 0,
      riskSignals: [],
      allocationData: [],
    }

    const positions: PositionItem[] = trades.map((tr) => {
      const capital = convertAmount(
        tr.entryPrice * tr.positionSize,
        tr.rateToUsd, preferredRate, tr.currency, currency,
      )
      const unrealizedPnl = convertAmount(
        tr.unrealizedResult ?? 0,
        tr.rateToUsd, preferredRate, tr.currency, currency,
      )
      const entryValue = convertAmount(
        tr.entryPrice * tr.positionSize,
        tr.rateToUsd, preferredRate, tr.currency, currency,
      )
      const unrealizedPct = entryValue > 0 ? (unrealizedPnl / entryValue) * 100 : 0
      return { id: tr.id, symbol: tr.symbol, unrealizedPnl, unrealizedPct, capitalDeployed: capital, portfolioWeight: 0 }
    })

    const totalCapital = positions.reduce((s, p) => s + p.capitalDeployed, 0)
    positions.forEach((p) => {
      p.portfolioWeight = totalCapital > 0 ? (p.capitalDeployed / totalCapital) * 100 : 0
    })

    const totalUnrealized = positions.reduce((s, p) => s + p.unrealizedPnl, 0)

    const winning = positions.filter((p) => p.unrealizedPnl >= 0).sort((a, b) => b.unrealizedPnl - a.unrealizedPnl)
    const losing = positions.filter((p) => p.unrealizedPnl < 0).sort((a, b) => a.unrealizedPnl - b.unrealizedPnl)

    // Allocation donut data (group by symbol, summing capital)
    const bySymbol: Record<string, number> = {}
    positions.forEach((p) => {
      bySymbol[p.symbol] = (bySymbol[p.symbol] ?? 0) + p.capitalDeployed
    })
    const allocationData = Object.entries(bySymbol).map(([symbol, value]) => ({ symbol, value }))

    // Risk signals
    const riskSignals: RiskSignal[] = []
    const biggestLoser = losing[0]
    if (biggestLoser && biggestLoser.portfolioWeight > 20) {
      riskSignals.push({
        message: `${biggestLoser.symbol} is ${biggestLoser.unrealizedPct.toFixed(1)}% and holds ${biggestLoser.portfolioWeight.toFixed(0)}% of your portfolio`,
      })
    }
    positions.forEach((p) => {
      if (p.portfolioWeight > 50) {
        riskSignals.push({
          message: `${p.symbol} accounts for ${p.portfolioWeight.toFixed(0)}% of total exposure`,
        })
      }
    })

    return { positions, winning, losing, totalCapital, totalUnrealized, riskSignals, allocationData }
  }, [tradesData, preferredRate, currency])

  return { ...result, isLoading, currency }
}
