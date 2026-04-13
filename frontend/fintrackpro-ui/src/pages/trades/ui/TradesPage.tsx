import { useState, useMemo } from 'react'
import { toast } from 'sonner'
import { useTranslation } from 'react-i18next'
import { useTrades, useTradesSummary, useDeleteTrade } from '@/entities/trade'
import type { Trade } from '@/entities/trade'
import { AddTradeForm } from '@/features/add-trade'
import { EditTradeModal } from '@/features/edit-trade'
import { ClosePositionModal } from '@/features/close-position'
import { TradeFilterBar } from '@/features/filter-trades'
import type { TradeFilters } from '@/features/filter-trades'
import { useLocaleStore } from '@/features/locale'
import { useExchangeRates } from '@/entities/exchange-rate'
import { convertAmount } from '@/shared/lib/convertAmount'
import { formatCurrency } from '@/shared/lib/formatCurrency'
import { cn } from '@/shared/lib/cn'
import { errorToastMessage } from '@/shared/lib/apiError'
import { useGuardedMutation } from '@/shared/lib/useGuardedMutation'
import { useDebounce } from '@/shared/lib/useDebounce'
import { ConfirmDeleteDialog, Pagination, SortableColumnHeader } from '@/shared/ui'

type SortDir = 'asc' | 'desc' | null

export function TradesPage() {
  const { t, i18n } = useTranslation()
  const currency = useLocaleStore((s) => s.currency)

  const [filters, setFilters] = useState<TradeFilters>({
    search: '',
    status: '',
    direction: '',
    dateFrom: '',
    dateTo: '',
  })
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(20)
  const [sortBy, setSortBy] = useState<string | null>('date')
  const [sortDir, setSortDir] = useState<SortDir>('desc')
  const [showForm, setShowForm] = useState(false)
  const [editingTrade, setEditingTrade] = useState<Trade | null>(null)
  const [closingTrade, setClosingTrade] = useState<Trade | null>(null)
  const [pendingDeleteId, setPendingDeleteId] = useState<string | null>(null)

  const debouncedSearch = useDebounce(filters.search, 300)

  const tableParams = {
    page,
    pageSize,
    search: debouncedSearch || undefined,
    status: filters.status || undefined,
    direction: filters.direction || undefined,
    dateFrom: filters.dateFrom || undefined,
    dateTo: filters.dateTo || undefined,
    sortBy: sortBy ?? 'date',
    sortDir: sortDir ?? 'desc',
  }

  const { data: rates } = useExchangeRates([currency])
  const preferredRate = rates?.[currency] ?? 0

  const summaryParams = useMemo(() => ({
    status: filters.status || undefined,
    direction: filters.direction || undefined,
    dateFrom: filters.dateFrom || undefined,
    dateTo: filters.dateTo || undefined,
    preferredCurrency: currency,
    preferredRate: preferredRate > 0 ? preferredRate : undefined,
  }), [filters.status, filters.direction, filters.dateFrom, filters.dateTo, currency, preferredRate])

  const { data, isLoading } = useTrades(tableParams)
  const { data: summary, isLoading: isSummaryLoading } = useTradesSummary(summaryParams, preferredRate > 0)
  const { mutate: deleteTrade } = useDeleteTrade()
  const { guarded: guardedDelete, isPending: isDeleting } = useGuardedMutation(deleteTrade)

  // KPI values — backend returns totals already in preferredCurrency
  const totalPnl = summary?.totalPnl ?? 0
  const winRate = summary?.winRate ?? 0
  const totalAllTrades = summary?.totalTrades ?? 0
  const unrealizedPnl = summary?.unrealizedPnl ?? 0

  function handleSort(field: string, dir: SortDir) {
    if (dir === null) { setSortBy(null); setSortDir(null) }
    else { setSortBy(field); setSortDir(dir) }
    setPage(1)
  }

  const trades = data?.items ?? []
  const totalPages = data?.totalPages ?? 1

  return (
    <div className="mx-auto max-w-4xl space-y-6 p-4 md:p-6">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <h1 className="text-2xl font-bold">{t('trades.title')}</h1>
        <button
          onClick={() => setShowForm((v) => !v)}
          className="rounded-md bg-blue-600 px-4 py-2 text-sm text-white hover:bg-blue-700 transition-colors"
        >
          {showForm ? t('common.cancel') : `+ ${t('trades.addTrade')}`}
        </button>
      </div>

      {showForm && (
        <div>
          <AddTradeForm />
        </div>
      )}

      {/* Filter bar */}
      <TradeFilterBar
        value={filters}
        onChange={(next) => { setFilters((prev) => ({ ...prev, ...next })); setPage(1) }}
      />

      {/* Summary cards — sourced from /summary, always over full filtered dataset */}
      <div className={cn('grid grid-cols-1 gap-4', unrealizedPnl !== 0 ? 'sm:grid-cols-4' : 'sm:grid-cols-3')}>
        <div className="page-card p-4">
          <p className="text-xs text-gray-500 dark:text-slate-400">{t('trades.totalPnl')}</p>
          <p className={cn('text-xl font-semibold', totalPnl >= 0 ? 'text-green-600' : 'text-red-600')}>
            {isSummaryLoading
              ? <span className="text-gray-300 dark:text-slate-600 animate-pulse">—</span>
              : <>{totalPnl >= 0 ? '+' : ''}{formatCurrency(totalPnl, currency, i18n.language)}</>}
          </p>
        </div>
        <div className="page-card p-4">
          <p className="text-xs text-gray-500 dark:text-slate-400">{t('trades.winRate')}</p>
          <p className="text-xl font-semibold">{isSummaryLoading ? <span className="text-gray-300 dark:text-slate-600 animate-pulse">—</span> : `${winRate}%`}</p>
        </div>
        <div className="page-card p-4">
          <p className="text-xs text-gray-500 dark:text-slate-400">{t('trades.totalTrades')}</p>
          <p className="text-xl font-semibold">{isSummaryLoading ? <span className="text-gray-300 dark:text-slate-600 animate-pulse">—</span> : totalAllTrades}</p>
        </div>
        {unrealizedPnl !== 0 && (
          <div className="rounded-lg border border-emerald-200 bg-emerald-50 p-4 dark:border-emerald-500/20 dark:bg-emerald-500/10">
            <p className="text-xs text-emerald-600 dark:text-emerald-400">{t('trades.unrealizedPnl')}</p>
            <p className={cn('text-xl font-semibold', unrealizedPnl >= 0 ? 'text-emerald-700 dark:text-emerald-400' : 'text-red-600')}>
              {isSummaryLoading
                ? <span className="text-gray-300 dark:text-slate-600 animate-pulse">—</span>
                : <>{unrealizedPnl >= 0 ? '+' : ''}{formatCurrency(unrealizedPnl, currency, i18n.language)}</>}
            </p>
          </div>
        )}
      </div>

      {/* Trade list */}
      {isLoading ? (
        <div className="space-y-2">
          {[...Array(4)].map((_, i) => (
            <div key={i} className="animate-pulse h-16 rounded-lg bg-gray-100 dark:bg-white/5" />
          ))}
        </div>
      ) : trades.length === 0 ? (
        <p className="text-center text-sm text-gray-400 dark:text-slate-500 py-8">
          {t('trades.noTrades')}
        </p>
      ) : (
        <div className="page-card overflow-hidden">
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead className="border-b border-gray-100 dark:border-white/6 bg-gray-50/60 dark:bg-white/3">
                <tr>
                  <th className="px-4 py-3 text-left">
                    <SortableColumnHeader label={t('trades.symbol')} field="symbol" currentSortBy={sortBy} currentSortDir={sortDir} onSort={handleSort} />
                  </th>
                  <th className="px-4 py-3 text-left text-xs font-semibold text-gray-400 dark:text-slate-500 uppercase tracking-wide">{t('trades.direction')}</th>
                  <th className="px-4 py-3 text-left text-xs font-semibold text-gray-400 dark:text-slate-500 uppercase tracking-wide">{t('trades.status')}</th>
                  <th className="px-4 py-3 text-right">
                    <SortableColumnHeader label={t('trades.pnl')} field="pnl" currentSortBy={sortBy} currentSortDir={sortDir} onSort={handleSort} align="right" />
                  </th>
                  <th className="px-4 py-3 text-right">
                    <SortableColumnHeader label={t('trades.entryPrice')} field="entryprice" currentSortBy={sortBy} currentSortDir={sortDir} onSort={handleSort} align="right" />
                  </th>
                  <th className="px-4 py-3 text-right text-xs font-semibold text-gray-400 dark:text-slate-500 uppercase tracking-wide">{t('trades.exitCurrentPrice')}</th>
                  <th className="px-4 py-3 text-right">
                    <SortableColumnHeader label={t('trades.positionSize')} field="size" currentSortBy={sortBy} currentSortDir={sortDir} onSort={handleSort} align="right" />
                  </th>
                  <th className="px-4 py-3 text-right">
                    <SortableColumnHeader label={t('trades.fees')} field="fees" currentSortBy={sortBy} currentSortDir={sortDir} onSort={handleSort} align="right" />
                  </th>
                  <th className="px-4 py-3 text-left">
                    <SortableColumnHeader label={t('trades.date')} field="date" currentSortBy={sortBy} currentSortDir={sortDir} onSort={handleSort} />
                  </th>
                  <th className="px-4 py-3 w-20" />
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100 dark:divide-white/5">
                {trades.map((trade) => {
                  const isOpen = trade.status === 'Open'
                  const displayPrice = isOpen ? trade.currentPrice : trade.exitPrice
                  const pnlRaw = isOpen ? (trade.unrealizedResult ?? null) : trade.result
                  const displayPnl = pnlRaw != null
                    ? convertAmount(pnlRaw, trade.rateToUsd, preferredRate)
                    : null
                  const displayFees = convertAmount(trade.fees, trade.rateToUsd, preferredRate)

                  return (
                    <tr key={trade.id} className="group hover:bg-gray-50/70 dark:hover:bg-white/3 transition-colors">
                      <td className="px-4 py-3">
                        <span className="font-mono font-semibold text-gray-900 dark:text-slate-100">{trade.symbol}</span>
                      </td>
                      <td className="px-4 py-3">
                        <span className={cn('inline-flex w-fit rounded px-1.5 py-0.5 text-xs font-semibold',
                          trade.direction === 'Long'
                            ? 'bg-green-100 text-green-700 dark:bg-green-500/15 dark:text-green-400'
                            : 'bg-red-100 text-red-700 dark:bg-red-500/15 dark:text-red-400',
                        )}>
                          {trade.direction === 'Long' ? t('trades.long') : t('trades.short')}
                        </span>
                      </td>
                      <td className="px-4 py-3">
                        <span className={cn('inline-flex w-fit rounded px-1.5 py-0.5 text-xs font-medium',
                          isOpen
                            ? 'bg-emerald-100 text-emerald-700 dark:bg-emerald-500/15 dark:text-emerald-400'
                            : 'bg-gray-100 text-gray-500 dark:bg-white/5 dark:text-slate-500',
                        )}>
                          {isOpen ? t('trades.open') : t('trades.closed')}
                        </span>
                      </td>
                      <td className="px-4 py-3 text-right">
                        {displayPnl != null ? (
                          <span className={cn('text-sm font-bold',
                            isOpen
                              ? 'text-gray-400 dark:text-slate-500'
                              : displayPnl >= 0 ? 'text-green-600 dark:text-green-400' : 'text-red-600 dark:text-red-400',
                          )}>
                            {displayPnl >= 0 ? '+' : ''}{formatCurrency(displayPnl, currency, i18n.language)}
                            {isOpen && <span className="ml-1 text-xs font-normal italic">(unrlzd)</span>}
                          </span>
                        ) : (
                          <span className="text-gray-300 dark:text-slate-600">—</span>
                        )}
                      </td>
                      <td className="px-4 py-3 text-right text-gray-600 dark:text-slate-300 tabular-nums">
                        {formatCurrency(convertAmount(trade.entryPrice, trade.rateToUsd, preferredRate), currency, i18n.language)}
                      </td>
                      <td className="px-4 py-3 text-right tabular-nums">
                        {displayPrice != null ? (
                          <span className="text-gray-600 dark:text-slate-300">
                            {formatCurrency(convertAmount(displayPrice, trade.rateToUsd, preferredRate), currency, i18n.language)}
                          </span>
                        ) : (
                          <span className="text-gray-300 dark:text-slate-600">—</span>
                        )}
                      </td>
                      <td className="px-4 py-3 text-right text-gray-500 dark:text-slate-400 tabular-nums">
                        {trade.positionSize}
                      </td>
                      <td className="px-4 py-3 text-right text-gray-400 dark:text-slate-500 tabular-nums text-xs">
                        {formatCurrency(displayFees, currency, i18n.language)}
                      </td>
                      <td className="px-4 py-3 text-xs text-gray-400 dark:text-slate-500 whitespace-nowrap">
                        {new Date(trade.createdAt).toLocaleDateString(i18n.language, { month: 'short', day: 'numeric' })}
                      </td>
                      <td className="px-4 py-3">
                        <div className="flex items-center gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
                          {isOpen && (
                            <button
                              onClick={() => setClosingTrade(trade)}
                              title={t('trades.closeTrade')}
                              className="rounded px-1.5 py-1 text-xs font-medium text-emerald-600 hover:bg-emerald-50 dark:text-emerald-400 dark:hover:bg-emerald-500/10 transition-colors"
                            >
                              ✓
                            </button>
                          )}
                          <button
                            onClick={() => setEditingTrade(trade)}
                            title={t('common.edit')}
                            className="rounded px-1.5 py-1 text-xs text-gray-400 hover:text-blue-500 hover:bg-blue-50 dark:hover:bg-blue-500/10 transition-colors"
                          >
                            ✎
                          </button>
                          <button
                            onClick={() => setPendingDeleteId(trade.id)}
                            disabled={isDeleting(trade.id)}
                            title={t('common.delete')}
                            className="rounded px-1.5 py-1 text-xs text-gray-400 hover:text-red-500 hover:bg-red-50 dark:hover:bg-red-500/10 disabled:opacity-50 transition-colors"
                          >
                            ✕
                          </button>
                        </div>
                      </td>
                    </tr>
                  )
                })}
              </tbody>
            </table>
          </div>
        </div>
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

      <EditTradeModal trade={editingTrade} onClose={() => setEditingTrade(null)} />
      <ClosePositionModal trade={closingTrade} onClose={() => setClosingTrade(null)} />
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
