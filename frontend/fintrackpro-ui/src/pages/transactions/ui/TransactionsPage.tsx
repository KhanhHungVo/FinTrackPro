import { useState } from 'react'
import { toast } from 'sonner'
import { useTranslation } from 'react-i18next'
import { useTransactions, useDeleteTransaction } from '@/entities/transaction'
import type { TransactionType } from '@/entities/transaction'
import { AddTransactionForm } from '@/features/add-transaction'
import { useLocaleStore } from '@/features/locale'
import { useExchangeRates } from '@/entities/exchange-rate'
import { convertAmount } from '@/shared/lib/convertAmount'
import { formatCurrency } from '@/shared/lib/formatCurrency'
import { cn } from '@/shared/lib/cn'
import { errorToastMessage } from '@/shared/lib/apiError'
import { useGuardedMutation } from '@/shared/lib/useGuardedMutation'

function monthsBack(n: number): string {
  const d = new Date()
  d.setMonth(d.getMonth() - n)
  return d.toISOString().slice(0, 7)
}

const TYPE_COLORS: Record<TransactionType, string> = {
  Income: 'text-green-600',
  Expense: 'text-red-600',
}

export function TransactionsPage() {
  const { t, i18n } = useTranslation()
  const currency = useLocaleStore((s) => s.currency)
  const [month, setMonth] = useState(monthsBack(0))
  const { data: transactions, isLoading } = useTransactions(month)
  const { mutate: deleteTx } = useDeleteTransaction()
  const { guarded: handleDelete, isPending: isDeleting } = useGuardedMutation<unknown, Error, string>(deleteTx)
  const onDeleteClick = (id: string) =>
    handleDelete(id, { onError: (err) => toast.error(errorToastMessage(err)) })
  const { data: rates } = useExchangeRates([currency])
  const preferredRate = rates?.[currency] ?? 0

  const income  = transactions?.filter(t => t.type === 'Income').reduce((s, t) => s + convertAmount(t.amount, t.rateToUsd, preferredRate), 0) ?? 0
  const expense = transactions?.filter(t => t.type === 'Expense').reduce((s, t) => s + convertAmount(t.amount, t.rateToUsd, preferredRate), 0) ?? 0
  const net = income - expense

  // Build last 6 months for the selector
  const monthOptions = Array.from({ length: 6 }, (_, i) => monthsBack(i))

  return (
    <div className="mx-auto max-w-3xl space-y-6 p-4 md:p-6">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <h1 className="text-2xl font-bold">{t('transactions.title')}</h1>
        <select
          value={month}
          onChange={(e) => setMonth(e.target.value)}
          className="rounded-md border px-3 py-1.5 text-sm"
        >
          {monthOptions.map((m) => (
            <option key={m} value={m}>{m}</option>
          ))}
        </select>
      </div>

      {/* Summary */}
      <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
        <div className="rounded-lg border p-4">
          <p className="text-xs text-gray-500">{t('transactions.income')}</p>
          <p className="text-xl font-semibold text-green-600">+{formatCurrency(income, currency, i18n.language)}</p>
        </div>
        <div className="rounded-lg border p-4">
          <p className="text-xs text-gray-500">{t('transactions.expense')}</p>
          <p className="text-xl font-semibold text-red-600">-{formatCurrency(expense, currency, i18n.language)}</p>
        </div>
        <div className="rounded-lg border p-4">
          <p className="text-xs text-gray-500">{t('transactions.net')}</p>
          <p className={cn('text-xl font-semibold', net >= 0 ? 'text-green-600' : 'text-red-600')}>
            {net >= 0 ? '+' : ''}{formatCurrency(net, currency, i18n.language)}
          </p>
        </div>
      </div>

      {/* Add form */}
      <AddTransactionForm />

      {/* List */}
      {isLoading ? (
        <div className="space-y-2">
          {[...Array(4)].map((_, i) => (
            <div key={i} className="animate-pulse h-14 rounded-lg bg-gray-100" />
          ))}
        </div>
      ) : transactions?.length === 0 ? (
        <p className="text-center text-sm text-gray-400 py-8">
          {t('transactions.noTransactions')}
        </p>
      ) : (
        <ul className="space-y-2">
          {transactions?.map((tx) => {
            const displayAmount = convertAmount(tx.amount, tx.rateToUsd, preferredRate)
            return (
              <li
                key={tx.id}
                className="flex items-center justify-between rounded-lg border px-4 py-3"
              >
                <div className="flex items-center gap-3">
                  <span
                    className={cn(
                      'rounded px-2 py-0.5 text-xs font-medium',
                      tx.type === 'Income'
                        ? 'bg-green-100 text-green-700'
                        : 'bg-red-100 text-red-700',
                    )}
                  >
                    {tx.type === 'Income' ? t('transactions.income') : t('transactions.expense')}
                  </span>
                  <div>
                    <p className="text-sm font-medium">{tx.category}</p>
                    {tx.note && (
                      <p className="text-xs text-gray-400">{tx.note}</p>
                    )}
                  </div>
                </div>
                <div className="flex items-center gap-4">
                  <span className={cn('text-sm font-semibold', TYPE_COLORS[tx.type])}>
                    {tx.type === 'Income' ? '+' : '-'}{formatCurrency(displayAmount, currency, i18n.language)}
                  </span>
                  <span className="text-xs text-gray-400">
                    {new Date(tx.createdAt).toLocaleDateString(i18n.language)}
                  </span>
                  <button
                    onClick={() => onDeleteClick(tx.id)}
                    disabled={isDeleting(tx.id)}
                    className="text-xs text-gray-300 hover:text-red-500 transition-colors disabled:opacity-40 disabled:cursor-not-allowed"
                    title={t('common.delete')}
                  >
                    ✕
                  </button>
                </div>
              </li>
            )
          })}
        </ul>
      )}
    </div>
  )
}
