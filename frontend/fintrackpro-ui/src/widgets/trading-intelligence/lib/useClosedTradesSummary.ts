import { useMemo } from 'react'
import { useTrades, useTradesSummary } from '@/entities/trade'
import { useExchangeRates } from '@/entities/exchange-rate'
import { useLocaleStore } from '@/features/locale'
import { convertAmount } from '@/shared/lib/convertAmount'

function startOfMonth(): string {
  const d = new Date()
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-01`
}

function today(): string {
  return new Date().toISOString().slice(0, 10)
}

export interface WeekBucket {
  label: string
  cumulativePnl: number
}

export interface SymbolBreakdown {
  symbol: string
  pnl: number
}

export function useClosedTradesSummary() {
  const currency = useLocaleStore((s) => s.currency)
  const dateFrom = startOfMonth()
  const dateTo = today()

  const { data: summary } = useTradesSummary({ status: 'Closed', dateFrom, dateTo })
  const { data: tradesData, isLoading } = useTrades({ status: 'Closed', dateFrom, dateTo, pageSize: 100 })
  const { data: rates } = useExchangeRates([currency])
  const preferredRate = rates?.[currency] ?? 1

  const result = useMemo(() => {
    const trades = tradesData?.items?.filter((t) => t.status === 'Closed') ?? []
    const totalTrades = trades.length

    // Realised P&L
    const realisedPnl = trades.reduce((s, tr) =>
      s + convertAmount(tr.result, tr.rateToUsd, preferredRate, tr.currency, currency), 0)

    // Win rate
    const wins = trades.filter((tr) => {
      const pnl = convertAmount(tr.result, tr.rateToUsd, preferredRate, tr.currency, currency)
      return pnl > 0
    })
    const winRate = totalTrades > 0 ? (wins.length / totalTrades) * 100 : 0
    const winRateFraction = `${wins.length}/${totalTrades}`

    // Avg P&L per trade
    const avgPnl = totalTrades > 0 ? realisedPnl / totalTrades : 0

    // Avg win / avg loss / R:R
    const winPnls = trades
      .map((tr) => convertAmount(tr.result, tr.rateToUsd, preferredRate, tr.currency, currency))
      .filter((v) => v > 0)
    const lossPnls = trades
      .map((tr) => convertAmount(tr.result, tr.rateToUsd, preferredRate, tr.currency, currency))
      .filter((v) => v < 0)
    const avgWin = winPnls.length > 0 ? winPnls.reduce((s, v) => s + v, 0) / winPnls.length : 0
    const avgLoss = lossPnls.length > 0 ? Math.abs(lossPnls.reduce((s, v) => s + v, 0) / lossPnls.length) : 0
    const riskReward = avgLoss > 0 ? avgWin / avgLoss : null

    // Cumulative P&L by week bucket
    const monthStart = new Date(dateFrom)
    const sortedTrades = [...trades].sort(
      (a, b) => new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime()
    )
    let cumulative = 0
    const weekBuckets: WeekBucket[] = ['W1', 'W2', 'W3', 'W4'].map((label, wi) => {
      const weekStart = wi * 7
      const weekEnd = (wi + 1) * 7
      sortedTrades.forEach((tr) => {
        const dayOfMonth = Math.floor(
          (new Date(tr.createdAt).getTime() - monthStart.getTime()) / (1000 * 60 * 60 * 24)
        )
        if (dayOfMonth >= weekStart && dayOfMonth < weekEnd) {
          cumulative += convertAmount(tr.result, tr.rateToUsd, preferredRate, tr.currency, currency)
        }
      })
      return { label, cumulativePnl: cumulative }
    })

    // By symbol breakdown (top 3)
    const bySymbol: Record<string, number> = {}
    trades.forEach((tr) => {
      const pnl = convertAmount(tr.result, tr.rateToUsd, preferredRate, tr.currency, currency)
      bySymbol[tr.symbol] = (bySymbol[tr.symbol] ?? 0) + pnl
    })
    const symbolBreakdown: SymbolBreakdown[] = Object.entries(bySymbol)
      .map(([symbol, pnl]) => ({ symbol, pnl }))
      .sort((a, b) => Math.abs(b.pnl) - Math.abs(a.pnl))
      .slice(0, 3)

    // By direction
    const longPnl = trades
      .filter((tr) => tr.direction === 'Long')
      .reduce((s, tr) => s + convertAmount(tr.result, tr.rateToUsd, preferredRate, tr.currency, currency), 0)
    const shortPnl = trades
      .filter((tr) => tr.direction === 'Short')
      .reduce((s, tr) => s + convertAmount(tr.result, tr.rateToUsd, preferredRate, tr.currency, currency), 0)

    return {
      totalTrades,
      realisedPnl,
      winRate,
      winRateFraction,
      avgPnl,
      avgWin,
      avgLoss,
      riskReward,
      weekBuckets,
      symbolBreakdown,
      longPnl,
      shortPnl,
    }
  }, [tradesData, preferredRate, currency, dateFrom])

  return { ...result, summary, isLoading, currency }
}
