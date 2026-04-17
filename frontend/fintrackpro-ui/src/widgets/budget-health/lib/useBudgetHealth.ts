import { useMemo } from 'react'
import { useBudgets } from '@/entities/budget'
import { useTransactions } from '@/entities/transaction'
import { useExchangeRates } from '@/entities/exchange-rate'
import { useLocaleStore } from '@/features/locale'
import { convertAmount } from '@/shared/lib/convertAmount'

export interface BudgetHealthItem {
  id: string
  category: string
  spent: number
  limit: number
  pct: number
  overrun: boolean
}

export function useBudgetHealth(month: string) {
  const currency = useLocaleStore((s) => s.currency)
  const { data: budgets, isLoading: loadingBudgets } = useBudgets(month)
  const { data: txData, isLoading: loadingTx } = useTransactions({ month, type: 'Expense', pageSize: 100 })
  const { data: rates } = useExchangeRates([currency])
  const preferredRate = rates?.[currency] ?? 1

  const { items, onTrackCount, totalCount } = useMemo(() => {
    if (!budgets) return { items: [], onTrackCount: 0, totalCount: 0 }

    const spentByCategory: Record<string, number> = {}
    txData?.items
      ?.filter((tx) => tx.type === 'Expense')
      .forEach((tx) => {
        const converted = convertAmount(tx.amount, tx.rateToUsd, preferredRate, tx.currency, currency)
        spentByCategory[tx.category] = (spentByCategory[tx.category] ?? 0) + converted
      })

    const items: BudgetHealthItem[] = budgets
      .map((b) => {
        const limit = convertAmount(b.limitAmount, b.rateToUsd, preferredRate, b.currency, currency)
        const spent = spentByCategory[b.category] ?? 0
        const pct = limit > 0 ? (spent / limit) * 100 : 0
        return { id: b.id, category: b.category, spent, limit, pct, overrun: spent > limit }
      })
      // Sort worst-first
      .sort((a, b) => b.pct - a.pct)
      .slice(0, 5)

    const totalCount = budgets.length
    const onTrackCount = budgets.filter((b) => {
      const limit = convertAmount(b.limitAmount, b.rateToUsd, preferredRate, b.currency, currency)
      const spent = spentByCategory[b.category] ?? 0
      return spent <= limit
    }).length

    return { items, onTrackCount, totalCount }
  }, [budgets, txData, preferredRate, currency])

  return { items, onTrackCount, totalCount, isLoading: loadingBudgets || loadingTx, currency }
}
