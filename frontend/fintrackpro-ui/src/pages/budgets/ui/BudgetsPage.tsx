import { useState, useMemo } from 'react'
import { Pencil, X } from 'lucide-react'
import { toast } from 'sonner'
import { useTranslation } from 'react-i18next'
import { useBudgets, useDeleteBudget, useUpdateBudget } from '@/entities/budget'
import { useTransactions } from '@/entities/transaction'
import { useTransactionCategories } from '@/entities/transaction-category'
import { AddBudgetForm } from '@/features/add-budget'
import { useLocaleStore } from '@/features/locale'
import { useExchangeRates } from '@/entities/exchange-rate'
import { convertAmount } from '@/shared/lib/convertAmount'
import { formatCurrency } from '@/shared/lib/formatCurrency'
import { cn } from '@/shared/lib/cn'
import { errorToastMessage } from '@/shared/lib/apiError'
import { useGuardedMutation } from '@/shared/lib/useGuardedMutation'
import { ConfirmDeleteDialog } from '@/shared/ui'

function monthsBack(n: number): string {
  const d = new Date()
  d.setMonth(d.getMonth() - n)
  return d.toISOString().slice(0, 7)
}

type BudgetSortField = 'spentPct' | 'category'

export function BudgetsPage() {
  const { t, i18n } = useTranslation()
  const currency = useLocaleStore((s) => s.currency)
  const language = useLocaleStore((s) => s.language)
  const [month, setMonth] = useState(monthsBack(0))
  const [editingId, setEditingId] = useState<string | null>(null)
  const [editLimit, setEditLimit] = useState('')
  const [savedId, setSavedId] = useState<string | null>(null)
  const [pendingDeleteId, setPendingDeleteId] = useState<string | null>(null)
  const [showOverBudgetOnly, setShowOverBudgetOnly] = useState(false)
  const [budgetSort, setBudgetSort] = useState<BudgetSortField>('spentPct')
  const [budgetSortDir, setBudgetSortDir] = useState<'asc' | 'desc'>('desc')

  const { data: budgets, isLoading } = useBudgets(month)
  const { data: transactionData } = useTransactions({ month, pageSize: 100 })
  const { data: allCategories } = useTransactionCategories()

  const resolveCategoryLabel = (slug: string) => {
    const cat = allCategories?.find((c) => c.slug === slug)
    if (!cat) return slug
    return `${cat.icon} ${language === 'vi' ? cat.labelVi : cat.labelEn}`
  }

  const { mutate: deleteBudget } = useDeleteBudget()
  const { guarded: guardedDelete, isPending: isDeleting } = useGuardedMutation(deleteBudget)
  const { mutate: updateBudget, isPending: isSaving } = useUpdateBudget()
  const { data: rates } = useExchangeRates([currency])
  const preferredRate = rates?.[currency] ?? 1

  function startEdit(id: string, current: number) {
    setEditingId(id)
    setEditLimit(String(current))
  }

  function commitEdit(id: string) {
    const val = parseFloat(editLimit)
    if (!isNaN(val) && val > 0) {
      updateBudget(
        { id, limitAmount: val },
        {
          onSuccess: () => {
            setEditingId(null)
            setSavedId(id)
            setTimeout(() => setSavedId(null), 2000)
          },
          onError: (err) => { setEditingId(null); toast.error(errorToastMessage(err)) },
        },
      )
    } else {
      setEditingId(null)
    }
  }

  const monthOptions = Array.from({ length: 6 }, (_, i) => monthsBack(i))

  // Calculate spending per category from transactions (normalise to USD first, then to preferred)
  const spentByCategory = useMemo(() => {
    const acc: Record<string, number> = {}
    transactionData?.items
      ?.filter((tx) => tx.type === 'Expense')
      .forEach((tx) => {
        const converted = convertAmount(tx.amount, tx.rateToUsd, preferredRate, tx.currency, currency)
        acc[tx.category] = (acc[tx.category] ?? 0) + converted
      })
    return acc
  }, [transactionData?.items, preferredRate, currency])

  const displayBudgets = useMemo(() => {
    if (!budgets) return []
    return budgets
      .map((b) => {
        const limitInPreferred = convertAmount(b.limitAmount, b.rateToUsd, preferredRate, b.currency, currency)
        const spent = spentByCategory[b.category] ?? 0
        const pct = limitInPreferred > 0 ? (spent / limitInPreferred) * 100 : 0
        return { ...b, limitInPreferred, spent, pct, overrun: spent > limitInPreferred }
      })
      .filter((b) => !showOverBudgetOnly || b.overrun)
      .sort((a, b) => {
        const aVal = budgetSort === 'spentPct' ? a.pct : a.category
        const bVal = budgetSort === 'spentPct' ? b.pct : b.category
        if (aVal < bVal) return budgetSortDir === 'asc' ? -1 : 1
        if (aVal > bVal) return budgetSortDir === 'asc' ? 1 : -1
        return 0
      })
  }, [budgets, showOverBudgetOnly, budgetSort, budgetSortDir, spentByCategory, preferredRate, currency])

  function toggleSort(field: BudgetSortField) {
    if (budgetSort === field) {
      setBudgetSortDir((d) => (d === 'desc' ? 'asc' : 'desc'))
    } else {
      setBudgetSort(field)
      setBudgetSortDir('desc')
    }
  }

  const sortIndicator = (field: BudgetSortField) =>
    budgetSort === field ? (budgetSortDir === 'desc' ? ' ↓' : ' ↑') : ' ↕'

  return (
    <div className="mx-auto max-w-3xl space-y-6 p-4 md:p-6">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <h1 className="text-2xl font-bold">{t('budgets.title')}</h1>
        <div className="flex items-center gap-2">
          {/* Over-budget toggle */}
          <button
            onClick={() => setShowOverBudgetOnly((v) => !v)}
            className={cn(
              'rounded-md border px-3 py-1.5 text-sm transition-colors',
              showOverBudgetOnly
                ? 'border-red-400 bg-red-50 text-red-600 dark:bg-red-500/10 dark:border-red-500/40 dark:text-red-400'
                : 'border-gray-300 text-gray-600 hover:bg-gray-50 dark:border-white/12 dark:text-slate-300 dark:hover:bg-white/5',
            )}
          >
            {t('budgets.overBudgetOnly')}
          </button>
          <select
            value={month}
            onChange={(e) => setMonth(e.target.value)}
            className="rounded-md border border-gray-300 px-3 py-1.5 text-sm outline-none focus:border-transparent focus:ring-2 focus:ring-blue-500 dark:bg-slate-800 dark:border-white/12 dark:text-white"
          >
            {monthOptions.map((m) => (
              <option key={m} value={m}>{m}</option>
            ))}
          </select>
        </div>
      </div>

      <AddBudgetForm month={month} />

      {/* Sort controls */}
      {!isLoading && (displayBudgets.length > 0 || budgets?.length) && (
        <div className="flex items-center gap-3 text-xs">
          <span className="text-gray-400 dark:text-slate-500">{t('common.sortBy')}:</span>
          <button
            onClick={() => toggleSort('spentPct')}
            className={cn(
              'font-semibold uppercase tracking-wide transition-colors',
              budgetSort === 'spentPct' ? 'text-blue-600 dark:text-blue-400' : 'text-gray-400 dark:text-slate-500 hover:text-gray-600',
            )}
          >
            % {t('budgets.spent')}{sortIndicator('spentPct')}
          </button>
          <button
            onClick={() => toggleSort('category')}
            className={cn(
              'font-semibold uppercase tracking-wide transition-colors',
              budgetSort === 'category' ? 'text-blue-600 dark:text-blue-400' : 'text-gray-400 dark:text-slate-500 hover:text-gray-600',
            )}
          >
            {t('transactions.category')}{sortIndicator('category')}
          </button>
        </div>
      )}

      {isLoading ? (
        <div className="space-y-2">
          {[...Array(3)].map((_, i) => (
            <div key={i} className="animate-pulse h-20 rounded-lg bg-gray-100 dark:bg-white/5" />
          ))}
        </div>
      ) : displayBudgets.length === 0 ? (
        <p className="text-center text-sm text-gray-400 dark:text-slate-500 py-8">
          {t('budgets.noBudgets')}
        </p>
      ) : (
        <ul className="space-y-3">
          {displayBudgets.map((budget) => {
            const pct = Math.min(budget.pct, 100)

            return (
              <li key={budget.id} className="page-card p-4 space-y-2">
                <div className="flex items-center justify-between">
                  <p className="font-medium">{resolveCategoryLabel(budget.category)}</p>
                  <div className="flex items-center gap-2">
                    {editingId === budget.id ? (
                      <>
                        <input
                          type="number"
                          min="0"
                          step="0.01"
                          value={editLimit}
                          onChange={(e) => setEditLimit(e.target.value)}
                          onKeyDown={(e) => {
                            if (e.key === 'Enter') commitEdit(budget.id)
                            if (e.key === 'Escape') setEditingId(null)
                          }}
                          disabled={isSaving}
                          className="w-28 rounded border px-2 py-0.5 text-sm text-right focus:outline-none focus:ring-1 focus:ring-blue-400 dark:bg-slate-800 dark:border-white/10 dark:text-white"
                          autoFocus
                        />
                        {/* Save button */}
                        <button
                          onClick={() => commitEdit(budget.id)}
                          disabled={isSaving}
                          className="flex items-center justify-center w-6 h-6 rounded text-green-500 hover:bg-green-500/15 transition-colors disabled:opacity-50"
                          aria-label={t('common.save')}
                          title={t('common.save')}
                        >
                          {isSaving ? (
                            <svg className="animate-spin w-3.5 h-3.5" fill="none" viewBox="0 0 24 24">
                              <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
                              <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
                            </svg>
                          ) : (
                            <svg className="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth="2.5">
                              <path strokeLinecap="round" strokeLinejoin="round" d="M5 13l4 4L19 7" />
                            </svg>
                          )}
                        </button>
                        {/* Cancel button */}
                        <button
                          onClick={() => setEditingId(null)}
                          disabled={isSaving}
                          className="flex items-center justify-center w-6 h-6 rounded text-gray-400 hover:text-red-400 hover:bg-red-500/10 transition-colors disabled:opacity-50"
                          aria-label={t('common.cancel')}
                          title={t('common.cancel')}
                        >
                          <svg className="w-3 h-3" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth="2.5">
                            <path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" />
                          </svg>
                        </button>
                      </>
                    ) : (
                      <>
                        {savedId === budget.id ? (
                          <span className="flex items-center gap-1 text-sm font-medium text-green-500">
                            <svg className="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth="2.5">
                              <path strokeLinecap="round" strokeLinejoin="round" d="M5 13l4 4L19 7" />
                            </svg>
                            {t('common.saved')}
                          </span>
                        ) : (
                          <span
                            className={cn(
                              'text-sm font-semibold',
                              budget.overrun ? 'text-red-600' : 'text-gray-700 dark:text-slate-300',
                            )}
                          >
                            {formatCurrency(budget.spent, currency, i18n.language)}
                            <span className="font-normal text-gray-400 dark:text-slate-500">
                              {' '}/ {formatCurrency(budget.limitInPreferred, currency, i18n.language)}
                            </span>
                          </span>
                        )}
                        <button
                          onClick={() => startEdit(budget.id, budget.limitAmount)}
                          className="text-gray-400 hover:text-blue-500 transition-colors text-sm dark:text-slate-600"
                          aria-label={t('common.edit')}
                          title={t('common.edit')}
                        >
                          <Pencil size={12} aria-hidden="true" />
                        </button>
                        <button
                          onClick={() => setPendingDeleteId(budget.id)}
                          disabled={isDeleting(budget.id)}
                          className="text-gray-400 hover:text-red-500 transition-colors disabled:opacity-50 dark:text-slate-600"
                          aria-label={t('common.delete')}
                          title={t('common.delete')}
                        >
                          <X size={12} aria-hidden="true" />
                        </button>
                      </>
                    )}
                  </div>
                </div>

                {/* Progress bar */}
                <div className="h-2 w-full rounded-full bg-gray-100 dark:bg-slate-700">
                  <div
                    className={cn(
                      'h-2 rounded-full transition-all',
                      budget.overrun ? 'bg-red-500' : pct > 80 ? 'bg-yellow-400' : 'bg-green-500',
                    )}
                    style={{ width: `${pct}%` }}
                  />
                </div>

                {budget.overrun && (
                  <p className="text-xs text-red-500">
                    {t('budgets.overBudget')}: {formatCurrency(budget.spent - budget.limitInPreferred, currency, i18n.language)}
                  </p>
                )}
              </li>
            )
          })}
        </ul>
      )}
      <ConfirmDeleteDialog
        open={pendingDeleteId !== null}
        onConfirm={() => {
          if (pendingDeleteId) {
            guardedDelete(pendingDeleteId, {
              onError: (err) => toast.error(errorToastMessage(err)),
              onSettled: () => setPendingDeleteId(null),
            })
          }
        }}
        onCancel={() => setPendingDeleteId(null)}
      />
    </div>
  )
}
