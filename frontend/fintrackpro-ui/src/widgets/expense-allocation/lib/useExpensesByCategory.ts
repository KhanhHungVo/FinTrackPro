import { useMemo } from 'react'
import { useTransactions } from '@/entities/transaction'
import { useExchangeRates } from '@/entities/exchange-rate'
import { useLocaleStore } from '@/features/locale'
import { convertAmount } from '@/shared/lib/convertAmount'
import { useCategoryLabel } from '@/shared/lib/useCategoryLabel'

export interface CategorySlice {
  category: string
  amount: number
  percentage: number
}

export function useExpensesByCategory(month: string) {
  const currency = useLocaleStore((s) => s.currency)
  const { data: txData, isLoading } = useTransactions({ month, type: 'Expense', pageSize: 100 })
  const { data: rates } = useExchangeRates([currency])
  const preferredRate = rates?.[currency] ?? 1
  const categoryLabel = useCategoryLabel()

  const { slices, total } = useMemo(() => {
    const expenses = txData?.items?.filter((tx) => tx.type === 'Expense') ?? []
    const byCategory: Record<string, number> = {}

    for (const tx of expenses) {
      const converted = convertAmount(tx.amount, tx.rateToUsd, preferredRate, tx.currency, currency)
      byCategory[tx.category] = (byCategory[tx.category] ?? 0) + converted
    }

    const total = Object.values(byCategory).reduce((s, v) => s + v, 0)
    const slices: CategorySlice[] = Object.entries(byCategory)
      .map(([category, amount]) => ({
        category: categoryLabel(category),
        amount,
        percentage: total > 0 ? (amount / total) * 100 : 0,
      }))
      .sort((a, b) => b.amount - a.amount)

    return { slices, total }
  }, [txData, preferredRate, currency, categoryLabel])

  return { slices, total, isLoading, currency }
}
