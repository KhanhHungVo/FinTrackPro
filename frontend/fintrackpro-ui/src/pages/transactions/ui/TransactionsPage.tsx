import { useState } from 'react'
import { toast } from 'sonner'
import { useTranslation } from 'react-i18next'
import { useTransactions, useTransactionSummary, useDeleteTransaction } from '@/entities/transaction'
import type { Transaction } from '@/entities/transaction'
import { useTransactionCategories } from '@/entities/transaction-category'
import { AddTransactionForm } from '@/features/add-transaction'
import { EditTransactionModal } from '@/features/edit-transaction'
import { TransactionFilterBar } from '@/features/filter-transactions'
import type { TransactionFilters } from '@/features/filter-transactions'
import { useLocaleStore } from '@/features/locale'
import { useExchangeRates } from '@/entities/exchange-rate'
import { convertAmount } from '@/shared/lib/convertAmount'
import { formatCurrency } from '@/shared/lib/formatCurrency'
import { cn } from '@/shared/lib/cn'
import { errorToastMessage } from '@/shared/lib/apiError'
import { useGuardedMutation } from '@/shared/lib/useGuardedMutation'
import { useDebounce } from '@/shared/lib/useDebounce'
import { useCategoryLabel } from '@/shared/lib/useCategoryLabel'
import { ConfirmDeleteDialog, Pagination, SortableColumnHeader } from '@/shared/ui'

type SortDir = 'asc' | 'desc' | null

function monthsBack(n: number): string {
  const d = new Date()
  d.setMonth(d.getMonth() - n)
  return d.toISOString().slice(0, 7)
}

const TYPE_COLORS: Record<string, string> = {
  Income: 'text-green-600',
  Expense: 'text-red-600',
}

const MONTHS = Array.from({ length: 6 }, (_, i) => monthsBack(i))

export function TransactionsPage() {
  const { t, i18n } = useTranslation()
  const currency = useLocaleStore((s) => s.currency)
  const resolveCategoryLabel = useCategoryLabel()

  const [filters, setFilters] = useState<TransactionFilters>({
    search: '',
    month: monthsBack(0),
    type: '',
    categoryId: '',
  })
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(20)
  const [sortBy, setSortBy] = useState<string | null>('date')
  const [sortDir, setSortDir] = useState<SortDir>('desc')
  const [editingTx, setEditingTx] = useState<Transaction | null>(null)
  const [pendingDeleteId, setPendingDeleteId] = useState<string | null>(null)
  const [formOpen, setFormOpen] = useState(false)

  const debouncedSearch = useDebounce(filters.search, 300)

  const tableParams = {
    page,
    pageSize,
    search: debouncedSearch || undefined,
    month: filters.month || undefined,
    type: filters.type || undefined,
    categoryId: filters.categoryId || undefined,
    sortBy: sortBy ?? 'date',
    sortDir: sortDir ?? 'desc',
  }

  const { data: rates } = useExchangeRates([currency])
  const preferredRate = rates?.[currency] ?? 0

  const summaryParams = {
    month: filters.month || undefined,
    type: filters.type || undefined,
    categoryId: filters.categoryId || undefined,
    preferredCurrency: currency,
    preferredRate: preferredRate || undefined,
  }

  const { data, isLoading } = useTransactions(tableParams)
  const { data: summary } = useTransactionSummary(summaryParams)
  const { mutate: deleteTx } = useDeleteTransaction()
  const { guarded: handleDelete, isPending: isDeleting } = useGuardedMutation<unknown, Error, string>(deleteTx)
  const { data: allCategories } = useTransactionCategories()

  // KPI values — backend returns totals already in preferredCurrency
  const income = summary?.totalIncome ?? 0
  const expense = summary?.totalExpense ?? 0
  const net = income - expense

  function handleSort(field: string, dir: SortDir) {
    if (dir === null) { setSortBy(null); setSortDir(null) }
    else { setSortBy(field); setSortDir(dir) }
    setPage(1)
  }

  const transactions = data?.items ?? []
  const totalPages = data?.totalPages ?? 1

  return (
    <div className="mx-auto max-w-3xl space-y-6 p-4 md:p-6">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <h1 className="text-2xl font-bold">{t('transactions.title')}</h1>
        <button
          onClick={() => setFormOpen((v) => !v)}
          className="rounded-md bg-blue-600 px-4 py-2 text-sm text-white hover:bg-blue-700 transition-colors"
        >
          {formOpen ? t('common.cancel') : `+ ${t('transactions.addTransaction')}`}
        </button>
      </div>

      {/* Add form */}
      {formOpen && <AddTransactionForm onSuccess={() => setFormOpen(false)} />}

      {/* Filter bar */}
      <TransactionFilterBar
        value={filters}
        onChange={(next) => { setFilters((prev) => ({ ...prev, ...next })); setPage(1) }}
        categories={allCategories}
        monthOptions={MONTHS}
      />

      {/* Summary KPIs — sourced from /summary, always over full filtered dataset */}
      <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
        <div className="page-card p-4">
          <p className="text-xs text-gray-500 dark:text-slate-400">{t('transactions.income')}</p>
          <p className="text-xl font-semibold text-green-600">+{formatCurrency(income, currency, i18n.language)}</p>
        </div>
        <div className="page-card p-4">
          <p className="text-xs text-gray-500 dark:text-slate-400">{t('transactions.expense')}</p>
          <p className="text-xl font-semibold text-red-600">-{formatCurrency(expense, currency, i18n.language)}</p>
        </div>
        <div className="page-card p-4">
          <p className="text-xs text-gray-500 dark:text-slate-400">{t('transactions.net')}</p>
          <p className={cn('text-xl font-semibold', net >= 0 ? 'text-green-600' : 'text-red-600')}>
            {net >= 0 ? '+' : ''}{formatCurrency(net, currency, i18n.language)}
          </p>
        </div>
      </div>

      {/* Column headers (sortable) */}
      {!isLoading && transactions.length > 0 && (
        <div className="flex items-center gap-4 px-4 text-xs">
          <SortableColumnHeader
            label={t('transactions.category')}
            field="category"
            currentSortBy={sortBy}
            currentSortDir={sortDir}
            onSort={handleSort}
          />
          <SortableColumnHeader
            label={t('transactions.amount')}
            field="amount"
            currentSortBy={sortBy}
            currentSortDir={sortDir}
            onSort={handleSort}
            align="right"
            className="ml-auto"
          />
          <SortableColumnHeader
            label={t('transactions.month')}
            field="date"
            currentSortBy={sortBy}
            currentSortDir={sortDir}
            onSort={handleSort}
          />
        </div>
      )}

      {/* List */}
      {isLoading ? (
        <div className="space-y-2">
          {[...Array(4)].map((_, i) => (
            <div key={i} className="animate-pulse h-14 rounded-lg bg-gray-100 dark:bg-white/5" />
          ))}
        </div>
      ) : transactions.length === 0 ? (
        <p className="text-center text-sm text-gray-400 dark:text-slate-500 py-8">
          {t('transactions.noTransactions')}
        </p>
      ) : (
        <ul className="space-y-2">
          {transactions.map((tx) => {
            const displayAmount = convertAmount(tx.amount, tx.rateToUsd, preferredRate, tx.currency, currency)
            return (
              <li
                key={tx.id}
                className="page-card flex items-center justify-between px-4 py-3"
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
                    <p className="text-sm font-medium">{resolveCategoryLabel(tx.category)}</p>
                    {tx.note && (
                      <p className="text-xs text-gray-400 dark:text-slate-500">{tx.note}</p>
                    )}
                  </div>
                </div>
                <div className="flex items-center gap-4">
                  <span className={cn('text-sm font-semibold', TYPE_COLORS[tx.type])}>
                    {tx.type === 'Income' ? '+' : '-'}{formatCurrency(displayAmount, currency, i18n.language)}
                  </span>
                  <span className="text-xs text-gray-400 dark:text-slate-500">
                    {new Date(tx.createdAt).toLocaleDateString(i18n.language)}
                  </span>
                  <button
                    onClick={() => setEditingTx(tx)}
                    className="text-xs text-gray-300 hover:text-blue-500 transition-colors dark:text-slate-600"
                    title={t('common.edit')}
                  >
                    ✎
                  </button>
                  <button
                    onClick={() => setPendingDeleteId(tx.id)}
                    disabled={isDeleting(tx.id)}
                    className="text-xs text-gray-300 hover:text-red-500 transition-colors disabled:opacity-40 disabled:cursor-not-allowed dark:text-slate-600"
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

      {/* Pagination */}
      <Pagination
        page={page}
        totalPages={totalPages}
        pageSize={pageSize}
        onPageChange={setPage}
        onPageSizeChange={(s) => { setPageSize(s); setPage(1) }}
        disabled={isLoading}
      />

      <EditTransactionModal transaction={editingTx} onClose={() => setEditingTx(null)} />
      <ConfirmDeleteDialog
        open={pendingDeleteId !== null}
        onConfirm={() => {
          if (pendingDeleteId) {
            handleDelete(pendingDeleteId, {
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
