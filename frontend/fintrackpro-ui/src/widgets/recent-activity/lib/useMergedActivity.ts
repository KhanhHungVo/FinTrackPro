import { useMemo } from 'react'
import { useTransactions } from '@/entities/transaction'
import { useTrades } from '@/entities/trade'
import { useExchangeRates } from '@/entities/exchange-rate'
import { useLocaleStore } from '@/features/locale'
import { convertAmount } from '@/shared/lib/convertAmount'
import { useCategoryLabel } from '@/shared/lib/useCategoryLabel'

export type ActivityKind = 'income' | 'expense' | 'trade'

export interface ActivityItem {
  id: string
  kind: ActivityKind
  label: string
  amount: number
  currency: string
  detail: string
  createdAt: string
}

export function useMergedActivity() {
  const currency = useLocaleStore((s) => s.currency)
  const { data: txData, isLoading: loadingTx } = useTransactions({ pageSize: 5, sortBy: 'date', sortDir: 'desc' })
  const { data: tradesData, isLoading: loadingTrades } = useTrades({ pageSize: 5, sortBy: 'date', sortDir: 'desc' })
  const { data: rates } = useExchangeRates([currency])
  const preferredRate = rates?.[currency] ?? 1
  const resolveLabel = useCategoryLabel()

  const items = useMemo<ActivityItem[]>(() => {
    const txItems: ActivityItem[] = (txData?.items ?? []).map((tx) => ({
      id: `tx-${tx.id}`,
      kind: tx.type === 'Income' ? 'income' : 'expense',
      label: resolveLabel(tx.category),
      amount: convertAmount(tx.amount, tx.rateToUsd, preferredRate, tx.currency, currency),
      currency,
      detail: tx.type,
      createdAt: tx.createdAt,
    }))

    const tradeItems: ActivityItem[] = (tradesData?.items ?? []).map((tr) => {
      const pnl = tr.status === 'Closed'
        ? convertAmount(tr.result, tr.rateToUsd, preferredRate, tr.currency, currency)
        : convertAmount(tr.unrealizedResult ?? 0, tr.rateToUsd, preferredRate, tr.currency, currency)
      return {
        id: `tr-${tr.id}`,
        kind: 'trade',
        label: tr.symbol,
        amount: pnl,
        currency,
        detail: `${tr.direction}/${tr.status === 'Closed' ? 'Closed' : 'Open'}`,
        createdAt: tr.createdAt,
      }
    })

    return [...txItems, ...tradeItems]
      .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())
      .slice(0, 8)
  }, [txData, tradesData, preferredRate, currency, resolveLabel])

  return { items, isLoading: loadingTx || loadingTrades }
}
